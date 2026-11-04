#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ventana de diagnóstico: conecta el guardado de economía a una Cartera REAL
/// de la escena mientras se ejecuta el juego en el editor.
///
/// Nunca escribe en las tres ranuras de producción.
/// No deja componentes en la escena ni modifica prefabs.
/// </summary>
public sealed class PiedrasIncrEconomySceneTestWindow : EditorWindow
{
    private const int TestSlot = 1;
    private Cartera cartera;
    private string ultimoMensaje = "Sin pruebas realizadas.";

    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "EconomyScene");

    [MenuItem("Tools/PiedrasIncr/Guardado/Probar economía en escena (Play)")]
    public static void Abrir()
    {
        GetWindow<PiedrasIncrEconomySceneTestWindow>("Prueba economía").Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += CambioModo;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= CambioModo;
    }

    private void CambioModo(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredPlayMode ||
            estado == PlayModeStateChange.ExitingPlayMode)
        {
            cartera = null;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ECONOMÍA REAL · PRUEBA CONTROLADA", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "El archivo se guarda en DeveloperTests/EconomyScene, NO en las tres partidas reales. " +
            "Esta herramienta solo funciona durante Play y no realiza guardado automático.",
            MessageType.Info);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Entra en Play en la escena Piedra para continuar.", MessageType.Warning);
            EditorGUILayout.LabelField("Último resultado", ultimoMensaje, EditorStyles.wordWrappedLabel);
            return;
        }

        EditorGUILayout.Space();
        cartera = (Cartera)EditorGUILayout.ObjectField("Cartera del jugador", cartera, typeof(Cartera), true);

        if (GUILayout.Button("Buscar la Cartera del jugador"))
            BuscarCartera();

        if (cartera == null)
        {
            EditorGUILayout.HelpBox("Selecciona la Cartera o usa el botón de búsqueda.", MessageType.Warning);
            EditorGUILayout.LabelField("Último resultado", ultimoMensaje, EditorStyles.wordWrappedLabel);
            return;
        }

        EditorGUILayout.LabelField("Saldo actual (en memoria)", cartera.Monedas.ToString());
        EditorGUILayout.Space();

        if (GUILayout.Button("1. Añadir 25 monedas de prueba (solo Play)"))
        {
            Ejecutar(() =>
            {
                cartera.AnadirMonedas(25);
                ultimoMensaje = "Saldo en memoria: " + cartera.Monedas + ".";
            });
        }

        if (GUILayout.Button("2. Guardar saldo en archivo de diagnóstico"))
        {
            Ejecutar(() =>
            {
                var data = new GameSaveData
                {
                    sceneName = SceneManager.GetActiveScene().name
                };
                EconomySaveAdapter.Capture(cartera, data);
                new SaveStorage(TestDirectory).Save(TestSlot, data);
                ultimoMensaje = "GUARDADO DE PRUEBA: " + data.economy.coins + " monedas.";
                Debug.Log("[GUARDADO] ECONOMÍA ESCENA: " + ultimoMensaje);
            });
        }

        if (GUILayout.Button("3. Cargar saldo desde archivo de diagnóstico"))
        {
            Ejecutar(() =>
            {
                GameSaveData data;
                bool desdeRespaldo;
                if (!new SaveStorage(TestDirectory).TryLoad(TestSlot, out data, out desdeRespaldo))
                {
                    ultimoMensaje = "No hay guardado de diagnóstico válido. Usa primero Guardar.";
                    Debug.LogWarning("[GUARDADO] " + ultimoMensaje);
                    return;
                }

                if (data.sceneName != SceneManager.GetActiveScene().name)
                    throw new InvalidOperationException("El archivo de prueba pertenece a otra escena: " + data.sceneName);

                EconomySaveAdapter.Restore(cartera, data);
                ultimoMensaje = "CARGA DE PRUEBA: " + cartera.Monedas +
                                " monedas" + (desdeRespaldo ? " (respaldo)" : "") + ".";
                Debug.Log("[GUARDADO] ECONOMÍA ESCENA: " + ultimoMensaje);
            });
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Borrar únicamente el archivo de diagnóstico"))
        {
            Ejecutar(() =>
            {
                new SaveStorage(TestDirectory).DeleteSlot(TestSlot);
                ultimoMensaje = "Archivo de diagnóstico eliminado. Las partidas reales no se han tocado.";
            });
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Último resultado", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(ultimoMensaje, EditorStyles.wordWrappedLabel);
    }

    private void BuscarCartera()
    {
        Cartera[] carteras = UnityEngine.Object.FindObjectsByType<Cartera>(FindObjectsSortMode.None);
        if (carteras.Length == 1)
        {
            cartera = carteras[0];
            ultimoMensaje = "Cartera encontrada: " + cartera.gameObject.name;
        }
        else if (carteras.Length == 0)
        {
            cartera = null;
            ultimoMensaje = "No se encontró ninguna Cartera activa en la escena.";
        }
        else
        {
            cartera = null;
            ultimoMensaje = "Hay " + carteras.Length + " carteras. Selecciona manualmente la del jugador.";
        }
    }

    private void Ejecutar(Action accion)
    {
        try
        {
            accion();
        }
        catch (Exception ex)
        {
            ultimoMensaje = "ERROR: " + ex.Message;
            Debug.LogException(ex);
        }
    }
}
#endif
