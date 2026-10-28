
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TerminalCompraBots : MonoBehaviour
{
    [Header("Interaccion")]
    public KeyCode teclaAbrir = KeyCode.E;
    public GameObject avisoInteraccion;

    [Header("Tienda de bots")]
    public GameObject panelCompraBots;
    public TiendaTrabajo tiendaTrabajo;

    [Header("Jugador")]
    public MonoBehaviour controlCamaraJugador;

    private bool jugadorCerca;
    private bool tiendaAbierta;
    private bool camaraEstabaActiva;

    private CursorLockMode bloqueoCursorAnterior;
    private bool cursorVisibleAnterior;

    void Start()
    {
        if (panelCompraBots != null)
            panelCompraBots.SetActive(false);

        if (avisoInteraccion != null)
            avisoInteraccion.SetActive(false);
    }

    void Update()
    {
        if (tiendaAbierta)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CerrarTienda();

            return;
        }

        if (jugadorCerca && Input.GetKeyDown(teclaAbrir))
            AbrirTienda();
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.GetComponentInParent<CharacterController>() == null)
            return;

        jugadorCerca = true;

        if (avisoInteraccion != null)
            avisoInteraccion.SetActive(true);
    }

    void OnTriggerExit(Collider otro)
    {
        if (otro.GetComponentInParent<CharacterController>() == null)
            return;

        jugadorCerca = false;

        if (avisoInteraccion != null)
            avisoInteraccion.SetActive(false);
    }

    void AbrirTienda()
    {
        if (panelCompraBots == null || tiendaTrabajo == null)
        {
            Debug.LogWarning("Faltan referencias de la tienda de bots.");
            return;
        }

        tiendaAbierta = true;

        bloqueoCursorAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;

        if (controlCamaraJugador != null)
        {
            camaraEstabaActiva = controlCamaraJugador.enabled;
            controlCamaraJugador.enabled = false;
        }

        if (avisoInteraccion != null)
            avisoInteraccion.SetActive(false);

        panelCompraBots.SetActive(true);
        tiendaTrabajo.RefrescarTienda();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarTienda()
    {
        if (!tiendaAbierta)
            return;

        tiendaAbierta = false;

        if (panelCompraBots != null)
            panelCompraBots.SetActive(false);

        if (controlCamaraJugador != null)
            controlCamaraJugador.enabled = camaraEstabaActiva;

        Cursor.lockState = bloqueoCursorAnterior;
        Cursor.visible = cursorVisibleAnterior;

        if (avisoInteraccion != null)
            avisoInteraccion.SetActive(jugadorCerca);
    }

    void OnDisable()
    {
        if (tiendaAbierta)
            CerrarTienda();
    }
}
