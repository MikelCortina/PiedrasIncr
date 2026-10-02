using UnityEngine;

public class EstadisticasBot : MonoBehaviour
{
    // =====================================================
    // ESTADÍSTICAS
    // =====================================================

    [Header("Estadísticas")]

    [SerializeField]
    private int piedrasRecogidas = 0;

    [SerializeField]
    private int entregasAgujero = 0;

    [SerializeField]
    private int piedrasProcesadas = 0;

    [SerializeField]
    private float sumaPurezaRecogida = 0f;

    [SerializeField]
    private float tiempoTrabajando = 0f;


    // =====================================================
    // REFERENCIAS
    // =====================================================

    private ConfiguracionBot configuracionBot;


    // =====================================================
    // PROPIEDADES
    // =====================================================

    public int PiedrasRecogidas
        => piedrasRecogidas;

    public int EntregasAgujero
        => entregasAgujero;

    public int PiedrasProcesadas
        => piedrasProcesadas;

    public float TiempoTrabajando
        => tiempoTrabajando;


    public float PurezaMedia
    {
        get
        {
            if (piedrasRecogidas <= 0)
                return 0f;

            return sumaPurezaRecogida /
                   piedrasRecogidas;
        }
    }


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        configuracionBot =
            GetComponent<ConfiguracionBot>();
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (configuracionBot == null)
            return;


        if (configuracionBot.modoActual ==
            ConfiguracionBot.ModoBot.Trabajar)
        {
            tiempoTrabajando +=
                Time.deltaTime;
        }
    }


    // =====================================================
    // PIEDRA RECOGIDA
    // =====================================================

    public void RegistrarPiedraRecogida(
        float pureza)
    {
        piedrasRecogidas++;

        sumaPurezaRecogida +=
            Mathf.Clamp(
                pureza,
                0f,
                100f
            );
    }


    // =====================================================
    // AGUJERO
    // =====================================================

    public void RegistrarEntregaAgujero()
    {
        entregasAgujero++;
    }


    // =====================================================
    // PROCESADORA
    // =====================================================

    public void RegistrarPiedraProcesada()
    {
        piedrasProcesadas++;
    }


    // =====================================================
    // RESET
    // =====================================================

    public void ResetearEstadisticas()
    {
        piedrasRecogidas =
            0;

        entregasAgujero =
            0;

        piedrasProcesadas =
            0;

        sumaPurezaRecogida =
            0f;

        tiempoTrabajando =
            0f;
    }


    // =====================================================
    // TIEMPO FORMATEADO
    // =====================================================

    public string ObtenerTiempoFormateado()
    {
        int segundosTotales =
            Mathf.FloorToInt(
                tiempoTrabajando
            );


        int minutos =
            segundosTotales / 60;


        int segundos =
            segundosTotales % 60;


        return minutos.ToString("00") +
               ":" +
               segundos.ToString("00");
    }
}