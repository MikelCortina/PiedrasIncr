using UnityEngine;
using UnityEngine.AI;

public class ConfiguracionBot : MonoBehaviour
{
    public enum ModoBot
    {
        Trabajar,
        Pausa,
        Parking
    }

    public enum PrioridadRecogida
    {
        MasCercana,
        MayorPureza,
        MenorPureza,
        Aleatoria
    }

    [Header("Identidad")]
    public string nombreBot = "BOT-01";


    [Header("Modo")]
    public ModoBot modoActual = ModoBot.Trabajar;


    [Header("Movimiento")]
    [Range(1f, 5f)]
    public float velocidadMovimiento = 2.3f;


    [Header("Búsqueda")]
    [Range(5f, 60f)]
    public float radioPrioridad = 20f;

    [Header("Prioridad de recogida")]
    public PrioridadRecogida prioridadRecogida =
    PrioridadRecogida.MasCercana;

    [Header("Piedras en movimiento")]
    public bool ignorarPiedrasEnMovimiento = true;

    [Tooltip("Si la piedra supera esta velocidad, el Bot la considera todavía en movimiento.")]
    public float velocidadMaximaPiedra = 1f;


    private BotRecolector bot;

    private NavMeshAgent agente;


    private void Awake()
    {
        bot =
            GetComponent<BotRecolector>();


        agente =
            GetComponent<NavMeshAgent>();
    }


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


        if (bot != null)
        {
            bot.radioBusqueda =
                radioPrioridad;
        }
    }


    // =====================================================
    // VELOCIDAD
    // =====================================================

    public void SetVelocidad(float nuevaVelocidad)
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
    // RADIO
    // =====================================================

    public void SetRadioPrioridad(float nuevoRadio)
    {
        radioPrioridad =
            nuevoRadio;


        if (bot != null)
        {
            bot.radioBusqueda =
                radioPrioridad;
        }
    }


    // =====================================================
    // PIEDRAS EN MOVIMIENTO
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

    public void SetPrioridadRecogida(
    PrioridadRecogida nuevaPrioridad)
    {
        prioridadRecogida =
            nuevaPrioridad;
    }
}