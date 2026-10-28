using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
}

#if UNITY_EDITOR
[CustomEditor(typeof(SceneObjectPainter))]
public class SceneObjectPainterEditor : Editor
{
    private SceneObjectPainter painter;
    private Vector3 lastPaintPosition = Vector3.positiveInfinity;

    private void OnEnable()
    {
        painter = (SceneObjectPainter)target;
    }

    private void OnSceneGUI()
    {
        // Validamos que el pincel esté activo y haya al menos un prefab en la lista
        if (!painter.activarPincel || painter.prefabsToPaint == null || painter.prefabsToPaint.Length == 0) return;

        Event e = Event.current;

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlID);
        }

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !e.alt)
        {
            Paint(e.mousePosition);
            e.Use();
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            lastPaintPosition = Vector3.positiveInfinity;
        }
    }

    private void Paint(Vector2 mousePosition)
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