
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 5f;
    public float gravedad = -9.81f;
    public float fuerzaPegadoAlSuelo = -5f;

    [Header("Botas de Velocidad")]
    [SerializeField, Range(0, 3)]
    private int nivelBotas = 0;

    [SerializeField]
    private float[] multiplicadoresBotas =
    {
        1f, 1.15f, 1.30f, 1.50f
    };

    public int NivelBotas => nivelBotas;

    public float VelocidadActual =>
        velocidad * ObtenerMultiplicadorBotas();

    [Header("Jetpack")]
    [SerializeField, Range(0, 4)]
    private int nivelJetpack = 0;

    public KeyCode teclaSalto = KeyCode.Space;

    [Min(0.1f)]
    public float alturaSalto = 2f;

    public SistemaConstruccion sistemaConstruccion;

    [Header("Propulsion Jetpack")]
    [Min(0f)] public float aceleracionPropulsion = 25f;
    [Min(0f)] public float aceleracionNivel4 = 35f;
    [Min(0f)] public float velocidadVerticalMaxima = 10f;

    [Header("Combustible Jetpack")]
    [Min(0f)] public float combustibleNivel2 = 2f;
    [Min(0f)] public float combustibleNivel3 = 4f;
    [Min(0f)] public float combustibleNivel4 = 6f;
    [Min(0f)] public float velocidadRecarga = 2f;

    public int NivelJetpack => nivelJetpack;
    public float CombustibleActual => combustibleActual;
    public float CombustibleMaximo => ObtenerCombustibleMaximo();
    public bool EstaPropulsandose => estaPropulsandose;

    private float combustibleActual;
    private bool estaPropulsandose;

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

        if (sistemaConstruccion == null)
        {
            sistemaConstruccion =
                FindFirstObjectByType<SistemaConstruccion>();
        }

        combustibleActual = ObtenerCombustibleMaximo();

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

        if (Time.timeScale == 0f)
        {
            estaPropulsandose = false;
            return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direccion =
            transform.right * h +
            transform.forward * v;

        float velocidadMovimiento = VelocidadActual;

        if (modoRescate)
        {
            velocidadCaida = Vector3.zero;
            movimientoCinta = Vector3.zero;
            estaPropulsandose = false;

            if (permitirMovimientoEnRescate)
            {
                Vector3 horizontal =
                    Vector3.ProjectOnPlane(
                        direccion,
                        Vector3.up
                    ) * velocidadMovimiento;

                controller.Move(horizontal * Time.deltaTime);
            }

            ManejarSonidoPasos(0f, 0f);
            return;
        }

        DetectarSueloYCinta();

        if (tocandoSueloExtra)
        {
            direccion =
                Vector3.ProjectOnPlane(
                    direccion,
                    normalDelSuelo
                ).normalized * direccion.magnitude;
        }

        bool enSuelo = controller.isGrounded;

        if (enSuelo && velocidadCaida.y < 0f)
            velocidadCaida.y = fuerzaPegadoAlSuelo;

        ManejarSalto(enSuelo);
        ManejarPropulsion(enSuelo);

        velocidadCaida.y += gravedad * Time.deltaTime;

        Vector3 movimientoFinal =
            direccion * velocidadMovimiento +
            velocidadCaida +
            movimientoCinta;

        controller.Move(movimientoFinal * Time.deltaTime);

        ManejarSonidoPasos(h, v);
    }

    private bool PuedeUsarJetpack()
    {
        if (modoRescate)
            return false;

        if (sistemaConstruccion != null &&
            sistemaConstruccion.modoConstruccion)
            return false;

        return true;
    }

    private void ManejarSalto(bool enSuelo)
    {
        if (nivelJetpack < 1 || !PuedeUsarJetpack())
            return;

        if (!enSuelo)
            return;

        if (!Input.GetKeyDown(teclaSalto))
            return;

        if (gravedad >= 0f)
            return;

        velocidadCaida.y =
            Mathf.Sqrt(alturaSalto * -2f * gravedad);
    }

    private void ManejarPropulsion(bool enSuelo)
    {
        estaPropulsandose = false;

        if (nivelJetpack < 2)
            return;

        float combustibleMaximo = ObtenerCombustibleMaximo();

        if (enSuelo)
        {
            combustibleActual = Mathf.MoveTowards(
                combustibleActual,
                combustibleMaximo,
                velocidadRecarga * Time.deltaTime
            );
        }

        if (!PuedeUsarJetpack())
            return;

        if (enSuelo)
            return;

        if (!Input.GetKey(teclaSalto))
            return;

        if (combustibleActual <= 0f)
            return;

        float tiempoPropulsion = Mathf.Min(
            Time.deltaTime,
            combustibleActual
        );

        float aceleracion = nivelJetpack >= 4
            ? aceleracionNivel4
            : aceleracionPropulsion;

        velocidadCaida.y +=
            aceleracion * tiempoPropulsion;

        velocidadCaida.y = Mathf.Min(
            velocidadCaida.y,
            velocidadVerticalMaxima
        );

        combustibleActual -= tiempoPropulsion;
        combustibleActual = Mathf.Max(0f, combustibleActual);

        estaPropulsandose = true;
    }

    private float ObtenerCombustibleMaximo()
    {
        switch (nivelJetpack)
        {
            case 2:
                return combustibleNivel2;

            case 3:
                return combustibleNivel3;

            case 4:
                return combustibleNivel4;

            default:
                return 0f;
        }
    }

    public float ObtenerMultiplicadorBotas()
    {
        if (multiplicadoresBotas == null ||
            multiplicadoresBotas.Length == 0)
            return 1f;

        int indice = Mathf.Clamp(
            nivelBotas,
            0,
            multiplicadoresBotas.Length - 1
        );

        return Mathf.Max(
            0f,
            multiplicadoresBotas[indice]
        );
    }

    public void EstablecerNivelBotas(int nivel)
    {
        nivelBotas = Mathf.Clamp(nivel, 0, 3);
    }

    public bool MejorarBotas()
    {
        if (nivelBotas >= 3)
            return false;

        nivelBotas++;

        Debug.Log(
            "Botas nivel " + nivelBotas +
            " - Velocidad: " + VelocidadActual
        );

        return true;
    }

    public void EstablecerNivelJetpack(int nivel)
    {
        nivelJetpack = Mathf.Clamp(nivel, 0, 4);
        combustibleActual = ObtenerCombustibleMaximo();
        estaPropulsandose = false;
    }

    public bool MejorarJetpack()
    {
        if (nivelJetpack >= 4)
            return false;

        EstablecerNivelJetpack(nivelJetpack + 1);

        Debug.Log("Jetpack nivel " + nivelJetpack);
        return true;
    }

    private void DetectarSueloYCinta()
    {
        float radioEsfera = controller.radius * 0.9f;
        float distanciaRayo = (controller.height / 2f) + 0.3f;
        Vector3 origen = transform.position + controller.center;

        if (Physics.SphereCast(
            origen,
            radioEsfera,
            Vector3.down,
            out RaycastHit hit,
            distanciaRayo,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            tocandoSueloExtra = true;
            normalDelSuelo = hit.normal;
            puntoSueloDetectado = hit.point;
            colliderSueloDetectado = hit.collider;

            CintaTransportadora cinta =
                hit.collider.GetComponentInParent<CintaTransportadora>();

            if (cinta != null)
            {
                Vector3 direccionMundo =
                    cinta.DireccionEn(hit.point);

                movimientoCinta =
                    direccionMundo * cinta.velocidad;
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

    public void ComenzarAscensoRescate()
    {
        modoRescate = true;
        permitirMovimientoEnRescate = false;
        velocidadCaida = Vector3.zero;
        movimientoCinta = Vector3.zero;
        combustibleActual = ObtenerCombustibleMaximo();
        estaPropulsandose = false;
        tocandoSueloExtra = false;
        colliderSueloDetectado = null;
        normalDelSuelo = Vector3.up;
        ApagarPasos();
    }

    public void ComenzarLevitacionRescate()
    {
        modoRescate = true;
        permitirMovimientoEnRescate = true;
        velocidadCaida = Vector3.zero;
        movimientoCinta = Vector3.zero;
        estaPropulsandose = false;
        ApagarPasos();
    }

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
        combustibleActual = ObtenerCombustibleMaximo();
        estaPropulsandose = false;
        ApagarPasos();
    }

    public void TerminarLevitacionRescate()
    {
        modoRescate = false;
        permitirMovimientoEnRescate = false;
        velocidadCaida = Vector3.zero;
        movimientoCinta = Vector3.zero;
        estaPropulsandose = false;
    }

    private void ApagarPasos()
    {
        if (audioPasos == null)
            return;

        audioPasos.volume = 0f;

        if (audioPasos.isPlaying)
            audioPasos.Pause();
    }

    private void ManejarSonidoPasos(float h, float v)
    {
        bool pulsandoTeclas =
            Mathf.Abs(h) > 0.1f ||
            Mathf.Abs(v) > 0.1f;

        bool deberiaCaminar =
            pulsandoTeclas &&
            controller.isGrounded &&
            !modoRescate;

        if (sonidoPasos == null || audioPasos == null)
            return;

        if (deberiaCaminar)
        {
            if (!audioPasos.isPlaying)
                audioPasos.Play();

            audioPasos.volume = Mathf.Lerp(
                audioPasos.volume,
                volumenPasos,
                velocidadFade * Time.deltaTime
            );
        }
        else
        {
            audioPasos.volume = Mathf.Lerp(
                audioPasos.volume,
                0f,
                velocidadFade * Time.deltaTime
            );

            if (audioPasos.volume < 0.01f &&
                audioPasos.isPlaying)
            {
                audioPasos.volume = 0f;
                audioPasos.Pause();
            }
        }
    }
}
