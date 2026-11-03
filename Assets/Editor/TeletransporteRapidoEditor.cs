
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class TeletransporteRapidoEditor : EditorWindow
{
    private GameObject objetivo;
    private float distancia = 3f;

    [MenuItem("Tools/PiedrasIncr/Teletransporte Rapido")]
    public static void Abrir()
    {
        GetWindow<TeletransporteRapidoEditor>(
            "Teletransporte Rapido"
        );
    }

    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            objetivo = Selection.activeGameObject;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(
            "TELETRANSPORTE DEL JUGADOR",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "En Play, selecciona un objeto de la " +
            "Hierarchy y pulsa Teletransportar.",
            MessageType.Info
        );

        objetivo = (GameObject)EditorGUILayout.ObjectField(
            "Objeto destino",
            objetivo,
            typeof(GameObject),
            true
        );

        distancia = EditorGUILayout.Slider(
            "Separacion",
            distancia,
            1.5f,
            10f
        );

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(
            !Application.isPlaying || objetivo == null))
        {
            if (GUILayout.Button(
                "TELETRANSPORTAR",
                GUILayout.Height(42)))
            {
                Teletransportar();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Activa Play para utilizar la herramienta.",
                MessageType.Warning
            );
        }
    }

    private void Teletransportar()
    {
        if (objetivo == null ||
            !objetivo.scene.IsValid() ||
            !objetivo.scene.isLoaded)
        {
            Debug.LogWarning(
                "[TELEPORT] Selecciona un objeto de la escena."
            );
            return;
        }

        MovimientoPersonaje jugador = null;
        int encontrados = 0;

        foreach (MovimientoPersonaje movimiento in
            Object.FindObjectsByType<MovimientoPersonaje>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None))
        {
            if (movimiento.gameObject.scene !=
                SceneManager.GetActiveScene())
                continue;

            jugador = movimiento;
            encontrados++;
        }

        if (encontrados != 1)
        {
            Debug.LogError(
                "[TELEPORT] Se esperaba un jugador. " +
                "Encontrados: " + encontrados
            );
            return;
        }

        CharacterController controlador =
            jugador.GetComponent<CharacterController>();

        RescateVacioJugador rescate =
            jugador.GetComponent<RescateVacioJugador>();

        if (controlador == null ||
            !controlador.enabled ||
            !jugador.enabled ||
            (rescate != null && rescate.RescateActivo))
        {
            Debug.LogWarning(
                "[TELEPORT] El jugador no esta " +
                "preparado o se encuentra en un rescate."
            );
            return;
        }

        if (objetivo == jugador.gameObject ||
            objetivo.transform.IsChildOf(jugador.transform))
        {
            Debug.LogWarning(
                "[TELEPORT] Selecciona otro objeto."
            );
            return;
        }

        Physics.SyncTransforms();

        Bounds limites = new Bounds(
            objetivo.transform.position,
            Vector3.zero
        );

        Renderer[] renderers =
            objetivo.GetComponentsInChildren<Renderer>();

        bool hayBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            if (!hayBounds)
            {
                limites = renderer.bounds;
                hayBounds = true;
            }
            else
            {
                limites.Encapsulate(renderer.bounds);
            }
        }

        if (!hayBounds)
        {
            Collider[] colliders =
                objetivo.GetComponentsInChildren<Collider>();

            foreach (Collider col in colliders)
            {
                if (!col.enabled)
                    continue;

                if (!hayBounds)
                {
                    limites = col.bounds;
                    hayBounds = true;
                }
                else
                {
                    limites.Encapsulate(col.bounds);
                }
            }
        }

        Vector3 centro = limites.center;

        float radioObjeto = Mathf.Max(
            limites.extents.x,
            limites.extents.z
        );

        float escalaY =
            Mathf.Abs(jugador.transform.lossyScale.y);

        float escalaHorizontal = Mathf.Max(
            Mathf.Abs(jugador.transform.lossyScale.x),
            Mathf.Abs(jugador.transform.lossyScale.z)
        );

        float radioJugador =
            controlador.radius * escalaHorizontal;

        float alturaJugador =
            controlador.height * escalaY;

        float separacionInicial =
            radioObjeto + distancia + radioJugador;

        Vector3 direccion =
            jugador.transform.position - centro;

        direccion.y = 0;

        if (direccion.sqrMagnitude < 0.001f)
            direccion = Vector3.forward;

        direccion.Normalize();

        int capasSuelo = rescate != null
            ? rescate.capasSuelo.value
            : Physics.DefaultRaycastLayers;

        // Probar distintas direcciones y distancias
        // hasta encontrar una posicion segura.
        for (int anillo = 0; anillo < 5; anillo++)
        {
            float radioBusqueda =
                separacionInicial + anillo * 2f;

            for (int i = 0; i < 12; i++)
            {
                Vector3 direccionPrueba =
                    Quaternion.Euler(0, i * 30f, 0) *
                    direccion;

                Vector3 punto =
                    centro + direccionPrueba * radioBusqueda;

                Vector3 origen = new Vector3(
                    punto.x,
                    Mathf.Max(
                        limites.max.y,
                        jugador.transform.position.y
                    ) + 40f,
                    punto.z
                );

                RaycastHit[] impactos =
                    Physics.RaycastAll(
                        origen,
                        Vector3.down,
                        500f,
                        capasSuelo,
                        QueryTriggerInteraction.Ignore
                    );

                System.Array.Sort(
                    impactos,
                    (a, b) =>
                        Mathf.Abs(a.point.y - limites.min.y)
                        .CompareTo(
                            Mathf.Abs(b.point.y - limites.min.y)
                        )
                );

                foreach (RaycastHit hit in impactos)
                {
                    if (hit.collider == null)
                        continue;

                    Transform superficie =
                        hit.collider.transform;

                    if (superficie == jugador.transform ||
                        superficie.IsChildOf(jugador.transform) ||
                        superficie == objetivo.transform ||
                        superficie.IsChildOf(objetivo.transform))
                        continue;

                    if (Vector3.Angle(
                        hit.normal,
                        Vector3.up
                    ) > controlador.slopeLimit)
                        continue;

                    float piesRespectoRaiz =
                        (controlador.height * 0.5f -
                         controlador.center.y) * escalaY;

                    Vector3 destino = new Vector3(
                        punto.x,
                        hit.point.y + piesRespectoRaiz + 0.12f,
                        punto.z
                    );

                    Vector3 centroCapsula =
                        destino +
                        jugador.transform.rotation *
                        Vector3.Scale(
                            controlador.center,
                            jugador.transform.lossyScale
                        );

                    float radioCapsula =
                        radioJugador * 0.95f;

                    float tramo =
                        Mathf.Max(
                            0f,
                            alturaJugador * 0.5f -
                            radioCapsula
                        );

                    Vector3 abajo =
                        centroCapsula +
                        Vector3.down * tramo;

                    Vector3 arriba =
                        centroCapsula +
                        Vector3.up * tramo;

                    bool bloqueado = false;

                    Collider[] obstaculos =
                        Physics.OverlapCapsule(
                            abajo,
                            arriba,
                            radioCapsula,
                            Physics.DefaultRaycastLayers,
                            QueryTriggerInteraction.Ignore
                        );

                    foreach (Collider obstaculo in obstaculos)
                    {
                        if (obstaculo == hit.collider ||
                            obstaculo.transform ==
                                jugador.transform ||
                            obstaculo.transform.IsChildOf(
                                jugador.transform))
                            continue;

                        bloqueado = true;
                        break;
                    }

                    if (bloqueado)
                        continue;

                    // Teletransporte seguro.
                    Vector3 posicionAnterior =
                        jugador.transform.position;

                    try
                    {
                        controlador.enabled = false;

                        jugador.transform.position = destino;

                        Physics.SyncTransforms();

                        jugador.RestablecerEstadoTrasCarga();

                        if (rescate != null)
                            rescate.RestablecerEstadoTrasCarga();

                        controlador.enabled = true;

                        Physics.SyncTransforms();

                        Debug.Log(
                            "[TELEPORT] OK: " +
                            objetivo.name +
                            " | Posicion: " + destino
                        );

                        return;
                    }
                    catch (System.Exception ex)
                    {
                        controlador.enabled = false;
                        jugador.transform.position =
                            posicionAnterior;

                        jugador.RestablecerEstadoTrasCarga();

                        if (rescate != null)
                            rescate.RestablecerEstadoTrasCarga();

                        controlador.enabled = true;
                        Physics.SyncTransforms();

                        Debug.LogError(
                            "[TELEPORT] ERROR: " + ex
                        );

                        return;
                    }
                }
            }
        }

        Debug.LogWarning(
            "[TELEPORT] No se encontro suelo libre " +
            "cerca de " + objetivo.name +
            ". Prueba con otra separacion."
        );
    }
}
