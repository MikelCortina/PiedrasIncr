#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prueba de RESTAURACION FISICA del Player usando solo archivos de diagnostico.
/// No toca las ranuras de produccion ni restaura las monedas.
/// </summary>
public sealed class PiedrasIncrPlayerRestoreWindow : EditorWindow
{
    private Cartera wallet;
    private int testSlot = 1;
    private string status = "Sin pruebas.";

    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath, "PiedrasIncr", "DeveloperTests", "ProgressScene");

    [MenuItem("Tools/PiedrasIncr/Guardado/Probar RESTAURAR jugador (Play)")]
    public static void Open()
    {
        GetWindow<PiedrasIncrPlayerRestoreWindow>("Restaurar jugador").Show();
    }

    private void OnEnable() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
    private void OnDisable() { EditorApplication.playModeStateChanged -= OnPlayModeChanged; }

    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode ||
            change == PlayModeStateChange.EnteredPlayMode)
        {
            wallet = null;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("RESTAURAR PLAYER - SOLO DIAGNOSTICO", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Lee DeveloperTests/ProgressScene. Restaura SOLO posicion y orientacion. " +
            "No modifica monedas, ranuras reales, bots ni construcciones. " +
            "La introduccion debe haber terminado.", MessageType.Info);
        testSlot = EditorGUILayout.IntSlider("Ranura de prueba", testSlot, 1, 3);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Abre Piedra y entra en Play para continuar.", MessageType.Warning);
            ShowStatus();
            return;
        }

        wallet = (Cartera)EditorGUILayout.ObjectField(
            "Cartera / Player", wallet, typeof(Cartera), true);
        if (GUILayout.Button("Buscar Player"))
        {
            Cartera[] wallets = UnityEngine.Object.FindObjectsByType<Cartera>(FindObjectsSortMode.None);
            wallet = wallets.Length == 1 ? wallets[0] : null;
            status = wallet != null ? "Player localizado: " + wallet.gameObject.name :
                "No se ha localizado una unica Cartera; seleccionala manualmente.";
        }

        if (wallet != null)
        {
            EditorGUILayout.LabelField("Posicion actual", Vec(wallet.transform.position));
            if (GUILayout.Button("LEER destino guardado (sin mover)"))
                Execute(() =>
                {
                    var data = ReadTestSnapshot();
                    status = "Destino: " + Vec(PlayerSaveAdapter.GetSavedPosition(data.player)) +
                        " | yaw " + data.player.yawDegrees.ToString("F1", CultureInfo.InvariantCulture);
                    Debug.Log("[GUARDADO] RESTAURACION: " + status);
                });

            EditorGUILayout.Space();
            if (GUILayout.Button("RESTAURAR SOLO POSICION + GIRO (diagnostico)"))
                Execute(() =>
                {
                    var data = ReadTestSnapshot();
                    PlayerRestoreAdapter.Restore(wallet.gameObject, data);
                    status = "RESTAURACION TEST " + testSlot + ": " +
                        Vec(wallet.transform.position) + " | yaw " +
                        wallet.transform.eulerAngles.y.ToString("F1", CultureInfo.InvariantCulture) + ".";
                    Debug.Log("[GUARDADO] " + status);
                });
        }

        ShowStatus();
    }

    private GameSaveData ReadTestSnapshot()
    {
        var storage = new SaveStorage(TestDirectory);
        GameSaveData data;
        bool fromBackup;
        if (!storage.TryLoad(testSlot, out data, out fromBackup))
            throw new InvalidOperationException("No existe un archivo valido en la ranura " + testSlot +
                ". Guarda primero con 'Probar progreso en escena'.");
        if (data.sceneName != SceneManager.GetActiveScene().name)
            throw new InvalidOperationException("La posicion pertenece a otra escena: " + data.sceneName);
        PlayerSaveAdapter.Validate(data.player);
        return data;
    }

    private void Execute(Action action)
    {
        try { action(); }
        catch (Exception exception)
        {
            status = "ERROR: " + exception.Message;
            Debug.LogException(exception);
        }
    }

    private void ShowStatus()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Resultado", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
    }

    private static string Vec(Vector3 v)
    {
        return "(" + v.x.ToString("F2", CultureInfo.InvariantCulture) + ", " +
            v.y.ToString("F2", CultureInfo.InvariantCulture) + ", " +
            v.z.ToString("F2", CultureInfo.InvariantCulture) + ")";
    }
}
#endif
