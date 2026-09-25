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
    // IMÁN
    // =====================================================

    [Header("Imán - Mejora Alcance")]
    public RecolectorMagnetico recolectorMagnetico;

    public Button botonMejorarIman;
    public TextMeshProUGUI textoBotonIman;

    public int precioImanNivel2 = 5;
    public int precioImanNivel3 = 10;

    [Header("Imán - Compra")]
    public int precioIman = 3;

    public Button botonComprarIman;
    public TextMeshProUGUI textoBotonComprarIman;

    public GameObject panelMejorasIman;


    // =====================================================
    // PROCESADORA
    // =====================================================

    [Header("Procesadora - Compra")]
    public MaquinaErosion maquinaErosion;

    public int precioProcesadora = 10;

    public Button botonComprarProcesadora;
    public TextMeshProUGUI textoBotonComprarProcesadora;

    public GameObject panelMejorasProcesadora;


    [Header("Procesadora - Velocidad")]
    public Button botonMejorarProcesadora;
    public TextMeshProUGUI textoBotonProcesadora;

    public int precioProcesadoraNivel2 = 10;
    public int precioProcesadoraNivel3 = 20;

    // =====================================================
    // MARTILLO / CONSTRUCCIÓN
    // =====================================================

    [Header("Martillo - Compra")]
    public int precioMartillo = 10;

    public Button botonComprarMartillo;
    public TextMeshProUGUI textoBotonMartillo;

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
        if (recolectorMagnetico == null)
        {
            recolectorMagnetico =
                FindFirstObjectByType<RecolectorMagnetico>();
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

        if (panelMejorasTorbellino != null)
        {
            panelMejorasTorbellino.SetActive(
                torbellinoComprado
            );
        }


        // =================================================
        // MEJORAS TORBELLINO
        // =================================================

        if (torbellinoComprado &&
            herramientaTorbellino != null)
        {
            // -------------------------
            // RADIO
            // -------------------------

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


            // -------------------------
            // ALCANCE
            // -------------------------

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


            // -------------------------
            // MOVILIDAD
            // -------------------------

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



        // =================================================
        // IMÁN
        // =================================================

        if (recolectorMagnetico != null)
        {
            bool imanComprado =
                recolectorMagnetico.EstaImanDesbloqueado();

            // BOTÓN DE COMPRA
            if (botonComprarIman != null)
            {
                botonComprarIman.gameObject.SetActive(true);
                botonComprarIman.interactable = !imanComprado;
            }

            // TEXTO DEL BOTÓN DE COMPRA
            if (textoBotonComprarIman != null)
            {
                if (imanComprado)
                {
                    textoBotonComprarIman.text = "COMPRADO";
                }
                else
                {
                    textoBotonComprarIman.text =
                        "COMPRAR IMÁN - " +
                        precioIman +
                        " monedas";
                }
            }

            // PANEL DE MEJORAS
            if (panelMejorasIman != null)
            {
                panelMejorasIman.SetActive(imanComprado);
            }

            // ACTUALIZAR MEJORAS
            if (imanComprado)
            {
                ActualizarMejoraIman();
            }
        }

        // =================================================
        // PROCESADORA
        // =================================================

        if (maquinaErosion != null)
        {
            bool procesadoraComprada =
                maquinaErosion.ProcesadoraDesbloqueada;


            // BOTÓN DE COMPRA
            if (botonComprarProcesadora != null)
            {
                botonComprarProcesadora.gameObject.SetActive(true);

                botonComprarProcesadora.interactable =
                    !procesadoraComprada;
            }


            if (textoBotonComprarProcesadora != null)
            {
                textoBotonComprarProcesadora.text =
                    procesadoraComprada
                    ? "COMPRADO"
                    : "COMPRAR PROCESADORA - " +
                      precioProcesadora +
                      " monedas";
            }


            // PANEL DE MEJORAS
            if (panelMejorasProcesadora != null)
            {
                panelMejorasProcesadora.SetActive(
                    procesadoraComprada
                );
            }


            // MEJORAS
            if (procesadoraComprada)
            {
                if (maquinaErosion.NivelProcesado == 1)
                {
                    if (textoBotonProcesadora != null)
                    {
                        textoBotonProcesadora.text =
                            "VELOCIDAD NIVEL 2 - " +
                            precioProcesadoraNivel2 +
                            " monedas";
                    }

                    if (botonMejorarProcesadora != null)
                    {
                        botonMejorarProcesadora.interactable = true;
                    }
                }
                else if (maquinaErosion.NivelProcesado == 2)
                {
                    if (textoBotonProcesadora != null)
                    {
                        textoBotonProcesadora.text =
                            "VELOCIDAD NIVEL 3 - " +
                            precioProcesadoraNivel3 +
                            " monedas";
                    }

                    if (botonMejorarProcesadora != null)
                    {
                        botonMejorarProcesadora.interactable = true;
                    }
                }
                else
                {
                    if (textoBotonProcesadora != null)
                    {
                        textoBotonProcesadora.text =
                            "VELOCIDAD MÁXIMA";
                    }

                    if (botonMejorarProcesadora != null)
                    {
                        botonMejorarProcesadora.interactable = false;
                    }
                }
            }
        }
        // =================================================
        // MARTILLO
        // =================================================

        bool martilloComprado =
            gestorEquipamiento.MartilloDesbloqueado();


        if (textoBotonMartillo != null)
        {
            textoBotonMartillo.text =
                martilloComprado
                ? "COMPRADO"
                : "COMPRAR MARTILLO - " +
                  precioMartillo +
                  " monedas";
        }


        if (botonComprarMartillo != null)
        {
            botonComprarMartillo.interactable =
                !martilloComprado;
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

    public void ComprarMejoraAlcanceIman()
    {
        if (cartera == null ||
            recolectorMagnetico == null)
        {
            return;
        }

        // Tiene que estar comprado ANTES de mejorar
        if (!recolectorMagnetico.EstaImanDesbloqueado())
        {
            Debug.Log("Primero tienes que comprar el Imán.");
            return;
        }

        if (recolectorMagnetico.AlcanceAlMaximo())
        {
            Debug.Log(
                "El alcance del Imán ya está al máximo."
            );

            return;
        }

        int precio;

        if (recolectorMagnetico.NivelAlcance == 1)
        {
            precio = precioImanNivel2;
        }
        else if (recolectorMagnetico.NivelAlcance == 2)
        {
            precio = precioImanNivel3;
        }
        else
        {
            return;
        }

        if (!cartera.GastarMonedas(precio))
        {
            Debug.Log(
                "No tienes monedas suficientes para mejorar el Imán."
            );

            return;
        }

        recolectorMagnetico.MejorarAlcance();

        Debug.Log(
            "Imán mejorado a nivel " +
            recolectorMagnetico.NivelAlcance
        );

        ActualizarTienda();
    }

    public void ComprarIman()
    {
        if (cartera == null ||
            recolectorMagnetico == null)
        {
            return;
        }

        if (recolectorMagnetico.EstaImanDesbloqueado())
        {
            return;
        }

        if (!cartera.GastarMonedas(precioIman))
        {
            Debug.Log(
                "No tienes monedas suficientes para comprar el Imán."
            );

            return;
        }

        recolectorMagnetico.DesbloquearIman();

        Debug.Log("¡Imán comprado!");

        ActualizarTienda();
    }

    private void ActualizarMejoraIman()
    {
        if (recolectorMagnetico == null)
            return;

        if (recolectorMagnetico.NivelAlcance == 1)
        {
            if (textoBotonIman != null)
            {
                textoBotonIman.text =
                    "MEJORAR ALCANCE NIVEL 2 - " +
                    precioImanNivel2 +
                    " monedas";
            }

            if (botonMejorarIman != null)
            {
                botonMejorarIman.interactable = true;
            }
        }
        else if (recolectorMagnetico.NivelAlcance == 2)
        {
            if (textoBotonIman != null)
            {
                textoBotonIman.text =
                    "MEJORAR ALCANCE NIVEL 3 - " +
                    precioImanNivel3 +
                    " monedas";
            }

            if (botonMejorarIman != null)
            {
                botonMejorarIman.interactable = true;
            }
        }
        else
        {
            if (textoBotonIman != null)
            {
                textoBotonIman.text =
                    "ALCANCE MÁXIMO";
            }

            if (botonMejorarIman != null)
            {
                botonMejorarIman.interactable = false;
            }
        }
    }

    public void ComprarProcesadora()
    {
        if (cartera == null ||
            maquinaErosion == null)
        {
            return;
        }

        if (maquinaErosion.ProcesadoraDesbloqueada)
            return;

        if (!cartera.GastarMonedas(precioProcesadora))
        {
            Debug.Log(
                "No tienes monedas suficientes para comprar la Procesadora."
            );

            return;
        }

        maquinaErosion.DesbloquearProcesadora();

        Debug.Log("¡Procesadora comprada!");

        ActualizarTienda();
    }

    public void ComprarMejoraProcesadora()
    {
        if (cartera == null ||
            maquinaErosion == null)
        {
            return;
        }

        if (!maquinaErosion.ProcesadoraDesbloqueada)
        {
            Debug.Log("Primero tienes que comprar la Procesadora.");
            return;
        }

        if (maquinaErosion.ProcesadoAlMaximo())
        {
            Debug.Log("La Procesadora ya está al máximo.");
            return;
        }

        int precio;

        if (maquinaErosion.NivelProcesado == 1)
        {
            precio = precioProcesadoraNivel2;
        }
        else if (maquinaErosion.NivelProcesado == 2)
        {
            precio = precioProcesadoraNivel3;
        }
        else
        {
            return;
        }

        if (!cartera.GastarMonedas(precio))
        {
            Debug.Log(
                "No tienes monedas suficientes para mejorar la Procesadora."
            );

            return;
        }

        maquinaErosion.MejorarProcesado();

        ActualizarTienda();
    }

    public void ComprarMartillo()
    {
        if (cartera == null ||
            gestorEquipamiento == null)
        {
            return;
        }


        // Ya comprado
        if (gestorEquipamiento.MartilloDesbloqueado())
        {
            return;
        }


        // Intentamos pagar
        if (!cartera.GastarMonedas(precioMartillo))
        {
            Debug.Log(
                "No tienes monedas suficientes para comprar el Martillo."
            );

            return;
        }


        // Desbloqueamos
        gestorEquipamiento.DesbloquearMartillo();


        Debug.Log(
            "¡Martillo comprado!"
        );


        ActualizarTienda();
    }
}