using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boquilla de la manguera: se agarra mirándola y MANTENIENDO el clic derecho (como las piedras);
/// al soltar el clic se enchufa si está junto a la toma de la nave; si no, la manguera se recoge
/// sola hasta su sitio inicial (así nunca se queda tirada ni cae al vacío). Se puede volver a agarrar mientras vuelve.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AgarrarManguera : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camaraJugador;
    public Transform puntoAgarre;

    [Header("Interaccion")]
    [Tooltip("Botón del ratón que hay que mantener (0 izquierdo, 1 derecho, 2 central).")]
    public int botonAgarrar = 1;
    [Tooltip("Distancia máxima desde la cámara para agarrarla.")]
    public float distanciaMaximaAgarrar = 8f;
    [Tooltip("Margen al apuntar: no hace falta acertar justo en la boquilla.")]
    public float radioApuntado = 0.5f;

    [Header("Recogida al soltar")]
    [Tooltip("Metros por segundo a los que la boquilla vuelve a su sitio inicial al soltarla.")]
    [Min(0.1f)] public float velocidadRecogida = 5f;
    [Tooltip("Altura máxima del arco que hace al volver (para no arrastrarse por el suelo).")]
    [Min(0f)] public float alturaArcoRecogida = 1f;
    [Tooltip("Si se cae (p. ej. por un borde) más de estos metros por debajo de su sitio inicial, se recoge sola.")]
    [Min(0.5f)] public float caidaMaximaPermitida = 6f;

    [Header("Movimiento fisico")]
    public float fuerzaSeguimiento = 350f;
    public float amortiguacionSeguimiento = 45f;
    public float fuerzaMaxima = 500f;

    [Header("Conexion con la nave")]
    public Transform puntoConexionNave;
    public float distanciaConexion = 1.5f;

    [Header("Cambio al conectar")]
    [Tooltip("Objeto que se activa al conectar la manguera (ej: una manguera estática ya enchufada)")]
    public GameObject objetoActivarAlConectar;
    [Tooltip("Objetos que se desactivan al conectar. Vacío = la manguera física entera (su MangueraSistema, con el tubo, y esta boquilla)")]
    public GameObject[] objetosDesactivarAlConectar;
    [Tooltip("Segundos tras conectar antes de hacer el cambio (0 = en el mismo instante)")]
    [Min(0f)] public float retrasoCambioAlConectar = 0f;
    [Tooltip("Desactiva también esta boquilla al conectar, aunque no esté en la lista de objetos a desactivar")]
    public bool desactivarBoquillaAlConectar = true;

    /// <summary>Se lanza al enchufar la manguera a la nave.</summary>
    public static event System.Action OnMangueraConectada;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEventos() { OnMangueraConectada = null; }

    public bool Conectada => conectada;
    public bool Agarrada => agarrada;

    // Para que AgarreLanzamiento no coja una piedra con el mismo clic.
    private static readonly List<AgarrarManguera> instancias = new List<AgarrarManguera>();

    /// <summary>Alguna manguera está en la mano o la cámara la está apuntando (el clic derecho es para ella).</summary>
    public static bool OcupaElClic(Ray rayo)
    {
        foreach (AgarrarManguera m in instancias)
            if (m != null && (m.agarrada || m.EstaApuntada(rayo)))
                return true;
        return false;
    }

    private Rigidbody rb;
    private bool agarrada = false;
    private bool conectada = false;
    private bool volviendo = false;
    private Coroutine rutinaRecogida;
    private bool eraCinematicoAntesDeRecoger;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private GestorEquipamiento gestorEquipamiento;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Sitio al que vuelve al soltarla (en el espacio del padre, por si el surtidor se mueve).
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
    }

    void OnEnable() { instancias.Add(this); }
    void OnDisable() { instancias.Remove(this); }

    void Start()
    {
        gestorEquipamiento = FindFirstObjectByType<GestorEquipamiento>();
    }

    void Update()
    {
        if (conectada)
            return;

        if (agarrada)
        {
            // Al soltar el clic: si estamos junto a la toma de combustible se enchufa; si no, se suelta.
            if (!Input.GetMouseButton(botonAgarrar))
            {
                if (puntoConexionNave != null &&
                    Vector3.Distance(transform.position, puntoConexionNave.position) <= distanciaConexion)
                    ConectarANave();
                else
                    Soltar();
            }
            return;
        }

        if (!Input.GetMouseButtonDown(botonAgarrar) || camaraJugador == null || puntoAgarre == null)
            return;

        // Igual que las piedras: con una herramienta en la mano no se agarra.
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return;

        Camera cam = Camera.main;
        Ray rayo = cam != null
            ? cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f))
            : new Ray(camaraJugador.position, camaraJugador.forward);

        if (EstaApuntada(rayo))
            Agarrar();
    }

    // La cámara mira a la boquilla y está a distancia de agarre.
    bool EstaApuntada(Ray rayo)
    {
        if (conectada || rb == null) return false;
        if (camaraJugador != null && Vector3.Distance(camaraJugador.position, rb.position) > distanciaMaximaAgarrar) return false;

        RaycastHit[] impactos = Physics.SphereCastAll(rayo, radioApuntado, distanciaMaximaAgarrar,
                                                      Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit impacto in impactos)
            if (impacto.collider != null && impacto.collider.attachedRigidbody == rb)
                return true;
        return false;
    }

    void FixedUpdate()
    {
        if (conectada || volviendo)
            return;

        // Se ha caído por un borde (o algo la ha empujado lejos hacia abajo): se recoge.
        if (!agarrada && rb.position.y < PosicionInicialMundo().y - caidaMaximaPermitida)
        {
            EmpezarRecogida();
            return;
        }

        if (!agarrada || puntoAgarre == null)
            return;

        Vector3 diferencia = puntoAgarre.position - rb.position;

        Vector3 fuerza = diferencia * fuerzaSeguimiento;

        fuerza -= rb.linearVelocity * amortiguacionSeguimiento;

        fuerza = Vector3.ClampMagnitude(
            fuerza,
            fuerzaMaxima
        );

        rb.AddForce(fuerza, ForceMode.Force);
    }

    void Agarrar()
    {
        // Si se estaba recogiendo, se corta y vuelve a ser física.
        PararRecogida();
        agarrada = true;
        rb.WakeUp();
    }

    void Soltar()
    {
        agarrada = false;
        EmpezarRecogida();
    }

    Vector3 PosicionInicialMundo()
    {
        return transform.parent != null ? transform.parent.TransformPoint(posicionInicial) : posicionInicial;
    }

    Quaternion RotacionInicialMundo()
    {
        return transform.parent != null ? transform.parent.rotation * rotacionInicial : rotacionInicial;
    }

    void EmpezarRecogida()
    {
        if (volviendo || conectada) return;
        rutinaRecogida = StartCoroutine(Recoger());
    }

    void PararRecogida()
    {
        if (!volviendo) return;
        if (rutinaRecogida != null) StopCoroutine(rutinaRecogida);
        rutinaRecogida = null;
        volviendo = false;
        rb.isKinematic = eraCinematicoAntesDeRecoger;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // La boquilla vuelve a su sitio inicial a velocidad constante, con un pequeño arco,
    // y la manguera se va recogiendo en el carrete (MangueraFisica recoge lo que sobra).
    IEnumerator Recoger()
    {
        volviendo = true;

        eraCinematicoAntesDeRecoger = rb.isKinematic;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Vector3 desde = rb.position;
        Quaternion rotacionDesde = rb.rotation;
        float distancia = Vector3.Distance(desde, PosicionInicialMundo());
        float duracion = Mathf.Max(0.15f, distancia / velocidadRecogida);
        float arco = Mathf.Min(alturaArcoRecogida, distancia * 0.25f);

        for (float t = 0f; t < duracion; t += Time.fixedDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duracion);
            Vector3 punto = Vector3.Lerp(desde, PosicionInicialMundo(), k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * arco;
            rb.MovePosition(punto);
            rb.MoveRotation(Quaternion.Slerp(rotacionDesde, RotacionInicialMundo(), k));
            yield return new WaitForFixedUpdate();
        }

        rb.position = PosicionInicialMundo();
        rb.rotation = RotacionInicialMundo();
        rb.isKinematic = eraCinematicoAntesDeRecoger;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
        volviendo = false;
        rutinaRecogida = null;
    }

    void ConectarANave()
    {
        agarrada = false;
        conectada = true;

        // Detener el movimiento de la boquilla.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Desactivar el limite fisico mientras está conectada.
        ConfigurableJoint union =
            GetComponent<ConfigurableJoint>();

        if (union != null)
            Destroy(union);

        rb.useGravity = false;
        rb.isKinematic = true;

        // Encajar la boquilla y hacerla hija de la nave.
        MangueraFisica mangueraFisica =
    GetComponentInParent<MangueraFisica>();
        transform.SetParent(puntoConexionNave);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (mangueraFisica != null)
            mangueraFisica.TensarAlConectar();

        Debug.Log("MANGUERA CONECTADA A LA NAVE");
        OnMangueraConectada?.Invoke();

        if (retrasoCambioAlConectar > 0f)
            StartCoroutine(CambiarTrasRetraso(mangueraFisica));
        else
            CambiarPorObjetoConectado(mangueraFisica);
    }

    System.Collections.IEnumerator CambiarTrasRetraso(MangueraFisica mangueraFisica)
    {
        yield return new WaitForSeconds(retrasoCambioAlConectar);
        CambiarPorObjetoConectado(mangueraFisica);
    }

    // Sustituye la manguera física por el objeto configurado.
    // Ojo: al conectar, la boquilla pasa a ser hija de la nave, así que desactivar
    // MangueraSistema ya no la oculta; por eso por defecto se desactivan las dos.
    void CambiarPorObjetoConectado(MangueraFisica mangueraFisica)
    {
        if (objetoActivarAlConectar != null)
            objetoActivarAlConectar.SetActive(true);

        if (objetosDesactivarAlConectar != null && objetosDesactivarAlConectar.Length > 0)
        {
            foreach (GameObject objeto in objetosDesactivarAlConectar)
            {
                if (objeto != null)
                    objeto.SetActive(false);
            }

            // La boquilla va la última: al desactivarse a sí misma se detiene este script.
            if (desactivarBoquillaAlConectar)
                gameObject.SetActive(false);

            return;
        }

        if (mangueraFisica != null)
            mangueraFisica.gameObject.SetActive(false);

        // La boquilla va la última: al desactivarse a sí misma se detiene este script.
        gameObject.SetActive(false);
    }
}