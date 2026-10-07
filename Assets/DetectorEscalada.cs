using UnityEngine;
using UnityEngine.AI;

public class DetectorEscalada : MonoBehaviour
{
    // =====================================================
    // DETECCIÓN
    // =====================================================

    [Header("Detección de pared")]

    public LayerMask capaEscalable;

    public float distanciaDeteccion = 1.5f;

    public float alturaOrigen = 0.45f;


    // =====================================================
    // INICIO DE ESCALADA
    // =====================================================

    [Header("Inicio de escalada")]

    [Tooltip(
        "Distancia REAL desde el raycast hasta la pared " +
        "a la que consideramos que el bot ya está preparado."
    )]
    public float distanciaPararAntePared = 0.60f;

    public bool detenerAntePared = true;


    // =====================================================
    // DEBUG
    // =====================================================

    [Header("Debug")]

    public bool mostrarDebug = true;

    public bool mostrarMensajes = false;


    // =====================================================
    // INFORMACIÓN
    // =====================================================

    public bool HayParedEscalable
    {
        get;
        private set;
    }


    public bool EstaFrenteAPared
    {
        get;
        private set;
    }


    public Vector3 PuntoPared
    {
        get;
        private set;
    }


    public Vector3 NormalPared
    {
        get;
        private set;
    }


    public float DistanciaActualPared
    {
        get;
        private set;
    }


    // =====================================================
    // INTERNAS
    // =====================================================

    private NavMeshAgent agente;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        agente =
            GetComponent<NavMeshAgent>();
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        DetectarPared();
    }


    // =====================================================
    // DETECTAR
    // =====================================================

    private void DetectarPared()
    {
        Vector3 origen =
            transform.position +
            transform.up *
            alturaOrigen;


        Vector3 direccion =
            transform.forward.normalized;


        if (Physics.Raycast(
                origen,
                direccion,
                out RaycastHit hit,
                distanciaDeteccion,
                capaEscalable,
                QueryTriggerInteraction.Ignore))
        {
            HayParedEscalable =
                true;


            PuntoPared =
                hit.point;


            NormalPared =
                hit.normal.normalized;


            // =============================================
            // CAMBIO IMPORTANTE
            // =============================================
            //
            // Antes:
            //
            // Vector3.Distance(transform.position, hit.point)
            //
            // Eso incluía también la diferencia vertical.
            //
            // Ahora usamos la distancia REAL del raycast.
            // =============================================

            DistanciaActualPared =
                hit.distance;


            EstaFrenteAPared =
                hit.distance <=
                distanciaPararAntePared;


            // =============================================
            // DETENER
            // =============================================

            if (detenerAntePared &&
                EstaFrenteAPared)
            {
                if (agente != null &&
                    agente.enabled &&
                    agente.isOnNavMesh)
                {
                    agente.isStopped =
                        true;
                }
            }


            // =============================================
            // DEBUG
            // =============================================

            if (mostrarDebug)
            {
                Debug.DrawLine(
                    origen,
                    hit.point,
                    EstaFrenteAPared
                        ? Color.yellow
                        : Color.green
                );


                Debug.DrawRay(
                    hit.point,
                    hit.normal *
                    0.7f,
                    Color.cyan
                );
            }


            if (mostrarMensajes)
            {
                Debug.Log(
                    name +
                    " | Pared: " +
                    hit.collider.name +
                    " | Distancia REAL: " +
                    hit.distance.ToString("0.00") +
                    " | Preparado: " +
                    EstaFrenteAPared
                );
            }
        }
        else
        {
            HayParedEscalable =
                false;


            EstaFrenteAPared =
                false;


            PuntoPared =
                Vector3.zero;


            NormalPared =
                Vector3.zero;


            DistanciaActualPared =
                Mathf.Infinity;


            if (mostrarDebug)
            {
                Debug.DrawRay(
                    origen,
                    direccion *
                    distanciaDeteccion,
                    Color.red
                );
            }
        }
    }


    // =====================================================
    // REANUDAR
    // =====================================================

    public void ReanudarMovimiento()
    {
        if (agente == null ||
            !agente.enabled ||
            !agente.isOnNavMesh)
        {
            return;
        }


        agente.isStopped =
            false;
    }


    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        if (!mostrarDebug)
        {
            return;
        }


        Vector3 origen =
            transform.position +
            transform.up *
            alturaOrigen;


        Vector3 direccion =
            transform.forward.normalized;


        Gizmos.color =
            Color.red;


        Gizmos.DrawLine(
            origen,
            origen +
            direccion *
            distanciaDeteccion
        );


        // =============================================
        // MARCAR DISTANCIA DE ACTIVACIÓN
        // =============================================

        Gizmos.color =
            Color.yellow;


        Vector3 puntoActivacion =
            origen +
            direccion *
            distanciaPararAntePared;


        Gizmos.DrawWireSphere(
            puntoActivacion,
            0.08f
        );


        if (!Application.isPlaying ||
            !HayParedEscalable)
        {
            return;
        }


        Gizmos.color =
            EstaFrenteAPared
                ? Color.yellow
                : Color.green;


        Gizmos.DrawSphere(
            PuntoPared,
            0.07f
        );


        Gizmos.color =
            Color.cyan;


        Gizmos.DrawLine(
            PuntoPared,
            PuntoPared +
            NormalPared *
            0.7f
        );
    }
}