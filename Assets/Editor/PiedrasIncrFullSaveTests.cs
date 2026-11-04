
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrFullSaveTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar guardado completo (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[GUARDADO] Entra en Play.");
            return;
        }

        string carpeta = Path.Combine(
            Application.persistentDataPath,
            "PiedrasIncr",
            "DeveloperTests",
            "FullSave_" + Guid.NewGuid().ToString("N")
        );

        try
        {
            // 1. Localizar componentes reales.
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

            MovimientoPersonaje movimiento =
                BuscarUnico<MovimientoPersonaje>();

            DesbloqueoProcesadoraMonedas construccion =
                BuscarUnico<DesbloqueoProcesadoraMonedas>();

            Comprobar(
                construccion.maquinaErosion != null,
                "La procesadora no esta asignada."
            );

            GameSaveSceneReferences referencias =
                new GameSaveSceneReferences
                {
                    jugador = movimiento.gameObject,
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

            const double tiempoPrueba = 1234.5;

            // 2. Capturar sin escribir archivos.
            GameSaveData antes =
                GameSaveFullWriter.CapturarEstado(
                    referencias,
                    tiempoPrueba
                );

            string estadoAntes =
                JsonUtility.ToJson(antes);

            // 3. Guardar todo en ranura temporal.
            SaveStorage storage = new SaveStorage(carpeta);

            GameSaveData guardada =
                GameSaveFullWriter.GuardarEnRanura(
                    storage,
                    2,
                    referencias,
                    tiempoPrueba
                );

            Debug.Log(
                "[GUARDADO] PASO 77 ESCRITURA OK: " +
                "partida completa guardada."
            );

            // 4. Leer el archivo.
            GameSaveData leida;
            bool respaldo;

            Comprobar(
                storage.TryLoad(
                    2,
                    out leida,
                    out respaldo
                ),
                "No se puede leer la partida."
            );

            Comprobar(
                !respaldo,
                "Se ha utilizado un respaldo inesperado."
            );

            Comprobar(
                JsonUtility.ToJson(guardada) ==
                JsonUtility.ToJson(leida),
                "El contenido del archivo ha cambiado."
            );

            Debug.Log(
                "[GUARDADO] PASO 77 LECTURA OK: " +
                "datos completos recuperados."
            );

            // 5. Validar todas las secciones.
            Comprobar(
                leida.economy != null &&
                leida.economy.coins == cartera.Monedas,
                "Economia incorrecta."
            );

            PlayerSaveAdapter.Validate(leida.player);

            Comprobar(
                leida.hasEquipmentUnlocksData &&
                leida.equipmentUnlocks != null,
                "Faltan desbloqueos."
            );

            ToolUpgradesSaveAdapter.Read(leida);
            ProcessorSaveAdapter.Read(leida);
            BuildingInventorySaveAdapter.Read(leida);

            Comprobar(
                PlacedBuildingsSlotBridge
                    .TieneDatosDeEdificios(leida),
                "Faltan edificios."
            );

            PlacedBuildingsConnectionGraphReport grafo =
                PlacedBuildingsConnectionGraphValidator.Analizar(
                    leida
                );

            Comprobar(
                grafo.edificios >= 2 &&
                grafo.conexiones >= 1,
                "Necesitas dos edificios y una conexion."
            );

            Comprobar(
                leida.slotIndex == 2 &&
                leida.totalPlaySeconds == tiempoPrueba &&
                leida.sceneName ==
                    SceneManager.GetActiveScene().name,
                "Metadatos incorrectos."
            );

            Debug.Log(
                "[GUARDADO] PASO 77 DATOS OK: " +
                "jugador, economia, desbloqueos, mejoras, " +
                "procesadora, inventario y edificios."
            );

            // 6. Verificar que guardar no altero el juego.
            GameSaveData despues =
                GameSaveFullWriter.CapturarEstado(
                    referencias,
                    tiempoPrueba
                );

            Comprobar(
                JsonUtility.ToJson(despues) == estadoAntes,
                "El guardado ha modificado el estado real."
            );

            Debug.Log(
                "[GUARDADO] PASO 77 SUPERADO: " +
                "guardado completo verificado, " +
                "todas las secciones conservadas, " +
                "escena original sin modificaciones."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 77 FALLIDO: " + ex
            );
        }
        finally
        {
            try
            {
                if (Directory.Exists(carpeta))
                    Directory.Delete(carpeta, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[GUARDADO] Error limpiando prueba: " +
                    ex.Message
                );
            }
        }
    }

    private static T BuscarUnico<T>() where T : Component
    {
        T encontrado = null;
        int cantidad = 0;

        Scene escena = SceneManager.GetActiveScene();

        T[] componentes =
            UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (T componente in componentes)
        {
            if (componente == null ||
                componente.gameObject.scene != escena)
            {
                continue;
            }

            encontrado = componente;
            cantidad++;
        }

        if (cantidad != 1)
        {
            throw new InvalidOperationException(
                "Se esperaba un componente " +
                typeof(T).Name +
                " en la escena. Encontrados: " +
                cantidad
            );
        }

        return encontrado;
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
