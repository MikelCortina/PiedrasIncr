
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PiedrasIncrProcessorSceneTestWindow : EditorWindow
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
        "Tools/PiedrasIncr/Guardado/Probar procesadora en escena (Play)"
    )]
    public static void Abrir()
    {
        GetWindow<PiedrasIncrProcessorSceneTestWindow>(
            "Guardado procesadora"
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
            "PROCESADORA REAL - DIAGNOSTICO",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Captura y lee el progreso de la procesadora. " +
            "No modifica monedas, niveles ni construcciones. " +
            "Utiliza archivos de diagnostico independientes.",
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
            "Maquina", maquina,
            typeof(MaquinaErosion), true
        );

        construccion =
            (DesbloqueoProcesadoraMonedas)
            EditorGUILayout.ObjectField(
                "Construccion", construccion,
                typeof(DesbloqueoProcesadoraMonedas), true
            );

        cartera = (Cartera)EditorGUILayout.ObjectField(
            "Cartera", cartera,
            typeof(Cartera), true
        );

        if (GUILayout.Button("Buscar componentes de la procesadora"))
        {
            BuscarComponentes();
        }

        if (maquina == null ||
            construccion == null ||
            cartera == null)
        {
            EditorGUILayout.HelpBox(
                "Selecciona los tres componentes o pulsa Buscar.",
                MessageType.Warning
            );

            MostrarResultado();
            return;
        }

        if (construccion.maquinaErosion != maquina)
        {
            EditorGUILayout.HelpBox(
                "La construccion seleccionada no corresponde " +
                "a la maquina seleccionada.",
                MessageType.Error
            );

            MostrarResultado();
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Procesadora desbloqueada",
            maquina.ProcesadoraDesbloqueada ? "SI" : "NO"
        );

        EditorGUILayout.LabelField(
            "Nivel de procesado",
            maquina.NivelProcesado.ToString()
        );

        EditorGUILayout.LabelField(
            "Construccion",
            construccion.MonedasRecibidas + " / " +
            construccion.MonedasNecesarias
        );

        EditorGUILayout.LabelField(
            "Mejora actual",
            construccion.MonedasMejoraRecibidas + " / " +
            construccion.PrecioMejoraActual
        );

        EditorGUILayout.LabelField(
            "Monedas jugador",
            cartera.Monedas.ToString()
        );

        EditorGUILayout.Space();

        if (GUILayout.Button(
            "1. GUARDAR procesadora real (diagnostico)"
        ))
        {
            Ejecutar(GuardarDiagnostico);
        }

        if (GUILayout.Button(
            "2. LEER procesadora guardada (sin aplicar)"
        ))
        {
            Ejecutar(LeerDiagnostico);
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

        int parejasValidas = 0;

        foreach (var candidata in construcciones)
        {
            if (candidata.maquinaErosion == null)
                continue;

            parejasValidas++;

            if (parejasValidas == 1)
            {
                construccion = candidata;
                maquina = candidata.maquinaErosion;
            }
        }

        if (parejasValidas != 1)
        {
            construccion = null;
            maquina = null;
        }

        cartera = carteras.Length == 1
            ? carteras[0]
            : null;

        resultado =
            "Parejas procesadora/construccion=" +
            parejasValidas +
            ", carteras=" + carteras.Length + ".";

        if (parejasValidas != 1 || carteras.Length != 1)
        {
            resultado +=
                " Selecciona manualmente los componentes correctos.";
        }
    }

    private void GuardarDiagnostico()
    {
        int monedasAntes = cartera.Monedas;

        GameSaveData partida = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name
        };

        // Capturar economia y procesadora.
        EconomySaveAdapter.Capture(cartera, partida);

        ProcessorSaveAdapter.Capture(
            maquina, construccion, partida
        );

        SaveStorage storage = new SaveStorage(TestDirectory);

        // Conservar la identidad de esta ranura,
        // si ya contiene un diagnostico valido.
        GameSaveData anterior;
        bool desdeRespaldo;

        if (storage.TryLoad(
                ranuraPrueba, out anterior, out desdeRespaldo) &&
            anterior.sceneName == partida.sceneName)
        {
            partida.saveId = anterior.saveId;
            partida.createdUtc = anterior.createdUtc;
        }

        storage.Save(ranuraPrueba, partida);

        if (cartera.Monedas != monedasAntes)
        {
            throw new InvalidOperationException(
                "La captura ha modificado las monedas."
            );
        }

        resultado =
            "PROCESADORA GUARDADA TEST " +
            ranuraPrueba + ": " + Formato(partida);

        Debug.Log("[GUARDADO] " + resultado);
    }

    private void LeerDiagnostico()
    {
        int monedasAntes = cartera.Monedas;

        SaveStorage storage = new SaveStorage(TestDirectory);

        GameSaveData partida;
        bool desdeRespaldo;

        if (!storage.TryLoad(
            ranuraPrueba, out partida, out desdeRespaldo))
        {
            throw new InvalidOperationException(
                "No existe un guardado de diagnostico valido. " +
                "Pulsa primero GUARDAR."
            );
        }

        if (partida.sceneName !=
            SceneManager.GetActiveScene().name)
        {
            throw new InvalidOperationException(
                "El guardado pertenece a otra escena."
            );
        }

        // Validar datos sin aplicarlos.
        ProcessorSaveAdapter.Read(partida);

        if (cartera.Monedas != monedasAntes)
        {
            throw new InvalidOperationException(
                "La lectura ha modificado las monedas."
            );
        }

        resultado =
            "PROCESADORA LECTURA TEST " +
            ranuraPrueba + ": " + Formato(partida);

        if (desdeRespaldo)
            resultado += " (respaldo)";

        Debug.Log("[GUARDADO] " + resultado);
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
}

#endif
