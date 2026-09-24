using TMPro;
using UnityEngine;

public class MesaTrabajo : MonoBehaviour
{
    [Header("Interacción")]
    public KeyCode teclaInteractuar = KeyCode.E;

    [Header("UI")]
    public GameObject panelTienda;
    public TextMeshProUGUI textoInteraccion;

    [Header("Texto")]
    public string mensajeInteraccion = "Pulsa E para abrir la tienda";

    private bool jugadorCerca = false;
    private bool tiendaAbierta = false;

    private void Start()
    {
        // La tienda empieza cerrada
        if (panelTienda != null)
        {
            panelTienda.SetActive(false);
        }

        // El mensaje empieza oculto
        if (textoInteraccion != null)
        {
            textoInteraccion.text = mensajeInteraccion;
            textoInteraccion.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Si la tienda está abierta, E o Escape la cierran
        if (tiendaAbierta)
        {
            if (Input.GetKeyDown(teclaInteractuar) ||
                Input.GetKeyDown(KeyCode.Escape))
            {
                CerrarTienda();
            }

            return;
        }

        // Si estamos cerca de la mesa, E abre la tienda
        if (jugadorCerca && Input.GetKeyDown(teclaInteractuar))
        {
            AbrirTienda();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = true;

        if (textoInteraccion != null && !tiendaAbierta)
        {
            textoInteraccion.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorCerca = false;

        if (textoInteraccion != null)
        {
            textoInteraccion.gameObject.SetActive(false);
        }
    }

    public void AbrirTienda()
    {
        if (panelTienda == null)
            return;

        tiendaAbierta = true;

        panelTienda.SetActive(true);

        if (textoInteraccion != null)
        {
            textoInteraccion.gameObject.SetActive(false);
        }

        // Pausamos el gameplay
        Time.timeScale = 0f;

        // Liberamos el ratón para utilizar los botones de la tienda
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarTienda()
    {
        tiendaAbierta = false;

        if (panelTienda != null)
        {
            panelTienda.SetActive(false);
        }

        // Reanudamos el juego
        Time.timeScale = 1f;

        // Volvemos a bloquear el ratón para primera persona
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (textoInteraccion != null && jugadorCerca)
        {
            textoInteraccion.gameObject.SetActive(true);
        }
    }

    public bool EstaTiendaAbierta()
    {
        return tiendaAbierta;
    }
}