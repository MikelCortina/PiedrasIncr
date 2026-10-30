using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 5f;
    public float gravedad = -9.81f;
    [Tooltip("Fuerza con la que se pega al suelo al bajar rampas")]
    public float fuerzaPegadoAlSuelo = -5f;

    [Header("Sonido de Pasos")]
    public AudioClip sonidoPasos;
    [Range(0f, 1f)] public float volumenPasos = 0.5f;
    public float velocidadFade = 15f;

    private CharacterController controller;
    private Vector3 velocidadCaida;
    private AudioSource audioPasos;
    private Vector3 movimientoCinta = Vector3.zero;

    private Vector3 normalDelSuelo = Vector3.up;
    private bool tocandoSueloExtra = false;
    private Vector3 puntoSueloDetectado;
    private Collider colliderSueloDetectado;

    // Ascenso: el rescate anima la posición y bloquea WASD.
    // Levitación: WASD libre horizontal, pero sin gravedad ni cinta.
    private bool modoRescate = false;
    private bool permitirMovimientoEnRescate = false;

    public bool EstaLevitandoPorRescate => modoRescate;
    public bool SueloDetectado => tocandoSueloExtra;
    public Vector3 NormalSueloDetectado => normalDelSuelo;
    public Vector3 PuntoSueloDetectado => puntoSueloDetectado;
    public Collider ColliderSueloDetectado => colliderSueloDetectado;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        audioPasos = gameObject.AddComponent<AudioSource>();
        audioPasos.clip = sonidoPasos;
        audioPasos.volume = 0f;
        audioPasos.loop = true;
        audioPasos.spatialBlend = 0f;
        audioPasos.playOnAwake = false;
    }

    private void Update()
    {
        if (controller == null || !controller.enabled)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 direccion = transform.right * h + transform.forward * v;

        if (modoRescate)
        {
            velocidadCaida = Vector3.zero;
            movimientoCinta = Vector3.zero;

            if (permitirMovimientoEnRescate)
            {
                // La levitación conserva la altura. WASD permite volver al mapa.
                Vector3 horizontal = Vector3.ProjectOnPlane(direccion, Vector3.up) * velocidad;
                controller.Move(horizontal * Time.deltaTime);
            }

            ManejarSonidoPasos(0f, 0f);
            return;
        }

        // Se conserva el sistema original de detección de rampa y cinta.
        DetectarSueloYCinta();
        if (tocandoSueloExtra)
        {
            direccion = Vector3.ProjectOnPlane(direccion, normalDelSuelo).normalized
                        * direccion.magnitude;
        }

        if (controller.isGrounded && velocidadCaida.y < 0f)
            velocidadCaida.y = fuerzaPegadoAlSuelo;
        velocidadCaida.y += gravedad * Time.deltaTime;

        Vector3 movimientoFinal = direccion * velocidad + velocidadCaida + movimientoCinta;
        controller.Move(movimientoFinal * Time.deltaTime);
        ManejarSonidoPasos(h, v);
    }

    private void DetectarSueloYCinta()
    {
        float radioEsfera = controller.radius * 0.9f;
        float distanciaRayo = (controller.height / 2f) + 0.3f;
        Vector3 origen = transform.position + controller.center;

        if (Physics.SphereCast(origen, radioEsfera, Vector3.down, out RaycastHit hit,
                distanciaRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            tocandoSueloExtra = true;
            normalDelSuelo = hit.normal;
            puntoSueloDetectado = hit.point;
            colliderSueloDetectado = hit.collider;

            CintaTransportadora cinta = hit.collider.GetComponentInParent<CintaTransportadora>();
            if (cinta != null)
            {
                Vector3 direccionMundo =
                    cinta.transform.TransformDirection(cinta.direccionLocal).normalized;
                movimientoCinta = direccionMundo * cinta.velocidad;
            }
            else
            {
                movimientoCinta = Vector3.zero;
            }
        }
        else
        {
            tocandoSueloExtra = false;
            normalDelSuelo = Vector3.up;
            colliderSueloDetectado = null;
            movimientoCinta = Vector3.zero;
        }
    }

    // Llamado al comenzar la subida ANIMADA. NO cambia transform.position.
    public void ComenzarAscensoRescate()
    {
        modoRescate = true;
        permitirMovimientoEnRescate = false;
        velocidadCaida = Vector3.zero;
        movimientoCinta = Vector3.zero;
        tocandoSueloExtra = false;
        colliderSueloDetectado = null;
        normalDelSuelo = Vector3.up;
        ApagarPasos();
    }

    // Llamado al terminar la subida: ahora WASD sí puede mover al jugador.
    public void ComenzarLevitacionRescate()
    {
        modoRescate = true;
        permitirMovimientoEnRescate = true;
        velocidadCaida = Vector3.zero;
        movimientoCinta = Vector3.zero;
        ApagarPasos();
    }


    /// <summary>
    /// Limpia velocidades, suelo y modos especiales despues de recolocar al
    /// jugador desde un guardado. No cambia su posicion ni su orientacion.
    /// </summary>
    public void RestablecerEstadoTrasCarga()
    {
        modoRescate = false;
        permitirMovimientoEnRescate = false;
        velocidadCaida = Vector3.zero;
        movimientoCinta = Vector3.zero;
        normalDelSuelo = Vector3.up;
        tocandoSueloExtra = false;
        puntoSueloDetectado = Vector3.zero;
        colliderSueloDetectado = null;
        ApagarPasos();
    }

    // Solo se llama cuando se ha pulsado la tecla de aterrizar sobre suelo.
    public void TerminarLevitacionRescate()
    {
        modoRescate = false;
        permitirMovimientoEnRescate = false;
        velocidadCaida = Vector3.zero;
        movimientoCinta = Vector3.zero;
    }

    private void ApagarPasos()
    {
        if (audioPasos == null) return;
        audioPasos.volume = 0f;
        if (audioPasos.isPlaying) audioPasos.Pause();
    }

    private void ManejarSonidoPasos(float h, float v)
    {
        bool pulsandoTeclas = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        bool deberiaCaminar = pulsandoTeclas && controller.isGrounded && !modoRescate;

        if (sonidoPasos != null && audioPasos != null)
        {
            if (deberiaCaminar)
            {
                if (!audioPasos.isPlaying) audioPasos.Play();
                audioPasos.volume = Mathf.Lerp(audioPasos.volume, volumenPasos,
                    velocidadFade * Time.deltaTime);
            }
            else
            {
                audioPasos.volume = Mathf.Lerp(audioPasos.volume, 0f,
                    velocidadFade * Time.deltaTime);
                if (audioPasos.volume < 0.01f && audioPasos.isPlaying)
                {
                    audioPasos.volume = 0f;
                    audioPasos.Pause();
                }
            }
        }
    }
}
