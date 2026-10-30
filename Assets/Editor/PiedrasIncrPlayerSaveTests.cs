#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prueba serializacion/aislamiento de la pose. No mueve ningun jugador.
/// </summary>
public static class PiedrasIncrPlayerSaveTests
{
    [MenuItem("Tools/PiedrasIncr/Guardado/Probar datos del jugador (aislado)")]
    public static void Run()
    {
        string dir = Path.Combine(Path.GetTempPath(),
            "PiedrasIncr_PlayerTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new SaveStorage(dir);
            for (int slot = 1; slot <= 3; slot++)
            {
                var save = new GameSaveData();
                save.economy.coins = slot * 50;
                save.player = new PlayerSaveData
                {
                    hasSafePosition = true,
                    positionX = 2.5f * slot,
                    positionY = 10f + slot,
                    positionZ = -3f * slot,
                    yawDegrees = 45f * slot
                };
                storage.Save(slot, save);
            }

            for (int slot = 1; slot <= 3; slot++)
            {
                GameSaveData loaded;
                bool fromBackup;
                Check(storage.TryLoad(slot, out loaded, out fromBackup), "No se lee la ranura " + slot);
                PlayerSaveAdapter.Validate(loaded.player);
                Vector3 p = PlayerSaveAdapter.GetSavedPosition(loaded.player);
                Check(p == new Vector3(2.5f * slot, 10f + slot, -3f * slot),
                    "Se altero la posicion de la ranura " + slot);
                Check(Mathf.Approximately(loaded.player.yawDegrees, 45f * slot),
                    "Se altero la orientacion de la ranura " + slot);
                Check(loaded.economy.coins == slot * 50, "Se altero la economia de la ranura " + slot);
            }

            bool reject = false;
            try { PlayerSaveAdapter.Validate(null); }
            catch (InvalidOperationException) { reject = true; }
            Check(reject, "No se rechazo una pose inexistente.");

            reject = false;
            try { PlayerSaveAdapter.Validate(new PlayerSaveData { hasSafePosition = true,
                    positionX = float.NaN, yawDegrees = 0f }); }
            catch (InvalidOperationException) { reject = true; }
            Check(reject, "No se rechazo una coordenada NaN.");

            // Los guardados anteriores, que solo tenian monedas, no inventan una posicion.
            GameSaveData legacy = new GameSaveData { player = null };
            legacy.economy.coins = 987;
            storage.Save(1, legacy);
            GameSaveData legacyRead;
            bool backupRead;
            Check(storage.TryLoad(1, out legacyRead, out backupRead), "Fallo compatibilidad con v1.");
            Check(legacyRead.player == null || !legacyRead.player.hasSafePosition,
                "Un guardado antiguo ha inventado una posicion.");

            Debug.Log("[GUARDADO] JUGADOR SUPERADO: 3 ranuras, posicion, orientacion, validacion y compatibilidad v1.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GUARDADO] JUGADOR FALLIDO: " + ex);
            throw;
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    private static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
    }
}
#endif
