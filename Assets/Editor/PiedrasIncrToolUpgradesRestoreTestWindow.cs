
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PiedrasIncrToolUpgradesRestoreTestWindow : EditorWindow
{
    private HerramientaTorbellino torbellino;
    private RecolectorMagnetico iman;
    private Cartera cartera;

    private int ranuraPrueba = 1;
    private string resultado = "Sin pruebas realizadas.";

    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "ToolUpgradesScene"
    );

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar RESTAURAR mejoras (Play)"
    )]
    public static void Abrir()
    {
        GetWindow<PiedrasIncrToolUpgradesRestoreTestWindow>(
            "Restaurar mejoras"
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
            torbellino = null;
            iman = null;
            cartera = null;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "RESTAURACION DE MEJORAS",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Prueba controlada. No toca las partidas reales " +
            "ni compra mejoras. Al terminar recupera los " +
            "niveles originales del jugador.",
            MessageType.Info
        );

        ranuraPrueba = EditorGUILayout.IntSlider(
            "Ranura de prueba", ranuraPrueba, 1, 3
        );

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Entra en Play para realizar la prueba.",
                MessageType.Warning
            );
            MostrarResultado();
            return;
        }

        torbellino = (HerramientaTorbellino)
            EditorGUILayout.ObjectField(
                "Torbellino", torbellino,
                typeof(HerramientaTorbellino), true
            );

        iman = (RecolectorMagnetico)
            EditorGUILayout.ObjectField(
                "Iman", iman,
                typeof(RecolectorMagnetico), true
            );

        cartera = (Cartera)
            EditorGUILayout.ObjectField(
                "Cartera", cartera,
                typeof(Cartera), true
            );

        if (GUILayout.Button("Buscar componentes del jugador"))
            BuscarComponentes();

        if (torbellino == null ||
            iman == null ||
            cartera == null)
        {
            MostrarResultado();
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Radio Torbellino", torbellino.NivelRadio.ToString()
        );
        EditorGUILayout.LabelField(
            "Alcance Torbellino", torbellino.NivelAlcance.ToString()
        );
        EditorGUILayout.LabelField(
            "Movilidad", torbellino.NivelMovilidad.ToString()
        );
        EditorGUILayout.LabelField(
            "Iman desbloqueado",
            iman.EstaImanDesbloqueado() ? "SI" : "NO"
        );
        EditorGUILayout.LabelField(
            "Alcance Iman", iman.NivelAlcance.ToString()
        );
        EditorGUILayout.LabelField(
            "Monedas", cartera.Monedas.ToString()
        );

        EditorGUILayout.Space();

        if (GUILayout.Button(
            "COMPROBAR RESTAURACION (simulacion controlada)"
        ))
        {
            try
            {
                EjecutarPrueba();
            }
            catch (Exception ex)
            {
                resultado = "ERROR: " + ex.Message;
                Debug.LogException(ex);
            }
        }

        MostrarResultado();
    }

    private void BuscarComponentes()
    {
        var torbellinos =
            UnityEngine.Object.FindObjectsByType<HerramientaTorbellino>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        var imanes =
            UnityEngine.Object.FindObjectsByType<RecolectorMagnetico>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        var carteras =
            UnityEngine.Object.FindObjectsByType<Cartera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        torbellino = torbellinos.Length == 1
            ? torbellinos[0] : null;

        iman = imanes.Length == 1
            ? imanes[0] : null;

        cartera = carteras.Length == 1
            ? carteras[0] : null;

        resultado = "Componentes encontrados. " +
                    "Torbellino=" + torbellinos.Length +
                    ", Iman=" + imanes.Length +
                    ", Cartera=" + carteras.Length;
    }

    private void EjecutarPrueba()
    {
        var equipo = cartera.GetComponent<GestorEquipamiento>();

        if (equipo != null && !equipo.ManosLibres)
        {
            throw new InvalidOperationException(
                "Deja las manos vacias antes de hacer la prueba."
            );
        }

        var storage = new SaveStorage(TestDirectory);

        GameSaveData partida;
        bool desdeRespaldo;

        if (!storage.TryLoad(
            ranuraPrueba, out partida, out desdeRespaldo))
        {
            throw new InvalidOperationException(
                "No existe un guardado de diagnostico valido."
            );
        }

        if (partida.sceneName != SceneManager.GetActiveScene().name)
        {
            throw new InvalidOperationException(
                "El guardado pertenece a otra escena."
            );
        }

        ToolUpgradesSaveData guardado =
            ToolUpgradesSaveAdapter.Read(partida);

        // Recordar el estado actual del jugador.
        int radioOriginal = torbellino.NivelRadio;
        int alcanceOriginal = torbellino.NivelAlcance;
        int movilidadOriginal = torbellino.NivelMovilidad;

        bool imanOriginal = iman.EstaImanDesbloqueado();
        int alcanceImanOriginal = iman.NivelAlcance;

        int monedasOriginales = cartera.Monedas;

        try
        {
            // Alterar temporalmente todos los estados.
            torbellino.RestaurarNiveles(
                guardado.nivelRadioTorbellino == 4 ? 1 : 4,
                guardado.nivelAlcanceTorbellino == 3 ? 1 : 3,
                guardado.nivelMovilidadTorbellino == 3 ? 1 : 3
            );

            iman.RestaurarEstadoIman(
                !guardado.imanDesbloqueado,
                guardado.nivelAlcanceIman == 3 ? 1 : 3
            );

            // Restaurar desde el archivo.
            ToolUpgradesSaveAdapter.Restore(
                torbellino, iman, partida
            );

            // Comprobar los resultados.
            if (!CoincideCon(guardado))
            {
                throw new Exception(
                    "Los niveles restaurados no coinciden."
                );
            }

            if (cartera.Monedas != monedasOriginales)
            {
                throw new Exception(
                    "La restauracion ha modificado las monedas."
                );
            }
        }
        finally
        {
            // Devolver siempre el estado original,
            // incluso si la prueba falla.
            torbellino.RestaurarNiveles(
                radioOriginal,
                alcanceOriginal,
                movilidadOriginal
            );

            iman.RestaurarEstadoIman(
                imanOriginal,
                alcanceImanOriginal
            );
        }

        if (cartera.Monedas != monedasOriginales)
        {
            throw new Exception(
                "Las monedas cambiaron durante la prueba."
            );
        }

        if (torbellino.NivelRadio != radioOriginal ||
            torbellino.NivelAlcance != alcanceOriginal ||
            torbellino.NivelMovilidad != movilidadOriginal ||
            iman.EstaImanDesbloqueado() != imanOriginal ||
            iman.NivelAlcance != alcanceImanOriginal)
        {
            throw new Exception(
                "No se ha recuperado el estado original."
            );
        }

        resultado =
            "RESTAURACION MEJORAS SUPERADA TEST " +
            ranuraPrueba +
            ": radio=" + guardado.nivelRadioTorbellino +
            ", alcanceTorbellino=" +
            guardado.nivelAlcanceTorbellino +
            ", movilidad=" + guardado.nivelMovilidadTorbellino +
            ", iman=" + (guardado.imanDesbloqueado ? "SI" : "NO") +
            ", alcanceIman=" + guardado.nivelAlcanceIman +
            ", monedas intactas=" + monedasOriginales + ".";

        Debug.Log("[GUARDADO] " + resultado);
    }

    private bool CoincideCon(ToolUpgradesSaveData datos)
    {
        return
            torbellino.NivelRadio ==
                datos.nivelRadioTorbellino &&
            torbellino.NivelAlcance ==
                datos.nivelAlcanceTorbellino &&
            torbellino.NivelMovilidad ==
                datos.nivelMovilidadTorbellino &&
            iman.EstaImanDesbloqueado() ==
                datos.imanDesbloqueado &&
            iman.NivelAlcance ==
                datos.nivelAlcanceIman;
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
