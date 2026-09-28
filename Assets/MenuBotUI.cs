using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuBotUI : MonoBehaviour
{
    // =====================================================
    // PANEL
    // =====================================================

    [Header("Panel")]
    public GameObject panel;


    // =====================================================
    // INFORMACIÓN
    // =====================================================

    [Header("Información")]
    public TMP_Text textoNombre;
    public TMP_Text textoEstado;


    // =====================================================
    // BOTONES
    // =====================================================

    [Header("Botones")]
    public Button botonTrabajar;
    public Button botonPausa;
    public Button botonParking;
    public Button botonCerrar;


    // =====================================================
    // VELOCIDAD
    // =====================================================

    [Header("Velocidad")]
    public Slider sliderVelocidad;
    public TMP_Text textoVelocidad;


    // =====================================================
    // RADIO
    // =====================================================

    [Header("Radio prioridad")]
    public Slider sliderRadio;
    public TMP_Text textoRadio;

    [Header("Control del jugador")]
    [Tooltip("Arrastra aquí el script del jugador que controla la cámara/mirada con el ratón.")]
    public MonoBehaviour controladorMiradaJugador;

    // =====================================================
    // MOVIMIENTO DE PIEDRAS
    // =====================================================

    [Header("Movimiento de piedras")]
    public Toggle toggleIgnorarMovimiento;

    [Tooltip("Texto que aparece al lado del Toggle.")]
    public TMP_Text textoToggleIgnorar;

    [Header("Prioridad de recogida")]
    public TMP_Dropdown dropdownPrioridad;

    // =====================================================
    // VARIABLES INTERNAS
    // =====================================================

    private ConfiguracionBot botActual;

    private BotRecolector recolectorActual;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        // =============================================
        // CONFIGURAR TEXTOS FIJOS
        // =============================================

        ConfigurarTextosBotones();


        if (textoToggleIgnorar != null)
        {
            textoToggleIgnorar.text =
                "Ignorar piedras en movimiento";
        }


        // =============================================
        // BOTONES
        // =============================================

        if (botonTrabajar != null)
        {
            botonTrabajar.onClick.AddListener(
                BotonTrabajar
            );
        }


        if (botonPausa != null)
        {
            botonPausa.onClick.AddListener(
                BotonPausa
            );
        }


        if (botonParking != null)
        {
            botonParking.onClick.AddListener(
                BotonParking
            );
        }


        if (botonCerrar != null)
        {
            botonCerrar.onClick.AddListener(
                CerrarMenu
            );
        }


        // =============================================
        // SLIDER VELOCIDAD
        // =============================================

        if (sliderVelocidad != null)
        {
            sliderVelocidad.minValue =
                1f;

            sliderVelocidad.maxValue =
                5f;


            sliderVelocidad.onValueChanged.AddListener(
                CambiarVelocidad
            );
        }


        // =============================================
        // SLIDER RADIO
        // =============================================

        if (sliderRadio != null)
        {
            sliderRadio.minValue =
                5f;

            sliderRadio.maxValue =
                60f;


            sliderRadio.onValueChanged.AddListener(
                CambiarRadio
            );
        }


        // =============================================
        // TOGGLE
        // =============================================

        if (toggleIgnorarMovimiento != null)
        {
            toggleIgnorarMovimiento.onValueChanged.AddListener(
                CambiarIgnorarMovimiento
            );
        }


        // =============================================
        // CERRAR AL INICIAR
        // =============================================

        if (panel != null)
        {
            panel.SetActive(
                false
            );
        }

        // =============================================
        // PRIORIDAD DE RECOGIDA
        // =============================================

        if (dropdownPrioridad != null)
        {
            dropdownPrioridad.ClearOptions();


            dropdownPrioridad.AddOptions(
                new List<string>
                {
            "Más cercana",
            "Mayor pureza",
            "Menor pureza",
            "Aleatoria"
                }
            );


            dropdownPrioridad.onValueChanged.AddListener(
                CambiarPrioridad
            );
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (botActual == null)
            return;


        // El modo puede cambiar mientras
        // tenemos abierto el menú.
        ActualizarEstado();


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarMenu();
        }
    }


    // =====================================================
    // ABRIR MENÚ
    // =====================================================

    public void AbrirMenu(ConfiguracionBot bot)
    {
        if (bot == null)
            return;


        // =====================================================
        // GUARDAR BOT ACTUAL
        // =====================================================

        botActual = bot;

        recolectorActual =
            bot.GetComponent<BotRecolector>();


        // =====================================================
        // PARAR BOT Y HACER QUE NOS MIRE
        // =====================================================

        if (recolectorActual != null)
        {
            Transform objetivoMirada = null;


            if (Camera.main != null)
            {
                objetivoMirada =
                    Camera.main.transform;
            }


            recolectorActual.IniciarInteraccion(
                objetivoMirada
            );
        }


        // =====================================================
        // ABRIR PANEL
        // =====================================================

        if (panel != null)
        {
            panel.SetActive(true);
        }


        // =====================================================
        // BLOQUEAR CÁMARA DEL JUGADOR
        // =====================================================

        if (controladorMiradaJugador != null)
        {
            controladorMiradaJugador.enabled =
                false;
        }


        // =====================================================
        // LIBERAR CURSOR
        // =====================================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        // =====================================================
        // NOMBRE DEL BOT
        // =====================================================

        if (textoNombre != null)
        {
            textoNombre.text =
                botActual.nombreBot;
        }


        // =====================================================
        // VELOCIDAD
        // =====================================================

        if (sliderVelocidad != null)
        {
            sliderVelocidad.SetValueWithoutNotify(
                botActual.velocidadMovimiento
            );
        }


        // =====================================================
        // RADIO
        // =====================================================

        if (sliderRadio != null)
        {
            sliderRadio.SetValueWithoutNotify(
                botActual.radioPrioridad
            );
        }


        // =====================================================
        // TOGGLE
        // =====================================================

        if (toggleIgnorarMovimiento != null)
        {
            toggleIgnorarMovimiento.SetIsOnWithoutNotify(
                botActual.ignorarPiedrasEnMovimiento
            );
        }


        // =============================================
        // PRIORIDAD
        // =============================================

        if (dropdownPrioridad != null)
        {
            dropdownPrioridad.SetValueWithoutNotify(
                (int)botActual.prioridadRecogida
            );


            dropdownPrioridad.RefreshShownValue();
        }

        // =====================================================
        // ACTUALIZAR INFORMACIÓN
        // =====================================================

        ActualizarTextos();

        ActualizarEstado();
    }

    public void CerrarMenu()
    {
        // =====================================================
        // REANUDAR BOT
        // =====================================================

        if (recolectorActual != null)
        {
            recolectorActual.FinalizarInteraccion();
        }


        recolectorActual =
            null;

        botActual =
            null;


        // =====================================================
        // CERRAR PANEL
        // =====================================================

        if (panel != null)
        {
            panel.SetActive(false);
        }


        // =====================================================
        // DEVOLVER CONTROL DE CÁMARA
        // =====================================================

        if (controladorMiradaJugador != null)
        {
            controladorMiradaJugador.enabled =
                true;
        }


        // =====================================================
        // VOLVER A BLOQUEAR CURSOR
        // =====================================================

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    // =====================================================
    // CONSULTA
    // =====================================================

    public bool EstaAbierto()
    {
        return panel != null &&
               panel.activeSelf;
    }


    // =====================================================
    // BOTÓN TRABAJAR
    // =====================================================

    public void BotonTrabajar()
    {
        if (botActual == null)
            return;


        botActual.SetModoTrabajar();


        ActualizarEstado();
    }


    // =====================================================
    // BOTÓN PAUSA
    // =====================================================

    public void BotonPausa()
    {
        if (botActual == null)
            return;


        botActual.SetModoPausa();


        ActualizarEstado();
    }


    // =====================================================
    // BOTÓN PARKING
    // =====================================================

    public void BotonParking()
    {
        if (botActual == null)
            return;


        botActual.SetModoParking();


        ActualizarEstado();
    }


    // =====================================================
    // VELOCIDAD
    // =====================================================

    private void CambiarVelocidad(
        float valor)
    {
        if (botActual == null)
            return;


        botActual.SetVelocidad(
            valor
        );


        ActualizarTextos();
    }


    // =====================================================
    // RADIO
    // =====================================================

    private void CambiarRadio(
        float valor)
    {
        if (botActual == null)
            return;


        botActual.SetRadioPrioridad(
            valor
        );


        ActualizarTextos();
    }


    // =====================================================
    // IGNORAR PIEDRAS EN MOVIMIENTO
    // =====================================================

    private void CambiarIgnorarMovimiento(
        bool valor)
    {
        if (botActual == null)
            return;


        botActual.SetIgnorarPiedrasEnMovimiento(
            valor
        );
    }


    // =====================================================
    // ACTUALIZAR TEXTOS VARIABLES
    // =====================================================

    private void ActualizarTextos()
    {
        if (botActual == null)
            return;


        if (textoNombre != null)
        {
            textoNombre.text =
                botActual.nombreBot;
        }


        if (textoVelocidad != null)
        {
            textoVelocidad.text =
                "Velocidad: " +
                botActual.velocidadMovimiento.ToString("0.0");
        }


        if (textoRadio != null)
        {
            textoRadio.text =
                "Radio prioridad: " +
                botActual.radioPrioridad.ToString("0") +
                " m";
        }
    }


    // =====================================================
    // ACTUALIZAR ESTADO
    // =====================================================

    private void ActualizarEstado()
    {
        if (botActual == null ||
            textoEstado == null)
        {
            return;
        }


        string modo;


        switch (botActual.modoActual)
        {
            case ConfiguracionBot.ModoBot.Trabajar:

                modo =
                    "Trabajando";

                break;


            case ConfiguracionBot.ModoBot.Pausa:

                modo =
                    "En pausa";

                break;


            case ConfiguracionBot.ModoBot.Parking:

                modo =
                    "En parking";

                break;


            default:

                modo =
                    botActual.modoActual.ToString();

                break;
        }


        textoEstado.text =
            "Modo: " +
            modo;
    }


    // =====================================================
    // TEXTOS DE LOS BOTONES
    // =====================================================

    private void ConfigurarTextosBotones()
    {
        PonerTextoBoton(
            botonTrabajar,
            "TRABAJAR"
        );


        PonerTextoBoton(
            botonPausa,
            "PAUSA"
        );


        PonerTextoBoton(
            botonParking,
            "VOLVER AL PARKING"
        );


        PonerTextoBoton(
            botonCerrar,
            "CERRAR"
        );
    }


    private void PonerTextoBoton(
        Button boton,
        string texto)
    {
        if (boton == null)
            return;


        TMP_Text textoBoton =
            boton.GetComponentInChildren<TMP_Text>(
                true
            );


        if (textoBoton != null)
        {
            textoBoton.text =
                texto;
        }
    }

    private void CambiarPrioridad(
    int indice)
    {
        if (botActual == null)
            return;


        ConfiguracionBot.PrioridadRecogida prioridad =
            (ConfiguracionBot.PrioridadRecogida)indice;


        botActual.SetPrioridadRecogida(
            prioridad
        );
    }
}