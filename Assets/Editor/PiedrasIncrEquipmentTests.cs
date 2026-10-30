#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prueba aislada sin Play. No toca al jugador real ni a las ranuras reales.
/// </summary>
public static class PiedrasIncrEquipmentTests
{
    [MenuItem("Tools/PiedrasIncr/Guardado/Probar desbloqueos (aislado)")]
    public static void Run()
    {
        string folder = Path.Combine(Path.GetTempPath(),
            "PiedrasIncr_EquipmentTest_" + Guid.NewGuid().ToString("N"));
        GameObject testObject = null;

        try
        {
            testObject = new GameObject("__PRUEBA_EQUIPAMIENTO__");
            testObject.hideFlags = HideFlags.HideAndDontSave;
            GestorEquipamiento equipment = testObject.AddComponent<GestorEquipamiento>();
            Cartera wallet = testObject.AddComponent<Cartera>();
            wallet.RestaurarMonedas(333);

            var storage = new SaveStorage(folder);
            int currencyChanges = 0;
            wallet.OnMonedasCambiadas += _ => currencyChanges++;

            // Ranura 1: arma exclusivamente.
            equipment.RestaurarDesbloqueos(true, false, false);
            var slot1 = new GameSaveData();
            EconomySaveAdapter.Capture(wallet, slot1);
            EquipmentUnlocksSaveAdapter.Capture(equipment, slot1);
            storage.Save(1, slot1);

            // Ranura 2: todas las herramientas.
            equipment.RestaurarDesbloqueos(true, true, true);
            var slot2 = new GameSaveData();
            EconomySaveAdapter.Capture(wallet, slot2);
            EquipmentUnlocksSaveAdapter.Capture(equipment, slot2);
            storage.Save(2, slot2);

            // Ranura 3: sin desbloqueos.
            equipment.RestaurarDesbloqueos(false, false, false);
            var slot3 = new GameSaveData();
            EconomySaveAdapter.Capture(wallet, slot3);
            EquipmentUnlocksSaveAdapter.Capture(equipment, slot3);
            storage.Save(3, slot3);

            GameSaveData read;
            bool backup;
            Check(storage.TryLoad(1, out read, out backup) && !backup, "Ranura 1 ilegible.");
            Check(read.hasEquipmentUnlocksData, "Ranura 1 sin marca de datos de equipo.");
            EquipmentUnlocksSaveAdapter.Restore(equipment, read);
            Check(equipment.ArmaLanzadoraDesbloqueada() &&
                  !equipment.TorbellinoDesbloqueado() &&
                  !equipment.MartilloDesbloqueado(), "Ranura 1 incorrecta.");
            Check(equipment.ManosLibres, "La carga debe dejar las manos vacias.");

            Check(storage.TryLoad(2, out read, out backup), "Ranura 2 ilegible.");
            Check(read.hasEquipmentUnlocksData, "Ranura 2 sin marca de datos de equipo.");
            EquipmentUnlocksSaveAdapter.Restore(equipment, read);
            Check(equipment.ArmaLanzadoraDesbloqueada() &&
                  equipment.TorbellinoDesbloqueado() &&
                  equipment.MartilloDesbloqueado(), "Ranura 2 incorrecta.");

            Check(storage.TryLoad(3, out read, out backup), "Ranura 3 ilegible.");
            Check(read.hasEquipmentUnlocksData, "Ranura 3 sin marca de datos de equipo.");
            EquipmentUnlocksSaveAdapter.Restore(equipment, read);
            Check(!equipment.ArmaLanzadoraDesbloqueada() &&
                  !equipment.TorbellinoDesbloqueado() &&
                  !equipment.MartilloDesbloqueado(), "Ranura 3 incorrecta.");

            // Un archivo anterior a este modulo sigue siendo legible, pero
            // la ausencia de la marca impide tratar datos fabricados como reales.
            var oldSave = new GameSaveData();
            Check(!oldSave.hasEquipmentUnlocksData, "Un guardado antiguo tiene marca de equipo.");
            storage.Save(1, oldSave);
            Check(storage.TryLoad(1, out read, out backup), "El archivo antiguo no se puede leer.");
            Check(!read.hasEquipmentUnlocksData, "El archivo antiguo ha fabricado una marca de equipo.");

            // Aseguramos que rechazar una carga antigua no altera el estado previo.
            equipment.RestaurarDesbloqueos(true, true, true);
            bool rejected = false;
            try { EquipmentUnlocksSaveAdapter.Restore(equipment, read); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Se ha restaurado un archivo sin estado de equipamiento.");
            Check(equipment.ArmaLanzadoraDesbloqueada() &&
                  equipment.TorbellinoDesbloqueado() &&
                  equipment.MartilloDesbloqueado(), "Se modifico el equipo tras rechazar el archivo antiguo.");

            Check(wallet.Monedas == 333 && currencyChanges == 0,
                "La restauracion de desbloqueos ha modificado las monedas.");

            Debug.Log("[GUARDADO] DESBLOQUEOS SUPERADOS: 3 ranuras, restauracion exacta, manos vacias, economia intacta y compatibilidad v1.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GUARDADO] DESBLOQUEOS FALLIDOS: " + ex);
            throw;
        }
        finally
        {
            if (testObject != null) UnityEngine.Object.DestroyImmediate(testObject);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static void Check(bool condition, string reason)
    {
        if (!condition) throw new Exception(reason);
    }
}
#endif
