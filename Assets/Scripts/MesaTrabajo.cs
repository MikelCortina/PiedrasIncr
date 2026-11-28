using TMPro;
using UnityEngine;

/// <summary>
/// Mesa de la tienda principal: al pulsar E cerca abre la tienda holográfica (TiendaHolografica, que se añade sola).
/// Pausa, ratón, cámara y arma: MenuTienda. Las compras: TiendaTrabajo.
/// </summary>
public class MesaTrabajo : MonoBehaviour
{
    [Header("Interacción")]
    public KeyCode teclaInteractuar = KeyCode.E;

    [Tooltip("Texto en pantalla de los avisos 'Pulsa E...'. Lo usan también el resto de tiendas.")]
    public TextMeshProUGUI textoInteraccion;

    public string mensajeInteraccion = "Pulsa E para abrir la tienda";

    [Header("Cerrada al empezar")]
    [Tooltip("La tienda no se puede usar hasta meter estas piedras en el depósito (0 = abierta desde el principio). " +
             "Al abrirse: Botro la presenta (GuionBotro, 'Al abrirse la tienda'), el rombo la señala y el Fusil sale gratis.")]
    [Min(0)] public int piedrasParaAbrir = 2;
    [Tooltip("Aviso al acercarse mientras está cerrada.")]
    public string mensajeCerrada = "Tienda cerrada: mete más piedras en el depósito";
    [Tooltip("Al abrirse, el primer nivel del Fusil de monedas es gratis.")]
    public bool fusilGratisAlAbrir = true;

    /// <summary>La tienda ya se puede usar.</summary>
    public static bool TiendaDisponible { get; private set; }
    public static event System.Action OnTiendaAbierta;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        TiendaDisponible = false;
        OnTiendaAbierta = null;
    }

    private bool jugadorCerca;
    private bool tiendaAbierta;
    private int piedrasMetidas;
    private TiendaHolografica holografica;

    /// <summary>El jugador está dentro del trigger de la tienda.</summary>
    public bool JugadorCerca => jugadorCerca;

    private void Awake()
    {
        MenuTienda.RegistrarTextoAviso(textoInteraccion);
        TiendaDisponible = false;
    }

    private void OnEnable() { AgujeroSimple.OnPiedraTragada += AlTragarPiedra; }

    private void Start()
    {
        // Partida cargada: ya pasó por aquí (no se repite la presentación de Botro).
        bool cargada = GuardadoPartidaJuego.InicioActual == GuardadoPartidaJuego.TipoInicio.PartidaCargada;
        if (piedrasParaAbrir <= 0 || cargada)
            AbrirTiendaAlPublico(false);
    }

    private void AlTragarPiedra(AgujeroSimple agujero, float pureza, float gasolina)
    {
        if (TiendaDisponible) return;
        piedrasMetidas++;
        if (piedrasMetidas >= piedrasParaAbrir)
            AbrirTiendaAlPublico(true);
    }

    // La tienda pasa a estar disponible. Con 'presentar', Botro la explica y el rombo la señala hasta que se abre.
    private void AbrirTiendaAlPublico(bool presentar)
    {
        if (TiendaDisponible) return;
        TiendaDisponible = true;

        if (fusilGratisAlAbrir)
        {
            TiendaTrabajo tienda = FindAnyObjectByType<TiendaTrabajo>(FindObjectsInactive.Include);
            if (tienda != null) tienda.fusilGratis = true;
        }

        if (presentar)
        {
            BotroHabla.Lanzar(GuionBotro.Cuando.TiendaAbierta);
            SecuenciaIntro.SenalarObjetivo(transform);
        }

        if (jugadorCerca && !tiendaAbierta) MenuTienda.MostrarAviso(this, mensajeInteraccion);
        OnTiendaAbierta?.Invoke();
    }

    private string MensajeAviso => TiendaDisponible ? mensajeInteraccion : mensajeCerrada;

    private TiendaHolografica Holografica
    {
        get
        {
            if (holografica == null) holografica = GetComponent<TiendaHolografica>();
            return holografica != null && holografica.isActiveAndEnabled ? holografica : null;
        }
    }

    private void Update()
    {
        if (tiendaAbierta)
        {
            if (Input.GetKeyDown(teclaInteractuar) || Input.GetKeyDown(KeyCode.Escape))
                CerrarTienda();
            return;
        }

        if (jugadorCerca && TiendaDisponible && Input.GetKeyDown(teclaInteractuar))
            AbrirTienda();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorCerca = true;
        if (!tiendaAbierta) MenuTienda.MostrarAviso(this, MensajeAviso);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorCerca = false;
        MenuTienda.OcultarAviso(this);
    }

    public void AbrirTienda()
    {
        TiendaHolografica holo = Holografica;
        if (tiendaAbierta || holo == null || !TiendaDisponible) return;

        tiendaAbierta = true;
        // Ya la ha encontrado: el rombo deja de señalarla.
        SecuenciaIntro.QuitarObjetivo(transform);
        MenuTienda.OcultarAviso(this);
        holo.Mostrar();
        MenuTienda.Abrir();
    }

    public void CerrarTienda()
    {
        if (!tiendaAbierta) return;
        tiendaAbierta = false;

        TiendaHolografica holo = Holografica;
        if (holo != null) holo.Ocultar();

        MenuTienda.Cerrar();
        if (jugadorCerca) MenuTienda.MostrarAviso(this, MensajeAviso);
    }

    public bool EstaTiendaAbierta()
    {
        return tiendaAbierta;
    }

    private void OnDisable()
    {
        AgujeroSimple.OnPiedraTragada -= AlTragarPiedra;
        if (tiendaAbierta)
        {
            tiendaAbierta = false;
            MenuTienda.Cerrar();
        }
        MenuTienda.OcultarAviso(this);
    }
}
