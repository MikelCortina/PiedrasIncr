using TMPro;
using UnityEngine;

public class InteraccionBotJugador : MonoBehaviour
{
    [Header("Interacción")]
    public float distanciaInteraccion = 3f;
    public KeyCode teclaInteraccion = KeyCode.E;

    [Header("Referencias")]
    public Camera camaraJugador;
    public MenuBotUI menuBot;

    [Header("UI de Interacción")]
    public TMP_Text textoInteraccion;

    [Header("Debug")]
    public bool mostrarRayoDebug = true;

    private ConfiguracionBot botMirado;


    private void Start()
    {
        if (camaraJugador == null)
        {
            camaraJugador = Camera.main;
        }

        OcultarTextoInteraccion();
    }


    private void Update()
    {
        // Si el menú está abierto no necesitamos
        // seguir buscando Bots.
        if (menuBot != null &&
            menuBot.EstaAbierto())
        {
            botMirado = null;
            OcultarTextoInteraccion();
            return;
        }


        DetectarBot();


        if (botMirado != null &&
            Input.GetKeyDown(teclaInteraccion))
        {
            AbrirBot();
        }
    }


    // =====================================================
    // DETECTAR BOT
    // =====================================================

    private void DetectarBot()
    {
        botMirado = null;


        if (camaraJugador == null)
        {
            OcultarTextoInteraccion();
            return;
        }


        Vector3 origen =
            camaraJugador.transform.position;

        Vector3 direccion =
            camaraJugador.transform.forward;


        if (mostrarRayoDebug)
        {
            Debug.DrawRay(
                origen,
                direccion * distanciaInteraccion,
                Color.yellow
            );
        }


        if (Physics.Raycast(
                origen,
                direccion,
                out RaycastHit hit,
                distanciaInteraccion,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            ConfiguracionBot bot =
                hit.collider.GetComponentInParent<ConfiguracionBot>();


            if (bot != null)
            {
                botMirado = bot;

                MostrarTextoInteraccion(bot);

                return;
            }
        }


        OcultarTextoInteraccion();
    }


    // =====================================================
    // ABRIR
    // =====================================================

    private void AbrirBot()
    {
        if (botMirado == null)
            return;


        if (menuBot == null)
        {
            Debug.LogError(
                "InteraccionBotJugador: falta asignar MenuBotUI."
            );

            return;
        }


        Debug.Log(
            "Abriendo configuración de " +
            botMirado.nombreBot
        );


        menuBot.AbrirMenu(
            botMirado
        );


        OcultarTextoInteraccion();
    }


    // =====================================================
    // TEXTO
    // =====================================================

    private void MostrarTextoInteraccion(
        ConfiguracionBot bot)
    {
        if (textoInteraccion == null)
            return;


        textoInteraccion.gameObject.SetActive(
            true
        );


        textoInteraccion.text =
            "E - Configurar " +
            bot.nombreBot;
    }


    private void OcultarTextoInteraccion()
    {
        if (textoInteraccion == null)
            return;


        textoInteraccion.gameObject.SetActive(
            false
        );
    }
}