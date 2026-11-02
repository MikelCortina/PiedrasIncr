
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrRollbackTests
{
    private const string MarcaFallo =
        "PASO80_FALLO_CONTROLADO";

    private static string Carpeta => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "Paso80"
    );

    private sealed class Estado
    {
        public GameSaveSceneReferences referencias;
        public GameSaveData anterior;
        public int fotograma;
    }

    private static Estado estado;

    // ==========================================
    // FASE 1: PREPARAR GUARDADO
    // ==========================================

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 80 - 1 Preparar rollback (Play)"
    )]
    public static void Preparar()
    {
        try
        {
            Comprobar(Application.isPlaying, "Entra en Play.");

            GameSaveSceneReferences r = Referencias();

            GameSaveData original =
                GameSaveFullWriter.CapturarEstado(r, 0);

            var grafo =
                PlacedBuildingsConnectionGraphValidator
                    .Analizar(original);

            Comprobar(
                grafo.edificios >= 2 && grafo.conexiones >= 1,
                "Necesitas dos edificios conectados."
            );

            foreach (var edificio
                in original.placedBuildings.edificios)
            {
                Comprobar(
                    edificio.curvaturaRampa == 0f,
                    "Utiliza rampas rectas."
                );
            }

            SaveStorage storage = new SaveStorage(Carpeta);

            GameSaveData guardada =
                GameSaveFullWriter.GuardarEnRanura(
                    storage, 1, r, 0
                );

            // Cambios deliberados SOLO en el archivo.
            // La escena real permanece intacta.
            guardada.economy.coins =
                r.cartera.Monedas < int.MaxValue
                    ? r.cartera.Monedas + 1
                    : r.cartera.Monedas - 1;

            guardada.equipmentUnlocks.martilloDesbloqueado =
                !original.equipmentUnlocks
                    .martilloDesbloqueado;

            guardada.toolUpgrades.nivelRadioTorbellino =
                original.toolUpgrades.nivelRadioTorbellino == 1
                    ? 2
                    : 1;

            storage.Save(1, guardada);

            Debug.Log(
                "[GUARDADO] PASO 80 PREPARADO: " +
                "archivo temporal con monedas, desbloqueos " +
                "y mejoras diferentes. Reinicia Play " +
                "con la escena sin construcciones."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 80 PREPARACION FALLIDA: " +
                ex
            );
        }
    }

    // ==========================================
    // FASE 2: PROVOCAR FALLO
    // ==========================================

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 80 - 2 Probar rollback (Play)"
    )]
    public static void Probar()
    {
        if (estado != null)
        {
            Debug.LogWarning(
                "[GUARDADO] Ya existe una prueba en curso."
            );
            return;
        }

        try
        {
            Comprobar(Application.isPlaying, "Entra en Play.");

            GameSaveSceneReferences r = Referencias();

            GameSaveData anterior =
                GameSaveFullWriter.CapturarEstado(r, 0);

            Comprobar(
                anterior.placedBuildings.edificios.Count == 0,
                "La escena debe estar sin construcciones."
            );

            SaveStorage storage = new SaveStorage(Carpeta);

            bool respaldo;

            GameSaveData guardada =
                GameSaveFullLoadValidator.LeerYValidar(
                    storage, 1, r, out respaldo
                );

            GameSaveFullLoadValidator.ExigirEscenaPreparada(
                guardada, r
            );

            Comprobar(
                anterior.economy.coins !=
                    guardada.economy.coins,
                "Las monedas de prueba deben ser distintas."
            );

            Debug.Log(
                "[GUARDADO] PASO 80 VALIDACION OK: " +
                "escena limpia y partida compatible."
            );

            bool falloAlcanzado = false;

            // Fallar DESPUES de que se restauren:
            // edificios, jugador, mejoras, equipo,
            // procesadora, inventario y monedas.
            GameSaveFullRestoreCoordinator.FalloDiagnostico =
                fase =>
                {
                    if (fase != "TrasEconomia")
                        return;

                    falloAlcanzado = true;

                    Comprobar(
                        r.cartera.Monedas ==
                            guardada.economy.coins,
                        "No se aplicaron las monedas."
                    );

                    Comprobar(
                        r.torbellino.NivelRadio ==
                            guardada.toolUpgrades
                                .nivelRadioTorbellino,
                        "No se aplicaron las mejoras."
                    );

                    Comprobar(
                        r.equipamiento.MartilloDesbloqueado() ==
                            guardada.equipmentUnlocks
                                .martilloDesbloqueado,
                        "No se aplicaron los desbloqueos."
                    );

                    throw new InvalidOperationException(
                        MarcaFallo
                    );
                };

            bool falloEsperado = false;

            try
            {
                GameSaveFullRestoreCoordinator.Restaurar(
                    storage, 1, r
                );
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message != MarcaFallo)
                    throw;

                falloEsperado = true;
            }
            finally
            {
                GameSaveFullRestoreCoordinator.FalloDiagnostico =
                    null;
            }

            Comprobar(
                falloAlcanzado && falloEsperado,
                "No se ha ejecutado el fallo controlado."
            );

            Debug.Log(
                "[GUARDADO] PASO 80 FALLO CONTROLADO OK: " +
                "error provocado tras restaurar la economia."
            );

            // Destroy se completa al final del fotograma.
            // Esperamos antes de verificar la limpieza.
            estado = new Estado
            {
                referencias = r,
                anterior = anterior,
                fotograma = Time.frameCount
            };

            EditorApplication.update += Continuar;
        }
        catch (Exception ex)
        {
            GameSaveFullRestoreCoordinator.FalloDiagnostico =
                null;

            Debug.LogError(
                "[GUARDADO] PASO 80 FALLIDO: " + ex
            );
        }
    }

    // ==========================================
    // COMPROBAR ROLLBACK
    // ==========================================

    private static void Continuar()
    {
        if (estado == null)
        {
            EditorApplication.update -= Continuar;
            return;
        }

        if (!Application.isPlaying)
        {
            Finalizar();
            return;
        }

        if (Time.frameCount <= estado.fotograma)
            return;

        try
        {
            GameSaveSceneReferences r = estado.referencias;

            // Comprobar edificios fisicos.
            Scene escena = SceneManager.GetActiveScene();

            int edificiosRestantes = 0;

            foreach (EdificioConstruido edificio
                in UnityEngine.Object.FindObjectsByType<
                    EdificioConstruido>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
            {
                if (edificio != null &&
                    edificio.gameObject.scene == escena)
                {
                    edificiosRestantes++;
                }
            }

            Comprobar(
                edificiosRestantes == 0,
                "Han quedado edificios despues del rollback: " +
                edificiosRestantes
            );

            Debug.Log(
                "[GUARDADO] PASO 80 LIMPIEZA OK: " +
                "ningun edificio temporal restante."
            );

            GameSaveData despues =
                GameSaveFullWriter.CapturarEstado(r, 0);

            GameSaveData antes = estado.anterior;

            Comprobar(
                despues.placedBuildings.edificios.Count == 0,
                "La captura contiene edificios inesperados."
            );

            Comprobar(
                despues.economy.coins ==
                    antes.economy.coins,
                "Las monedas no han vuelto al valor anterior."
            );

            Iguales(
                despues.equipmentUnlocks,
                antes.equipmentUnlocks,
                "Desbloqueos"
            );

            Iguales(
                despues.toolUpgrades,
                antes.toolUpgrades,
                "Mejoras"
            );

            Iguales(
                despues.processor,
                antes.processor,
                "Procesadora"
            );

            Iguales(
                despues.buildingInventory,
                antes.buildingInventory,
                "Inventario"
            );

            Vector3 posicionAntes =
                PlayerSaveAdapter.GetSavedPosition(
                    antes.player
                );

            Vector3 posicionDespues =
                PlayerSaveAdapter.GetSavedPosition(
                    despues.player
                );

            Comprobar(
                Vector3.Distance(
                    posicionAntes,
                    posicionDespues
                ) < 0.05f,
                "La posicion del jugador no se ha recuperado."
            );

            Comprobar(
                Mathf.Abs(
                    Mathf.DeltaAngle(
                        antes.player.yawDegrees,
                        despues.player.yawDegrees
                    )
                ) < 0.2f,
                "La orientacion del jugador ha cambiado."
            );

            Debug.Log(
                "[GUARDADO] PASO 80 SISTEMAS OK: " +
                "jugador, monedas, mejoras, desbloqueos, " +
                "procesadora e inventario recuperados."
            );

            Debug.Log(
                "[GUARDADO] PASO 80 SUPERADO: " +
                "fallo controlado, rollback completo " +
                "de los datos comprobados y limpieza " +
                "de construcciones verificada."
            );

            // Borrar solamente la ranura de diagnostico.
            new SaveStorage(Carpeta).DeleteSlot(1);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 80 FALLIDO: " + ex
            );
        }
        finally
        {
            Finalizar();
        }
    }

    private static void Finalizar()
    {
        EditorApplication.update -= Continuar;

        GameSaveFullRestoreCoordinator.FalloDiagnostico = null;

        estado = null;
    }

    // ==========================================
    // REFERENCIAS
    // ==========================================

    private static GameSaveSceneReferences Referencias()
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
            if (componente != null &&
                componente.gameObject.scene == escena)
            {
                resultado = componente;
                cantidad++;
            }
        }

        Comprobar(
            cantidad == 1,
            "Se esperaba un componente " +
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
            nombre + " no coincide con el estado anterior."
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
