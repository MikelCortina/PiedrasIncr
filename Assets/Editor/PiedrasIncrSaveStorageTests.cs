#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pruebas aisladas: NO modifica las partidas reales del usuario.
/// Menú: Tools > PiedrasIncr > Guardado > Probar núcleo
/// </summary>
public static class PiedrasIncrSaveStorageTests
{
    [MenuItem("Tools/PiedrasIncr/Guardado/Probar núcleo (3 ranuras)")]
    public static void Run()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(),
            "PiedrasIncr_SaveTest_" + Guid.NewGuid().ToString("N"));

        try
        {
            var storage = new SaveStorage(testDirectory);

            // 1: tres ranuras independientes.
            for (int slot = 1; slot <= 3; slot++)
            {
                var save = new GameSaveData();
                save.economy.coins = slot * 100;
                storage.Save(slot, save);
            }

            for (int slot = 1; slot <= 3; slot++)
            {
                GameSaveData loaded;
                bool backup;
                Check(storage.TryLoad(slot, out loaded, out backup), "No se pudo cargar la ranura " + slot);
                Check(!backup, "Se cargó respaldo inesperadamente");
                Check(loaded.economy.coins == slot * 100, "Ranuras mezcladas");
            }

            // 2: segunda escritura crea respaldo válido.
            GameSaveData data1;
            bool fromBackup;
            Check(storage.TryLoad(1, out data1, out fromBackup), "Ranura 1 ilegible");
            data1.economy.coins = 999;
            storage.Save(1, data1);
            Check(storage.TryLoad(1, out data1, out fromBackup) &&
                  data1.economy.coins == 999 && !fromBackup, "Falló el segundo guardado");

            // 3: simulamos corrupción y recuperamos el guardado anterior.
            File.WriteAllText(storage.GetPrimaryPathForDiagnostics(1), "ARCHIVO DAÑADO");
            Check(storage.TryLoad(1, out data1, out fromBackup) && fromBackup &&
                  data1.economy.coins == 100, "No se recuperó el respaldo");

            // 4: borrar una ranura no afecta a otra.
            storage.DeleteSlot(2);
            Check(!storage.HasValidSave(2), "La ranura 2 no se eliminó");
            Check(storage.HasValidSave(3), "La ranura 3 se modificó por error");

            Debug.Log("[GUARDADO] PRUEBAS SUPERADAS: 3 ranuras, aislamiento, respaldo, corrupción y borrado.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GUARDADO] PRUEBA FALLIDA: " + ex);
            throw;
        }
        finally
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
            throw new Exception(description);
    }
}
#endif
