using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Almacenamiento local para tres ranuras independientes.
/// No conoce escenas ni componentes del juego: SOLO lee y escribe datos.
/// </summary>
public sealed class SaveStorage
{
    [Serializable]
    private sealed class SaveEnvelope
    {
        public int envelopeVersion = 1;
        public string payload;
        public string sha256;
    }

    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private readonly string directory;

    public SaveStorage(string customDirectory = null)
    {
        directory = string.IsNullOrEmpty(customDirectory)
            ? Path.Combine(Application.persistentDataPath, "PiedrasIncr", "Saves")
            : customDirectory;
    }

    public string DirectoryPath => directory;

    public void Save(int slot, GameSaveData data)
    {
        CheckSlot(slot);
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.formatVersion != GameSaveData.CurrentFormatVersion)
            throw new InvalidDataException("El formato no coincide con la versión actual del guardado.");

        data.slotIndex = slot;
        if (string.IsNullOrEmpty(data.saveId))
            data.saveId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrEmpty(data.createdUtc))
            data.createdUtc = UtcNow();
        data.savedUtc = UtcNow();

        ValidateData(data, slot);
        Directory.CreateDirectory(directory);

        string primary = PrimaryPath(slot);
        string backup = BackupPath(slot);
        string temporary = TemporaryPath(slot);
        string payload = JsonUtility.ToJson(data);
        var envelope = new SaveEnvelope
        {
            envelopeVersion = 1,
            payload = payload,
            sha256 = Hash(payload)
        };
        byte[] bytes = Utf8.GetBytes(JsonUtility.ToJson(envelope, true));

        // Volcamos primero el archivo completo a disco. Nunca escribimos directamente
        // encima de la única partida válida.
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        GameSaveData verification;
        if (!TryReadSnapshot(temporary, slot, out verification))
            throw new IOException("El archivo temporal no superó la validación. No se ha sustituido la partida.");

        if (!File.Exists(primary))
        {
            File.Move(temporary, primary);
        }
        else
        {
            GameSaveData oldPrimary;
            if (TryReadSnapshot(primary, slot, out oldPrimary))
            {
                try
                {
                    // Sustitución atómica cuando el sistema operativo lo permite,
                    // conservando el último archivo principal válido como .bak.
                    File.Replace(temporary, primary, backup);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceWithRecoverableCopy(temporary, primary, backup);
                }
                catch (NotSupportedException)
                {
                    ReplaceWithRecoverableCopy(temporary, primary, backup);
                }
            }
            else
            {
                // Si el principal está dañado, NO lo copiamos encima del respaldo.
                GameSaveData oldBackup;
                if (!TryReadSnapshot(backup, slot, out oldBackup))
                    throw new IOException("La partida y su respaldo están dañados. No se sobrescribirán automáticamente.");

                File.Copy(temporary, primary, true); // El respaldo válido permanece intacto.
                File.Delete(temporary);
            }
        }

        GameSaveData saved;
        if (!TryReadSnapshot(primary, slot, out saved))
            throw new IOException("El archivo final no ha superado la comprobación. Recupera el respaldo.");
    }

    /// <summary>
    /// Si el archivo principal falla, intenta cargar su respaldo.
    /// fromBackup indica que la última versión principal no se pudo recuperar.
    /// </summary>
    public bool TryLoad(int slot, out GameSaveData data, out bool fromBackup)
    {
        CheckSlot(slot);
        fromBackup = false;
        if (TryReadSnapshot(PrimaryPath(slot), slot, out data))
            return true;

        if (TryReadSnapshot(BackupPath(slot), slot, out data))
        {
            fromBackup = true;
            return true;
        }

        data = null;
        return false;
    }

    public bool HasValidSave(int slot)
    {
        GameSaveData data;
        bool fromBackup;
        return TryLoad(slot, out data, out fromBackup);
    }

    public void DeleteSlot(int slot)
    {
        CheckSlot(slot);
        DeleteIfExists(PrimaryPath(slot));
        DeleteIfExists(BackupPath(slot));
        DeleteIfExists(TemporaryPath(slot));
    }

    public string GetPrimaryPathForDiagnostics(int slot)
    {
        CheckSlot(slot);
        return PrimaryPath(slot);
    }

    private static void ReplaceWithRecoverableCopy(string temporary, string primary, string backup)
    {
        // Alternativa para sistemas donde File.Replace no esté disponible.
        // La copia no es atómica, pero siempre existe un respaldo previo válido.
        File.Copy(primary, backup, true);
        File.Copy(temporary, primary, true);
        File.Delete(temporary);
    }

    private static bool TryReadSnapshot(string path, int expectedSlot, out GameSaveData data)
    {
        data = null;
        if (!File.Exists(path))
            return false;

        try
        {
            string fileText = File.ReadAllText(path, Utf8);
            var envelope = JsonUtility.FromJson<SaveEnvelope>(fileText);
            if (envelope == null || envelope.envelopeVersion != 1 ||
                string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.sha256))
                return false;

            if (!string.Equals(Hash(envelope.payload), envelope.sha256, StringComparison.OrdinalIgnoreCase))
                return false;

            GameSaveData candidate = JsonUtility.FromJson<GameSaveData>(envelope.payload);
            ValidateData(candidate, expectedSlot);
            data = candidate;
            return true;
        }
        catch (Exception ex)
        {
            // Un archivo inválido nunca debe provocar que el juego cargue datos parciales.
            Debug.LogWarning("SaveStorage: guardado ilegible: " + path + " (" + ex.Message + ")");
            return false;
        }
    }

    private static void ValidateData(GameSaveData data, int slot)
    {
        if (data == null || data.formatVersion != GameSaveData.CurrentFormatVersion ||
            data.slotIndex != slot || string.IsNullOrEmpty(data.saveId) ||
            string.IsNullOrEmpty(data.createdUtc) || string.IsNullOrEmpty(data.savedUtc) ||
            data.economy == null || data.economy.coins < 0 ||
            double.IsNaN(data.totalPlaySeconds) || double.IsInfinity(data.totalPlaySeconds) ||
            data.totalPlaySeconds < 0)
        {
            throw new InvalidDataException("Datos de partida incorrectos o incompatibles.");
        }
    }

    private static string Hash(string text)
    {
        using (var sha = SHA256.Create())
        {
            byte[] digest = sha.ComputeHash(Utf8.GetBytes(text));
            var hex = new StringBuilder(digest.Length * 2);
            foreach (byte b in digest)
                hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return hex.ToString();
        }
    }

    private static void CheckSlot(int slot)
    {
        if (slot < 1 || slot > 3)
            throw new ArgumentOutOfRangeException(nameof(slot), "Solo existen las ranuras 1, 2 y 3.");
    }

    private static string UtcNow() => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    private string PrimaryPath(int slot) => Path.Combine(directory, "slot_" + slot + ".json");
    private string BackupPath(int slot) => PrimaryPath(slot) + ".bak";
    private string TemporaryPath(int slot) => PrimaryPath(slot) + ".tmp";

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
