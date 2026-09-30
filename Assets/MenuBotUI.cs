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
    // CONTROL DEL JUGADOR
    // =====================================================

    [Header("Control del jugador")]

    [Tooltip(
        "Arrastra aquí el script del jugador que controla " +
        "la cámara/mirada con el ratón."
    )]
    public MonoBehaviour controladorMiradaJugador;


    // =====================================================
    // MOVIMIENTO DE PIEDRAS
    // =====================================================

    [Header("Movimiento de piedras")]

    public Toggle toggleIgnorarMovimiento;


    [Tooltip("Texto que aparece al lado del Toggle.")]
    public TMP_Text textoToggleIgnorar;


    // =====================================================
    // PRIORIDAD
    // =====================================================

    [Header("Prioridad de recogida")]

    public TMP_Dropdown dropdownPrioridad;


    // =====================================================
    // PUREZA
    // =====================================================

    [Header("Filtro de Pureza")]

    public Slider sliderPurezaMin;
    public TMP_Text textoPurezaMin;

    public Slider sliderPurezaMax;
    public TMP_Text textoPurezaMax;


    // =====================================================
    // DESTINO
    // =====================================================

    [Header("Destino de la piedra")]

    public TMP_Dropdown dropdownDestino;


    [Tooltip(
        "En modo Automático, las piedras con esta pureza " +
        "o superior irán directamente al agujero."
    )]
    public Slider sliderPurezaAgujero;


    public TMP_Text textoPurezaAgujero;


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
        // =================================================
        // TEXTOS FIJOS
        // =================================================

        ConfigurarTextosBotones();


        if (textoToggleIgnorar != null)
        {
            textoToggleIgnorar.text =
                "Ignorar piedras en movimiento";
        }


        // =================================================
        // BOTÓN TRABAJAR
        // =================================================

        if (botonTrabajar != null)
        {
            botonTrabajar.onClick.AddListener(
                BotonTrabajar
            );
        }


        // =================================================
        // BOTÓN PAUSA
        // =================================================

        if (botonPausa != null)
        {
            botonPausa.onClick.AddListener(
                BotonPausa
            );
        }


        // =================================================
        // BOTÓN PARKING
        // =================================================

        if (botonParking != null)
        {
            botonParking.onClick.AddListener(
                BotonParking
            );
        }


        // =================================================
        // BOTÓN CERRAR
        // =================================================

        if (botonCerrar != null)
        {
            botonCerrar.onClick.AddListener(
                CerrarMenu
            );
        }


        // =================================================
        // VELOCIDAD
        // =================================================

        if (sliderVelocidad != null)
        {
            sliderVelocidad.minValue =
                1f;


            sliderVelocidad.maxValue =
                5f;


            sliderVelocidad.wholeNumbers =
                false;


            sliderVelocidad.onValueChanged.AddListener(
                CambiarVelocidad
            );
        }


        // =================================================
        // IGNORAR PIEDRAS EN MOVIMIENTO
        // =================================================

        if (toggleIgnorarMovimiento != null)
        {
            toggleIgnorarMovimiento
                .onValueChanged
                .AddListener(
                    CambiarIgnorarMovimiento
                );
        }


        // =================================================
        // PRIORIDAD
        // =================================================

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


            dropdownPrioridad
                .onValueChanged
                .AddListener(
                    CambiarPrioridad
                );
        }


        // =================================================
        // PUREZA MÍNIMA
        // =================================================

        if (sliderPurezaMin != null)
        {
            sliderPurezaMin.minValue =
                0f;


            sliderPurezaMin.maxValue =
                100f;


            sliderPurezaMin.wholeNumbers =
                false;


            sliderPurezaMin
                .onValueChanged
                .AddListener(
                    CambiarPurezaMinima
                );
        }


        // =================================================
        // PUREZA MÁXIMA
        // =================================================

        if (sliderPurezaMax != null)
        {
            sliderPurezaMax.minValue =
                0f;


            sliderPurezaMax.maxValue =
                100f;


            sliderPurezaMax.wholeNumbers =
                false;


            sliderPurezaMax
                .onValueChanged
                .AddListener(
                    CambiarPurezaMaxima
                );
        }


        // =================================================
        // DESTINO
        // =================================================

        if (dropdownDestino != null)
        {
            dropdownDestino.ClearOptions();


            dropdownDestino.AddOptions(
                new List<string>
                {
                    "Agujero",
                    "Procesadora",
                    "Automático"
                }
            );


            dropdownDestino
                .onValueChanged
                .AddListener(
                    CambiarDestino
                );
        }


        // =================================================
        // PUREZA DIRECTA AL AGUJERO
        // =================================================

        if (sliderPurezaAgujero != null)
        {
            sliderPurezaAgujero.minValue =
                0f;


            sliderPurezaAgujero.maxValue =
                100f;


            sliderPurezaAgujero.wholeNumbers =
                false;


            sliderPurezaAgujero
                .onValueChanged
                .AddListener(
                    CambiarPurezaAgujero
                );
        }


        // =================================================
        // CERRAR PANEL AL INICIAR
        // =================================================

        if (panel != null)
        {
            panel.SetActive(
                false
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


        ActualizarEstado();


        if (Input.GetKeyDown(
                KeyCode.Escape))
        {
            CerrarMenu();
        }
    }


    // =====================================================
    // ABRIR MENÚ
    // =====================================================

    public void AbrirMenu(
        ConfiguracionBot bot)
    {
        if (bot == null)
            return;


        // =================================================
        // BOT ACTUAL
        // =================================================

        botActual =
            bot;


        recolectorActual =
            bot.GetComponent<BotRecolector>();


        // =================================================
        // PARAR BOT Y HACER QUE MIRE AL JUGADOR
        // =================================================

        if (recolectorActual != null)
        {
            Transform objetivoMirada =
                null;


            if (Camera.main != null)
            {
                objetivoMirada =
                    Camera.main.transform;
            }


            recolectorActual
                .IniciarInteraccion(
                    objetivoMirada
                );
        }


        // =================================================
        // ABRIR PANEL
        // =================================================

        if (panel != null)
        {
            panel.SetActive(
                true
            );
        }


        // =================================================
        // BLOQUEAR CÁMARA
        // =================================================

        if (controladorMiradaJugador != null)
        {
            controladorMiradaJugador.enabled =
                false;
        }


        // =================================================
        // CURSOR
        // =================================================

        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            true;


        // =================================================
        // NOMBRE
        // =================================================

        if (textoNombre != null)
        {
            textoNombre.text =
                botActual.nombreBot;
        }


        // =================================================
        // VELOCIDAD
        // =================================================

        if (sliderVelocidad != null)
        {
            sliderVelocidad
                .SetValueWithoutNotify(
                    botActual.velocidadMovimiento
                );
        }


        // =================================================
        // IGNORAR MOVIMIENTO
        // =================================================

        if (toggleIgnorarMovimiento != null)
        {
            toggleIgnorarMovimiento
                .SetIsOnWithoutNotify(
                    botActual
                        .ignorarPiedrasEnMovimiento
                );
        }


        // =================================================
        // PRIORIDAD
        // =================================================

        if (dropdownPrioridad != null)
        {
            dropdownPrioridad
                .SetValueWithoutNotify(
                    (int)botActual
                        .prioridadRecogida
                );


            dropdownPrioridad
                .RefreshShownValue();
        }


        // =================================================
        // PUREZA MÍNIMA
        // =================================================

        if (sliderPurezaMin != null)
        {
            sliderPurezaMin
                .SetValueWithoutNotify(
                    botActual.purezaMinima
                );
        }


        // =================================================
        // PUREZA MÁXIMA
        // =================================================

        if (sliderPurezaMax != null)
        {
            sliderPurezaMax
                .SetValueWithoutNotify(
                    botActual.purezaMaxima
                );
        }


        // =================================================
        // DESTINO
        // =================================================

        if (dropdownDestino != null)
        {
            dropdownDestino
                .SetValueWithoutNotify(
                    (int)botActual
                        .destinoTrabajo
                );


            dropdownDestino
                .RefreshShownValue();
        }


        // =================================================
        // PUREZA DIRECTA AL AGUJERO
        // =================================================

        if (sliderPurezaAgujero != null)
        {
            sliderPurezaAgujero
                .SetValueWithoutNotify(
                    botActual
                        .purezaDirectaAgujero
                );
        }


        // =================================================
        // ACTUALIZAR INFORMACIÓN
        // =================================================

        ActualizarTextos();

        ActualizarEstado();

        ActualizarVisibilidadDestino();
    }


    // =====================================================
    // CERRAR MENÚ
    // =====================================================

    public void CerrarMenu()
    {
        // =================================================
        // REANUDAR BOT
        // =================================================

        if (recolectorActual != null)
        {
            recolectorActual
                .FinalizarInteraccion();
        }


        recolectorActual =
            null;


        botActual =
            null;


        // =================================================
        // CERRAR PANEL
        // =================================================

        if (panel != null)
        {
            panel.SetActive(
                false
            );
        }


        // =================================================
        // DEVOLVER CONTROL DE CÁMARA
        // =================================================

        if (controladorMiradaJugador != null)
        {
            controladorMiradaJugador.enabled =
                true;
        }


        // =================================================
        // CURSOR
        // =================================================

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
    // IGNORAR PIEDRAS EN MOVIMIENTO
    // =====================================================

    private void CambiarIgnorarMovimiento(
        bool valor)
    {
        if (botActual == null)
            return;


        botActual
            .SetIgnorarPiedrasEnMovimiento(
                valor
            );
    }


    // =====================================================
    // PRIORIDAD
    // =====================================================

    private void CambiarPrioridad(
        int indice)
    {
        if (botActual == null)
            return;


        ConfiguracionBot.PrioridadRecogida prioridad =
            (ConfiguracionBot.PrioridadRecogida)
            indice;


        botActual.SetPrioridadRecogida(
            prioridad
        );
    }


    // =====================================================
    // PUREZA MÍNIMA
    // =====================================================

    private void CambiarPurezaMinima(
        float valor)
    {
        if (botActual == null)
            return;


        botActual.SetPurezaMinima(
            valor
        );


        // ConfiguracionBot puede modificar el máximo
        // si el mínimo lo supera.

        if (sliderPurezaMax != null)
        {
            sliderPurezaMax
                .SetValueWithoutNotify(
                    botActual.purezaMaxima
                );
        }


        if (sliderPurezaMin != null)
        {
            sliderPurezaMin
                .SetValueWithoutNotify(
                    botActual.purezaMinima
                );
        }


        ActualizarTextos();
    }


    // =====================================================
    // PUREZA MÁXIMA
    // =====================================================

    private void CambiarPurezaMaxima(
        float valor)
    {
        if (botActual == null)
            return;


        botActual.SetPurezaMaxima(
            valor
        );


        // ConfiguracionBot puede modificar el mínimo
        // si el máximo baja demasiado.

        if (sliderPurezaMin != null)
        {
            sliderPurezaMin
                .SetValueWithoutNotify(
                    botActual.purezaMinima
                );
        }


        if (sliderPurezaMax != null)
        {
            sliderPurezaMax
                .SetValueWithoutNotify(
                    botActual.purezaMaxima
                );
        }


        ActualizarTextos();
    }


    // =====================================================
    // DESTINO
    // =====================================================

    private void CambiarDestino(
        int indice)
    {
        if (botActual == null)
            return;


        ConfiguracionBot.DestinoTrabajo destino =
            (ConfiguracionBot.DestinoTrabajo)
            indice;


        botActual.SetDestinoTrabajo(
            destino
        );


        ActualizarVisibilidadDestino();

        ActualizarTextos();
    }


    // =====================================================
    // PUREZA DIRECTA AL AGUJERO
    // =====================================================

    private void CambiarPurezaAgujero(
        float valor)
    {
        if (botActual == null)
            return;


        botActual
            .SetPurezaDirectaAgujero(
                valor
            );


        if (sliderPurezaAgujero != null)
        {
            sliderPurezaAgujero
                .SetValueWithoutNotify(
                    botActual
                        .purezaDirectaAgujero
                );
        }


        ActualizarTextos();
    }


    // =====================================================
    // VISIBILIDAD OPCIONES DE DESTINO
    // =====================================================

    private void ActualizarVisibilidadDestino()
    {
        if (botActual == null)
            return;


        bool automatico =
            botActual.destinoTrabajo ==
            ConfiguracionBot
                .DestinoTrabajo
                .Automatico;


        // El umbral solo tiene sentido
        // cuando el destino es Automático.

        if (sliderPurezaAgujero != null)
        {
            sliderPurezaAgujero.gameObject
                .SetActive(
                    automatico
                );
        }


        if (textoPurezaAgujero != null)
        {
            textoPurezaAgujero.gameObject
                .SetActive(
                    automatico
                );
        }
    }


    // =====================================================
    // ACTUALIZAR TEXTOS
    // =====================================================

    private void ActualizarTextos()
    {
        if (botActual == null)
            return;


        // =================================================
        // NOMBRE
        // =================================================

        if (textoNombre != null)
        {
            textoNombre.text =
                botActual.nombreBot;
        }


        // =================================================
        // VELOCIDAD
        // =================================================

        if (textoVelocidad != null)
        {
            textoVelocidad.text =
                "Velocidad: " +
                botActual
                    .velocidadMovimiento
                    .ToString("0.0");
        }


        // =================================================
        // PUREZA MÍNIMA
        // =================================================

        if (textoPurezaMin != null)
        {
            textoPurezaMin.text =
                "Pureza mínima: " +
                botActual
                    .purezaMinima
                    .ToString("0") +
                "%";
        }


        // =================================================
        // PUREZA MÁXIMA
        // =================================================

        if (textoPurezaMax != null)
        {
            textoPurezaMax.text =
                "Pureza máxima: " +
                botActual
                    .purezaMaxima
                    .ToString("0") +
                "%";
        }


        // =================================================
        // PUREZA AUTOMÁTICA
        // =================================================

        if (textoPurezaAgujero != null)
        {
            textoPurezaAgujero.text =
                "Directa al agujero desde: " +
                botActual
                    .purezaDirectaAgujero
                    .ToString("0") +
                "%";
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
            case ConfiguracionBot
                .ModoBot.Trabajar:

                modo =
                    "Trabajando";

                break;


            case ConfiguracionBot
                .ModoBot.Pausa:

                modo =
                    "En pausa";

                break;


            case ConfiguracionBot
                .ModoBot.Parking:

                modo =
                    "En parking";

                break;


            default:

                modo =
                    botActual
                        .modoActual
                        .ToString();

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


    // =====================================================
    // PONER TEXTO BOTÓN
    // =====================================================

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
}