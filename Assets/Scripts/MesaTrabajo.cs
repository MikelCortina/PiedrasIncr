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

    private bool jugadorCerca;
    private bool tiendaAbierta;
    private TiendaHolografica holografica;

    /// <summary>El jugador está dentro del trigger de la tienda.</summary>
    public bool JugadorCerca => jugadorCerca;

    private void Awake()
    {
        MenuTienda.RegistrarTextoAviso(textoInteraccion);
    }

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

        if (jugadorCerca && Input.GetKeyDown(teclaInteractuar))
            AbrirTienda();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorCerca = true;
        if (!tiendaAbierta) MenuTienda.MostrarAviso(this, mensajeInteraccion);
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
        if (tiendaAbierta || holo == null) return;

        tiendaAbierta = true;
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
        if (jugadorCerca) MenuTienda.MostrarAviso(this, mensajeInteraccion);
    }

    public bool EstaTiendaAbierta()
    {
        return tiendaAbierta;
    }

    private void OnDisable()
    {
        if (tiendaAbierta)
        {
            tiendaAbierta = false;
            MenuTienda.Cerrar();
        }
        MenuTienda.OcultarAviso(this);
    }
}
