
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrRollbackEtapasTests
{
    // Ejecutaremos los fallos en este orden.
    private static readonly string[] Etapas =
    {
        "TrasEdificios",
        "TrasJugador",
        "TrasMejoras",
        "TrasEquipo",
        "TrasProcesadora",
        "TrasInventario",
        "TrasEconomia"
    };

    private const string Marca = "PASO81_FALLO_";

    // Reutilizamos el archivo de diagnostico
    // preparado por el paso 80.
    private static string Carpeta => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "Paso80"
    );

    private static GameSaveSceneReferences referencias;
    private static GameSaveData estadoInicial;
    private static GameSaveData partidaGuardada;
    private static SaveStorage almacenamiento;

    private static int indice;
    private static int fotogramaFallo;
    private static bool ejecutando;

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 81 - Probar rollback por etapas (Play)"
    )]
    public static void Ejecutar()
    {
        if (ejecutando)
        {
            Debug.LogWarning(
                "[GUARDADO] PASO 81 ya esta ejecutandose."
            );
            return;
        }

        try
        {
            Comprobar(
                Application.isPlaying,
                "Debes entrar en Play."
            );

            referencias = BuscarReferencias();

            // La prueba empieza con una escena limpia.
            estadoInicial =
                GameSaveFullWriter.CapturarEstado(
                    referencias, 0
                );

            Comprobar(
                estadoInicial.placedBuildings.edificios.Count == 0,
                "La escena debe estar sin construcciones."
            );

            almacenamiento = new SaveStorage(Carpeta);

            Comprobar(
                almacenamiento.HasValidSave(1),
                "Falta el archivo de diagnostico. " +
                "Ejecuta primero la fase 1 del paso 80."
            );

            bool respaldo;

            partidaGuardada =
                GameSaveFullLoadValidator.LeerYValidar(
                    almacenamiento,
                    1,
                    referencias,
                    out respaldo
                );

            GameSaveFullLoadValidator.ExigirEscenaPreparada(
                partidaGuardada, referencias
            );

            Comprobar(
                partidaGuardada.placedBuildings.edificios.Count
                    >= 2,
                "El guardado debe contener dos edificios."
            );

            // Garantizar que existe una diferencia real
            // que recuperar durante el rollback.
            Comprobar(
                partidaGuardada.economy.coins !=
                    estadoInicial.economy.coins,
                "Las monedas guardadas deben ser diferentes."
            );

            Debug.Log(
                "[GUARDADO] PASO 81 PREPARACION OK: " +
                "escena limpia, archivo valido y " +
                "siete etapas pendientes."
            );

            indice = 0;
            ejecutando = true;

            EditorApplication.update += Continuar;

            EjecutarEtapa();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 81 FALLIDO: " + ex
            );

            Finalizar();
        }
    }

    // =====================================
    // PROVOCAR UN FALLO EN UNA ETAPA
    // =====================================

    private static void EjecutarEtapa()
    {
        string etapa = Etapas[indice];
        bool alcanzada = false;
        bool falloEsperado = false;

        try
        {
            // Cada iteracion debe comenzar sin edificios.
            GameSaveFullLoadValidator.ExigirEscenaPreparada(
                partidaGuardada,
                referencias
            );

            GameSaveFullRestoreCoordinator.FalloDiagnostico =
                fase =>
                {
                    if (fase != etapa)
                        return;

                    alcanzada = true;

                    // El codigo llega aqui cuando
                    // el adaptador de esta etapa
                    // ya ha terminado su restauracion.
                    throw new InvalidOperationException(
                        Marca + etapa
                    );
                };

            try
            {
                GameSaveFullRestoreCoordinator.Restaurar(
                    almacenamiento,
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
                alcanzada && falloEsperado,
                "No se ha provocado correctamente " +
                "el fallo en " + etapa
            );

            // Destroy se completa al terminar el frame.
            fotogramaFallo = Time.frameCount;
        }
        finally
        {
            GameSaveFullRestoreCoordinator.FalloDiagnostico =
                null;
        }
    }

    // =====================================
    // ESPERAR LA LIMPIEZA DE UNITY
    // =====================================

    private static void Continuar()
    {
        if (!ejecutando)
        {
            EditorApplication.update -= Continuar;
            return;
        }

        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] PASO 81 interrumpido: " +
                "se ha salido de Play."
            );

            Finalizar();
            return;
        }

        if (Time.frameCount <= fotogramaFallo)
            return;

        try
        {
            string etapa = Etapas[indice];

            VerificarRollback();

            Debug.Log(
                "[GUARDADO] PASO 81 " + etapa +
                " OK: estado anterior recuperado y " +
                "edificios temporales eliminados."
            );

            indice++;

            if (indice >= Etapas.Length)
            {
                Debug.Log(
                    "[GUARDADO] PASO 81 SUPERADO: " +
                    "siete fallos controlados, " +
                    "rollback correcto en todas las etapas " +
                    "y escena sin construcciones residuales."
                );

                // Borrar solamente el archivo de prueba.
                almacenamiento.DeleteSlot(1);

                Finalizar();
                return;
            }

            // Iniciar la siguiente prueba desde
            // la misma escena, ya recuperada.
            EjecutarEtapa();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 81 FALLIDO " +
                Etapas[indice] + ": " + ex
            );

            Finalizar();
        }
    }

    // =====================================
    // VERIFICAR EL ESTADO ORIGINAL
    // =====================================

    private static void VerificarRollback()
    {
        Scene escena = SceneManager.GetActiveScene();

        int edificios = 0;

        foreach (EdificioConstruido edificio
            in UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            ))
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena)
            {
                edificios++;
            }
        }

        Comprobar(
            edificios == 0,
            "Quedan construcciones despues del rollback: " +
            edificios
        );

        // Capturar los sistemas de nuevo.
        GameSaveData actual =
            GameSaveFullWriter.CapturarEstado(
                referencias, 0
            );

        Comprobar(
            actual.placedBuildings.edificios.Count == 0,
            "Quedan edificios en el estado capturado."
        );

        Comprobar(
            actual.economy.coins ==
                estadoInicial.economy.coins,
            "Las monedas no se han recuperado."
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
            "La posicion del jugador no coincide."
        );

        Comprobar(
            Mathf.Abs(
                Mathf.DeltaAngle(
                    estadoInicial.player.yawDegrees,
                    actual.player.yawDegrees
                )
            ) < 0.2f,
            "La rotacion del jugador no coincide."
        );
    }

    // =====================================
    // LOCALIZAR COMPONENTES
    // =====================================

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
        {
            cartera =
                inventario.GetComponentInParent<Cartera>();
        }

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
        T encontrado = null;
        int cantidad = 0;

        Scene escena = SceneManager.GetActiveScene();

        foreach (T componente
            in UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            ))
        {
            if (componente == null ||
                componente.gameObject.scene != escena)
            {
                continue;
            }

            encontrado = componente;
            cantidad++;
        }

        Comprobar(
            cantidad == 1,
            "Se esperaba un componente " +
            typeof(T).Name +
            ". Encontrados: " + cantidad
        );

        return encontrado;
    }

    private static void Iguales(
        object actual,
        object esperado,
        string seccion)
    {
        Comprobar(
            JsonUtility.ToJson(actual) ==
                JsonUtility.ToJson(esperado),
            seccion + " no coincide con su estado anterior."
        );
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }

    private static void Finalizar()
    {
        EditorApplication.update -= Continuar;

        GameSaveFullRestoreCoordinator.FalloDiagnostico =
            null;

        ejecutando = false;
        referencias = null;
        estadoInicial = null;
        partidaGuardada = null;
        almacenamiento = null;
    }
}

#endif
