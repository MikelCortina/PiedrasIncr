using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Ventana para poner caras LED en pantallas de la escena.
// Arrastra a la lista los objetos (o sus padres) a los que quieres ponérsela, o pulsa "Buscar pantallas decorativas"
// para rellenarla sola. Dentro de cada objeto se busca el Renderer de la pantalla por su material de cristal
// ('Material.001' por defecto); si no hay ninguno, se usa el Renderer del propio objeto entero (p. ej. un Quad).
// La búsqueda automática se salta las informativas: shader LightGrid, RenderTexture o textos TMP al lado.
public sealed class CarasPantallasEditor : EditorWindow
{
    [SerializeField] private List<GameObject> objetos = new List<GameObject>();
    [SerializeField] private string materialPantalla = "Material.001";
    [SerializeField] private CaraLedArana.Expresion expresion = CaraLedArana.Expresion.SonrienteConFallos;
    [SerializeField, Min(6)] private int columnas = 32;

    private Vector2 scroll;
    private SerializedObject serializado;

    [MenuItem("Herramientas/Cara LED/Caras en pantallas")]
    static void Abrir()
    {
        GetWindow<CarasPantallasEditor>("Caras en pantallas");
    }

    private void OnEnable()
    {
        serializado = new SerializedObject(this);
    }

    private void OnGUI()
    {
        serializado.Update();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Arrastra a la lista las pantallas (o los objetos que las contienen) y pulsa 'Poner caras'.\n" +
            "'Buscar pantallas decorativas' añade solas las que encuentre en la escena, sin las informativas.",
            MessageType.Info);

        EditorGUILayout.PropertyField(serializado.FindProperty("materialPantalla"),
            new GUIContent("Material de pantalla", "Nombre del material del cristal de la pantalla en el modelo."));
        EditorGUILayout.PropertyField(serializado.FindProperty("expresion"), new GUIContent("Expresión"));
        EditorGUILayout.PropertyField(serializado.FindProperty("columnas"),
            new GUIContent("LEDs a lo ancho", "Resolución de la cara: más LEDs = más detalle en la cara y el fondo."));
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(serializado.FindProperty("objetos"), new GUIContent("Pantallas"), true);
        serializado.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Buscar pantallas decorativas")) BuscarPantallas();
            if (GUILayout.Button("Añadir selección"))
            {
                Undo.RecordObject(this, "Añadir selección");
                foreach (GameObject go in Selection.gameObjects)
                    if (!EditorUtility.IsPersistent(go) && !objetos.Contains(go)) objetos.Add(go);
            }
            if (GUILayout.Button("Vaciar lista"))
            {
                Undo.RecordObject(this, "Vaciar lista");
                objetos.Clear();
            }
        }

        EditorGUILayout.Space(5);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Poner caras", GUILayout.Height(32))) PonerCaras();
                if (GUILayout.Button("Quitar caras", GUILayout.Height(32))) QuitarCaras();
            }
        }
        if (EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Sal de Play para poner o quitar caras (en Play no se guardan).", MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    // =====================================================
    // BUSCAR
    // =====================================================

    private void BuscarPantallas()
    {
        Undo.RecordObject(this, "Buscar pantallas");
        List<string> informativas = new List<string>();
        int nuevas = 0;

        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (EditorUtility.IsPersistent(r) || r.GetComponent<CaraLedArana>() != null) continue;
            if (!EsPantalla(r)) continue;

            if (EsInformativa(r))
            {
                informativas.Add(Ruta(r.transform));
                continue;
            }

            if (!objetos.Contains(r.gameObject))
            {
                objetos.Add(r.gameObject);
                nuevas++;
            }
        }

        Debug.Log("[Cara LED] " + nuevas + " pantallas añadidas a la lista. Informativas que no se añaden (" +
                  informativas.Count + "):\n  " + string.Join("\n  ", informativas));
    }

    // =====================================================
    // PONER / QUITAR
    // =====================================================

    private void PonerCaras()
    {
        List<string> puestas = new List<string>();
        List<string> sinRenderer = new List<string>();

        foreach (GameObject go in objetos)
        {
            if (go == null) continue;

            List<Renderer> destinos = RenderersPantalla(go);
            if (destinos.Count == 0)
            {
                sinRenderer.Add(Ruta(go.transform));
                continue;
            }

            foreach (Renderer r in destinos)
            {
                CaraLedArana cara = r.GetComponent<CaraLedArana>();
                if (cara == null) cara = Undo.AddComponent<CaraLedArana>(r.gameObject);
                else Undo.RecordObject(cara, "Configurar cara");

                bool tieneSlot = TieneMaterial(r, materialPantalla);
                cara.nombreMaterial = tieneSlot ? materialPantalla : "";
                cara.indiceMaterial = tieneSlot ? cara.indiceMaterial : -1; // sin slot: todo el objeto
                cara.superponerQuad = true;
                cara.apagarNeonDelSlot = false;
                cara.expresion = expresion;
                cara.columnas = columnas;

                Guardar(cara);
                puestas.Add(Ruta(r.transform));
            }
        }

        Debug.Log("[Cara LED] Caras puestas (" + puestas.Count + "):\n  " + string.Join("\n  ", puestas) +
                  (sinRenderer.Count > 0 ? "\n\nSin Renderer (no se ha puesto cara):\n  " + string.Join("\n  ", sinRenderer) : "") +
                  "\n\nGuarda la escena para conservarlas.");
    }

    private void QuitarCaras()
    {
        int quitadas = 0;
        foreach (GameObject go in objetos)
        {
            if (go == null) continue;
            foreach (CaraLedArana cara in go.GetComponentsInChildren<CaraLedArana>(true))
            {
                // Las caras de las arañas (siguen a la piedra) no se tocan.
                if (cara.GetComponentInParent<BotRecolector>(true) != null) continue;
                EditorSceneManager.MarkSceneDirty(cara.gameObject.scene);
                Undo.DestroyObjectImmediate(cara);
                quitadas++;
            }
        }
        Debug.Log("[Cara LED] Caras quitadas: " + quitadas + ". Guarda la escena.");
    }

    // Pantallas dentro del objeto: los Renderers con el material de cristal; si no hay, el Renderer del propio objeto.
    private List<Renderer> RenderersPantalla(GameObject go)
    {
        List<Renderer> lista = new List<Renderer>();
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            if (r.name != "CaraLedQuad" && TieneMaterial(r, materialPantalla)) lista.Add(r);

        if (lista.Count == 0)
        {
            Renderer propio = go.GetComponent<Renderer>();
            if (propio != null) lista.Add(propio);
        }
        return lista;
    }

    private static void Guardar(Object objeto)
    {
        EditorUtility.SetDirty(objeto);
        if (PrefabUtility.IsPartOfPrefabInstance(objeto))
            PrefabUtility.RecordPrefabInstancePropertyModifications(objeto);
        if (objeto is Component componente)
            EditorSceneManager.MarkSceneDirty(componente.gameObject.scene);
    }

    // =====================================================
    // CRITERIOS
    // =====================================================

    private bool EsPantalla(Renderer r)
    {
        foreach (Material m in r.sharedMaterials)
        {
            if (m == null) continue;
            if (m.name == materialPantalla) return true;
            if (m.shader != null && m.shader.name.Contains("LightGrid")) return true;
        }
        return false;
    }

    private static bool TieneMaterial(Renderer r, string nombre)
    {
        foreach (Material m in r.sharedMaterials)
            if (m != null && m.name == nombre) return true;
        return false;
    }

    private static bool EsInformativa(Renderer r)
    {
        foreach (Material m in r.sharedMaterials)
        {
            if (m == null) continue;
            if (m.shader != null && m.shader.name.Contains("LightGrid")) return true;
            foreach (string propiedad in m.GetTexturePropertyNames())
                if (m.GetTexture(propiedad) is RenderTexture) return true;
        }

        // Textos en la misma pieza (contadores, carteles con datos).
        Transform raiz = r.transform.parent != null ? r.transform.parent : r.transform;
        return raiz.GetComponentInChildren<TMP_Text>(true) != null;
    }

    private static string Ruta(Transform t)
    {
        string ruta = t.name;
        for (Transform p = t.parent; p != null; p = p.parent) ruta = p.name + "/" + ruta;
        return ruta;
    }
}
