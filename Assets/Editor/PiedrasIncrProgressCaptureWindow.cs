#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Diagnostico del jugador real. Puede escribir y leer snapshots de prueba,
/// PERO NO teletransporta al jugador ni toca las ranuras de produccion.
/// </summary>
public sealed class PiedrasIncrProgressCaptureWindow : EditorWindow
{
    private Cartera cartera;
    private int testSlot = 1;
    private string status = "Sin pruebas realizadas.";
    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath, "PiedrasIncr", "DeveloperTests", "ProgressScene");

    [MenuItem("Tools/PiedrasIncr/Guardado/Probar progreso en escena (Play)")]
    public static void Open()
    {
        GetWindow<PiedrasIncrProgressCaptureWindow>("Prueba progreso").Show();
    }

    private void OnEnable() { EditorApplication.playModeStateChanged += OnPlayChanged; }
    private void OnDisable() { EditorApplication.playModeStateChanged -= OnPlayChanged; }

    private void OnPlayChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            cartera = null;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("PROGRESO REAL - SOLO DIAGNOSTICO", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Guarda monedas y posicion segura en DeveloperTests/ProgressScene. " +
            "NO toca las tres partidas reales, NO teletransporta y NO hace autosave.",
            MessageType.Info);

        testSlot = EditorGUILayout.IntSlider("Ranura de prueba", testSlot, 1, 3);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Abre la escena Piedra y entra en Play.", MessageType.Warning);
            ShowStatus();
            return;
        }

        cartera = (Cartera)EditorGUILayout.ObjectField("Cartera del jugador", cartera, typeof(Cartera), true);
        if (GUILayout.Button("Buscar Cartera del jugador"))
            FindWallet();
        if (cartera == null)
        {
            ShowStatus();
            return;
        }

        GameObject player = cartera.gameObject;
        EditorGUILayout.LabelField("Jugador", player.name);
        EditorGUILayout.LabelField("Monedas actuales", cartera.Monedas.ToString());
        EditorGUILayout.LabelField("Posicion actual", Vec(player.transform.position));

        if (GUILayout.Button("1. GUARDAR monedas + posicion segura (diagnostico)"))
        {
            Run(() =>
            {
                var save = new GameSaveData { sceneName = SceneManager.GetActiveScene().name };
                EconomySaveAdapter.Capture(cartera, save);
                PlayerSaveAdapter.Capture(player, save);

                // En guardados repetidos del mismo diagnostico, conservar identidad/creacion.
                var storage = new SaveStorage(TestDirectory);
                GameSaveData old;
                bool fromBackup;
                if (storage.TryLoad(testSlot, out old, out fromBackup) && old.sceneName == save.sceneName)
                {
                    save.saveId = old.saveId;
                    save.createdUtc = old.createdUtc;
                }

                storage.Save(testSlot, save);
                status = "GUARDADO TEST " + testSlot + ": " + save.economy.coins +
                         " monedas, posicion " + Vec(PlayerSaveAdapter.GetSavedPosition(save.player)) +
                         ", yaw " + save.player.yawDegrees.ToString("F1", CultureInfo.InvariantCulture) + ".";
                Debug.Log("[GUARDADO] " + status);
            });
        }

        if (GUILayout.Button("2. LEER archivo de diagnostico (sin mover al jugador)"))
        {
            Run(() =>
            {
                var storage = new SaveStorage(TestDirectory);
                GameSaveData save;
                bool fromBackup;
                if (!storage.TryLoad(testSlot, out save, out fromBackup))
                {
                    status = "No hay archivo de diagnostico valido en la ranura " + testSlot + ".";
                    return;
                }
                if (save.sceneName != SceneManager.GetActiveScene().name)
                    throw new InvalidOperationException("El snapshot es de otra escena: " + save.sceneName);
                PlayerSaveAdapter.Validate(save.player);
                status = "LECTURA TEST " + testSlot + ": " + save.economy.coins +
                         " monedas, posicion " + Vec(PlayerSaveAdapter.GetSavedPosition(save.player)) +
                         ", yaw " + save.player.yawDegrees.ToString("F1", CultureInfo.InvariantCulture) +
                         (fromBackup ? " (respaldo)." : ".");
                Debug.Log("[GUARDADO] " + status);
            });
        }

        if (GUILayout.Button("Borrar SOLO esta ranura de diagnostico"))
        {
            Run(() =>
            {
                new SaveStorage(TestDirectory).DeleteSlot(testSlot);
                status = "Eliminado archivo de diagnostico de ranura " + testSlot + ".";
            });
        }

        ShowStatus();
    }

    private void FindWallet()
    {
        var wallets = UnityEngine.Object.FindObjectsByType<Cartera>(FindObjectsSortMode.None);
        if (wallets.Length == 1)
        {
            cartera = wallets[0];
            status = "Jugador detectado: " + cartera.gameObject.name;
        }
        else
        {
            cartera = null;
            status = wallets.Length == 0 ? "No hay Cartera activa." :
                "Hay varias carteras; selecciona la del Player manualmente.";
        }
    }

    private static string Vec(Vector3 v)
    {
        return "(" + v.x.ToString("F2", CultureInfo.InvariantCulture) + ", " +
               v.y.ToString("F2", CultureInfo.InvariantCulture) + ", " +
               v.z.ToString("F2", CultureInfo.InvariantCulture) + ")";
    }

    private void ShowStatus()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ultimo resultado", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
    }

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception exception)
        {
            status = "ERROR: " + exception.Message;
            Debug.LogException(exception);
        }
    }
}
#endif
