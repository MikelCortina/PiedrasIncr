#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prueba controlada de restauracion de desbloqueos en el Player real.
/// Lee exclusivamente la carpeta DeveloperTests/EquipmentScene.
/// Cambia el estado de equipo brevemente durante una accion de editor,
/// valida la restauracion y devuelve el estado original (sin escribir archivos).
/// Nunca accede a las ranuras definitivas ni modifica las monedas.
/// </summary>
public sealed class PiedrasIncrEquipmentRestoreTestWindow : EditorWindow
{
    private GestorEquipamiento equipo;
    private Cartera cartera;
    private int ranura = 1;
    private string resultado = "Sin pruebas realizadas.";

    private static string TestDirectory => Path.Combine(
        Application.persistentDataPath, "PiedrasIncr", "DeveloperTests", "EquipmentScene");

    [MenuItem("Tools/PiedrasIncr/Guardado/Probar RESTAURAR desbloqueos (Play)")]
    private static void Abrir()
    {
        GetWindow<PiedrasIncrEquipmentRestoreTestWindow>("Restaurar equipo").Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += CambioDePlay;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= CambioDePlay;
    }

    private void CambioDePlay(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredPlayMode ||
            estado == PlayModeStateChange.ExitingPlayMode)
        {
            equipo = null;
            cartera = null;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("RESTAURACION REAL DE DESBLOQUEOS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Prueba un cambio temporal de los tres desbloqueos, carga el estado guardado " +
            "y finalmente restituye el estado que tenias antes de la prueba. " +
            "NO escribe archivos, NO cobra monedas y NO usa las partidas reales.",
            MessageType.Info);

        ranura = EditorGUILayout.IntSlider("Ranura de diagnostico", ranura, 1, 3);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Entra en Play en la escena Piedra.", MessageType.Warning);
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
            EditorGUILayout.HelpBox("Selecciona GestorEquipamiento y Cartera del Player.", MessageType.Warning);
            MostrarResultado();
            return;
        }

        EditorGUILayout.LabelField("Jugador", equipo.gameObject.name);
        EditorGUILayout.LabelField("Monedas actuales", cartera.Monedas.ToString());
        EditorGUILayout.LabelField("Arma", SiNo(equipo.ArmaLanzadoraDesbloqueada()));
        EditorGUILayout.LabelField("Torbellino", SiNo(equipo.TorbellinoDesbloqueado()));
        EditorGUILayout.LabelField("Martillo", SiNo(equipo.MartilloDesbloqueado()));

        EditorGUILayout.Space();
        if (GUILayout.Button("COMPROBAR RESTAURACION (simulacion controlada)"))
            EjecutarPrueba();

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
            resultado = "Componentes detectados en " + equipo.gameObject.name + ".";
        }
        else
        {
            equipo = null;
            cartera = null;
            resultado = "Se detectaron " + equipos.Length + " equipos y " + carteras.Length +
                        " carteras. Seleccionalos manualmente.";
        }
    }

    private void EjecutarPrueba()
    {
        try
        {
            if (!EditorApplication.isPlaying || equipo == null || cartera == null)
                throw new InvalidOperationException("El jugador debe estar activo en Play.");
            if (!equipo.ManosLibres)
                throw new InvalidOperationException("Desequipa la herramienta antes de probar la restauracion.");
            if (equipo.agarreLanzamiento != null && equipo.agarreLanzamiento.EstaSosteniendoPiedra())
                throw new InvalidOperationException("Suelta la piedra antes de probar la restauracion.");

            var storage = new SaveStorage(TestDirectory);
            GameSaveData guardado;
            bool desdeRespaldo;
            if (!storage.TryLoad(ranura, out guardado, out desdeRespaldo))
                throw new InvalidOperationException("No hay desbloqueos de diagnostico guardados en esta ranura.");
            if (guardado.sceneName != SceneManager.GetActiveScene().name)
                throw new InvalidOperationException("El guardado corresponde a otra escena: " + guardado.sceneName);
            if (!guardado.hasEquipmentUnlocksData || guardado.equipmentUnlocks == null)
                throw new InvalidOperationException("El archivo no contiene un estado valido de desbloqueos.");

            bool armaOriginal = equipo.ArmaLanzadoraDesbloqueada();
            bool torbellinoOriginal = equipo.TorbellinoDesbloqueado();
            bool martilloOriginal = equipo.MartilloDesbloqueado();
            int saldoOriginal = cartera.Monedas;
            int eventosMonedas = 0;
            Action<int> contarEventos = _ => eventosMonedas++;
            cartera.OnMonedasCambiadas += contarEventos;
            try
            {
                var e = guardado.equipmentUnlocks;

                // Forzamos un estado diferente al almacenado, para demostrar
                // que Restore realmente aplica el contenido del archivo.
                equipo.RestaurarDesbloqueos(
                    !e.armaLanzadoraDesbloqueada,
                    !e.torbellinoDesbloqueado,
                    !e.martilloDesbloqueado);

                if (Coincide(equipo, e))
                    throw new InvalidOperationException("La simulacion no ha cambiado los desbloqueos.");

                EquipmentUnlocksSaveAdapter.Restore(equipo, guardado);

                if (!Coincide(equipo, e))
                    throw new InvalidOperationException("Los desbloqueos restaurados no coinciden con el archivo.");
                if (!equipo.ManosLibres)
                    throw new InvalidOperationException("El equipo no ha quedado en manos vacias.");
                if (cartera.Monedas != saldoOriginal || eventosMonedas != 0)
                    throw new InvalidOperationException("La restauracion ha afectado a la cartera.");

                resultado = "RESTAURACION EQUIPO SUPERADA TEST " + ranura +
                            ": arma=" + SiNo(e.armaLanzadoraDesbloqueada) +
                            ", torbellino=" + SiNo(e.torbellinoDesbloqueado) +
                            ", martillo=" + SiNo(e.martilloDesbloqueado) +
                            ", monedas intactas=" + saldoOriginal +
                            (desdeRespaldo ? " (desde respaldo)." : ".");
            }
            finally
            {
                // La ventana nunca deja el Player con desbloqueos cambiados.
                try
                {
                    equipo.RestaurarDesbloqueos(armaOriginal, torbellinoOriginal, martilloOriginal);
                }
                finally
                {
                    cartera.OnMonedasCambiadas -= contarEventos;
                }
            }

            if (cartera.Monedas != saldoOriginal || eventosMonedas != 0 ||
                equipo.ArmaLanzadoraDesbloqueada() != armaOriginal ||
                equipo.TorbellinoDesbloqueado() != torbellinoOriginal ||
                equipo.MartilloDesbloqueado() != martilloOriginal)
                throw new InvalidOperationException("No se ha restaurado el estado original tras la prueba.");

            Debug.Log("[GUARDADO] " + resultado);
        }
        catch (Exception ex)
        {
            resultado = "ERROR: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private static bool Coincide(GestorEquipamiento actual, EquipmentUnlocksSaveData guardado)
    {
        return actual.ArmaLanzadoraDesbloqueada() == guardado.armaLanzadoraDesbloqueada &&
               actual.TorbellinoDesbloqueado() == guardado.torbellinoDesbloqueado &&
               actual.MartilloDesbloqueado() == guardado.martilloDesbloqueado;
    }

    private static string SiNo(bool valor) => valor ? "SI" : "NO";

    private void MostrarResultado()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ultimo resultado", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(resultado, EditorStyles.wordWrappedLabel);
    }
}
#endif
