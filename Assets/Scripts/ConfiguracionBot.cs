using UnityEngine;
using UnityEngine.AI;

public class ConfiguracionBot : MonoBehaviour
{
    // =====================================================
    // MODOS
    // =====================================================

    public enum ModoBot
    {
        Trabajar,
        Pausa,
        Parking
    }


    // =====================================================
    // PRIORIDAD
    // =====================================================

    public enum PrioridadRecogida
    {
        MasCercana,
        MayorPureza,
        MenorPureza,
        Aleatoria
    }


    // =====================================================
    // IDENTIDAD
    // =====================================================

    [Header("Identidad")]
    public string nombreBot = "BOT-01";


    // =====================================================
    // MODO
    // =====================================================

    [Header("Modo")]
    public ModoBot modoActual = ModoBot.Pausa;


    // =====================================================
    // MOVIMIENTO
    // =====================================================

    [Header("Movimiento")]

    [Range(1f, 5f)]
    public float velocidadMovimiento = 2.3f;


    // =====================================================
    // PRIORIDAD
    // =====================================================

    [Header("Prioridad de recogida")]

    public PrioridadRecogida prioridadRecogida =
        PrioridadRecogida.MasCercana;


    // =====================================================
    // FILTRO DE PUREZA
    // =====================================================

    [Header("Filtro de Pureza")]

    [Range(0f, 100f)]
    public float purezaMinima = 0f;


    [Range(0f, 100f)]
    public float purezaMaxima = 100f;


    // =====================================================
    // PIEDRAS EN MOVIMIENTO
    // =====================================================

    [Header("Piedras en movimiento")]

    public bool ignorarPiedrasEnMovimiento = true;


    [Tooltip(
        "Si supera esta velocidad el Bot considera " +
        "que la piedra todavía está moviéndose."
    )]
    public float velocidadMaximaPiedra = 1f;


    // =====================================================
    // INTERNAS
    // =====================================================

    private BotRecolector bot;

    private NavMeshAgent agente;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        bot =
            GetComponent<BotRecolector>();


        agente =
            GetComponent<NavMeshAgent>();
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        AplicarConfiguracion();
    }


    // =====================================================
    // APLICAR
    // =====================================================

    public void AplicarConfiguracion()
    {
        if (agente != null)
        {
            agente.speed =
                velocidadMovimiento;
        }


        purezaMinima =
            Mathf.Clamp(
                purezaMinima,
                0f,
                100f
            );


        purezaMaxima =
            Mathf.Clamp(
                purezaMaxima,
                0f,
                100f
            );


        if (purezaMinima > purezaMaxima)
        {
            purezaMaxima =
                purezaMinima;
        }
    }


    // =====================================================
    // VELOCIDAD
    // =====================================================

    public void SetVelocidad(
        float nuevaVelocidad)
    {
        velocidadMovimiento =
            nuevaVelocidad;


        if (agente != null)
        {
            agente.speed =
                velocidadMovimiento;
        }
    }


    // =====================================================
    // PRIORIDAD
    // =====================================================

    public void SetPrioridadRecogida(
        PrioridadRecogida nuevaPrioridad)
    {
        prioridadRecogida =
            nuevaPrioridad;
    }


    // =====================================================
    // PUREZA MÍNIMA
    // =====================================================

    public void SetPurezaMinima(
        float nuevaPureza)
    {
        purezaMinima =
            Mathf.Clamp(
                nuevaPureza,
                0f,
                100f
            );


        // Si cruzamos el máximo,
        // hacemos que el máximo acompañe al mínimo.
        if (purezaMinima > purezaMaxima)
        {
            purezaMaxima =
                purezaMinima;
        }
    }


    // =====================================================
    // PUREZA MÁXIMA
    // =====================================================

    public void SetPurezaMaxima(
        float nuevaPureza)
    {
        purezaMaxima =
            Mathf.Clamp(
                nuevaPureza,
                0f,
                100f
            );


        // Si cruzamos el mínimo,
        // hacemos que el mínimo acompañe al máximo.
        if (purezaMaxima < purezaMinima)
        {
            purezaMinima =
                purezaMaxima;
        }
    }


    // =====================================================
    // COMPROBAR PUREZA
    // =====================================================

    public bool PurezaPermitida(
        float pureza)
    {
        return pureza >= purezaMinima &&
               pureza <= purezaMaxima;
    }


    // =====================================================
    // MOVIMIENTO DE PIEDRAS
    // =====================================================

    public void SetIgnorarPiedrasEnMovimiento(
        bool ignorar)
    {
        ignorarPiedrasEnMovimiento =
            ignorar;
    }


    public bool PiedraEstaDemasiadoMovida(
        Rigidbody piedra)
    {
        if (!ignorarPiedrasEnMovimiento)
            return false;


        if (piedra == null)
            return false;


        return piedra.linearVelocity.magnitude >
               velocidadMaximaPiedra;
    }


    // =====================================================
    // MODOS
    // =====================================================

    public void SetModoTrabajar()
    {
        modoActual =
            ModoBot.Trabajar;


        if (bot != null)
        {
            bot.ConfigurarModoTrabajar();
        }
    }


    public void SetModoPausa()
    {
        modoActual =
            ModoBot.Pausa;


        if (bot != null)
        {
            bot.ConfigurarModoPausa();
        }
    }


    public void SetModoParking()
    {
        modoActual =
            ModoBot.Parking;


        if (bot != null)
        {
            bot.ConfigurarModoParking();
        }
    }
}