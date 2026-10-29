using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgarrarManguera : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camaraJugador;
    public Transform puntoAgarre;

    [Header("Interaccion")]
    public KeyCode teclaAgarrar = KeyCode.E;
    public float distanciaMaximaAgarrar = 8f;

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

    private Rigidbody rb;
    private bool agarrada = false;
    private bool conectada = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(teclaAgarrar))
            return;

        if (conectada)
            return;

        if (agarrada)
        {
            // Si estamos cerca de la toma de combustible,
            // conectamos la manguera en lugar de soltarla.
            if (puntoConexionNave != null &&
                Vector3.Distance(transform.position,
                    puntoConexionNave.position) <= distanciaConexion)
            {
                ConectarANave();
            }
            else
            {
                Soltar();
            }

            return;
        }

        if (camaraJugador == null || puntoAgarre == null)
            return;

        float distancia = Vector3.Distance(
            camaraJugador.position,
            rb.position
        );

        if (distancia <= distanciaMaximaAgarrar)
        {
            Agarrar();
        }
    }

    void FixedUpdate()
    {
        if (!agarrada || conectada || puntoAgarre == null)
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
        agarrada = true;
        rb.WakeUp();

        Debug.Log("Manguera agarrada");
    }

    void Soltar()
    {
        agarrada = false;

        Debug.Log("Manguera soltada");
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

            return;
        }

        if (mangueraFisica != null)
            mangueraFisica.gameObject.SetActive(false);

        // La boquilla va la última: al desactivarse a sí misma se detiene este script.
        gameObject.SetActive(false);
    }
}