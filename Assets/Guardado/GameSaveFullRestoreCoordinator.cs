
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Paso 81: carga coordinada de la partida
/// con recuperacion ante fallos y pruebas
/// independientes en cada etapa.
///
/// Solo admite una escena preparada
/// y sin edificios existentes.
/// </summary>
public static class GameSaveFullRestoreCoordinator
{
#if UNITY_EDITOR
    // Permite provocar fallos controlados.
    // Solo existe dentro del Editor de Unity.
    public static Action<string> FalloDiagnostico;
#endif

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
        // ======================================
        // 1. LEER Y VALIDAR
        // ======================================

        bool respaldo;

        GameSaveData partida =
            GameSaveFullLoadValidator.LeerYValidar(
                storage,
                ranura,
                r,
                out respaldo
            );

        GameSaveFullLoadValidator.ExigirEscenaPreparada(
            partida,
            r
        );

        // ======================================
        // 2. CAPTURAR ESTADO ANTERIOR
        // ======================================

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
            // ======================================
            // 3. RESTAURAR EDIFICIOS
            // ======================================

            creados =
                PlacedBuildingsSlotBridge.RestaurarEnEscenaVacia(
                    r.sistema,
                    partida
                );

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke("TrasEdificios");
#endif

            // ======================================
            // 4. RESTAURAR JUGADOR
            // ======================================

            jugadorIniciado = true;

            PlayerRestoreAdapter.Restore(
                r.jugador,
                partida
            );

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke("TrasJugador");
#endif

            // ======================================
            // 5. RESTAURAR MEJORAS
            // ======================================

            mejorasIniciadas = true;

            ToolUpgradesSaveAdapter.Restore(
                r.torbellino,
                r.iman,
                partida
            );

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke("TrasMejoras");
#endif

            // ======================================
            // 6. RESTAURAR DESBLOQUEOS
            // ======================================

            equipoIniciado = true;

            EquipmentUnlocksSaveAdapter.Restore(
                r.equipamiento,
                partida
            );

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke("TrasEquipo");
#endif

            // ======================================
            // 7. RESTAURAR PROCESADORA
            // ======================================

            procesadoraIniciada = true;

            ProcessorSaveAdapter.Restore(
                r.procesadora,
                r.construccionProcesadora,
                partida
            );

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke("TrasProcesadora");
#endif

            // ======================================
            // 8. RESTAURAR INVENTARIO
            // ======================================

            inventarioIniciado = true;

            BuildingInventorySaveAdapter.Restore(
                r.inventario,
                partida
            );

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke("TrasInventario");
#endif

            // ======================================
            // 9. RESTAURAR ECONOMIA
            // ======================================

            economiaIniciada = true;

            EconomySaveAdapter.Restore(
                r.cartera,
                partida
            );

#if UNITY_EDITOR
            FalloDiagnostico?.Invoke("TrasEconomia");
#endif

            // ======================================
            // 10. RESULTADO CORRECTO
            // ======================================

            return new Resultado
            {
                partida = partida,
                desdeRespaldo = respaldo,
                construcciones = creados
            };
        }
        catch (Exception errorOriginal)
        {
            // ======================================
            // 11. RECUPERACION ANTE FALLOS
            // ======================================

            List<Exception> errores =
                new List<Exception>();

            // Recuperar economia.
            if (economiaIniciada)
            {
                Intentar(
                    () => EconomySaveAdapter.Restore(
                        r.cartera,
                        anterior
                    ),
                    errores
                );
            }

            // Recuperar inventario.
            if (inventarioIniciado)
            {
                Intentar(
                    () => BuildingInventorySaveAdapter.Restore(
                        r.inventario,
                        anterior
                    ),
                    errores
                );
            }

            // Recuperar procesadora.
            if (procesadoraIniciada)
            {
                Intentar(
                    () => ProcessorSaveAdapter.Restore(
                        r.procesadora,
                        r.construccionProcesadora,
                        anterior
                    ),
                    errores
                );
            }

            // Recuperar desbloqueos.
            if (equipoIniciado)
            {
                Intentar(
                    () => EquipmentUnlocksSaveAdapter.Restore(
                        r.equipamiento,
                        anterior
                    ),
                    errores
                );
            }

            // Recuperar mejoras.
            if (mejorasIniciadas)
            {
                Intentar(
                    () => ToolUpgradesSaveAdapter.Restore(
                        r.torbellino,
                        r.iman,
                        anterior
                    ),
                    errores
                );
            }

            // Recuperar jugador.
            if (jugadorIniciado)
            {
                Intentar(
                    () => PlayerRestoreAdapter.Restore(
                        r.jugador,
                        anterior
                    ),
                    errores
                );
            }

            // ======================================
            // 12. RETIRAR EDIFICIOS TEMPORALES
            // ======================================

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

            // ======================================
            // 13. COMPROBAR RECUPERACION
            // ======================================

            if (errores.Count > 0)
            {
                errores.Insert(0, errorOriginal);

                throw new AggregateException(
                    "La carga fallo y la recuperacion " +
                    "del estado anterior fue incompleta.",
                    errores
                );
            }

            // Propagar el error original.
            throw;
        }
    }

    // ======================================
    // RECUPERACION INDIVIDUAL
    // ======================================

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
