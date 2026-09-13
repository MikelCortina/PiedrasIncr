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

    // --- NUEVAS VARIABLES PARA RAMPAS ---
    private Vector3 normalDelSuelo = Vector3.up;
    private bool tocandoSueloExtra = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        audioPasos = gameObject.AddComponent<AudioSource>();
        audioPasos.clip = sonidoPasos;
        audioPasos.volume = 0f;
        audioPasos.loop = true;
        audioPasos.spatialBlend = 0f;
        audioPasos.playOnAwake = false;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direccion = transform.right * h + transform.forward * v;

        // 1. Escaneamos el suelo (Detecta la cinta y el ángulo de la rampa)
        DetectarSueloYCinta();

        // 2. --- LA MAGIA CONTRA EL TEMBLEQUE ---
        // Si estamos en el suelo, inclinamos la dirección del movimiento para que sea paralela a la rampa
        if (tocandoSueloExtra)
        {
            direccion = Vector3.ProjectOnPlane(direccion, normalDelSuelo).normalized * direccion.magnitude;
        }

        // 3. Aplicamos la gravedad
        if (controller.isGrounded && velocidadCaida.y < 0)
        {
            // Usamos una fuerza mayor a -2f para obligarle a pegarse a la rampa de Blender
            velocidadCaida.y = fuerzaPegadoAlSuelo;
        }
        velocidadCaida.y += gravedad * Time.deltaTime;

        // 4. Movimiento Final
        Vector3 movimientoFinal = (direccion * velocidad) + velocidadCaida + movimientoCinta;
        controller.Move(movimientoFinal * Time.deltaTime);

        // 5. Sistema de pasos
        ManejarSonidoPasos(h, v);
    }

    // --- ESCÁNER MEJORADO (SphereCast) ---
    void DetectarSueloYCinta()
    {
        float radioEsfera = controller.radius * 0.9f;
        float distanciaRayo = (controller.height / 2f) + 0.3f;
        Vector3 origen = transform.position + controller.center;

        // Usamos SphereCast en lugar de Raycast. Es "gordito" y no falla en mallas de Blender.
        if (Physics.SphereCast(origen, radioEsfera, Vector3.down, out RaycastHit hit, distanciaRayo))
        {
            tocandoSueloExtra = true;
            normalDelSuelo = hit.normal; // Guardamos el ángulo de la rampa

            // Comprobamos la cinta
            CintaTransportadora cinta = hit.collider.GetComponentInParent<CintaTransportadora>();
            if (cinta != null)
            {
                Vector3 direccionMundo = cinta.transform.TransformDirection(cinta.direccionLocal).normalized;
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
            movimientoCinta = Vector3.zero;
        }
    }

    void ManejarSonidoPasos(float h, float v)
    {
        bool pulsandoTeclas = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);
        bool deberiaCaminar = pulsandoTeclas && controller.isGrounded;

        if (sonidoPasos != null)
        {
            if (deberiaCaminar)
            {
                if (!audioPasos.isPlaying) audioPasos.Play();
                audioPasos.volume = Mathf.Lerp(audioPasos.volume, volumenPasos, velocidadFade * Time.deltaTime);
            }
            else
            {
                audioPasos.volume = Mathf.Lerp(audioPasos.volume, 0f, velocidadFade * Time.deltaTime);
                if (audioPasos.volume < 0.01f && audioPasos.isPlaying)
                {
                    audioPasos.volume = 0f;
                    audioPasos.Pause();
                }
            }
        }
    }
}