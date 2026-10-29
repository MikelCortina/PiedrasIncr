using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

[ExecuteAlways]
public class SceneObjectPainter : MonoBehaviour
{
    public enum ModoAlineacion { Suelo, Pared }

    [Header("Herramienta de Pintura")]
    [Tooltip("Activa esta casilla para empezar a pintar.")]
    public bool activarPincel = false;

    [Header("Configuración")]
    [Tooltip("Añade aquí todos los modelos que quieras. El script elegirá uno al azar cada vez.")]
    public GameObject[] prefabsToPaint; // <-- Ahora es un Array para permitir múltiples objetos
    public LayerMask paintableLayer = ~0;
    public float spacing = 1f;

    [Header("Orientación")]
    public ModoAlineacion modoAlineacion = ModoAlineacion.Suelo;

    [Header("Randomización de Escala")]
    public float minScale = 0.5f;
    public float maxScale = 1.5f;

#if UNITY_EDITOR
    // Pintores cargados en las escenas abiertas (el pincel no depende de tenerlos seleccionados).
    public static readonly HashSet<SceneObjectPainter> Activos = new HashSet<SceneObjectPainter>();

    private void OnEnable()
    {
        Activos.Add(this);
    }

    private void OnDisable()
    {
        Activos.Remove(this);
    }

    public bool PuedePintar => activarPincel && prefabsToPaint != null && prefabsToPaint.Length > 0;
#endif
}

#if UNITY_EDITOR
// El pincel escucha la Scene view directamente. Antes vivía en el OnSceneGUI de un Editor
// personalizado, que Unity solo llama con el objeto seleccionado y su componente desplegado
// en el Inspector; si no, el clic llegaba a la Scene view y seleccionaba el objeto de debajo.
[InitializeOnLoad]
public static class SceneObjectPainterPincel
{
    private static readonly int pistaControl = "SceneObjectPainterPincel".GetHashCode();
    private static Vector3 lastPaintPosition = Vector3.positiveInfinity;

    static SceneObjectPainterPincel()
    {
        SceneView.duringSceneGui -= AlDibujarEscena;
        SceneView.duringSceneGui += AlDibujarEscena;
    }

    private static SceneObjectPainter PintorActivo()
    {
        // Si hay varios con la casilla activada, manda el que esté seleccionado.
        SceneObjectPainter elegido = null;
        foreach (SceneObjectPainter painter in SceneObjectPainter.Activos)
        {
            if (painter == null || !painter.PuedePintar)
                continue;

            if (Selection.activeGameObject == painter.gameObject)
                return painter;

            if (elegido == null)
                elegido = painter;
        }

        return elegido;
    }

    private static void AlDibujarEscena(SceneView vista)
    {
        if (EditorApplication.isPlaying)
            return;

        SceneObjectPainter painter = PintorActivo();
        if (painter == null)
            return;

        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(pistaControl, FocusType.Passive);

        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlID);
        }

        // El pincel se queda con el ratón (hotControl) desde que se pulsa hasta que se suelta:
        // si el MouseUp queda libre, la Scene view lo usa para seleccionar el objeto de debajo.
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && !e.alt)
                {
                    GUIUtility.hotControl = controlID;
                    Paint(painter, e.mousePosition);
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    Paint(painter, e.mousePosition);
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    lastPaintPosition = Vector3.positiveInfinity;
                    e.Use();
                }
                break;
        }
    }

    private static void Paint(SceneObjectPainter painter, Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, painter.paintableLayer))
        {
            if (Vector3.Distance(lastPaintPosition, hit.point) > painter.spacing)
            {
                // --- SELECCIÓN ALEATORIA DEL PREFAB ---
                // Elegimos un índice al azar dentro del tamaño de nuestro Array de prefabs
                GameObject prefabSeleccionado = painter.prefabsToPaint[Random.Range(0, painter.prefabsToPaint.Length)];

                // Medida de seguridad: por si dejas un hueco vacío en el Inspector
                if (prefabSeleccionado == null) return;

                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabSeleccionado);
                newObj.transform.position = hit.point;

                // Guardamos la rotación original del prefab seleccionado (por ejemplo, los -90 en X)
                Quaternion originalPrefabRotation = prefabSeleccionado.transform.rotation;

                if (painter.modoAlineacion == SceneObjectPainter.ModoAlineacion.Suelo)
                {
                    Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    Quaternion randomY = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    newObj.transform.rotation = surfaceRotation * randomY * originalPrefabRotation;
                }
                else // Modo Pared
                {
                    Quaternion wallRotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                    newObj.transform.rotation = wallRotation * originalPrefabRotation;
                }

                newObj.transform.localScale = Vector3.one * Random.Range(painter.minScale, painter.maxScale);
                newObj.transform.SetParent(painter.transform);

                Undo.RegisterCreatedObjectUndo(newObj, "Pintar Objeto");
                lastPaintPosition = hit.point;
            }
        }
    }
}
#endif
