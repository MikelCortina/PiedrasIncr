
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Paso 79: carga coordinada de la partida.
/// Solo admite una escena preparada y sin edificios.
/// </summary>
public static class GameSaveFullRestoreCoordinator
{
    public sealed class Resultado
    {
        public GameSaveData partida;
        public bool desdeRespaldo;
        public PlacedBuildingsRestoreResult construcciones;
    }

    public static Resultado Restaurar(
        SaveStorage storage,
        int ranura,
        GameSaveSceneReferences r)
    {
        // 1. Leer y validar antes de modificar nada.
        bool respaldo;

        GameSaveData partida =
            GameSaveFullLoadValidator.LeerYValidar(
                storage, ranura, r, out respaldo
            );

        GameSaveFullLoadValidator.ExigirEscenaPreparada(
            partida, r
        );

        // 2. Capturar el estado anterior para recuperarlo
        // si falla alguna etapa de la restauracion.
        GameSaveData anterior =
            GameSaveFullWriter.CapturarEstado(r, 0);

        PlacedBuildingsRestoreResult creados = null;

        bool jugadorIniciado = false;
        bool mejorasIniciadas = false;
        bool equipoIniciado = false;
        bool procesadoraIniciada = false;
        bool inventarioIniciado = false;
        bool economiaIniciada = false;

        try
        {
            // 3. Reconstruir el mundo primero.
            creados =
                PlacedBuildingsSlotBridge.RestaurarEnEscenaVacia(
                    r.sistema, partida
                );

            // 4. Recuperar posicion del jugador
            // cuando ya existen las construcciones.
            jugadorIniciado = true;
            PlayerRestoreAdapter.Restore(
                r.jugador, partida
            );

            // 5. Mejoras.
            mejorasIniciadas = true;
            ToolUpgradesSaveAdapter.Restore(
                r.torbellino, r.iman, partida
            );

            // 6. Herramientas desbloqueadas.
            equipoIniciado = true;
            EquipmentUnlocksSaveAdapter.Restore(
                r.equipamiento, partida
            );

            // 7. Procesadora.
            procesadoraIniciada = true;
            ProcessorSaveAdapter.Restore(
                r.procesadora,
                r.construccionProcesadora,
                partida
            );

            // 8. Inventario.
            inventarioIniciado = true;
            BuildingInventorySaveAdapter.Restore(
                r.inventario, partida
            );

            // 9. Economia al final.
            economiaIniciada = true;
            EconomySaveAdapter.Restore(
                r.cartera, partida
            );

            return new Resultado
            {
                partida = partida,
                desdeRespaldo = respaldo,
                construcciones = creados
            };
        }
        catch (Exception errorOriginal)
        {
            // Intentar recuperar las secciones modificadas
            // en orden inverso. Continuar aunque alguna falle.
            List<Exception> errores =
                new List<Exception>();

            if (economiaIniciada)
                Intentar(
                    () => EconomySaveAdapter.Restore(
                        r.cartera, anterior),
                    errores
                );

            if (inventarioIniciado)
                Intentar(
                    () => BuildingInventorySaveAdapter.Restore(
                        r.inventario, anterior),
                    errores
                );

            if (procesadoraIniciada)
                Intentar(
                    () => ProcessorSaveAdapter.Restore(
                        r.procesadora,
                        r.construccionProcesadora,
                        anterior),
                    errores
                );

            if (equipoIniciado)
                Intentar(
                    () => EquipmentUnlocksSaveAdapter.Restore(
                        r.equipamiento, anterior),
                    errores
                );

            if (mejorasIniciadas)
                Intentar(
                    () => ToolUpgradesSaveAdapter.Restore(
                        r.torbellino, r.iman, anterior),
                    errores
                );

            if (jugadorIniciado)
                Intentar(
                    () => PlayerRestoreAdapter.Restore(
                        r.jugador, anterior),
                    errores
                );

            // Destruir exclusivamente edificios creados
            // por esta restauracion, nunca originales.
            if (creados != null)
            {
                foreach (EdificioConstruido edificio
                    in creados.edificios)
                {
                    if (edificio == null)
                        continue;

                    try
                    {
                        edificio.gameObject.SetActive(false);
                        UnityEngine.Object.Destroy(
                            edificio.gameObject
                        );
                    }
                    catch (Exception ex)
                    {
                        errores.Add(ex);
                    }
                }
            }

            if (errores.Count > 0)
            {
                errores.Insert(0, errorOriginal);

                throw new AggregateException(
                    "La carga fallo y la recuperacion " +
                    "del estado anterior fue incompleta.",
                    errores
                );
            }

            throw;
        }
    }

    private static void Intentar(
        Action accion,
        List<Exception> errores)
    {
        try
        {
            accion();
        }
        catch (Exception ex)
        {
            errores.Add(ex);
            Debug.LogException(ex);
        }
    }
}
