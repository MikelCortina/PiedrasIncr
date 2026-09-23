using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TiendaTrabajo : MonoBehaviour
{
    // =====================================================
    // REFERENCIAS GENERALES
    // =====================================================

    [Header("Referencias")]
    public Cartera cartera;
    public GestorEquipamiento gestorEquipamiento;


    // =====================================================
    // ARMA DE LA LUNA
    // =====================================================

    [Header("Arma de la Luna")]
    public int precioArmaLuna = 1;

    public Button botonComprarArma;
    public TextMeshProUGUI textoBotonArma;


    // =====================================================
    // TORBELLINO - COMPRA
    // =====================================================

    [Header("Torbellino - Compra")]
    public int precioTorbellino = 1;

    public Button botonComprarTorbellino;
    public TextMeshProUGUI textoBotonTorbellino;

    public HerramientaTorbellino herramientaTorbellino;


    // =====================================================
    // PANEL DE MEJORAS
    // =====================================================

    [Header("Torbellino - Panel Mejoras")]
    public GameObject panelMejorasTorbellino;


    // =====================================================
    // MEJORA RADIO
    // =====================================================

    [Header("Torbellino - Mejora Radio")]
    public Button botonMejorarRadio;
    public TextMeshProUGUI textoBotonRadio;

    public int precioRadioNivel2 = 5;
    public int precioRadioNivel3 = 10;


    // =====================================================
    // MEJORA ALCANCE
    // =====================================================

    [Header("Torbellino - Mejora Alcance")]
    public Button botonMejorarAlcance;
    public TextMeshProUGUI textoBotonAlcance;

    public int precioAlcanceNivel2 = 5;
    public int precioAlcanceNivel3 = 10;

    [Header("Torbellino - Mejora Movilidad")]
    public Button botonMejorarMovilidad;
    public TextMeshProUGUI textoBotonMovilidad;

    public int precioMovilidadNivel2 = 5;
    public int precioMovilidadNivel3 = 10;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (cartera == null)
        {
            cartera = FindFirstObjectByType<Cartera>();
        }

        if (gestorEquipamiento == null)
        {
            gestorEquipamiento =
                FindFirstObjectByType<GestorEquipamiento>();
        }

        if (herramientaTorbellino == null)
        {
            herramientaTorbellino =
                FindFirstObjectByType<HerramientaTorbellino>();
        }

        ActualizarTienda();
    }


    // =====================================================
    // ARMA DE LA LUNA
    // =====================================================

    public void ComprarArmaLuna()
    {
        if (cartera == null || gestorEquipamiento == null)
            return;

        if (gestorEquipamiento.ArmaLanzadoraDesbloqueada())
            return;

        if (!cartera.GastarMonedas(precioArmaLuna))
        {
            Debug.Log(
                "No tienes monedas suficientes para comprar el Arma de la Luna."
            );

            return;
        }

        gestorEquipamiento.DesbloquearArmaLanzadora();

        Debug.Log("¡Arma de la Luna comprada!");

        ActualizarTienda();
    }


    // =====================================================
    // COMPRAR TORBELLINO
    // =====================================================

    public void ComprarTorbellino()
    {
        if (cartera == null || gestorEquipamiento == null)
            return;

        if (gestorEquipamiento.TorbellinoDesbloqueado())
            return;

        if (!cartera.GastarMonedas(precioTorbellino))
        {
            Debug.Log(
                "No tienes monedas suficientes para comprar el Torbellino."
            );

            return;
        }

        gestorEquipamiento.DesbloquearTorbellino();

        Debug.Log("¡Torbellino comprado!");

        ActualizarTienda();
    }


    // =====================================================
    // MEJORAR RADIO
    // =====================================================

    public void ComprarMejoraRadioTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null ||
            herramientaTorbellino == null)
        {
            return;
        }

        if (!gestorEquipamiento.TorbellinoDesbloqueado())
        {
            Debug.Log("Primero tienes que comprar el Torbellino.");
            return;
        }

        if (herramientaTorbellino.RadioAlMaximo())
        {
            Debug.Log("El radio del Torbellino ya está al máximo.");
            return;
        }

        int precio;

        if (herramientaTorbellino.NivelRadio == 1)
        {
            precio = precioRadioNivel2;
        }
        else if (herramientaTorbellino.NivelRadio == 2)
        {
            precio = precioRadioNivel3;
        }
        else
        {
            return;
        }

        if (!cartera.GastarMonedas(precio))
        {
            Debug.Log(
                "No tienes monedas suficientes para mejorar el radio."
            );

            return;
        }

        herramientaTorbellino.MejorarRadio();

        Debug.Log(
            "Radio mejorado a nivel " +
            herramientaTorbellino.NivelRadio
        );

        ActualizarTienda();
    }


    // =====================================================
    // MEJORAR ALCANCE
    // =====================================================

    public void ComprarMejoraAlcanceTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null ||
            herramientaTorbellino == null)
        {
            return;
        }

        if (!gestorEquipamiento.TorbellinoDesbloqueado())
        {
            Debug.Log("Primero tienes que comprar el Torbellino.");
            return;
        }

        if (herramientaTorbellino.AlcanceAlMaximo())
        {
            Debug.Log("El alcance del Torbellino ya está al máximo.");
            return;
        }

        int precio;

        if (herramientaTorbellino.NivelAlcance == 1)
        {
            precio = precioAlcanceNivel2;
        }
        else if (herramientaTorbellino.NivelAlcance == 2)
        {
            precio = precioAlcanceNivel3;
        }
        else
        {
            return;
        }

        if (!cartera.GastarMonedas(precio))
        {
            Debug.Log(
                "No tienes monedas suficientes para mejorar el alcance."
            );

            return;
        }

        herramientaTorbellino.MejorarAlcance();

        Debug.Log(
            "Alcance mejorado a nivel " +
            herramientaTorbellino.NivelAlcance
        );

        ActualizarTienda();
    }


    // =====================================================
    // ACTUALIZAR TIENDA
    // =====================================================

    private void ActualizarTienda()
    {
        if (gestorEquipamiento == null)
            return;


        // =================================================
        // ARMA DE LA LUNA
        // =================================================

        bool armaComprada =
            gestorEquipamiento.ArmaLanzadoraDesbloqueada();

        if (textoBotonArma != null)
        {
            textoBotonArma.text = armaComprada
                ? "COMPRADO"
                : "COMPRAR - " + precioArmaLuna + " moneda";
        }

        if (botonComprarArma != null)
        {
            botonComprarArma.interactable = !armaComprada;
        }


        // =================================================
        // TORBELLINO
        // =================================================

        bool torbellinoComprado =
            gestorEquipamiento.TorbellinoDesbloqueado();


        // -------------------------------------------------
        // BOTÓN DE COMPRA
        // -------------------------------------------------

        if (botonComprarTorbellino != null)
        {
            botonComprarTorbellino.gameObject.SetActive(
                !torbellinoComprado
            );
        }

        if (textoBotonTorbellino != null)
        {
            textoBotonTorbellino.text =
                "COMPRAR TORBELLINO - " +
                precioTorbellino +
                " monedas";
        }


        // -------------------------------------------------
        // MOSTRAR/OCULTAR MEJORAS
        // -------------------------------------------------

        if (panelMejorasTorbellino != null)
        {
            panelMejorasTorbellino.SetActive(
                torbellinoComprado
            );
        }


        // Si todavía no está comprado,
        // no necesitamos actualizar las mejoras.
        if (!torbellinoComprado)
            return;

        if (herramientaTorbellino == null)
            return;


        // =================================================
        // RADIO
        // =================================================

        if (herramientaTorbellino.NivelRadio == 1)
        {
            if (textoBotonRadio != null)
            {
                textoBotonRadio.text =
                    "MEJORAR RADIO NIVEL 2 - " +
                    precioRadioNivel2 +
                    " monedas";
            }

            if (botonMejorarRadio != null)
            {
                botonMejorarRadio.interactable = true;
            }
        }
        else if (herramientaTorbellino.NivelRadio == 2)
        {
            if (textoBotonRadio != null)
            {
                textoBotonRadio.text =
                    "MEJORAR RADIO NIVEL 3 - " +
                    precioRadioNivel3 +
                    " monedas";
            }

            if (botonMejorarRadio != null)
            {
                botonMejorarRadio.interactable = true;
            }
        }
        else
        {
            if (textoBotonRadio != null)
            {
                textoBotonRadio.text =
                    "RADIO NIVEL MÁXIMO";
            }

            if (botonMejorarRadio != null)
            {
                botonMejorarRadio.interactable = false;
            }
        }


        // =================================================
        // ALCANCE
        // =================================================

        if (herramientaTorbellino.NivelAlcance == 1)
        {
            if (textoBotonAlcance != null)
            {
                textoBotonAlcance.text =
                    "MEJORAR ALCANCE NIVEL 2 - " +
                    precioAlcanceNivel2 +
                    " monedas";
            }

            if (botonMejorarAlcance != null)
            {
                botonMejorarAlcance.interactable = true;
            }
        }
        else if (herramientaTorbellino.NivelAlcance == 2)
        {
            if (textoBotonAlcance != null)
            {
                textoBotonAlcance.text =
                    "MEJORAR ALCANCE NIVEL 3 - " +
                    precioAlcanceNivel3 +
                    " monedas";
            }

            if (botonMejorarAlcance != null)
            {
                botonMejorarAlcance.interactable = true;
            }
        }
        else
        {
            if (textoBotonAlcance != null)
            {
                textoBotonAlcance.text =
                    "ALCANCE NIVEL MÁXIMO";
            }

            if (botonMejorarAlcance != null)
            {
                botonMejorarAlcance.interactable = false;
            }
        }

        // =================================================
        // MOVILIDAD
        // =================================================

        if (herramientaTorbellino.NivelMovilidad == 1)
        {
            if (textoBotonMovilidad != null)
            {
                textoBotonMovilidad.text =
                    "MEJORAR MOVILIDAD NIVEL 2 - " +
                    precioMovilidadNivel2 +
                    " monedas";
            }

            if (botonMejorarMovilidad != null)
            {
                botonMejorarMovilidad.interactable = true;
            }
        }
        else if (herramientaTorbellino.NivelMovilidad == 2)
        {
            if (textoBotonMovilidad != null)
            {
                textoBotonMovilidad.text =
                    "MEJORAR MOVILIDAD NIVEL 3 - " +
                    precioMovilidadNivel3 +
                    " monedas";
            }

            if (botonMejorarMovilidad != null)
            {
                botonMejorarMovilidad.interactable = true;
            }
        }
        else
        {
            if (textoBotonMovilidad != null)
            {
                textoBotonMovilidad.text =
                    "MOVILIDAD NIVEL MÁXIMO";
            }

            if (botonMejorarMovilidad != null)
            {
                botonMejorarMovilidad.interactable = false;
            }
        }
    }

    public void ComprarMejoraMovilidadTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null ||
            herramientaTorbellino == null)
        {
            return;
        }

        if (!gestorEquipamiento.TorbellinoDesbloqueado())
        {
            Debug.Log("Primero tienes que comprar el Torbellino.");
            return;
        }

        if (herramientaTorbellino.MovilidadAlMaximo())
        {
            Debug.Log("La movilidad del Torbellino ya está al máximo.");
            return;
        }

        int precio;

        if (herramientaTorbellino.NivelMovilidad == 1)
        {
            precio = precioMovilidadNivel2;
        }
        else if (herramientaTorbellino.NivelMovilidad == 2)
        {
            precio = precioMovilidadNivel3;
        }
        else
        {
            return;
        }

        if (!cartera.GastarMonedas(precio))
        {
            Debug.Log(
                "No tienes monedas suficientes para mejorar la movilidad."
            );

            return;
        }

        herramientaTorbellino.MejorarMovilidad();

        Debug.Log(
            "Movilidad mejorada a nivel " +
            herramientaTorbellino.NivelMovilidad
        );

        ActualizarTienda();
    }
}