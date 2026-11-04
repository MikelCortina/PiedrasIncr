
#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrFullRestoreTests
{
    private static string Carpeta => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "Paso79"
    );

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 79 - 1 Guardar prueba (Play)"
    )]
    public static void Preparar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[GUARDADO] Entra en Play.");
            return;
        }

        try
        {
            GameSaveSceneReferences r = Referencias();

            GameSaveData captura =
                GameSaveFullWriter.CapturarEstado(
                    r, 3456.5
                );

            PlacedBuildingsConnectionGraphReport grafo =
                PlacedBuildingsConnectionGraphValidator.Analizar(
                    captura
                );

            Comprobar(
                grafo.edificios >= 2 &&
                grafo.conexiones >= 1,
                "Coloca al menos dos edificios conectados."
            );

            foreach (PlacedBuildingSaveData edificio
                in captura.placedBuildings.edificios)
            {
                Comprobar(
                    edificio.curvaturaRampa == 0f,
                    "La prueba requiere rampas sin curvatura."
                );
            }

            SaveStorage storage = new SaveStorage(Carpeta);

            GameSaveFullWriter.GuardarEnRanura(
                storage, 1, r, 3456.5
            );

            Debug.Log(
                "[GUARDADO] PASO 79 PREPARADO: " +
                "partida completa guardada en diagnostico. " +
                "Sal de Play, vuelve a entrar con la escena " +
                "sin construcciones y ejecuta la fase 2."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 79 PREPARACION FALLIDA: " +
                ex
            );
        }
    }

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 79 - 2 Restaurar prueba (Play)"
    )]
    public static void Restaurar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[GUARDADO] Entra en Play.");
            return;
        }

        try
        {
            GameSaveSceneReferences r = Referencias();
            SaveStorage storage = new SaveStorage(Carpeta);

            // Comprobar que la escena esta preparada.
            GameSaveData guardada;
            bool respaldo;

            guardada =
                GameSaveFullLoadValidator.LeerYValidar(
                    storage, 1, r, out respaldo
                );

            GameSaveFullLoadValidator.ExigirEscenaPreparada(
                guardada, r
            );

            Debug.Log(
                "[GUARDADO] PASO 79 VALIDACION OK: " +
                "escena limpia y datos compatibles."
            );

            // Restaurar todos los sistemas.
            GameSaveFullRestoreCoordinator.Resultado resultado =
                GameSaveFullRestoreCoordinator.Restaurar(
                    storage, 1, r
                );

            Comprobar(
                resultado.construcciones.edificios.Count ==
                    guardada.placedBuildings.edificios.Count,
                "Cantidad de edificios incorrecta."
            );

            int conexiones = 0;

            foreach (PlacedBuildingSaveData edificio
                in guardada.placedBuildings.edificios)
            {
                if (edificio.hasConnection)
                    conexiones++;
            }

            Comprobar(
                resultado.construcciones.conexionesRestauradas ==
                    conexiones,
                "Cantidad de conexiones incorrecta."
            );

            Debug.Log(
                "[GUARDADO] PASO 79 EDIFICIOS OK: " +
                "prefabs y conexiones reconstruidos."
            );

            // Comprobar posicion del jugador.
            Vector3 posicionGuardada =
                PlayerSaveAdapter.GetSavedPosition(
                    guardada.player
                );

            Comprobar(
                Vector3.Distance(
                    r.jugador.transform.position,
                    posicionGuardada
                ) < 0.01f,
                "Posicion del jugador incorrecta."
            );

            Comprobar(
                Quaternion.Angle(
                    r.jugador.transform.rotation,
                    Quaternion.Euler(
                        0f,
                        guardada.player.yawDegrees,
                        0f
                    )
                ) < 0.2f,
                "Rotacion del jugador incorrecta."
            );

            // Recapturar los otros sistemas.
            GameSaveData comprobacion = new GameSaveData();

            EconomySaveAdapter.Capture(
                r.cartera, comprobacion
            );

            EquipmentUnlocksSaveAdapter.Capture(
                r.equipamiento, comprobacion
            );

            ToolUpgradesSaveAdapter.Capture(
                r.torbellino, r.iman, comprobacion
            );

            ProcessorSaveAdapter.Capture(
                r.procesadora,
                r.construccionProcesadora,
                comprobacion
            );

            BuildingInventorySaveAdapter.Capture(
                r.inventario, comprobacion
            );

            Comprobar(
                comprobacion.economy.coins ==
                    guardada.economy.coins,
                "Monedas incorrectas."
            );

            ComprobarIguales(
                comprobacion.equipmentUnlocks,
                guardada.equipmentUnlocks,
                "Desbloqueos"
            );

            ComprobarIguales(
                comprobacion.toolUpgrades,
                guardada.toolUpgrades,
                "Mejoras"
            );

            ComprobarIguales(
                comprobacion.processor,
                guardada.processor,
                "Procesadora"
            );

            ComprobarIguales(
                comprobacion.buildingInventory,
                guardada.buildingInventory,
                "Inventario"
            );

            Debug.Log(
                "[GUARDADO] PASO 79 SISTEMAS OK: " +
                "jugador, monedas, desbloqueos, mejoras, " +
                "procesadora e inventario restaurados."
            );

            // Verificar identidad y datos de edificios.
            PlacedBuildingsSlotBridge.CapturarParaRanura(
                r.sistema, comprobacion
            );

            Dictionary<string, PlacedBuildingSaveData> actuales =
                new Dictionary<string, PlacedBuildingSaveData>();

            foreach (PlacedBuildingSaveData edificio
                in comprobacion.placedBuildings.edificios)
            {
                actuales.Add(edificio.idPersistente, edificio);
            }

            Comprobar(
                actuales.Count ==
                    guardada.placedBuildings.edificios.Count,
                "Cantidad final de edificios incorrecta."
            );

            foreach (PlacedBuildingSaveData esperado
                in guardada.placedBuildings.edificios)
            {
                PlacedBuildingSaveData actual;

                Comprobar(
                    actuales.TryGetValue(
                        esperado.idPersistente, out actual
                    ),
                    "Falta un edificio restaurado."
                );


                bool identidadCorrecta =
                    actual.tipo == esperado.tipo &&
                    actual.variantePrefab == esperado.variantePrefab &&
                    actual.hasConnection == esperado.hasConnection;

                bool conexionCorrecta;

                if (esperado.hasConnection)
                {
                    // Si hay conexion, todos los campos
                    // deben coincidir exactamente.
                    conexionCorrecta =
                        actual.connectedToBuildingId ==
                            esperado.connectedToBuildingId &&
                        actual.connectorPath ==
                            esperado.connectorPath &&
                        actual.connectorTag ==
                            esperado.connectorTag;
                }
                else
                {
                    // Sin conexion, null y "" son equivalentes.
                    conexionCorrecta =
                        string.IsNullOrEmpty(actual.connectedToBuildingId) &&
                        string.IsNullOrEmpty(actual.connectorPath) &&
                        string.IsNullOrEmpty(actual.connectorTag);
                }

                if (!identidadCorrecta || !conexionCorrecta)
                {
                    throw new InvalidOperationException(
                        "Identidad o conexion incorrecta. ID=" +
                        esperado.idPersistente +
                        " | Tipo=" + actual.tipo +
                        " / " + esperado.tipo +
                        " | Variante=" + actual.variantePrefab +
                        " / " + esperado.variantePrefab +
                        " | Conectado=" + actual.hasConnection +
                        " / " + esperado.hasConnection +
                        " | Padre=" + actual.connectedToBuildingId +
                        " / " + esperado.connectedToBuildingId +
                        " | Ruta=" + actual.connectorPath +
                        " / " + esperado.connectorPath +
                        " | Tag=" + actual.connectorTag +
                        " / " + esperado.connectorTag
                    );
                }


                Vector3 posicionActual = new Vector3(
                    actual.positionX,
                    actual.positionY,
                    actual.positionZ
                );

                Vector3 posicionEsperada = new Vector3(
                    esperado.positionX,
                    esperado.positionY,
                    esperado.positionZ
                );

                Comprobar(
                    Vector3.Distance(
                        posicionActual,
                        posicionEsperada
                    ) < 0.01f,
                    "Posicion de edificio incorrecta."
                );
            }

            Debug.Log(
                "[GUARDADO] PASO 79 SUPERADO: " +
                "carga completa desde archivo, " +
                "edificios y conexiones correctos, " +
                "jugador, economia y progreso restaurados."
            );

            // Borrar solo la ranura de diagnostico.
            storage.DeleteSlot(1);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 79 FALLIDO: " + ex
            );
        }
    }

    private static GameSaveSceneReferences Referencias()
    {
        SistemaConstruccion sistema =
            SistemaConstruccion.Instancia;

        Comprobar(
            sistema != null,
            "Falta SistemaConstruccion."
        );

        InventarioEdificios inventario =
            sistema.inventarioEdificios;

        Comprobar(
            inventario != null,
            "Falta InventarioEdificios."
        );

        Cartera cartera = inventario.cartera;

        if (cartera == null)
            cartera = inventario.GetComponentInParent<Cartera>();

        Comprobar(
            cartera != null,
            "Falta Cartera."
        );

        DesbloqueoProcesadoraMonedas construccion =
            BuscarUnico<DesbloqueoProcesadoraMonedas>();

        Comprobar(
            construccion.maquinaErosion != null,
            "Falta MaquinaErosion."
        );

        return new GameSaveSceneReferences
        {
            jugador =
                BuscarUnico<MovimientoPersonaje>().gameObject,
            sistema = sistema,
            cartera = cartera,
            equipamiento =
                BuscarUnico<GestorEquipamiento>(),
            torbellino =
                BuscarUnico<HerramientaTorbellino>(),
            iman =
                BuscarUnico<RecolectorMagnetico>(),
            procesadora =
                construccion.maquinaErosion,
            construccionProcesadora = construccion,
            inventario = inventario
        };
    }

    private static T BuscarUnico<T>() where T : Component
    {
        T encontrado = null;
        int cantidad = 0;

        Scene escena = SceneManager.GetActiveScene();

        foreach (T componente
            in UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (componente == null ||
                componente.gameObject.scene != escena)
                continue;

            encontrado = componente;
            cantidad++;
        }

        Comprobar(
            cantidad == 1,
            "Se esperaba exactamente un componente " +
            typeof(T).Name +
            ". Encontrados: " + cantidad
        );

        return encontrado;
    }

    private static void ComprobarIguales(
        object actual,
        object esperado,
        string seccion)
    {
        Comprobar(
            JsonUtility.ToJson(actual) ==
                JsonUtility.ToJson(esperado),
            seccion + " no coincide con el guardado."
        );
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}

#endif
