
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PiedrasIncrProcessorRestoreTestWindow : EditorWindow
{
    private MaquinaErosion maquina;
    private DesbloqueoProcesadoraMonedas construccion;
    private Cartera cartera;

    private int ranuraPrueba = 1;
    private string resultado = "Sin pruebas realizadas.";

    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "ProcessorScene"
    );

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar RESTAURAR procesadora (Play)"
    )]
    public static void Abrir()
    {
        GetWindow<PiedrasIncrProcessorRestoreTestWindow>(
            "Restaurar procesadora"
        ).Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += AlCambiarPlay;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= AlCambiarPlay;
    }

    private void AlCambiarPlay(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredPlayMode ||
            estado == PlayModeStateChange.ExitingPlayMode)
        {
            maquina = null;
            construccion = null;
            cartera = null;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "RESTAURACION PROCESADORA",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Prueba controlada. Cambia temporalmente " +
            "el estado de la procesadora y luego lo recupera. " +
            "No realiza compras ni modifica partidas reales.",
            MessageType.Info
        );

        ranuraPrueba = EditorGUILayout.IntSlider(
            "Ranura de prueba", ranuraPrueba, 1, 3
        );

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Entra en Play para continuar.",
                MessageType.Warning
            );

            MostrarResultado();
            return;
        }

        maquina = (MaquinaErosion)EditorGUILayout.ObjectField(
            "Maquina",
            maquina,
            typeof(MaquinaErosion),
            true
        );

        construccion = (DesbloqueoProcesadoraMonedas)
            EditorGUILayout.ObjectField(
                "Construccion",
                construccion,
                typeof(DesbloqueoProcesadoraMonedas),
                true
            );

        cartera = (Cartera)EditorGUILayout.ObjectField(
            "Cartera",
            cartera,
            typeof(Cartera),
            true
        );

        if (GUILayout.Button("Buscar componentes"))
        {
            BuscarComponentes();
        }

        if (maquina == null ||
            construccion == null ||
            cartera == null)
        {
            MostrarResultado();
            return;
        }

        if (construccion.maquinaErosion != maquina)
        {
            EditorGUILayout.HelpBox(
                "La construccion no pertenece a esta maquina.",
                MessageType.Error
            );

            MostrarResultado();
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Desbloqueada",
            maquina.ProcesadoraDesbloqueada ? "SI" : "NO"
        );

        EditorGUILayout.LabelField(
            "Nivel",
            maquina.NivelProcesado.ToString()
        );

        EditorGUILayout.LabelField(
            "Construccion",
            construccion.MonedasRecibidas + "/" +
            construccion.MonedasNecesarias
        );

        EditorGUILayout.LabelField(
            "Mejora",
            construccion.MonedasMejoraRecibidas + "/" +
            construccion.PrecioMejoraActual
        );

        EditorGUILayout.LabelField(
            "Monedas",
            cartera.Monedas.ToString()
        );

        EditorGUILayout.Space();

        if (GUILayout.Button(
            "1. GUARDAR estado actual (diagnostico)"
        ))
        {
            Ejecutar(GuardarDiagnostico);
        }

        if (GUILayout.Button(
            "2. COMPROBAR RESTAURACION CONTROLADA"
        ))
        {
            Ejecutar(ProbarRestauracion);
        }

        MostrarResultado();
    }

    private void BuscarComponentes()
    {
        var construcciones =
            UnityEngine.Object.FindObjectsByType<
                DesbloqueoProcesadoraMonedas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        var carteras =
            UnityEngine.Object.FindObjectsByType<Cartera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        construccion = null;
        maquina = null;

        int encontradas = 0;

        foreach (var candidata in construcciones)
        {
            if (candidata.maquinaErosion == null)
                continue;

            encontradas++;

            if (encontradas == 1)
            {
                construccion = candidata;
                maquina = candidata.maquinaErosion;
            }
        }

        if (encontradas != 1)
        {
            construccion = null;
            maquina = null;
        }

        cartera = carteras.Length == 1
            ? carteras[0]
            : null;

        resultado =
            "Procesadoras=" + encontradas +
            ", carteras=" + carteras.Length + ".";
    }

    private void GuardarDiagnostico()
    {
        int monedasAntes = cartera.Monedas;

        GameSaveData partida = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name
        };

        EconomySaveAdapter.Capture(cartera, partida);

        ProcessorSaveAdapter.Capture(
            maquina,
            construccion,
            partida
        );

        SaveStorage storage = new SaveStorage(TestDirectory);

        storage.Save(ranuraPrueba, partida);

        Comprobar(
            cartera.Monedas == monedasAntes,
            "Guardar ha modificado las monedas."
        );

        resultado =
            "PROCESADORA PREPARADA TEST " +
            ranuraPrueba + ": " + Formato(partida);

        Debug.Log("[GUARDADO] " + resultado);
    }

    private void ProbarRestauracion()
    {
        SaveStorage storage = new SaveStorage(TestDirectory);

        GameSaveData guardado;
        bool desdeRespaldo;

        Comprobar(
            storage.TryLoad(
                ranuraPrueba,
                out guardado,
                out desdeRespaldo
            ),
            "No existe un guardado de diagnostico. " +
            "Pulsa primero GUARDAR."
        );

        Comprobar(
            guardado.sceneName ==
                SceneManager.GetActiveScene().name,
            "El archivo pertenece a otra escena."
        );

        ProcessorSaveData esperado =
            ProcessorSaveAdapter.Read(guardado);

        // Estado real antes de iniciar el test.
        GameSaveData estadoOriginal = new GameSaveData();

        EconomySaveAdapter.Capture(
            cartera,
            estadoOriginal
        );

        ProcessorSaveAdapter.Capture(
            maquina,
            construccion,
            estadoOriginal
        );

        int monedasOriginales = cartera.Monedas;

        // Preparar un estado alternativo:
        // procesadora construida, nivel 2,
        // con un pago parcial hacia nivel 3.
        int precioNivel3 = construccion.precioNivel3;

        ProcessorSaveData estadoAlterado =
            new ProcessorSaveData
            {
                desbloqueada = true,
                nivelProcesado = 2,

                monedasConstruccionNecesarias =
                    construccion.MonedasNecesarias,

                monedasConstruccionAportadas =
                    construccion.MonedasNecesarias,

                precioSiguienteMejora = precioNivel3,

                monedasMejoraAportadas =
                    Mathf.Min(4, precioNivel3 - 1)
            };

        ProcessorSaveAdapter.Validate(estadoAlterado);

        // Todo ocurre durante esta misma pulsacion.
        // El estado original se recupera al finalizar.
        try
        {
            maquina.RestaurarEstadoGuardado(
                estadoAlterado.desbloqueada,
                estadoAlterado.nivelProcesado
            );

            construccion.RestaurarPagosYAspecto(
                estadoAlterado
            );

            // Confirmar que realmente hemos cambiado
            // el estado temporalmente.
            GameSaveData comprobacionAlterada =
                new GameSaveData();

            ProcessorSaveAdapter.Capture(
                maquina,
                construccion,
                comprobacionAlterada
            );

            Comprobar(
                Coinciden(
                    comprobacionAlterada.processor,
                    estadoAlterado
                ),
                "No se ha podido preparar el estado alternativo."
            );

            // RESTAURAR DESDE EL ARCHIVO.
            ProcessorSaveAdapter.Restore(
                maquina,
                construccion,
                guardado
            );

            GameSaveData comprobacion =
                new GameSaveData();

            ProcessorSaveAdapter.Capture(
                maquina,
                construccion,
                comprobacion
            );

            Comprobar(
                Coinciden(comprobacion.processor, esperado),
                "La restauracion no coincide con el guardado."
            );

            Comprobar(
                cartera.Monedas == monedasOriginales,
                "La restauracion ha modificado las monedas."
            );
        }
        finally
        {
            // Recuperar SIEMPRE el estado que tenia
            // el jugador antes de pulsar el boton.
            ProcessorSaveData original =
                estadoOriginal.processor;

            maquina.RestaurarEstadoGuardado(
                original.desbloqueada,
                original.nivelProcesado
            );

            construccion.RestaurarPagosYAspecto(original);
        }

        GameSaveData estadoFinal = new GameSaveData();

        ProcessorSaveAdapter.Capture(
            maquina,
            construccion,
            estadoFinal
        );

        Comprobar(
            Coinciden(
                estadoFinal.processor,
                estadoOriginal.processor
            ),
            "No se ha recuperado el estado original."
        );

        Comprobar(
            cartera.Monedas == monedasOriginales,
            "Las monedas han cambiado durante la prueba."
        );

        resultado =
            "RESTAURACION PROCESADORA SUPERADA TEST " +
            ranuraPrueba + ": " +
            Formato(guardado) +
            " Estado original recuperado.";

        Debug.Log("[GUARDADO] " + resultado);
    }

    private static bool Coinciden(
        ProcessorSaveData a,
        ProcessorSaveData b)
    {
        return
            a.desbloqueada == b.desbloqueada &&
            a.nivelProcesado == b.nivelProcesado &&
            a.monedasConstruccionAportadas ==
                b.monedasConstruccionAportadas &&
            a.monedasConstruccionNecesarias ==
                b.monedasConstruccionNecesarias &&
            a.monedasMejoraAportadas ==
                b.monedasMejoraAportadas &&
            a.precioSiguienteMejora ==
                b.precioSiguienteMejora;
    }

    private static string Formato(GameSaveData partida)
    {
        ProcessorSaveData datos =
            ProcessorSaveAdapter.Read(partida);

        return
            "desbloqueada=" +
            (datos.desbloqueada ? "SI" : "NO") +
            ", nivel=" + datos.nivelProcesado +
            ", construccion=" +
            datos.monedasConstruccionAportadas + "/" +
            datos.monedasConstruccionNecesarias +
            ", mejora=" +
            datos.monedasMejoraAportadas + "/" +
            datos.precioSiguienteMejora +
            ", monedas=" + partida.economy.coins + ".";
    }

    private void Ejecutar(Action accion)
    {
        try
        {
            accion();
        }
        catch (Exception ex)
        {
            resultado = "ERROR: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private void MostrarResultado()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Ultimo resultado",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            resultado,
            EditorStyles.wordWrappedLabel
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
