// Guardar en Assets/Editor/EscaladorAranaEditor.cs
// Diseñado para el Bot de PiedrasIncr. Uso únicamente en modo Edición.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public sealed class EscaladorAranaEditor : EditorWindow
{
    private GameObject bot;
    private float escalaObjetivo = 1f;

    // Campos del BotRecolector que representan distancias o velocidades
    // expresadas en unidades del mundo, no tiempos ni ángulos.
    private static readonly string[] DistanciasBot =
    {
        "distanciaEntregaProcesadora", "distanciaLlegadaEsperaProcesadora",
        "radioNavMeshPiedraProcesada", "distanciaEntrega",
        "distanciaEntregaZonaPiedras", "alturaArcoZonaPiedras",
        "alturaArcoLanzamiento", "alturaMaximaArcoEntrega",
        "profundidadVisualAgujero", "distanciaLlegadaParking",
        "radioBusqueda", "distanciaMaximaPiedraANavMesh",
        "distanciaRecogida", "distanciaMinimaAvance",
        "acercamientoVisualPared", "elevacionVisualTransicion",
        "velocidadEscalada", "distanciaMaximaPruebaEscalada",
        "distanciaComprobacionParedEscalada",
        "separacionOrigenRaycastEscalada", "avanceSondeoBorde",
        "alturaSondeoBorde", "distanciaRaycastSueloSuperior",
        "distanciaEntradaSueloSuperior", "alturaArcoSalidaBorde",
        "arcoMaximoCoronacion", "radioBuscarNavMeshSalida",
        "distanciaLlegadaConexionEscalada",
        "distanciaLlegadaBordeDescenso", "velocidadDescenso",
        "distanciaInicioSalidaNaturalDescenso",
        "toleranciaLlegadaSueloDescenso", "radioNavMeshDescenso"
    };

    [MenuItem("Tools/PiedrasIncr/Escalador de Arana")]
    public static void Abrir()
    {
        GetWindow<EscaladorAranaEditor>("Escalador de Arana");
    }

    private void OnEnable() => ElegirDesdeSeleccion();
    private void OnSelectionChange() { ElegirDesdeSeleccion(); Repaint(); }

    private void ElegirDesdeSeleccion()
    {
        if (Selection.activeGameObject == null) return;
        BotRecolector encontrado =
            Selection.activeGameObject.GetComponentInParent<BotRecolector>();
        if (encontrado == null) return;
        bot = encontrado.gameObject;
        Vector3 s = bot.transform.localScale;
        escalaObjetivo = s.x;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ESCALA UNIFORME DEL BOT", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Selecciona Bot en la jerarquia. Este tool escala todo el Bot, " +
            "su agente NavMesh y los umbrales principales del IK. " +
            "No cambia la escala de las piedras ni la del escenario.",
            MessageType.Info);

        bot = (GameObject)EditorGUILayout.ObjectField(
            "Bot de la escena", bot, typeof(GameObject), true);

        if (bot == null)
        {
            EditorGUILayout.HelpBox("Selecciona un BotRecolector.", MessageType.Warning);
            return;
        }

        BotRecolector recolector = bot.GetComponent<BotRecolector>();
        NavMeshAgent nav = bot.GetComponent<NavMeshAgent>();
        PataProcedural[] patas = bot.GetComponentsInChildren<PataProcedural>(true);
        Vector3 actual = bot.transform.localScale;

        if (recolector == null || nav == null || patas.Length != 6)
        {
            EditorGUILayout.HelpBox(
                "Se necesita BotRecolector, NavMeshAgent y exactamente 6 PataProcedural " +
                "dentro del Bot. No se aplicara ninguna escala.", MessageType.Error);
            return;
        }

        if (!EsUniforme(actual) || actual.x <= 0f)
        {
            EditorGUILayout.HelpBox(
                "La escala actual del Bot no es uniforme y positiva. " +
                "Primero hay que corregirla manualmente.", MessageType.Error);
            return;
        }

        if (EditorUtility.IsPersistent(bot))
        {
            EditorGUILayout.HelpBox(
                "Abre una escena y selecciona la instancia del Bot, " +
                "no el asset del prefab.", MessageType.Error);
            return;
        }

        foreach (PataProcedural pata in patas)
        {
            if (pata.cadera == null || pata.rodilla == null ||
                pata.pie == null || pata.objetivo == null ||
                pata.upperLeg == null || pata.lowerLeg == null ||
                !pata.cadera.IsChildOf(bot.transform) ||
                !pata.rodilla.IsChildOf(bot.transform) ||
                !pata.pie.IsChildOf(bot.transform) ||
                !pata.objetivo.IsChildOf(bot.transform) ||
                !pata.upperLeg.IsChildOf(bot.transform) ||
                !pata.lowerLeg.IsChildOf(bot.transform))
            {
                EditorGUILayout.HelpBox(
                    "Hay referencias IK vacias o fuera del Bot en " + pata.name +
                    ". Corrigelas antes de escalar.", MessageType.Error);
                return;
            }
        }

        EditorGUILayout.LabelField("Escala actual", actual.x.ToString("0.000") + "x");
        escalaObjetivo = EditorGUILayout.Slider(
            "Escala deseada", escalaObjetivo, 0.25f, 2f);
        escalaObjetivo = EditorGUILayout.FloatField("Valor exacto", escalaObjetivo);
        escalaObjetivo = Mathf.Clamp(escalaObjetivo, 0.25f, 2f);

        if (escalaObjetivo < 0.5f)
        {
            EditorGUILayout.HelpBox(
                "Por debajo de 0.5x deberas probar especialmente recogida, " +
                "escalada y navegacion: hay distancias internas no configurables.",
                MessageType.Warning);
        }

        EditorGUILayout.HelpBox(
            "IMPORTANTE: corrige ColocarSegmento de PataProcedural " +
            "para compensar la escala del padre. Si no, los segmentos " +
            "de las patas tendran longitud incorrecta.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(
            EditorApplication.isPlayingOrWillChangePlaymode ||
            Mathf.Abs(escalaObjetivo - actual.x) < 0.0001f))
        {
            if (GUILayout.Button("Aplicar escala con Undo", GUILayout.Height(34)))
            {
                if (EditorUtility.DisplayDialog(
                    "Escalar Bot",
                    "¿Aplicar " + escalaObjetivo.ToString("0.00") +
                    "x a este Bot? Guarda una copia de la escena antes de probar.",
                    "Aplicar", "Cancelar"))
                {
                    Aplicar(recolector, nav, patas, actual.x, escalaObjetivo);
                }
            }
        }

        EditorGUILayout.HelpBox(
            "Usa Ctrl+Z para deshacer. Despues prueba en Play las patas, " +
            "recogida, entrega y escalada. La herramienta no rebakea el NavMesh.",
            MessageType.None);
    }

    private static bool EsUniforme(Vector3 s)
    {
        return Mathf.Abs(s.x - s.y) < 0.0001f &&
               Mathf.Abs(s.x - s.z) < 0.0001f;
    }

    private static void Aplicar(BotRecolector bot,
        NavMeshAgent nav, PataProcedural[] patas, float anterior, float objetivo)
    {
        float ratio = objetivo / anterior;
        int grupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Escalar araña (Bot)");

        Undo.RecordObject(bot.transform, "Escalar Bot");
        bot.transform.localScale = Vector3.one * objetivo;
        GuardarCambio(bot.transform);

        Undo.RecordObject(nav, "Escalar NavMeshAgent");
        // Son tamaños y velocidades en unidades del mundo.
        nav.radius *= ratio;
        nav.height *= ratio;
        nav.baseOffset *= ratio;
        nav.stoppingDistance *= ratio;
        nav.speed *= ratio;
        nav.acceleration *= ratio;
        GuardarCambio(nav);

        ConfiguracionBot config = bot.GetComponent<ConfiguracionBot>();
        if (config != null)
        {
            Undo.RecordObject(config, "Escalar velocidad configurada");
            config.velocidadMovimiento *= ratio;
            GuardarCambio(config);
        }

        foreach (PataProcedural p in patas)
        {
            Undo.RecordObject(p, "Escalar parametros pata");
            p.distanciaPaso *= ratio;
            p.alturaPaso *= ratio;
            p.alturaRaycast *= ratio;
            p.distanciaRaycast *= ratio;
            p.offsetSuelo *= ratio;
            p.distanciaMaximaAnticipacion *= ratio;
            p.distanciaMinimaPasoGiro *= ratio;
            // grosorPata NO se multiplica: la escala del Bot ya lo reduce.
            // velocidadPaso tampoco: controla duracion, no metros/segundo.
            GuardarCambio(p);
        }

        Undo.RecordObject(bot, "Escalar distancias de trabajo");
        SerializedObject so = new SerializedObject(bot);
        so.Update();
        foreach (string nombre in DistanciasBot)
        {
            SerializedProperty p = so.FindProperty(nombre);
            if (p != null && p.propertyType == SerializedPropertyType.Float)
                p.floatValue *= ratio;
        }
        so.ApplyModifiedProperties();
        GuardarCambio(bot);

        if (bot.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(bot.gameObject.scene);

        Undo.CollapseUndoOperations(grupo);
        Debug.Log("[ESCALADOR ARANA] Bot: " + anterior.ToString("0.00") +
                  "x -> " + objetivo.ToString("0.00") +
                  "x. Prueba IK / NavMesh / recogida y escalada en Play.");
    }

    private static void GuardarCambio(Object objeto)
    {
        EditorUtility.SetDirty(objeto);
        if (PrefabUtility.IsPartOfPrefabInstance(objeto))
            PrefabUtility.RecordPrefabInstancePropertyModifications(objeto);
    }
}
