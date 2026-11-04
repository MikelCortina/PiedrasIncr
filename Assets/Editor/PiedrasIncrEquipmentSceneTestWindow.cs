#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Diagnostico sobre los desbloqueos REALES del personaje en Play.
/// Esta ventana captura y lee datos sin cambiar las herramientas ni las monedas.
/// Los ficheros se mantienen separados de las 3 ranuras definitivas.
/// </summary>
public sealed class PiedrasIncrEquipmentSceneTestWindow : EditorWindow
{
    private GestorEquipamiento equipo;
    private Cartera cartera;
    private int ranuraPrueba = 1;
    private string resultado = "Aun no se han realizado pruebas.";

    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr", "DeveloperTests", "EquipmentScene");

    [MenuItem("Tools/PiedrasIncr/Guardado/Probar desbloqueos en escena (Play)")]
    public static void Abrir()
    {
        GetWindow<PiedrasIncrEquipmentSceneTestWindow>("Prueba desbloqueos").Show();
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
        if (estado != PlayModeStateChange.EnteredPlayMode &&
            estado != PlayModeStateChange.ExitingPlayMode)
            return;

        equipo = null;
        cartera = null;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("DESBLOQUEOS REALES - SOLO DIAGNOSTICO", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Solo captura y lee los desbloqueos reales. " +
            "Los archivos van a DeveloperTests/EquipmentScene, no a las tres partidas definitivas. " +
            "No hace compras, no aplica cargas y no modifica las monedas.",
            MessageType.Info);

        ranuraPrueba = EditorGUILayout.IntSlider("Ranura de prueba", ranuraPrueba, 1, 3);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Abre la escena Piedra y entra en Play.", MessageType.Warning);
            MostrarResultado();
            return;
        }

        equipo = (GestorEquipamiento)EditorGUILayout.ObjectField(
            "GestorEquipamiento", equipo, typeof(GestorEquipamiento), true);
        cartera = (Cartera)EditorGUILayout.ObjectField(
            "Cartera", cartera, typeof(Cartera), true);

        if (GUILayout.Button("Buscar componentes del jugador"))
            BuscarComponentes();

        if (equipo == null || cartera == null)
        {
            EditorGUILayout.HelpBox(
                "Selecciona GestorEquipamiento y Cartera del jugador o usa la busqueda.",
                MessageType.Warning);
            MostrarResultado();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Objeto del equipo", equipo.gameObject.name);
        EditorGUILayout.LabelField("Objeto de la cartera", cartera.gameObject.name);
        EditorGUILayout.LabelField("Monedas actuales", cartera.Monedas.ToString());
        EditorGUILayout.LabelField("Arma de la Luna", SiNo(equipo.ArmaLanzadoraDesbloqueada()));
        EditorGUILayout.LabelField("Torbellino", SiNo(equipo.TorbellinoDesbloqueado()));
        EditorGUILayout.LabelField("Martillo", SiNo(equipo.MartilloDesbloqueado()));

        EditorGUILayout.Space();
        if (GUILayout.Button("1. GUARDAR desbloqueos reales (diagnostico)"))
        {
            Ejecutar(() =>
            {
                var partida = new GameSaveData
                {
                    sceneName = SceneManager.GetActiveScene().name
                };
                EconomySaveAdapter.Capture(cartera, partida);
                EquipmentUnlocksSaveAdapter.Capture(equipo, partida);

                var storage = new SaveStorage(TestDirectory);
                GameSaveData anterior;
                bool respaldo;
                if (storage.TryLoad(ranuraPrueba, out anterior, out respaldo) &&
                    anterior.sceneName == partida.sceneName)
                {
                    partida.saveId = anterior.saveId;
                    partida.createdUtc = anterior.createdUtc;
                }

                storage.Save(ranuraPrueba, partida);
                resultado = "EQUIPO GUARDADO TEST " + ranuraPrueba + ": " +
                            Formato(partida);
                Debug.Log("[GUARDADO] " + resultado);
            });
        }

        if (GUILayout.Button("2. LEER desbloqueos guardados (sin aplicar)"))
        {
            Ejecutar(() =>
            {
                var storage = new SaveStorage(TestDirectory);
                GameSaveData partida;
                bool respaldo;
                if (!storage.TryLoad(ranuraPrueba, out partida, out respaldo))
                    throw new InvalidOperationException(
                        "No existe un guardado de diagnostico valido para esta ranura.");

                if (partida.sceneName != SceneManager.GetActiveScene().name)
                    throw new InvalidOperationException(
                        "El guardado pertenece a otra escena: " + partida.sceneName);

                if (!partida.hasEquipmentUnlocksData || partida.equipmentUnlocks == null)
                    throw new InvalidOperationException(
                        "El archivo no contiene desbloqueos de equipamiento.");

                resultado = "EQUIPO LECTURA TEST " + ranuraPrueba + ": " +
                            Formato(partida) + (respaldo ? " (respaldo)" : "");
                Debug.Log("[GUARDADO] " + resultado);
            });
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Borrar SOLO esta ranura de diagnostico"))
        {
            Ejecutar(() =>
            {
                new SaveStorage(TestDirectory).DeleteSlot(ranuraPrueba);
                resultado = "Eliminada ranura de diagnostico " + ranuraPrueba + ".";
            });
        }

        MostrarResultado();
    }

    private void BuscarComponentes()
    {
        var equipos = UnityEngine.Object.FindObjectsByType<GestorEquipamiento>(FindObjectsSortMode.None);
        var carteras = UnityEngine.Object.FindObjectsByType<Cartera>(FindObjectsSortMode.None);

        if (equipos.Length == 1 && carteras.Length == 1)
        {
            equipo = equipos[0];
            cartera = carteras[0];
            resultado = "Componentes encontrados: " + equipo.gameObject.name +
                        " y " + cartera.gameObject.name + ".";
        }
        else
        {
            equipo = null;
            cartera = null;
            resultado = "Encontrados " + equipos.Length + " GestorEquipamiento y " +
                        carteras.Length + " Cartera. Selecciona los del jugador manualmente.";
        }
    }

    private static string Formato(GameSaveData data)
    {
        EquipmentUnlocksSaveData e = data.equipmentUnlocks;
        return "arma=" + SiNo(e.armaLanzadoraDesbloqueada) +
               ", torbellino=" + SiNo(e.torbellinoDesbloqueado) +
               ", martillo=" + SiNo(e.martilloDesbloqueado) +
               ", monedas=" + data.economy.coins + ".";
    }

    private static string SiNo(bool valor) => valor ? "SI" : "NO";

    private void MostrarResultado()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ultimo resultado", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(resultado, EditorStyles.wordWrappedLabel);
    }

    private void Ejecutar(Action accion)
    {
        try { accion(); }
        catch (Exception ex)
        {
            resultado = "ERROR: " + ex.Message;
            Debug.LogException(ex);
        }
    }
}
#endif
