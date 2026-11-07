
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public sealed class EscaladorAranaEditor : EditorWindow
{
    private GameObject bot;

    private float escalaObjetivo = 1f;
    private float velocidadObjetivo = 1.5f;

    // =====================================================
    // DISTANCIAS QUE SE ESCALAN CON EL BOT
    // =====================================================

    private static readonly string[] DistanciasBot =
    {
        "distanciaEntregaProcesadora",
        "distanciaLlegadaEsperaProcesadora",
        "radioNavMeshPiedraProcesada",
        "distanciaEntrega",
        "distanciaEntregaZonaPiedras",
        "alturaArcoZonaPiedras",
        "alturaArcoLanzamiento",
        "alturaMaximaArcoEntrega",
        "profundidadVisualAgujero",
        "distanciaLlegadaParking",
        "radioBusqueda",
        "distanciaMaximaPiedraANavMesh",
        "distanciaRecogida",
        "distanciaMinimaAvance",
        "acercamientoVisualPared",
        "elevacionVisualTransicion",
        "velocidadEscalada",
        "distanciaMaximaPruebaEscalada",
        "distanciaComprobacionParedEscalada",
        "separacionOrigenRaycastEscalada",
        "avanceSondeoBorde",
        "alturaSondeoBorde",
        "distanciaRaycastSueloSuperior",
        "distanciaEntradaSueloSuperior",
        "alturaArcoSalidaBorde",
        "arcoMaximoCoronacion",
        "radioBuscarNavMeshSalida",
        "distanciaLlegadaConexionEscalada",
        "distanciaLlegadaBordeDescenso",
        "velocidadDescenso",
        "distanciaInicioSalidaNaturalDescenso",
        "toleranciaLlegadaSueloDescenso",
        "radioNavMeshDescenso"
    };

    // =====================================================
    // ABRIR HERRAMIENTA
    // =====================================================

    [MenuItem("Tools/PiedrasIncr/Escalador de Arana")]
    public static void Abrir()
    {
        GetWindow<EscaladorAranaEditor>(
            "Escalador de Arana"
        );
    }

    private void OnEnable()
    {
        ElegirDesdeSeleccion();
    }

    private void OnSelectionChange()
    {
        ElegirDesdeSeleccion();
        Repaint();
    }

    private void OnInspectorUpdate()
    {
        // Refresca el slider si ConfiguracionBot cambia
        // desde el Inspector durante Play.
        if (bot != null)
            Repaint();
    }

    // =====================================================
    // SELECCIONAR BOT
    // =====================================================

    private void ElegirDesdeSeleccion()
    {
        if (Selection.activeGameObject == null)
            return;

        BotRecolector encontrado =
            Selection.activeGameObject
                .GetComponentInParent<BotRecolector>();

        if (encontrado == null)
            return;

        AsignarBot(encontrado.gameObject);
    }

    private void AsignarBot(GameObject nuevoBot)
    {
        bot = nuevoBot;

        if (bot == null)
            return;

        escalaObjetivo =
            bot.transform.localScale.x;

        ConfiguracionBot configuracion =
            bot.GetComponent<ConfiguracionBot>();

        NavMeshAgent agente =
            bot.GetComponent<NavMeshAgent>();

        if (configuracion != null)
        {
            velocidadObjetivo =
                configuracion.velocidadMovimiento;
        }
        else if (agente != null)
        {
            velocidadObjetivo = agente.speed;
        }
    }

    // =====================================================
    // INTERFAZ
    // =====================================================

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "PERSONALIZADOR DE ARANA",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Selecciona un Bot de la escena.\n" +
            "El tamano se cambia fuera de Play.\n" +
            "La velocidad tambien puede cambiarse durante Play.",
            MessageType.Info
        );

        GameObject seleccion =
            (GameObject)EditorGUILayout.ObjectField(
                "Bot de la escena",
                bot,
                typeof(GameObject),
                true
            );

        if (seleccion != bot)
            AsignarBot(seleccion);

        if (bot == null)
        {
            EditorGUILayout.HelpBox(
                "Selecciona un BotRecolector.",
                MessageType.Warning
            );

            return;
        }

        BotRecolector recolector =
            bot.GetComponent<BotRecolector>();

        NavMeshAgent nav =
            bot.GetComponent<NavMeshAgent>();

        ConfiguracionBot config =
            bot.GetComponent<ConfiguracionBot>();

        PataProcedural[] patas =
            bot.GetComponentsInChildren<PataProcedural>(true);

        Vector3 escalaActual =
            bot.transform.localScale;

        // =================================================
        // VALIDACIONES
        // =================================================

        if (recolector == null ||
            nav == null ||
            config == null ||
            patas.Length != 6)
        {
            EditorGUILayout.HelpBox(
                "Se necesita BotRecolector, " +
                "NavMeshAgent, ConfiguracionBot " +
                "y exactamente 6 PataProcedural.",
                MessageType.Error
            );

            return;
        }

        if (!EsUniforme(escalaActual) ||
            escalaActual.x <= 0f)
        {
            EditorGUILayout.HelpBox(
                "La escala del Bot debe ser uniforme " +
                "y positiva.",
                MessageType.Error
            );

            return;
        }

        if (EditorUtility.IsPersistent(bot))
        {
            EditorGUILayout.HelpBox(
                "Selecciona una instancia del Bot " +
                "dentro de una escena.",
                MessageType.Error
            );

            return;
        }

        foreach (PataProcedural pata in patas)
        {
            if (pata.cadera == null ||
                pata.rodilla == null ||
                pata.pie == null ||
                pata.objetivo == null ||
                pata.upperLeg == null ||
                pata.lowerLeg == null ||
                !pata.cadera.IsChildOf(bot.transform) ||
                !pata.rodilla.IsChildOf(bot.transform) ||
                !pata.pie.IsChildOf(bot.transform) ||
                !pata.objetivo.IsChildOf(bot.transform) ||
                !pata.upperLeg.IsChildOf(bot.transform) ||
                !pata.lowerLeg.IsChildOf(bot.transform))
            {
                EditorGUILayout.HelpBox(
                    "Referencias IK incorrectas en " +
                    pata.name,
                    MessageType.Error
                );

                return;
            }
        }

        // =================================================
        // TAMANO
        // =================================================

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "TAMANO DEL BOT",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "Escala actual",
            escalaActual.x.ToString("0.000") + "x"
        );

        using (new EditorGUI.DisabledScope(
            EditorApplication.isPlayingOrWillChangePlaymode))
        {
            escalaObjetivo =
                EditorGUILayout.Slider(
                    "Escala deseada",
                    escalaObjetivo,
                    0.25f,
                    2f
                );

            escalaObjetivo =
                EditorGUILayout.FloatField(
                    "Valor exacto",
                    escalaObjetivo
                );

            escalaObjetivo = Mathf.Clamp(
                escalaObjetivo,
                0.25f,
                2f
            );
        }

        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Para cambiar el tamano, sal de Play. " +
                "La velocidad si puede cambiarse ahora.",
                MessageType.Info
            );
        }

        if (escalaObjetivo < 0.5f)
        {
            EditorGUILayout.HelpBox(
                "Las escalas inferiores a 0.5 " +
                "requieren comprobar navegacion, " +
                "recogida y escalada.",
                MessageType.Warning
            );
        }

        // =================================================
        // VELOCIDAD
        // =================================================

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "VELOCIDAD DE MOVIMIENTO",
            EditorStyles.boldLabel
        );

        // Durante Play, ConfiguracionBot es la fuente
        // del valor mostrado en la herramienta.
        // Fuera de Play, permitimos preparar un valor
        // y guardarlo al pulsar Aplicar.
        float velocidadMostrada =
            EditorApplication.isPlaying
            ? config.velocidadMovimiento
            : velocidadObjetivo;

        EditorGUI.BeginChangeCheck();

        velocidadMostrada =
            EditorGUILayout.Slider(
                "Velocidad deseada",
                velocidadMostrada,
                1f,
                5f
            );

        velocidadMostrada =
            EditorGUILayout.FloatField(
                "Velocidad exacta",
                velocidadMostrada
            );

        velocidadMostrada = Mathf.Clamp(
            velocidadMostrada,
            1f,
            5f
        );

        bool velocidadCambiada =
            EditorGUI.EndChangeCheck();

        if (velocidadCambiada)
        {
            velocidadObjetivo = velocidadMostrada;

            if (EditorApplication.isPlaying)
            {
                // Cambia inmediatamente la velocidad base
                // que utiliza BotRecolector.
                // El propio método sincroniza ConfiguracionBot.
                recolector.EstablecerVelocidadBase(
                    velocidadMostrada
                );
            }
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Velocidad configurada",
            config.velocidadMovimiento.ToString("0.00") +
            " m/s"
        );

        EditorGUILayout.LabelField(
            "Speed actual NavMeshAgent",
            nav.speed.ToString("0.00") +
            " m/s"
        );

        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Puedes cambiar la velocidad mientras " +
                "la arana trabaja.\n" +
                "Los cambios realizados en Play " +
                "son temporales.",
                MessageType.Info
            );
        }

        if (velocidadMostrada > 1.8f)
        {
            EditorGUILayout.HelpBox(
                "Las velocidades altas pueden estirar " +
                "demasiado las patas. Hay que validar " +
                "el movimiento procedural.",
                MessageType.Warning
            );
        }

        // =================================================
        // APLICAR CAMBIOS FUERA DE PLAY
        // =================================================

        EditorGUILayout.Space(10);

        bool cambiaEscala =
            Mathf.Abs(
                escalaObjetivo - escalaActual.x
            ) > 0.0001f;

        bool cambiaVelocidad =
            Mathf.Abs(
                velocidadObjetivo -
                config.velocidadMovimiento
            ) > 0.0001f ||
            Mathf.Abs(
                velocidadObjetivo -
                nav.speed
            ) > 0.0001f;

        using (new EditorGUI.DisabledScope(
            EditorApplication.isPlayingOrWillChangePlaymode ||
            (!cambiaEscala && !cambiaVelocidad)))
        {
            if (GUILayout.Button(
                "APLICAR TAMANO Y VELOCIDAD",
                GUILayout.Height(36)))
            {
                if (EditorUtility.DisplayDialog(
                    "Personalizar Arana",
                    "Escala: " +
                    escalaObjetivo.ToString("0.00") +
                    "x\nVelocidad: " +
                    velocidadObjetivo.ToString("0.00") +
                    " m/s",
                    "Aplicar",
                    "Cancelar"))
                {
                    Aplicar(
                        recolector,
                        nav,
                        config,
                        patas,
                        escalaActual.x,
                        escalaObjetivo,
                        velocidadObjetivo
                    );
                }
            }
        }

        EditorGUILayout.HelpBox(
            "Ctrl+Z permite deshacer cambios en Edit. " +
            "Despues comprueba patas, recogida, " +
            "entrega y escalada en Play.",
            MessageType.None
        );
    }

    // =====================================================
    // COMPROBAR ESCALA UNIFORME
    // =====================================================

    private static bool EsUniforme(Vector3 escala)
    {
        return
            Mathf.Abs(
                escala.x - escala.y
            ) < 0.0001f &&
            Mathf.Abs(
                escala.x - escala.z
            ) < 0.0001f;
    }

    // =====================================================
    // APLICAR CONFIGURACION
    // =====================================================

    private static void Aplicar(
        BotRecolector bot,
        NavMeshAgent nav,
        ConfiguracionBot config,
        PataProcedural[] patas,
        float escalaAnterior,
        float escalaNueva,
        float velocidadNueva)
    {
        float ratio =
            escalaNueva / escalaAnterior;

        bool cambiarEscala =
            Mathf.Abs(ratio - 1f) > 0.0001f;

        int grupo =
            Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Personalizar arana"
        );

        // =================================================
        // ESCALA
        // =================================================

        if (cambiarEscala)
        {
            Undo.RecordObject(
                bot.transform,
                "Escalar Bot"
            );

            bot.transform.localScale =
                Vector3.one * escalaNueva;

            GuardarCambio(bot.transform);

            // NavMeshAgent
            Undo.RecordObject(
                nav,
                "Escalar NavMeshAgent"
            );

            nav.radius *= ratio;
            nav.height *= ratio;
            nav.baseOffset *= ratio;
            nav.stoppingDistance *= ratio;
            nav.acceleration *= ratio;

            // IMPORTANTE:
            // No multiplicamos nav.speed por ratio.
            // Tamano y velocidad son independientes.

            GuardarCambio(nav);

            // Patas
            foreach (PataProcedural pata in patas)
            {
                Undo.RecordObject(
                    pata,
                    "Escalar parametros de pata"
                );

                pata.distanciaPaso *= ratio;
                pata.alturaPaso *= ratio;
                pata.alturaRaycast *= ratio;
                pata.distanciaRaycast *= ratio;
                pata.offsetSuelo *= ratio;
                pata.distanciaMaximaAnticipacion *= ratio;
                pata.distanciaMinimaPasoGiro *= ratio;

                GuardarCambio(pata);
            }

            // Distancias del BotRecolector
            Undo.RecordObject(
                bot,
                "Escalar distancias de trabajo"
            );

            SerializedObject so =
                new SerializedObject(bot);

            so.Update();

            foreach (string nombre in DistanciasBot)
            {
                SerializedProperty propiedad =
                    so.FindProperty(nombre);

                if (propiedad != null &&
                    propiedad.propertyType ==
                    SerializedPropertyType.Float)
                {
                    propiedad.floatValue *= ratio;
                }
            }

            so.ApplyModifiedProperties();
            GuardarCambio(bot);
        }

        // =================================================
        // VELOCIDAD INDEPENDIENTE
        // =================================================

        Undo.RecordObject(
            config,
            "Cambiar velocidad configurada"
        );

        config.velocidadMovimiento =
            velocidadNueva;

        GuardarCambio(config);

        Undo.RecordObject(
            nav,
            "Sincronizar Speed NavMeshAgent"
        );

        nav.speed = velocidadNueva;

        GuardarCambio(nav);

        // =================================================
        // GUARDAR ESCENA
        // =================================================

        if (bot.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(
                bot.gameObject.scene
            );
        }

        Undo.CollapseUndoOperations(grupo);

        Debug.Log(
            "[ESCALADOR ARANA] Escala: " +
            escalaAnterior.ToString("0.00") +
            "x -> " +
            escalaNueva.ToString("0.00") +
            "x | Velocidad: " +
            velocidadNueva.ToString("0.00") +
            " m/s"
        );
    }

    // =====================================================
    // GUARDAR CAMBIOS DE PREFAB
    // =====================================================

    private static void GuardarCambio(
        UnityEngine.Object objeto)
    {
        EditorUtility.SetDirty(objeto);

        if (PrefabUtility.IsPartOfPrefabInstance(objeto))
        {
            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    objeto
                );
        }
    }
}

