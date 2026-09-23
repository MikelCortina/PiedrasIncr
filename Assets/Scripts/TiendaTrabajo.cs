using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TiendaTrabajo : MonoBehaviour
{
    // =====================================================
    // REFERENCIAS
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
    // TORBELLINO
    // =====================================================

    [Header("Torbellino")]
    public int precioTorbellino = 1;

    public Button botonComprarTorbellino;
    public TextMeshProUGUI textoBotonTorbellino;

    public HerramientaTorbellino herramientaTorbellino;


    // =====================================================
    // MEJORAS DEL TORBELLINO
    // =====================================================

    [Header("Mejoras Radio Torbellino")]
    public int precioRadioNivel2 = 5;
    public int precioRadioNivel3 = 10;


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
        {
            return;
        }

        // Si ya está comprada, no hacemos nada
        if (gestorEquipamiento.ArmaLanzadoraDesbloqueada())
        {
            return;
        }

        // Intentamos pagar
        if (!cartera.GastarMonedas(precioArmaLuna))
        {
            Debug.Log(
                "No tienes monedas suficientes para comprar el Arma de la Luna."
            );

            return;
        }

        // Desbloqueamos el arma
        gestorEquipamiento.DesbloquearArmaLanzadora();

        Debug.Log("¡Arma de la Luna comprada!");

        ActualizarTienda();
    }


    // =====================================================
    // TORBELLINO
    // COMPRA + MEJORAS CON EL MISMO BOTÓN
    // =====================================================

    public void AccionTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null ||
            herramientaTorbellino == null)
        {
            return;
        }


        // -------------------------------------------------
        // 1. TODAVÍA NO TENEMOS EL TORBELLINO
        // -------------------------------------------------

        if (!gestorEquipamiento.TorbellinoDesbloqueado())
        {
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

            return;
        }


        // -------------------------------------------------
        // 2. RADIO YA ESTÁ AL MÁXIMO
        // -------------------------------------------------

        if (herramientaTorbellino.RadioAlMaximo())
        {
            Debug.Log(
                "El radio del Torbellino ya está al máximo."
            );

            return;
        }


        // -------------------------------------------------
        // 3. CALCULAMOS EL PRECIO DE LA SIGUIENTE MEJORA
        // -------------------------------------------------

        int precioMejora;

        if (herramientaTorbellino.NivelRadio == 1)
        {
            precioMejora = precioRadioNivel2;
        }
        else if (herramientaTorbellino.NivelRadio == 2)
        {
            precioMejora = precioRadioNivel3;
        }
        else
        {
            return;
        }


        // -------------------------------------------------
        // 4. PAGAMOS
        // -------------------------------------------------

        if (!cartera.GastarMonedas(precioMejora))
        {
            Debug.Log(
                "No tienes monedas suficientes para mejorar el Torbellino."
            );

            return;
        }


        // -------------------------------------------------
        // 5. MEJORAMOS
        // -------------------------------------------------

        herramientaTorbellino.MejorarRadio();

        Debug.Log(
            "Torbellino mejorado. Nivel de radio: " +
            herramientaTorbellino.NivelRadio
        );

        ActualizarTienda();
    }


    // =====================================================
    // ACTUALIZAR INTERFAZ
    // =====================================================

    private void ActualizarTienda()
    {
        if (gestorEquipamiento == null)
        {
            return;
        }


        // =================================================
        // ARMA DE LA LUNA
        // =================================================

        bool armaComprada =
            gestorEquipamiento.ArmaLanzadoraDesbloqueada();

        if (textoBotonArma != null)
        {
            if (armaComprada)
            {
                textoBotonArma.text = "COMPRADO";
            }
            else
            {
                textoBotonArma.text =
                    "COMPRAR - " +
                    precioArmaLuna +
                    " moneda";
            }
        }

        if (botonComprarArma != null)
        {
            botonComprarArma.interactable =
                !armaComprada;
        }


        // =================================================
        // TORBELLINO
        // =================================================

        bool torbellinoComprado =
            gestorEquipamiento.TorbellinoDesbloqueado();


        // -------------------------------------------------
        // TODAVÍA NO COMPRADO
        // -------------------------------------------------

        if (!torbellinoComprado)
        {
            if (textoBotonTorbellino != null)
            {
                textoBotonTorbellino.text =
                    "COMPRAR TORBELLINO - " +
                    precioTorbellino +
                    " monedas";
            }

            if (botonComprarTorbellino != null)
            {
                botonComprarTorbellino.interactable = true;
            }

            return;
        }


        // -------------------------------------------------
        // SI NO ENCONTRAMOS LA HERRAMIENTA
        // -------------------------------------------------

        if (herramientaTorbellino == null)
        {
            if (textoBotonTorbellino != null)
            {
                textoBotonTorbellino.text =
                    "ERROR TORBELLINO";
            }

            if (botonComprarTorbellino != null)
            {
                botonComprarTorbellino.interactable = false;
            }

            return;
        }


        // -------------------------------------------------
        // NIVEL 1 -> NIVEL 2
        // -------------------------------------------------

        if (herramientaTorbellino.NivelRadio == 1)
        {
            if (textoBotonTorbellino != null)
            {
                textoBotonTorbellino.text =
                    "MEJORAR RADIO NIVEL 2 - " +
                    precioRadioNivel2 +
                    " monedas";
            }

            if (botonComprarTorbellino != null)
            {
                botonComprarTorbellino.interactable = true;
            }
        }

        // -------------------------------------------------
        // NIVEL 2 -> NIVEL 3
        // -------------------------------------------------

        else if (herramientaTorbellino.NivelRadio == 2)
        {
            if (textoBotonTorbellino != null)
            {
                textoBotonTorbellino.text =
                    "MEJORAR RADIO NIVEL 3 - " +
                    precioRadioNivel3 +
                    " monedas";
            }

            if (botonComprarTorbellino != null)
            {
                botonComprarTorbellino.interactable = true;
            }
        }

        // -------------------------------------------------
        // NIVEL 3
        // -------------------------------------------------

        else
        {
            if (textoBotonTorbellino != null)
            {
                textoBotonTorbellino.text =
                    "RADIO MÁXIMO";
            }

            if (botonComprarTorbellino != null)
            {
                botonComprarTorbellino.interactable = false;
            }
        }
    }
}