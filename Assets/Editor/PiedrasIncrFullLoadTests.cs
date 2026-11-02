
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrFullLoadTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar validacion carga completa (Play)"
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
            "FullLoad_" + Guid.NewGuid().ToString("N")
        );

        try
        {
            // =====================================
            // 1. REFERENCIAS REALES
            // =====================================

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

            MovimientoPersonaje movimiento =
                BuscarUnico<MovimientoPersonaje>();

            DesbloqueoProcesadoraMonedas construccion =
                BuscarUnico<DesbloqueoProcesadoraMonedas>();

            Comprobar(
                construccion.maquinaErosion != null,
                "Falta la referencia a MaquinaErosion."
            );

            GameSaveSceneReferences r =
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
                    procesadora = construccion.maquinaErosion,
                    construccionProcesadora = construccion,
                    inventario = inventario
                };

            // =====================================
            // 2. CAPTURAR Y GUARDAR
            // =====================================

            const double tiempo = 2345.5;

            GameSaveData original =
                GameSaveFullWriter.CapturarEstado(
                    r,
                    tiempo
                );

            string estadoAntes =
                JsonUtility.ToJson(original);

            Comprobar(
                original.placedBuildings.edificios.Count > 0,
                "Coloca al menos un edificio."
            );

            SaveStorage storage = new SaveStorage(carpeta);

            GameSaveFullWriter.GuardarEnRanura(
                storage,
                2,
                r,
                tiempo
            );

            Debug.Log(
                "[GUARDADO] PASO 78 ARCHIVO OK: " +
                "partida completa guardada."
            );

            // =====================================
            // 3. LECTURA Y VALIDACION COMPLETA
            // =====================================

            bool desdeRespaldo;

            GameSaveData leida =
                GameSaveFullLoadValidator.LeerYValidar(
                    storage,
                    2,
                    r,
                    out desdeRespaldo
                );

            Comprobar(
                !desdeRespaldo &&
                leida.slotIndex == 2 &&
                leida.totalPlaySeconds == tiempo,
                "Los datos de la ranura son incorrectos."
            );

            Debug.Log(
                "[GUARDADO] PASO 78 DATOS OK: " +
                "todas las secciones validadas."
            );

            // =====================================
            // 4. DATOS INVALIDOS
            // =====================================

            GameSaveData sinMejoras = Copiar(leida);

            sinMejoras.hasToolUpgradesData = false;

            EsperarRechazo(
                () => GameSaveFullLoadValidator.ValidarDatos(
                    sinMejoras, r
                ),
                "Guardado sin mejoras"
            );

            GameSaveData jugadorInvalido = Copiar(leida);

            jugadorInvalido.player.hasSafePosition = false;

            EsperarRechazo(
                () => GameSaveFullLoadValidator.ValidarDatos(
                    jugadorInvalido, r
                ),
                "Posicion de jugador invalida"
            );

            GameSaveData procesadoraInvalida = Copiar(leida);

            procesadoraInvalida.processor.nivelProcesado = 99;

            EsperarRechazo(
                () => GameSaveFullLoadValidator.ValidarDatos(
                    procesadoraInvalida, r
                ),
                "Nivel de procesadora invalido"
            );

            GameSaveData escenaIncorrecta = Copiar(leida);

            escenaIncorrecta.sceneName = "__OTRA_ESCENA__";

            EsperarRechazo(
                () => GameSaveFullLoadValidator.ValidarDatos(
                    escenaIncorrecta, r
                ),
                "Partida de otra escena"
            );

            Debug.Log(
                "[GUARDADO] PASO 78 CORRUPCION OK: " +
                "datos incompletos o incompatibles rechazados."
            );

            // =====================================
            // 5. PROTEGER ESCENA OCUPADA
            // =====================================

            EsperarRechazo(
                () =>
                    GameSaveFullLoadValidator.ExigirEscenaPreparada(
                        leida, r
                    ),
                "Restauracion sobre escena ocupada"
            );

            Debug.Log(
                "[GUARDADO] PASO 78 ESCENA OCUPADA OK: " +
                "carga bloqueada antes de modificar objetos."
            );

            // =====================================
            // 6. VERIFICAR QUE NADA CAMBIO
            // =====================================

            GameSaveData despues =
                GameSaveFullWriter.CapturarEstado(
                    r,
                    tiempo
                );

            Comprobar(
                JsonUtility.ToJson(despues) == estadoAntes,
                "La validacion ha modificado el juego."
            );

            Debug.Log(
                "[GUARDADO] PASO 78 SUPERADO: " +
                "lectura completa validada, " +
                "datos invalidos rechazados, " +
                "escena ocupada protegida, " +
                "estado original intacto."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 78 FALLIDO: " + ex
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

    private static GameSaveData Copiar(GameSaveData datos)
    {
        return JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(datos)
        );
    }

    private static void EsperarRechazo(
        Action accion,
        string escenario)
    {
        bool rechazado = false;

        try
        {
            accion();
        }
        catch (InvalidOperationException)
        {
            rechazado = true;
        }

        Comprobar(
            rechazado,
            "No se ha rechazado: " + escenario
        );
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
                continue;

            encontrado = componente;
            cantidad++;
        }

        if (cantidad != 1)
        {
            throw new InvalidOperationException(
                "Se esperaba un componente " +
                typeof(T).Name +
                ". Encontrados: " + cantidad
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
