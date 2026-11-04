#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prueba aislada sin Play: crea una Cartera temporal y archivos en Temp.
/// No modifica la escena abierta, el jugador real ni las partidas reales.
/// </summary>
public static class PiedrasIncrEconomyTests
{
    [MenuItem("Tools/PiedrasIncr/Guardado/Probar economía (aislado)")]
    public static void Run()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(),
            "PiedrasIncr_EconomyTest_" + Guid.NewGuid().ToString("N"));
        GameObject testObject = null;

        try
        {
            testObject = new GameObject("__PRUEBA_CARTERA_GUARDADO__");
            testObject.hideFlags = HideFlags.HideAndDontSave;
            Cartera cartera = testObject.AddComponent<Cartera>();
            var storage = new SaveStorage(testDirectory);

            int cambios = 0;
            int ultimoSaldoNotificado = -1;
            cartera.OnMonedasCambiadas += saldo =>
            {
                cambios++;
                ultimoSaldoNotificado = saldo;
            };

            // Restaurar no compra ni acumula; reemplaza el saldo exacto.
            cartera.RestaurarMonedas(125);
            Check(cartera.Monedas == 125 && cambios == 1 && ultimoSaldoNotificado == 125,
                "Restauracion inicial / evento incorrecto.");

            // Restaurar el mismo saldo no debe lanzar eventos duplicados.
            cartera.RestaurarMonedas(125);
            Check(cambios == 1, "Restaurar sin cambio ha repetido el evento.");

            var partida1 = new GameSaveData();
            EconomySaveAdapter.Capture(cartera, partida1);
            storage.Save(1, partida1);

            // Una segunda ranura totalmente independiente.
            cartera.RestaurarMonedas(950);
            var partida2 = new GameSaveData();
            EconomySaveAdapter.Capture(cartera, partida2);
            storage.Save(2, partida2);

            GameSaveData leida;
            bool respaldo;
            Check(storage.TryLoad(1, out leida, out respaldo) && !respaldo,
                "No se ha podido leer la ranura 1.");
            int antes = cambios;
            EconomySaveAdapter.Restore(cartera, leida);
            Check(cartera.Monedas == 125 && cambios == antes + 1 && ultimoSaldoNotificado == 125,
                "Carga ranura 1 / evento incorrectos.");

            Check(storage.TryLoad(2, out leida, out respaldo) && !respaldo,
                "No se ha podido leer la ranura 2.");
            EconomySaveAdapter.Restore(cartera, leida);
            Check(cartera.Monedas == 950, "Las ranuras comparten el saldo.");

            // Guardar un saldo cero tambien debe recuperarse correctamente.
            cartera.RestaurarMonedas(0);
            var partida3 = new GameSaveData();
            EconomySaveAdapter.Capture(cartera, partida3);
            storage.Save(3, partida3);
            cartera.RestaurarMonedas(15);
            Check(storage.TryLoad(3, out leida, out respaldo), "No se pudo leer ranura 3.");
            EconomySaveAdapter.Restore(cartera, leida);
            Check(cartera.Monedas == 0, "No se recupero correctamente el saldo cero.");

            // No se permite restaurar dinero negativo ni mutar la cartera.
            int saldoAntes = cartera.Monedas;
            bool negativoRechazado = false;
            try { cartera.RestaurarMonedas(-1); }
            catch (ArgumentOutOfRangeException) { negativoRechazado = true; }
            Check(negativoRechazado && cartera.Monedas == saldoAntes,
                "Se ha aceptado un saldo negativo.");

            // El adaptador tambien debe rechazar snapshots invalidos.
            leida.economy.coins = -10;
            bool snapshotRechazado = false;
            try { EconomySaveAdapter.Restore(cartera, leida); }
            catch (InvalidOperationException) { snapshotRechazado = true; }
            Check(snapshotRechazado && cartera.Monedas == saldoAntes,
                "Se ha restaurado un snapshot de economia invalido.");

            Debug.Log("[GUARDADO] ECONOMÍA SUPERADA: captura, 3 ranuras, saldo cero, restauración exacta, eventos y validación.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GUARDADO] ECONOMÍA FALLIDA: " + ex);
            throw;
        }
        finally
        {
            if (testObject != null)
                UnityEngine.Object.DestroyImmediate(testObject);
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
#endif
