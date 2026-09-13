using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwordPhysicsController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camara;

    [SerializeField] private Transform puntoFuerzaHorizontal;

    [SerializeField] private Transform puntoFuerzaVertical;

    [Header("Dirección actual de la espada")]
    [Tooltip("El eje Z debe apuntar desde la empuñadura hacia la punta.")]
    [SerializeField] private Transform direccionEspada;

    [Header("Eje de giro del filo")]
    [Tooltip("El eje Z representa el eje sobre el que gira la espada.")]
    [SerializeField] private Transform ejeGiroFilo;

    [Header("Carga del impulso con Espacio")]
    [SerializeField] private float tiempoMaximoCarga = 0.5f;

    [Header("Fuerza horizontal")]
    [SerializeField] private float fuerzaHorizontalMinima = 10f;

    [SerializeField] private float fuerzaHorizontalMaxima = 50f;

    [Header("Fuerza vertical")]
    [SerializeField] private float fuerzaVerticalMinima = 10f;

    [SerializeField] private float fuerzaVerticalMaxima = 50f;

    [Header("Velocidad del dash")]
    [SerializeField] private float fuerzaDashNormal = 50f;

    [SerializeField] private float fuerzaDashEnemigo = 15f;

    [SerializeField] private string tagEnemigo = "Enemie";

    [Tooltip("El dash elimina la velocidad y rotación anteriores.")]
    [SerializeField] private bool ignorarInerciaAlHacerDash = true;

    [Header("Lanzamiento con E")]
    [Tooltip("Impulso aplicado en la dirección de la punta.")]
    [SerializeField] private float fuerzaLanzamiento = 50f;

    [Tooltip("Giro aplicado sobre el eje Z de la espada.")]
    [SerializeField] private float torqueLanzamiento = 35f;

    [Tooltip("Si está activo, elimina la velocidad antes de lanzar.")]
    [SerializeField] private bool ignorarInerciaAlLanzar = true;

    [Tooltip("Si está activo, elimina la rotación antes de lanzar.")]
    [SerializeField] private bool ignorarRotacionAlLanzar = true;

    [Tooltip("Aplica el impulso en el punto de la punta.")]
    [SerializeField] private bool aplicarLanzamientoEnLaPunta = true;

    [Header("Reinicio de inercia del impulso")]
    [SerializeField] private bool ignorarVelocidadAnterior = true;

    [SerializeField] private bool ignorarRotacionAnterior = true;

    [Header("Física")]
    [SerializeField] private float amortiguacionAngular = 3f;

    [SerializeField] private float amortiguacionLineal = 0.1f;

    [SerializeField] private float velocidadAngularMaxima = 15f;

    [SerializeField] private float velocidadAngularMinima = 0.05f;

    [SerializeField] private float distanciaMaximaApuntado = 100f;

    [SerializeField]
    private LayerMask layersImpactables;

    [SerializeField]
    private QueryTriggerInteraction detectarTriggers =
        QueryTriggerInteraction.Ignore;

    private Rigidbody rb;

    // Impulso cargado con Espacio.
    private bool cargandoFuerza;
    private bool aplicarFuerzaPendiente;
    private float tiempoCarga;

    private float fuerzaHorizontalPendiente;
    private float fuerzaVerticalPendiente;


    // Lanzamiento con E.
    private bool lanzamientoPendiente;

    // Conserva el sentido de giro del dash.
    private float ultimoSentidoGiro = 1f;

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();

        if (camara == null)
        {
            camara =
                Camera.main;
        }

        ConfigurarFisica();
        ComprobarReferencias();
    }

    private void Update()
    {
        ControlarCargaConEspacio();
        DetectarLanzamiento();
    }

    private void FixedUpdate()
    {
        if (aplicarFuerzaPendiente)
        {
            aplicarFuerzaPendiente =
                false;

            AplicarImpulsoCargado(
                fuerzaHorizontalPendiente,
                fuerzaVerticalPendiente
            );
        }


        if (lanzamientoPendiente)
        {
            lanzamientoPendiente =
                false;

            AplicarLanzamiento();
        }

        DetenerRotacionPequena();
    }

    private void ControlarCargaConEspacio()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cargandoFuerza =
                true;

            tiempoCarga =
                0f;
        }

        if (cargandoFuerza &&
            Input.GetKey(KeyCode.Space))
        {
            tiempoCarga +=
                Time.deltaTime;

            tiempoCarga =
                Mathf.Clamp(
                    tiempoCarga,
                    0f,
                    Mathf.Max(
                        0.01f,
                        tiempoMaximoCarga
                    )
                );
        }

        if (cargandoFuerza &&
            Input.GetKeyUp(KeyCode.Space))
        {
            cargandoFuerza =
                false;

            float tiempoCargaSeguro =
                Mathf.Max(
                    0.01f,
                    tiempoMaximoCarga
                );

            float porcentajeCarga =
                Mathf.Clamp01(
                    tiempoCarga /
                    tiempoCargaSeguro
                );

            fuerzaHorizontalPendiente =
                Mathf.Lerp(
                    fuerzaHorizontalMinima,
                    fuerzaHorizontalMaxima,
                    porcentajeCarga
                );

            fuerzaVerticalPendiente =
                Mathf.Lerp(
                    fuerzaVerticalMinima,
                    fuerzaVerticalMaxima,
                    porcentajeCarga
                );

            aplicarFuerzaPendiente =
                true;
        }
    }



    private void DetectarLanzamiento()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lanzamientoPendiente =
                true;
        }
    }

    private void AplicarImpulsoCargado(
        float fuerzaHorizontal,
        float fuerzaVertical)
    {
        if (camara == null ||
            puntoFuerzaHorizontal == null ||
            puntoFuerzaVertical == null)
        {
            return;
        }

        if (ignorarVelocidadAnterior)
        {
            PonerVelocidadLinealEnCero();
        }

        if (ignorarRotacionAnterior)
        {
            rb.angularVelocity =
                Vector3.zero;
        }

        Vector3 direccionMirada =
            camara.transform.forward.normalized;

        Vector3 direccionHorizontal =
            Vector3.ProjectOnPlane(
                direccionMirada,
                Vector3.up
            );

        if (direccionHorizontal.sqrMagnitude >
            0.001f)
        {
            direccionHorizontal.Normalize();

            rb.AddForceAtPosition(
                direccionHorizontal *
                fuerzaHorizontal,
                puntoFuerzaHorizontal.position,
                ForceMode.Impulse
            );
        }

        float componenteVertical =
            direccionMirada.y;

        Vector3 fuerzaVerticalAplicada =
            Vector3.up *
            componenteVertical *
            fuerzaVertical;

        rb.AddForceAtPosition(
            fuerzaVerticalAplicada,
            puntoFuerzaVertical.position,
            ForceMode.Impulse
        );
    }


    private void AplicarLanzamiento()
    {
        if (camara == null)
        {
            camara = Camera.main;
        }

        if (camara == null)
        {
            Debug.LogWarning(
                "No se encontró una cámara para el lanzamiento."
            );

            return;
        }

        Ray rayoCentro =
            camara.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        Vector3 puntoObjetivo;

        bool haImpactado = Physics.Raycast(
            rayoCentro,
            out RaycastHit impacto,
            distanciaMaximaApuntado,
            layersImpactables,
            detectarTriggers
        );

        bool vaAChocarConEnemigo = false;

        if (haImpactado)
        {
            puntoObjetivo = impacto.point;

            // Comprueba el collider impactado y sus padres.
            vaAChocarConEnemigo =
                impacto.collider.CompareTag(tagEnemigo) ||
                impacto.collider.transform.root.CompareTag(tagEnemigo);
        }
        else
        {
            puntoObjetivo =
                rayoCentro.origin +
                rayoCentro.direction *
                distanciaMaximaApuntado;
        }

        Vector3 direccionLanzamiento =
            puntoObjetivo -
            rb.worldCenterOfMass;

        if (direccionLanzamiento.sqrMagnitude < 0.001f)
        {
            return;
        }

        direccionLanzamiento.Normalize();

        if (ignorarInerciaAlLanzar)
        {
            PonerVelocidadLinealEnCero();
        }

        if (ignorarRotacionAlLanzar)
        {
            rb.angularVelocity = Vector3.zero;
        }

        float fuerzaDash =
            vaAChocarConEnemigo
                ? fuerzaDashEnemigo
                : fuerzaDashNormal;

        if (aplicarLanzamientoEnLaPunta &&
            ejeGiroFilo != null)
        {
            rb.AddForceAtPosition(
                direccionLanzamiento * fuerzaDash,
                ejeGiroFilo.position,
                ForceMode.Impulse
            );
        }
        else
        {
            rb.AddForce(
                direccionLanzamiento * fuerzaDash,
                ForceMode.Impulse
            );
        }



        Debug.Log(
            vaAChocarConEnemigo
                ? "Dash hacia enemigo: fuerza reducida."
                : "Dash normal: no se ha detectado un enemigo."
        );
    }
    private void PonerVelocidadLinealEnCero()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity =
            Vector3.zero;
#else
        rb.velocity =
            Vector3.zero;
#endif
    }

    private void ConfigurarFisica()
    {
        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.maxAngularVelocity =
            velocidadAngularMaxima;

#if UNITY_6000_0_OR_NEWER
        rb.angularDamping =
            amortiguacionAngular;

        rb.linearDamping =
            amortiguacionLineal;
#else
        rb.angularDrag =
            amortiguacionAngular;

        rb.drag =
            amortiguacionLineal;
#endif
    }

    private void DetenerRotacionPequena()
    {
        float velocidadMinimaCuadrada =
            velocidadAngularMinima *
            velocidadAngularMinima;

        if (rb.angularVelocity.sqrMagnitude <
            velocidadMinimaCuadrada)
        {
            rb.angularVelocity =
                Vector3.zero;
        }
    }

    private void ComprobarReferencias()
    {
        if (camara == null)
        {
            Debug.LogWarning(
                "No se ha asignado ninguna cámara.",
                this
            );
        }

        if (puntoFuerzaHorizontal == null)
        {
            Debug.LogWarning(
                "No se ha asignado el punto de fuerza horizontal.",
                this
            );
        }

        if (puntoFuerzaVertical == null)
        {
            Debug.LogWarning(
                "No se ha asignado el punto de fuerza vertical.",
                this
            );
        }

        if (direccionEspada == null)
        {
            Debug.LogWarning(
                "No se ha asignado Dirección Espada. " +
                "Se usará el transform principal.",
                this
            );
        }

        if (ejeGiroFilo == null)
        {
            Debug.LogWarning(
                "No se ha asignado Eje Giro Filo. " +
                "El lanzamiento usará el centro de la espada.",
                this
            );
        }
    }
}


