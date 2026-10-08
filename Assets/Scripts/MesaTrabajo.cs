using TMPro;
using UnityEngine;

public class MesaTrabajo : MonoBehaviour
{
    // =====================================================
    // INTERACCIÓN
    // =====================================================

    [Header("Interacción")]
    public KeyCode teclaInteractuar = KeyCode.E;


    // =====================================================
    // UI
    // =====================================================

    [Header("UI")]
    public GameObject panelTienda;
    public TextMeshProUGUI textoInteraccion;


    // =====================================================
    // TEXTO
    // =====================================================

    [Header("Texto")]
    public string mensajeInteraccion =
        "Pulsa E para abrir la tienda";


    // =====================================================
    // BLOQUEO DE GAMEPLAY
    // =====================================================

    [Header("Bloqueo de gameplay")]

    [Tooltip(
        "Arrastra aquí el componente CamaraPrimeraPersona."
    )]
    public CamaraPrimeraPersona controladorCamara;


    [Tooltip(
        "Arrastra aquí el componente ArmaLanzadora."
    )]
    public ArmaLanzadora armaLanzadora;


    // =====================================================
    // INTERNAS
    // =====================================================

    private bool jugadorCerca = false;

    private bool tiendaAbierta = false;

    private bool armaEstabaActiva = false;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        // =================================================
        // TIENDA CERRADA AL EMPEZAR
        // =================================================

        if (panelTienda != null)
        {
            panelTienda.SetActive(false);
        }


        // =================================================
        // TEXTO
        // =================================================

        if (textoInteraccion != null)
        {
            textoInteraccion.text =
                mensajeInteraccion;


            textoInteraccion
                .gameObject
                .SetActive(false);
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // =================================================
        // TIENDA ABIERTA
        // =================================================

        if (tiendaAbierta)
        {
            if (Input.GetKeyDown(
                    teclaInteractuar) ||
                Input.GetKeyDown(
                    KeyCode.Escape))
            {
                CerrarTienda();
            }


            return;
        }


        // =================================================
        // ABRIR TIENDA
        // =================================================

        if (jugadorCerca &&
            Input.GetKeyDown(
                teclaInteractuar))
        {
            AbrirTienda();
        }
    }


    // =====================================================
    // JUGADOR ENTRA EN ZONA
    // =====================================================

    private void OnTriggerEnter(
        Collider other)
    {
        if (!other.CompareTag("Player"))
            return;


        jugadorCerca =
            true;


        if (textoInteraccion != null &&
            !tiendaAbierta)
        {
            textoInteraccion
                .gameObject
                .SetActive(true);
        }
    }


    // =====================================================
    // JUGADOR SALE DE ZONA
    // =====================================================

    private void OnTriggerExit(
        Collider other)
    {
        if (!other.CompareTag("Player"))
            return;


        jugadorCerca =
            false;


        if (textoInteraccion != null)
        {
            textoInteraccion
                .gameObject
                .SetActive(false);
        }
    }


    // =====================================================
    // ABRIR TIENDA
    // =====================================================

    public void AbrirTienda()
    {
        if (panelTienda == null)
            return;


        if (tiendaAbierta)
            return;


        tiendaAbierta =
            true;


        // =================================================
        // MOSTRAR PANEL
        // =================================================

        panelTienda.SetActive(
            true
        );


        if (textoInteraccion != null)
        {
            textoInteraccion
                .gameObject
                .SetActive(false);
        }


        // =================================================
        // BLOQUEAR CÁMARA
        // =================================================

        if (controladorCamara != null)
        {
            controladorCamara
                .SetBloqueadaPorUI(
                    true
                );
        }


        // =================================================
        // BLOQUEAR ARMA
        // =================================================

        if (armaLanzadora != null)
        {
            // Guardamos cómo estaba
            // antes de abrir la tienda.

            armaEstabaActiva =
                armaLanzadora.enabled;


            armaLanzadora.enabled =
                false;
        }


        // =================================================
        // PAUSAR JUEGO
        // =================================================

        Time.timeScale =
            0f;


        // =================================================
        // LIBERAR RATÓN
        // =================================================

        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            true;
    }


    // =====================================================
    // CERRAR TIENDA
    // =====================================================

    public void CerrarTienda()
    {
        if (!tiendaAbierta)
            return;


        tiendaAbierta =
            false;


        // =================================================
        // OCULTAR PANEL
        // =================================================

        if (panelTienda != null)
        {
            panelTienda.SetActive(
                false
            );
        }


        // =================================================
        // REANUDAR JUEGO
        // =================================================

        Time.timeScale =
            1f;


        // =================================================
        // DESBLOQUEAR CÁMARA
        // =================================================

        if (controladorCamara != null)
        {
            controladorCamara
                .SetBloqueadaPorUI(
                    false
                );
        }


        // =================================================
        // RESTAURAR ARMA
        // =================================================

        if (armaLanzadora != null)
        {
            armaLanzadora.enabled =
                armaEstabaActiva;
        }


        // =================================================
        // BLOQUEAR RATÓN
        // =================================================

        Cursor.lockState =
            CursorLockMode.Locked;


        Cursor.visible =
            false;


        // =================================================
        // VOLVER A MOSTRAR INTERACCIÓN
        // =================================================

        if (textoInteraccion != null &&
            jugadorCerca)
        {
            textoInteraccion
                .gameObject
                .SetActive(true);
        }
    }


    // =====================================================
    // CONSULTAR ESTADO
    // =====================================================

    public bool EstaTiendaAbierta()
    {
        return tiendaAbierta;
    }


    // =====================================================
    // SEGURIDAD
    // =====================================================

    private void OnDisable()
    {
        if (!tiendaAbierta)
            return;


        // =================================================
        // REANUDAR JUEGO
        // =================================================

        Time.timeScale =
            1f;


        // =================================================
        // DESBLOQUEAR CÁMARA
        // =================================================

        if (controladorCamara != null)
        {
            controladorCamara
                .SetBloqueadaPorUI(
                    false
                );
        }


        // =================================================
        // RESTAURAR ARMA
        // =================================================

        if (armaLanzadora != null)
        {
            armaLanzadora.enabled =
                armaEstabaActiva;
        }


        // =================================================
        // CURSOR
        // =================================================

        Cursor.lockState =
            CursorLockMode.Locked;


        Cursor.visible =
            false;
    }
}