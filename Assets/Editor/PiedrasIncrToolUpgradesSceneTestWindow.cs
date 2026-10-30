#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prueba de captura/lectura de mejoras del Torbellino e Iman reales en Play.
/// Utiliza una carpeta de diagnostico independiente de las tres ranuras reales.
/// NO aplica cambios, NO ejecuta compras y NO escribe sobre la escena.
/// </summary>
public sealed class PiedrasIncrToolUpgradesSceneTestWindow : EditorWindow
{
    private HerramientaTorbellino torbellino;
    private RecolectorMagnetico iman;
    private Cartera cartera;
    private int ranuraPrueba = 1;
    private string resultado = "Aun no se han realizado pruebas.";

    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath, "PiedrasIncr", "DeveloperTests", "ToolUpgradesScene");

    [MenuItem("Tools/PiedrasIncr/Guardado/Probar mejoras herramientas en escena (Play)")]
    public static void Abrir()
    {
        GetWindow<PiedrasIncrToolUpgradesSceneTestWindow>("Prueba mejoras").Show();
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

        torbellino = null;
        iman = null;
        cartera = null;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("MEJORAS REALES - SOLO DIAGNOSTICO", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Captura y lee niveles reales en DeveloperTests/ToolUpgradesScene. " +
            "NO toca las tres partidas definitivas. NO modifica niveles, monedas ni compras.",
            MessageType.Info);

        ranuraPrueba = EditorGUILayout.IntSlider("Ranura de prueba", ranuraPrueba, 1, 3);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Abre la escena Piedra y entra en Play.", MessageType.Warning);
            MostrarResultado();
            return;
        }

        torbellino = (HerramientaTorbellino)EditorGUILayout.ObjectField(
            "HerramientaTorbellino", torbellino, typeof(HerramientaTorbellino), true);
        iman = (RecolectorMagnetico)EditorGUILayout.ObjectField(
            "RecolectorMagnetico", iman, typeof(RecolectorMagnetico), true);
        cartera = (Cartera)EditorGUILayout.ObjectField(
            "Cartera", cartera, typeof(Cartera), true);

        if (GUILayout.Button("Buscar componentes del jugador"))
            BuscarComponentes();

        if (torbellino == null || iman == null || cartera == null)
        {
            EditorGUILayout.HelpBox(
                "Selecciona los tres componentes de la escena o usa la busqueda.",
                MessageType.Warning);
            MostrarResultado();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Torbellino", torbellino.gameObject.name);
        EditorGUILayout.LabelField("Iman", iman.gameObject.name);
        EditorGUILayout.LabelField("Cartera", cartera.gameObject.name);
        EditorGUILayout.LabelField("Radio Torbellino", torbellino.NivelRadio.ToString());
        EditorGUILayout.LabelField("Alcance Torbellino", torbellino.NivelAlcance.ToString());
        EditorGUILayout.LabelField("Movilidad Torbellino", torbellino.NivelMovilidad.ToString());
        EditorGUILayout.LabelField("Iman desbloqueado", SiNo(iman.EstaImanDesbloqueado()));
        EditorGUILayout.LabelField("Alcance Iman", iman.NivelAlcance.ToString());
        EditorGUILayout.LabelField("Monedas", cartera.Monedas.ToString());

        EditorGUILayout.Space();
        if (GUILayout.Button("1. GUARDAR mejoras reales (diagnostico)"))
        {
            Ejecutar(() =>
            {
                int monedasAntes = cartera.Monedas;
                var partida = new GameSaveData
                {
                    sceneName = SceneManager.GetActiveScene().name
                };
                EconomySaveAdapter.Capture(cartera, partida);
                ToolUpgradesSaveAdapter.Capture(torbellino, iman, partida);

                var storage = new SaveStorage(TestDirectory);
                GameSaveData anterior;
                bool desdeRespaldo;
                if (storage.TryLoad(ranuraPrueba, out anterior, out desdeRespaldo) &&
                    anterior.sceneName == partida.sceneName)
                {
                    partida.saveId = anterior.saveId;
                    partida.createdUtc = anterior.createdUtc;
                }

                storage.Save(ranuraPrueba, partida);
                if (cartera.Monedas != monedasAntes)
                    throw new InvalidOperationException("La prueba ha modificado las monedas.");

                resultado = "MEJORAS GUARDADAS TEST " + ranuraPrueba + ": " + Formato(partida);
                Debug.Log("[GUARDADO] " + resultado);
            });
        }

        if (GUILayout.Button("2. LEER mejoras guardadas (sin aplicar)"))
        {
            Ejecutar(() =>
            {
                int monedasAntes = cartera.Monedas;
                GameSaveData partida;
                bool desdeRespaldo;
                if (!new SaveStorage(TestDirectory).TryLoad(ranuraPrueba, out partida, out desdeRespaldo))
                    throw new InvalidOperationException("No existe un archivo valido en esta ranura de diagnostico.");
                if (partida.sceneName != SceneManager.GetActiveScene().name)
                    throw new InvalidOperationException("El archivo pertenece a otra escena: " + partida.sceneName);

                ToolUpgradesSaveAdapter.Read(partida); // Rechaza datos ausentes o fuera de rango.
                if (cartera.Monedas != monedasAntes)
                    throw new InvalidOperationException("La lectura ha modificado las monedas.");

                resultado = "MEJORAS LECTURA TEST " + ranuraPrueba + ": " + Formato(partida) +
                            (desdeRespaldo ? " (respaldo)" : "");
                Debug.Log("[GUARDADO] " + resultado);
            });
        }

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
        // Incluye objetos inactivos de la escena, pero no assets/prefabs externos.
        var torbellinos = UnityEngine.Object.FindObjectsByType<HerramientaTorbellino>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var imanes = UnityEngine.Object.FindObjectsByType<RecolectorMagnetico>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var carteras = UnityEngine.Object.FindObjectsByType<Cartera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        torbellino = torbellinos.Length == 1 ? torbellinos[0] : null;
        iman = imanes.Length == 1 ? imanes[0] : null;
        cartera = carteras.Length == 1 ? carteras[0] : null;

        resultado = "Encontrados Torbellino=" + torbellinos.Length +
                    ", Iman=" + imanes.Length + ", Cartera=" + carteras.Length +
                    ". Si hay varios, selecciona los del Player manualmente.";
    }

    private static string Formato(GameSaveData data)
    {
        ToolUpgradesSaveData u = ToolUpgradesSaveAdapter.Read(data);
        return "radio=" + u.nivelRadioTorbellino +
               ", alcanceTorbellino=" + u.nivelAlcanceTorbellino +
               ", movilidad=" + u.nivelMovilidadTorbellino +
               ", iman=" + SiNo(u.imanDesbloqueado) +
               ", alcanceIman=" + u.nivelAlcanceIman +
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
