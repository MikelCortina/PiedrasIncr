#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Captura y lectura en archivos TEMPORALES con componentes de prueba.
/// No altera jugadores de la escena ni guardados de produccion.
/// </summary>
public static class PiedrasIncrToolUpgradesTests
{
    [MenuItem("Tools/PiedrasIncr/Guardado/Probar mejoras herramientas (aislado)")]
    public static void Run()
    {
        string folder = Path.Combine(Path.GetTempPath(),
            "PiedrasIncr_ToolUpgradeTest_" + Guid.NewGuid().ToString("N"));
        GameObject[] objects = new GameObject[3];

        try
        {
            var storage = new SaveStorage(folder);

            for (int slot = 1; slot <= 3; slot++)
            {
                var go = new GameObject("__PRUEBA_MEJORAS_" + slot + "__");
                go.hideFlags = HideFlags.HideAndDontSave;
                objects[slot - 1] = go;

                var tornado = go.AddComponent<HerramientaTorbellino>();
                var magnet = go.AddComponent<RecolectorMagnetico>();
                var data = new GameSaveData();
                data.economy.coins = 400 + slot;

                if (slot == 1)
                {
                    tornado.EstablecerNivelRadio(4);
                    Check(tornado.MejorarAlcance() && tornado.MejorarAlcance(),
                        "No se ha podido poner el alcance del Torbellino a 3.");
                    Check(tornado.MejorarMovilidad(),
                        "No se ha podido poner la movilidad del Torbellino a 2.");
                    magnet.DesbloquearIman();
                    Check(magnet.MejorarAlcance() && magnet.MejorarAlcance(),
                        "No se ha podido poner el alcance del Iman a 3.");
                }
                else if (slot == 2)
                {
                    tornado.EstablecerNivelRadio(2);
                    Check(tornado.MejorarMovilidad() && tornado.MejorarMovilidad(),
                        "No se ha podido poner la movilidad del Torbellino a 3.");
                    magnet.DesbloquearIman();
                }
                // Ranura 3: niveles iniciales e iman bloqueado.

                ToolUpgradesSaveAdapter.Capture(tornado, magnet, data);
                Check(data.economy.coins == 400 + slot,
                    "La captura ha modificado la economia.");
                storage.Save(slot, data);
            }

            int[,] expected = { { 4, 3, 2, 3 }, { 2, 1, 3, 1 }, { 1, 1, 1, 1 } };
            for (int slot = 1; slot <= 3; slot++)
            {
                GameSaveData loaded;
                bool fromBackup;
                Check(storage.TryLoad(slot, out loaded, out fromBackup) && !fromBackup,
                    "No se ha podido leer la ranura " + slot + ".");
                var upgrades = ToolUpgradesSaveAdapter.Read(loaded);
                int row = slot - 1;
                Check(upgrades.nivelRadioTorbellino == expected[row, 0] &&
                      upgrades.nivelAlcanceTorbellino == expected[row, 1] &&
                      upgrades.nivelMovilidadTorbellino == expected[row, 2] &&
                      upgrades.nivelAlcanceIman == expected[row, 3] &&
                      upgrades.imanDesbloqueado == (slot != 3) &&
                      loaded.economy.coins == 400 + slot,
                      "Mezcla de ranuras o niveles incorrectos en la ranura " + slot + ".");
            }

            // Un archivo anterior a este modulo NO debe aparentar tener mejoras.
            var oldSave = new GameSaveData();
            storage.Save(1, oldSave);
            GameSaveData oldLoaded;
            bool backupFound;
            Check(storage.TryLoad(1, out oldLoaded, out backupFound),
                "El archivo anterior no se puede leer.");
            Check(!oldLoaded.hasToolUpgradesData,
                "El archivo antiguo ha fabricado la marca de mejoras.");
            bool rejectedOld = false;
            try { ToolUpgradesSaveAdapter.Read(oldLoaded); }
            catch (InvalidOperationException) { rejectedOld = true; }
            Check(rejectedOld, "Se han aceptado mejoras inexistentes.");

            var invalid = new GameSaveData { hasToolUpgradesData = true,
                toolUpgrades = new ToolUpgradesSaveData
                {
                    nivelRadioTorbellino = 5,
                    nivelAlcanceTorbellino = 1,
                    nivelMovilidadTorbellino = 1,
                    nivelAlcanceIman = 1
                }
            };
            bool rejectedInvalid = false;
            try { ToolUpgradesSaveAdapter.Read(invalid); }
            catch (InvalidOperationException) { rejectedInvalid = true; }
            Check(rejectedInvalid, "Se ha aceptado un nivel fuera de rango.");

            Debug.Log("[GUARDADO] MEJORAS SUPERADAS: Torbellino, Iman, 3 ranuras, economia intacta, rangos y compatibilidad v1.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GUARDADO] MEJORAS FALLIDAS: " + ex);
            throw;
        }
        finally
        {
            foreach (var go in objects)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static void Check(bool condition, string error)
    {
        if (!condition) throw new Exception(error);
    }
}
#endif
