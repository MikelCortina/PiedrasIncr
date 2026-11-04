
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrRollbackInternoTests
{
    private const string Marca = "PASO82_ERROR_CONTROLADO_";

    private static readonly string[] Etapas =
    {
        "Prefab",
        "Conexion"
    };

    private static string Carpeta => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "Paso82"
    );

    private static GameSaveSceneReferences referencias;
    private static GameSaveData estadoInicial;
    private static GameSaveData partidaGuardada;
    private static SaveStorage storage;

    private static bool ejecutando;
    private static int indice;
    private static int fotogramaFallo;

    // ===========================================
    // FASE 1: PREPARAR ARCHIVO
    // ===========================================

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 82 - 1 Preparar prueba (Play)"
    )]
    public static void Preparar()
    {
        try
        {
            Comprobar(
                Application.isPlaying,
                "Entra en Play."
            );

            GameSaveSceneReferences r = BuscarReferencias();

            GameSaveData captura =
                GameSaveFullWriter.CapturarEstado(r, 0);

            PlacedBuildingsConnectionGraphReport grafo =
                PlacedBuildingsConnectionGraphValidator
                    .Analizar(captura);

            Comprobar(
                grafo.edificios >= 2 &&
                grafo.conexiones >= 1,
                "Coloca una rampa recta y " +
                "una pared conectada."
            );

            foreach (PlacedBuildingSaveData edificio
                in captura.placedBuildings.edificios)
            {
                Comprobar(
                    edificio.curvaturaRampa == 0f,
                    "No utilices rampas curvadas."
                );
            }

            SaveStorage archivo = new SaveStorage(Carpeta);

            archivo.DeleteSlot(1);

            GameSaveFullWriter.GuardarEnRanura(
                archivo,
                1,
                r,
                0
            );

            Debug.Log(
                "[GUARDADO] PASO 82 PREPARADO: " +
                "edificios y conexiones guardados " +
                "en ranura de diagnostico. " +
                "Reinicia Play con la escena limpia."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 82 PREPARACION FALLIDA: " +
                ex
            );
        }
    }

    // ===========================================
    // FASE 2: PROBAR FALLOS INTERNOS
    // ===========================================

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 82 - 2 Probar fallos internos (Play)"
    )]
    public static void Probar()
    {
        if (ejecutando)
        {
            Debug.LogWarning(
                "[GUARDADO] PASO 82 ya esta en curso."
            );
            return;
        }

        try
        {
            Comprobar(
                Application.isPlaying,
                "Entra en Play."
            );

            referencias = BuscarReferencias();
            storage = new SaveStorage(Carpeta);

            estadoInicial =
                GameSaveFullWriter.CapturarEstado(
                    referencias, 0
                );

            Comprobar(
                estadoInicial.placedBuildings.edificios.Count == 0,
                "La escena debe estar sin construcciones."
            );

            bool respaldo;

            partidaGuardada =
                GameSaveFullLoadValidator.LeerYValidar(
                    storage,
                    1,
                    referencias,
                    out respaldo
                );

            GameSaveFullLoadValidator.ExigirEscenaPreparada(
                partidaGuardada,
                referencias
            );

            PlacedBuildingsConnectionGraphReport grafo =
                PlacedBuildingsConnectionGraphValidator
                    .Analizar(partidaGuardada);

            Comprobar(
                grafo.edificios >= 2 &&
                grafo.conexiones >= 1,
                "El archivo necesita dos edificios " +
                "y al menos una conexion."
            );

            indice = 0;
            ejecutando = true;

            Debug.Log(
                "[GUARDADO] PASO 82 VALIDACION OK: " +
                "escena limpia y datos compatibles."
            );

            EditorApplication.update += Continuar;

            EjecutarEtapa();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 82 FALLIDO: " + ex
            );

            Finalizar();
        }
    }

    // ===========================================
    // PROVOCAR FALLO DENTRO DE LOS ADAPTADORES
    // ===========================================

    private static void EjecutarEtapa()
    {
        string etapa = Etapas[indice];

        bool puntoAlcanzado = false;
        bool falloEsperado = false;

        try
        {
            GameSaveFullLoadValidator.ExigirEscenaPreparada(
                partidaGuardada,
                referencias
            );

            if (etapa == "Prefab")
            {
                PlacedBuildingsBatchSpawnAdapter.FalloDiagnostico =
                    cantidad =>
                    {
                        if (cantidad != 1)
                            return;

                        puntoAlcanzado = true;

                        throw new InvalidOperationException(
                            Marca + etapa
                        );
                    };
            }
            else
            {
                PlacedBuildingsConnectionsRestoreAdapter.FalloDiagnostico =
                    cantidad =>
                    {
                        if (cantidad != 1)
                            return;

                        puntoAlcanzado = true;

                        throw new InvalidOperationException(
                            Marca + etapa
                        );
                    };
            }

            try
            {
                GameSaveFullRestoreCoordinator.Restaurar(
                    storage,
                    1,
                    referencias
                );
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message != Marca + etapa)
                    throw;

                falloEsperado = true;
            }

            Comprobar(
                puntoAlcanzado && falloEsperado,
                "No se produjo el fallo interno: " +
                etapa
            );

            // Esperar al final del fotograma
            // para que Unity complete Destroy.
            fotogramaFallo = Time.frameCount;
        }
        finally
        {
            LimpiarDiagnosticos();
        }
    }

    // ===========================================
    // COMPROBAR LIMPIEZA ENTRE ETAPAS
    // ===========================================

    private static void Continuar()
    {
        if (!ejecutando)
        {
            Finalizar();
            return;
        }

        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] PASO 82 interrumpido: " +
                "se ha salido de Play."
            );

            Finalizar();
            return;
        }

        if (Time.frameCount <= fotogramaFallo)
            return;

        try
        {
            VerificarEstadoOriginal();

            string etapa = Etapas[indice];

            Debug.Log(
                "[GUARDADO] PASO 82 " + etapa +
                " OK: fallo interno detectado, " +
                "sin edificios residuales ni cambios " +
                "en el progreso del jugador."
            );

            indice++;

            if (indice == Etapas.Length)
            {
                Debug.Log(
                    "[GUARDADO] PASO 82 SUPERADO: " +
                    "rollback durante la creacion " +
                    "de prefabs y la aplicacion de " +
                    "conexiones, escena original intacta."
                );

                storage.DeleteSlot(1);

                Finalizar();
                return;
            }

            EjecutarEtapa();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 82 FALLIDO " +
                Etapas[indice] + ": " + ex
            );

            Finalizar();
        }
    }

    private static void VerificarEstadoOriginal()
    {
        Scene escena = SceneManager.GetActiveScene();

        int cantidadEdificios = 0;

        EdificioConstruido[] encontrados =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in encontrados)
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena)
            {
                cantidadEdificios++;
            }
        }

        Comprobar(
            cantidadEdificios == 0,
            "Quedan edificios temporales: " +
            cantidadEdificios
        );

        GameSaveData actual =
            GameSaveFullWriter.CapturarEstado(
                referencias,
                0
            );

        Comprobar(
            actual.placedBuildings.edificios.Count == 0,
            "La captura contiene edificios residuales."
        );

        Comprobar(
            actual.economy.coins ==
                estadoInicial.economy.coins,
            "Las monedas han cambiado."
        );

        Iguales(
            actual.equipmentUnlocks,
            estadoInicial.equipmentUnlocks,
            "Desbloqueos"
        );

        Iguales(
            actual.toolUpgrades,
            estadoInicial.toolUpgrades,
            "Mejoras"
        );

        Iguales(
            actual.processor,
            estadoInicial.processor,
            "Procesadora"
        );

        Iguales(
            actual.buildingInventory,
            estadoInicial.buildingInventory,
            "Inventario"
        );

        Vector3 antes =
            PlayerSaveAdapter.GetSavedPosition(
                estadoInicial.player
            );

        Vector3 despues =
            PlayerSaveAdapter.GetSavedPosition(
                actual.player
            );

        Comprobar(
            Vector3.Distance(antes, despues) < 0.1f,
            "La posicion del jugador ha cambiado."
        );

        Comprobar(
            Mathf.Abs(
                Mathf.DeltaAngle(
                    estadoInicial.player.yawDegrees,
                    actual.player.yawDegrees
                )
            ) < 0.2f,
            "La rotacion del jugador ha cambiado."
        );
    }

    // ===========================================
    // REFERENCIAS REALES
    // ===========================================

    private static GameSaveSceneReferences BuscarReferencias()
    {
        SistemaConstruccion sistema =
            SistemaConstruccion.Instancia;

        Comprobar(
            sistema != null,
            "No existe SistemaConstruccion."
        );

        InventarioEdificios inventario =
            sistema.inventarioEdificios;

        Comprobar(
            inventario != null,
            "No existe InventarioEdificios."
        );

        Cartera cartera = inventario.cartera;

        if (cartera == null)
            cartera = inventario.GetComponentInParent<Cartera>();

        Comprobar(
            cartera != null,
            "No existe Cartera."
        );

        DesbloqueoProcesadoraMonedas construccion =
            BuscarUnico<DesbloqueoProcesadoraMonedas>();

        Comprobar(
            construccion.maquinaErosion != null,
            "No existe MaquinaErosion."
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
        T resultado = null;
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

            resultado = componente;
            cantidad++;
        }

        Comprobar(
            cantidad == 1,
            "Se esperaba exactamente un componente " +
            typeof(T).Name +
            ". Encontrados: " + cantidad
        );

        return resultado;
    }

    private static void Iguales(
        object actual,
        object esperado,
        string nombre)
    {
        Comprobar(
            JsonUtility.ToJson(actual) ==
                JsonUtility.ToJson(esperado),
            nombre + " ha cambiado."
        );
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }

    private static void LimpiarDiagnosticos()
    {
        PlacedBuildingsBatchSpawnAdapter.FalloDiagnostico =
            null;

        PlacedBuildingsConnectionsRestoreAdapter.FalloDiagnostico =
            null;
    }

    private static void Finalizar()
    {
        EditorApplication.update -= Continuar;

        LimpiarDiagnosticos();

        ejecutando = false;
        referencias = null;
        estadoInicial = null;
        partidaGuardada = null;
        storage = null;
    }
}

#endif
