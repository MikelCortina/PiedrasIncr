using System.Collections.Generic;
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
    // TORBELLINO - PANEL
    // =====================================================

    [Header("Torbellino - Panel Mejoras")]

    public GameObject panelMejorasTorbellino;


    // =====================================================
    // TORBELLINO - RADIO
    // =====================================================

    [Header("Torbellino - Mejora Radio")]

    public Button botonMejorarRadio;
    public TextMeshProUGUI textoBotonRadio;

    public int precioRadioNivel2 = 5;
    public int precioRadioNivel3 = 10;


    // =====================================================
    // TORBELLINO - ALCANCE
    // =====================================================

    [Header("Torbellino - Mejora Alcance")]

    public Button botonMejorarAlcance;
    public TextMeshProUGUI textoBotonAlcance;

    public int precioAlcanceNivel2 = 5;
    public int precioAlcanceNivel3 = 10;


    // =====================================================
    // TORBELLINO - MOVILIDAD
    // =====================================================

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

    [Header("Procesadora")]

    public MaquinaErosion maquinaErosion;


    [Tooltip(
        "Se mantiene por compatibilidad, pero la procesadora " +
        "ya no se compra directamente desde la tienda."
    )]
    public int precioProcesadora = 10;


    [Tooltip(
        "Botón antiguo de compra. Ahora permanece oculto."
    )]
    public Button botonComprarProcesadora;


    [Tooltip(
        "Texto del antiguo botón de compra."
    )]
    public TextMeshProUGUI textoBotonComprarProcesadora;


    public GameObject panelMejorasProcesadora;


    // =====================================================
    // PROCESADORA - VELOCIDAD
    // =====================================================

    [Header("Procesadora - Velocidad")]

    public Button botonMejorarProcesadora;
    public TextMeshProUGUI textoBotonProcesadora;

    public int precioProcesadoraNivel2 = 10;
    public int precioProcesadoraNivel3 = 20;


    // =====================================================
    // MARTILLO
    // =====================================================

    [Header("Martillo - Compra")]

    public int precioMartillo = 10;

    public Button botonComprarMartillo;
    public TextMeshProUGUI textoBotonMartillo;


    // =====================================================
    // BOTS - COMPRA
    // =====================================================

    [Header("Bots - Compra")]

    [Tooltip(
        "Prefab completo del Bot que se creará al comprar."
    )]
    public GameObject prefabBot;


    [Tooltip(
        "Lugar donde aparece inicialmente el Bot."
    )]
    public Transform puntoSpawnBots;


    [Tooltip(
        "Gestor global de Bots."
    )]
    public GestorBots gestorBots;


    // =====================================================
    // BOTS - UI
    // =====================================================

    [Header("Bots - UI")]

    public Button botonComprarBot;

    public TextMeshProUGUI textoBotonBot;


    // =====================================================
    // BOTS - CONFIGURACIÓN
    // =====================================================

    [Header("Bots - Configuración")]

    [Min(1)]
    public int maximoBots = 4;


    [Tooltip(
        "Precio según cantidad actual de Bots. " +
        "Índice 0 = primer Bot."
    )]
    public List<int> preciosBots =
        new List<int>()
        {
            100,
            150,
            200,
            250
        };


    [Tooltip(
        "Precio utilizado si no existe un precio " +
        "específico en la lista."
    )]
    public int precioBotFallback = 250;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        // =================================================
        // CARTERA
        // =================================================

        if (cartera == null)
        {
            cartera =
                FindFirstObjectByType<Cartera>();
        }


        // =================================================
        // EQUIPAMIENTO
        // =================================================

        if (gestorEquipamiento == null)
        {
            gestorEquipamiento =
                FindFirstObjectByType<
                    GestorEquipamiento
                >();
        }


        // =================================================
        // TORBELLINO
        // =================================================

        if (herramientaTorbellino == null)
        {
            herramientaTorbellino =
                FindFirstObjectByType<
                    HerramientaTorbellino
                >();
        }


        // =================================================
        // IMÁN
        // =================================================

        if (recolectorMagnetico == null)
        {
            recolectorMagnetico =
                FindFirstObjectByType<
                    RecolectorMagnetico
                >();
        }


        // =================================================
        // PROCESADORA
        // =================================================

        if (maquinaErosion == null)
        {
            maquinaErosion =
                FindFirstObjectByType<
                    MaquinaErosion
                >();
        }


        // =================================================
        // BOTS
        // =================================================

        if (gestorBots == null)
        {
            gestorBots =
                GestorBots.Instancia;
        }


        if (gestorBots == null)
        {
            gestorBots =
                FindFirstObjectByType<
                    GestorBots
                >();
        }


        // =================================================
        // ACTUALIZAR
        // =================================================

        ActualizarTienda();
    }


    // =====================================================
    // ARMA DE LA LUNA
    // =====================================================

    public void ComprarArmaLuna()
    {
        if (cartera == null ||
            gestorEquipamiento == null)
        {
            return;
        }


        if (gestorEquipamiento
            .ArmaLanzadoraDesbloqueada())
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precioArmaLuna))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Arma de la Luna."
            );

            return;
        }


        gestorEquipamiento
            .DesbloquearArmaLanzadora();


        Debug.Log(
            "¡Arma de la Luna comprada!"
        );


        ActualizarTienda();
    }


    // =====================================================
    // TORBELLINO - COMPRA
    // =====================================================

    public void ComprarTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null)
        {
            return;
        }


        if (gestorEquipamiento
            .TorbellinoDesbloqueado())
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precioTorbellino))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Torbellino."
            );

            return;
        }


        gestorEquipamiento
            .DesbloquearTorbellino();


        Debug.Log(
            "¡Torbellino comprado!"
        );


        ActualizarTienda();
    }


    // =====================================================
    // TORBELLINO - MEJORAR RADIO
    // =====================================================

    public void ComprarMejoraRadioTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null ||
            herramientaTorbellino == null)
        {
            return;
        }


        if (!gestorEquipamiento
            .TorbellinoDesbloqueado())
        {
            Debug.Log(
                "Primero tienes que comprar el Torbellino."
            );

            return;
        }


        if (herramientaTorbellino
            .RadioAlMaximo())
        {
            Debug.Log(
                "El radio del Torbellino ya está al máximo."
            );

            return;
        }


        int precio;


        if (herramientaTorbellino
            .NivelRadio == 1)
        {
            precio =
                precioRadioNivel2;
        }
        else if (
            herramientaTorbellino
                .NivelRadio == 2)
        {
            precio =
                precioRadioNivel3;
        }
        else
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precio))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para mejorar el radio."
            );

            return;
        }


        herramientaTorbellino
            .MejorarRadio();


        Debug.Log(
            "Radio mejorado a nivel " +
            herramientaTorbellino
                .NivelRadio
        );


        ActualizarTienda();
    }


    // =====================================================
    // TORBELLINO - MEJORAR ALCANCE
    // =====================================================

    public void ComprarMejoraAlcanceTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null ||
            herramientaTorbellino == null)
        {
            return;
        }


        if (!gestorEquipamiento
            .TorbellinoDesbloqueado())
        {
            Debug.Log(
                "Primero tienes que comprar el Torbellino."
            );

            return;
        }


        if (herramientaTorbellino
            .AlcanceAlMaximo())
        {
            Debug.Log(
                "El alcance del Torbellino ya está al máximo."
            );

            return;
        }


        int precio;


        if (herramientaTorbellino
            .NivelAlcance == 1)
        {
            precio =
                precioAlcanceNivel2;
        }
        else if (
            herramientaTorbellino
                .NivelAlcance == 2)
        {
            precio =
                precioAlcanceNivel3;
        }
        else
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precio))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para mejorar el alcance."
            );

            return;
        }


        herramientaTorbellino
            .MejorarAlcance();


        Debug.Log(
            "Alcance mejorado a nivel " +
            herramientaTorbellino
                .NivelAlcance
        );


        ActualizarTienda();
    }


    // =====================================================
    // TORBELLINO - MEJORAR MOVILIDAD
    // =====================================================

    public void ComprarMejoraMovilidadTorbellino()
    {
        if (cartera == null ||
            gestorEquipamiento == null ||
            herramientaTorbellino == null)
        {
            return;
        }


        if (!gestorEquipamiento
            .TorbellinoDesbloqueado())
        {
            Debug.Log(
                "Primero tienes que comprar el Torbellino."
            );

            return;
        }


        if (herramientaTorbellino
            .MovilidadAlMaximo())
        {
            Debug.Log(
                "La movilidad del Torbellino " +
                "ya está al máximo."
            );

            return;
        }


        int precio;


        if (herramientaTorbellino
            .NivelMovilidad == 1)
        {
            precio =
                precioMovilidadNivel2;
        }
        else if (
            herramientaTorbellino
                .NivelMovilidad == 2)
        {
            precio =
                precioMovilidadNivel3;
        }
        else
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precio))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para mejorar la movilidad."
            );

            return;
        }


        herramientaTorbellino
            .MejorarMovilidad();


        Debug.Log(
            "Movilidad mejorada a nivel " +
            herramientaTorbellino
                .NivelMovilidad
        );


        ActualizarTienda();
    }


    // =====================================================
    // IMÁN - COMPRA
    // =====================================================

    public void ComprarIman()
    {
        if (cartera == null ||
            recolectorMagnetico == null)
        {
            return;
        }


        if (recolectorMagnetico
            .EstaImanDesbloqueado())
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precioIman))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Imán."
            );

            return;
        }


        recolectorMagnetico
            .DesbloquearIman();


        Debug.Log(
            "¡Imán comprado!"
        );


        ActualizarTienda();
    }


    // =====================================================
    // IMÁN - MEJORAR ALCANCE
    // =====================================================

    public void ComprarMejoraAlcanceIman()
    {
        if (cartera == null ||
            recolectorMagnetico == null)
        {
            return;
        }


        if (!recolectorMagnetico
            .EstaImanDesbloqueado())
        {
            Debug.Log(
                "Primero tienes que comprar el Imán."
            );

            return;
        }


        if (recolectorMagnetico
            .AlcanceAlMaximo())
        {
            Debug.Log(
                "El alcance del Imán ya está al máximo."
            );

            return;
        }


        int precio;


        if (recolectorMagnetico
            .NivelAlcance == 1)
        {
            precio =
                precioImanNivel2;
        }
        else if (
            recolectorMagnetico
                .NivelAlcance == 2)
        {
            precio =
                precioImanNivel3;
        }
        else
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precio))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para mejorar el Imán."
            );

            return;
        }


        recolectorMagnetico
            .MejorarAlcance();


        Debug.Log(
            "Imán mejorado a nivel " +
            recolectorMagnetico
                .NivelAlcance
        );


        ActualizarTienda();
    }


    // =====================================================
    // PROCESADORA - COMPRA ANTIGUA
    // =====================================================

    public void ComprarProcesadora()
    {
        /*
         * Ya no compramos la procesadora desde
         * la tienda.
         *
         * Se desbloquea disparándole monedas
         * físicamente en el mundo.
         */

        Debug.Log(
            "La Procesadora se desbloquea " +
            "disparándole monedas."
        );


        ActualizarTienda();
    }


    // =====================================================
    // PROCESADORA - MEJORAR
    // =====================================================

    public void ComprarMejoraProcesadora()
    {
        if (cartera == null ||
            maquinaErosion == null)
        {
            return;
        }


        if (!maquinaErosion
            .ProcesadoraDesbloqueada)
        {
            Debug.Log(
                "Primero tienes que desbloquear " +
                "la Procesadora."
            );

            return;
        }


        if (maquinaErosion
            .ProcesadoAlMaximo())
        {
            Debug.Log(
                "La Procesadora ya está al máximo."
            );

            return;
        }


        int precio;


        if (maquinaErosion
            .NivelProcesado == 1)
        {
            precio =
                precioProcesadoraNivel2;
        }
        else if (
            maquinaErosion
                .NivelProcesado == 2)
        {
            precio =
                precioProcesadoraNivel3;
        }
        else
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precio))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para mejorar la Procesadora."
            );

            return;
        }


        maquinaErosion
            .MejorarProcesado();


        ActualizarTienda();
    }


    // =====================================================
    // MARTILLO
    // =====================================================

    public void ComprarMartillo()
    {
        if (cartera == null ||
            gestorEquipamiento == null)
        {
            return;
        }


        if (gestorEquipamiento
            .MartilloDesbloqueado())
        {
            return;
        }


        if (!cartera.GastarMonedas(
                precioMartillo))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Martillo."
            );

            return;
        }


        gestorEquipamiento
            .DesbloquearMartillo();


        Debug.Log(
            "¡Martillo comprado!"
        );


        ActualizarTienda();
    }


    // =====================================================
    // BOT - COMPRA
    // =====================================================

    public void ComprarBot()
    {
        // =================================================
        // REFERENCIAS
        // =================================================

        if (cartera == null)
        {
            cartera =
                FindFirstObjectByType<
                    Cartera
                >();
        }


        if (gestorBots == null)
        {
            gestorBots =
                GestorBots.Instancia;
        }


        if (gestorBots == null)
        {
            gestorBots =
                FindFirstObjectByType<
                    GestorBots
                >();
        }


        // =================================================
        // COMPROBACIONES
        // =================================================

        if (cartera == null)
        {
            Debug.LogError(
                "TiendaTrabajo: no se ha encontrado Cartera."
            );

            return;
        }


        if (gestorBots == null)
        {
            Debug.LogError(
                "TiendaTrabajo: no se ha encontrado GestorBots."
            );

            return;
        }


        if (prefabBot == null)
        {
            Debug.LogError(
                "TiendaTrabajo: falta asignar el Prefab Bot."
            );

            return;
        }


        if (puntoSpawnBots == null)
        {
            Debug.LogError(
                "TiendaTrabajo: falta asignar Punto Spawn Bots."
            );

            return;
        }


        // =================================================
        // REQUIERE PROCESADORA
        // =================================================

        if (maquinaErosion == null ||
            !maquinaErosion
                .ProcesadoraDesbloqueada)
        {
            Debug.Log(
                "Primero tienes que desbloquear " +
                "la Procesadora."
            );


            ActualizarTienda();

            return;
        }


        // =================================================
        // CANTIDAD
        // =================================================

        int cantidadActual =
            gestorBots
                .ObtenerCantidadBots();


        if (cantidadActual >=
            maximoBots)
        {
            Debug.Log(
                "Ya has alcanzado el máximo de Bots: " +
                cantidadActual +
                "/" +
                maximoBots
            );


            ActualizarTienda();

            return;
        }


        // =================================================
        // PRECIO
        // =================================================

        int precio =
            ObtenerPrecioSiguienteBot(
                cantidadActual
            );


        // =================================================
        // PAGAR
        // =================================================

        if (!cartera.GastarMonedas(
                precio))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Bot."
            );

            return;
        }


        // =================================================
        // CREAR BOT
        // =================================================

        GameObject nuevoBot =
            Instantiate(
                prefabBot,
                puntoSpawnBots.position,
                puntoSpawnBots.rotation
            );


        if (nuevoBot == null)
        {
            Debug.LogError(
                "TiendaTrabajo: no se pudo crear el Bot."
            );

            return;
        }


        // =================================================
        // CONFIGURAR RECOLECTOR
        // =================================================

        BotRecolector recolector =
            nuevoBot.GetComponent<
                BotRecolector
            >();


        if (recolector != null)
        {
            recolector
                .aparecerEnParkingAlIniciar =
                true;


            recolector
                .empezarEnPausa =
                true;
        }


        // =================================================
        // CONFIGURACIÓN BOT
        // =================================================

        ConfiguracionBot configuracion =
            nuevoBot.GetComponent<
                ConfiguracionBot
            >();


        if (configuracion != null)
        {
            configuracion.modoActual =
                ConfiguracionBot
                    .ModoBot
                    .Pausa;


            // Registro inmediato.
            gestorBots
                .RegistrarBot(
                    configuracion
                );
        }


        // =================================================
        // DEBUG
        // =================================================

        string nombreBot =
            nuevoBot.name;


        if (configuracion != null)
        {
            nombreBot =
                configuracion
                    .nombreBot;
        }


        Debug.Log(
            "¡Bot comprado! " +
            nombreBot +
            " | Precio: " +
            precio +
            " monedas"
        );


        // =================================================
        // ACTUALIZAR
        // =================================================

        ActualizarTienda();
    }


    // =====================================================
    // PRECIO SIGUIENTE BOT
    // =====================================================

    private int ObtenerPrecioSiguienteBot(
        int cantidadActual)
    {
        if (preciosBots != null &&
            cantidadActual >= 0 &&
            cantidadActual <
            preciosBots.Count)
        {
            return Mathf.Max(
                0,
                preciosBots[
                    cantidadActual
                ]
            );
        }


        return Mathf.Max(
            0,
            precioBotFallback
        );
    }


    // =====================================================
    // ACTUALIZAR TIENDA
    // =====================================================

    private void ActualizarTienda()
    {
        // =================================================
        // ARMA
        // =================================================

        if (gestorEquipamiento != null)
        {
            bool armaComprada =
                gestorEquipamiento
                    .ArmaLanzadoraDesbloqueada();


            if (textoBotonArma != null)
            {
                textoBotonArma.text =
                    armaComprada
                    ? "COMPRADO"
                    : "COMPRAR - " +
                      precioArmaLuna +
                      " moneda";
            }


            if (botonComprarArma != null)
            {
                botonComprarArma
                    .interactable =
                    !armaComprada;
            }
        }


        // =================================================
        // TORBELLINO
        // =================================================

        if (gestorEquipamiento != null)
        {
            bool torbellinoComprado =
                gestorEquipamiento
                    .TorbellinoDesbloqueado();


            if (botonComprarTorbellino != null)
            {
                botonComprarTorbellino
                    .gameObject
                    .SetActive(
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
                panelMejorasTorbellino
                    .SetActive(
                        torbellinoComprado
                    );
            }


            // =================================================
            // MEJORAS TORBELLINO
            // =================================================

            if (torbellinoComprado &&
                herramientaTorbellino != null)
            {
                ActualizarMejorasTorbellino();
            }


            // =================================================
            // MARTILLO
            // =================================================

            bool martilloComprado =
                gestorEquipamiento
                    .MartilloDesbloqueado();


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
                botonComprarMartillo
                    .interactable =
                    !martilloComprado;
            }
        }


        // =================================================
        // IMÁN
        // =================================================

        if (recolectorMagnetico != null)
        {
            bool imanComprado =
                recolectorMagnetico
                    .EstaImanDesbloqueado();


            if (botonComprarIman != null)
            {
                botonComprarIman
                    .gameObject
                    .SetActive(true);


                botonComprarIman
                    .interactable =
                    !imanComprado;
            }


            if (textoBotonComprarIman != null)
            {
                if (imanComprado)
                {
                    textoBotonComprarIman.text =
                        "COMPRADO";
                }
                else
                {
                    textoBotonComprarIman.text =
                        "COMPRAR IMÁN - " +
                        precioIman +
                        " monedas";
                }
            }


            if (panelMejorasIman != null)
            {
                panelMejorasIman
                    .SetActive(
                        imanComprado
                    );
            }


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
            bool procesadoraDesbloqueada =
                maquinaErosion
                    .ProcesadoraDesbloqueada;


            // =================================================
            // NO SE COMPRA DESDE LA TIENDA
            // =================================================

            if (botonComprarProcesadora != null)
            {
                botonComprarProcesadora
                    .gameObject
                    .SetActive(false);
            }


            // =================================================
            // MOSTRAR MEJORAS SOLO AL DESBLOQUEARLA
            // =================================================

            if (panelMejorasProcesadora != null)
            {
                panelMejorasProcesadora
                    .SetActive(
                        procesadoraDesbloqueada
                    );
            }


            if (procesadoraDesbloqueada)
            {
                ActualizarMejoraProcesadora();
            }
        }
        else
        {
            if (botonComprarProcesadora != null)
            {
                botonComprarProcesadora
                    .gameObject
                    .SetActive(false);
            }


            if (panelMejorasProcesadora != null)
            {
                panelMejorasProcesadora
                    .SetActive(false);
            }
        }


        // =================================================
        // BOTS
        // =================================================

        ActualizarCompraBot();
    }


    // =====================================================
    // ACTUALIZAR MEJORAS TORBELLINO
    // =====================================================

    private void ActualizarMejorasTorbellino()
    {
        if (herramientaTorbellino == null)
            return;


        // =================================================
        // RADIO
        // =================================================

        if (herramientaTorbellino
            .NivelRadio == 1)
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
                botonMejorarRadio.interactable =
                    true;
            }
        }
        else if (
            herramientaTorbellino
                .NivelRadio == 2)
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
                botonMejorarRadio.interactable =
                    true;
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
                botonMejorarRadio.interactable =
                    false;
            }
        }


        // =================================================
        // ALCANCE
        // =================================================

        if (herramientaTorbellino
            .NivelAlcance == 1)
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
                botonMejorarAlcance.interactable =
                    true;
            }
        }
        else if (
            herramientaTorbellino
                .NivelAlcance == 2)
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
                botonMejorarAlcance.interactable =
                    true;
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
                botonMejorarAlcance.interactable =
                    false;
            }
        }


        // =================================================
        // MOVILIDAD
        // =================================================

        if (herramientaTorbellino
            .NivelMovilidad == 1)
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
                botonMejorarMovilidad
                    .interactable =
                    true;
            }
        }
        else if (
            herramientaTorbellino
                .NivelMovilidad == 2)
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
                botonMejorarMovilidad
                    .interactable =
                    true;
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
                botonMejorarMovilidad
                    .interactable =
                    false;
            }
        }
    }


    // =====================================================
    // ACTUALIZAR MEJORA IMÁN
    // =====================================================

    private void ActualizarMejoraIman()
    {
        if (recolectorMagnetico == null)
            return;


        if (recolectorMagnetico
            .NivelAlcance == 1)
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
                botonMejorarIman
                    .interactable =
                    true;
            }
        }
        else if (
            recolectorMagnetico
                .NivelAlcance == 2)
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
                botonMejorarIman
                    .interactable =
                    true;
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
                botonMejorarIman
                    .interactable =
                    false;
            }
        }
    }


    // =====================================================
    // ACTUALIZAR MEJORA PROCESADORA
    // =====================================================

    private void ActualizarMejoraProcesadora()
    {
        if (maquinaErosion == null)
            return;


        if (!maquinaErosion
            .ProcesadoraDesbloqueada)
        {
            return;
        }


        if (maquinaErosion
            .NivelProcesado == 1)
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
                botonMejorarProcesadora
                    .interactable =
                    true;
            }
        }
        else if (
            maquinaErosion
                .NivelProcesado == 2)
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
                botonMejorarProcesadora
                    .interactable =
                    true;
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
                botonMejorarProcesadora
                    .interactable =
                    false;
            }
        }
    }


    // =====================================================
    // ACTUALIZAR COMPRA BOT
    // =====================================================

    private void ActualizarCompraBot()
    {
        // =================================================
        // PRIMERO: REQUIERE PROCESADORA
        // =================================================

        if (maquinaErosion == null ||
            !maquinaErosion
                .ProcesadoraDesbloqueada)
        {
            if (textoBotonBot != null)
            {
                textoBotonBot.text =
                    "BOT BLOQUEADO\n" +
                    "REQUIERE PROCESADORA";
            }


            if (botonComprarBot != null)
            {
                botonComprarBot
                    .interactable =
                    false;
            }


            return;
        }


        // =================================================
        // BUSCAR GESTOR
        // =================================================

        if (gestorBots == null)
        {
            gestorBots =
                GestorBots.Instancia;
        }


        if (gestorBots == null)
        {
            gestorBots =
                FindFirstObjectByType<
                    GestorBots
                >();
        }


        // =================================================
        // SIN GESTOR
        // =================================================

        if (gestorBots == null)
        {
            if (textoBotonBot != null)
            {
                textoBotonBot.text =
                    "BOT NO DISPONIBLE";
            }


            if (botonComprarBot != null)
            {
                botonComprarBot
                    .interactable =
                    false;
            }


            return;
        }


        // =================================================
        // CANTIDAD ACTUAL
        // =================================================

        int cantidadActual =
            gestorBots
                .ObtenerCantidadBots();


        // =================================================
        // MÁXIMO
        // =================================================

        if (cantidadActual >=
            maximoBots)
        {
            if (textoBotonBot != null)
            {
                textoBotonBot.text =
                    "BOTS AL MÁXIMO (" +
                    cantidadActual +
                    "/" +
                    maximoBots +
                    ")";
            }


            if (botonComprarBot != null)
            {
                botonComprarBot
                    .interactable =
                    false;
            }


            return;
        }


        // =================================================
        // SIGUIENTE BOT
        // =================================================

        int precio =
            ObtenerPrecioSiguienteBot(
                cantidadActual
            );


        int numeroSiguienteBot =
            cantidadActual +
            1;


        if (textoBotonBot != null)
        {
            textoBotonBot.text =
                "COMPRAR BOT-" +
                numeroSiguienteBot
                    .ToString("00") +
                " - " +
                precio +
                " monedas";
        }


        if (botonComprarBot != null)
        {
            botonComprarBot
                .interactable =
                true;
        }
    }


    // =====================================================
    // REFRESCAR DESDE OTROS SISTEMAS
    // =====================================================

    public void RefrescarTienda()
    {
        ActualizarTienda();
    }
}