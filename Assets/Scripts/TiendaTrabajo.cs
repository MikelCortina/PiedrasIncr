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

    [Header("Economía demo v2")]
    [Tooltip("Lee los precios de EconomiaDemo (Excel). El botón del Arma de la Luna compra y mejora el Fusil Monedas (1-5) y el de Radio sube el nivel del Torbellino (2-5).")]
    public bool usarEconomiaDemo = true;

    private EconomiaDemo Eco => ProveedorEconomia.Actual;

    private int P(string id, int nivel, int respaldo)
    {
        return usarEconomiaDemo ? Eco.CosteOPorDefecto(id, nivel, respaldo) : respaldo;
    }

    private int Ep_precioBotasNivel1 => P(EconomiaDemo.Ids.Botas, 1, precioBotasNivel1);
    private int Ep_precioBotasNivel2 => P(EconomiaDemo.Ids.Botas, 2, precioBotasNivel2);
    private int Ep_precioBotasNivel3 => P(EconomiaDemo.Ids.Botas, 3, precioBotasNivel3);
    private int Ep_precioJetpackNivel1 => P(EconomiaDemo.Ids.Jetpack, 1, precioJetpackNivel1);
    private int Ep_precioJetpackNivel2 => P(EconomiaDemo.Ids.Jetpack, 2, precioJetpackNivel2);
    private int Ep_precioJetpackNivel3 => P(EconomiaDemo.Ids.Jetpack, 3, precioJetpackNivel3);
    private int Ep_precioJetpackNivel4 => P(EconomiaDemo.Ids.Jetpack, 4, precioJetpackNivel4);
    private int Ep_precioTorbellino => P(EconomiaDemo.Ids.Torbellino, 1, precioTorbellino);
    private int Ep_precioAlcanceNivel2 => P(EconomiaDemo.Ids.TorbellinoAlcance, 2, precioAlcanceNivel2);
    private int Ep_precioAlcanceNivel3 => P(EconomiaDemo.Ids.TorbellinoAlcance, 3, precioAlcanceNivel3);
    private int Ep_precioMovilidadNivel2 => P(EconomiaDemo.Ids.TorbellinoMovilidad, 2, precioMovilidadNivel2);
    private int Ep_precioMovilidadNivel3 => P(EconomiaDemo.Ids.TorbellinoMovilidad, 3, precioMovilidadNivel3);
    private int Ep_precioImanNivel2 => P(EconomiaDemo.Ids.ImanAlcance, 2, precioImanNivel2);
    private int Ep_precioImanNivel3 => P(EconomiaDemo.Ids.ImanAlcance, 3, precioImanNivel3);
    private int Ep_precioIman => P(EconomiaDemo.Ids.Iman, 1, precioIman);
    private int Ep_precioMartillo => P(EconomiaDemo.Ids.Martillo, 1, precioMartillo);

    [Header("Botas de velocidad")]
    public MovimientoPersonaje movimientoPersonaje;
    public Button botonComprarBotas;
    public TextMeshProUGUI textoBotonBotas;
    [HideInInspector] [Min(1)] public int precioBotasNivel1 = 5;
    [HideInInspector] [Min(1)] public int precioBotasNivel2 = 10;
    [HideInInspector] [Min(1)] public int precioBotasNivel3 = 20;



    // =====================================================
    // ARMA DE LA LUNA
    // =====================================================

    [Header("Jetpack")]
    public Button botonComprarJetpack;
    public TextMeshProUGUI textoBotonJetpack;
    [HideInInspector] [Min(1)] public int precioJetpackNivel1 = 5;
    [HideInInspector] [Min(1)] public int precioJetpackNivel2 = 10;
    [HideInInspector] [Min(1)] public int precioJetpackNivel3 = 20;
    [HideInInspector] [Min(1)] public int precioJetpackNivel4 = 40;


    [Header("Arma de la Luna")]

    public int precioArmaLuna = 1;

    public Button botonComprarArma;
    public TextMeshProUGUI textoBotonArma;


    // =====================================================
    // TORBELLINO - COMPRA
    // =====================================================

    [Header("Torbellino - Compra")]

    [HideInInspector] public int precioTorbellino = 1;

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
    public int precioRadioNivel4 = 20;


    // =====================================================
    // TORBELLINO - ALCANCE
    // =====================================================

    [Header("Torbellino - Mejora Alcance")]

    public Button botonMejorarAlcance;
    public TextMeshProUGUI textoBotonAlcance;

    [HideInInspector] public int precioAlcanceNivel2 = 5;
    [HideInInspector] public int precioAlcanceNivel3 = 10;


    // =====================================================
    // TORBELLINO - MOVILIDAD
    // =====================================================

    [Header("Torbellino - Mejora Movilidad")]

    public Button botonMejorarMovilidad;
    public TextMeshProUGUI textoBotonMovilidad;

    [HideInInspector] public int precioMovilidadNivel2 = 5;
    [HideInInspector] public int precioMovilidadNivel3 = 10;


    // =====================================================
    // IMÁN
    // =====================================================

    [Header("Imán - Mejora Alcance")]

    public RecolectorMagnetico recolectorMagnetico;

    public Button botonMejorarIman;
    public TextMeshProUGUI textoBotonIman;

    [HideInInspector] public int precioImanNivel2 = 5;
    [HideInInspector] public int precioImanNivel3 = 10;


    [Header("Imán - Compra")]

    [HideInInspector] public int precioIman = 3;

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

    [HideInInspector] public int precioMartillo = 10;

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


    [Tooltip(
        "Generador de arañas: si hay uno, los Bots comprados " +
        "salen por su puerta en vez de aparecer en " +
        "Punto Spawn Bots. Vacío = se busca en la escena."
    )]
    public GeneradorAranas generadorAranas;

    [Header("Demo (recorte de alcance)")]
    [Tooltip("Desactivado en la demo: no hay Desgastadora, así que los bots se compran sin ella.")]
    public bool botsRequierenProcesadora = false;

    // Solo cuenta si existe, está activa y desbloqueada (la demo la desactiva).
    private bool ProcesadoraDisponible =>
        maquinaErosion != null &&
        maquinaErosion.isActiveAndEnabled &&
        maquinaErosion.ProcesadoraDesbloqueada;

    private bool BotBloqueadoPorProcesadora =>
        botsRequierenProcesadora && !ProcesadoraDisponible;

    // Generador de Arañas desactivado = fuera de la demo: se usa puntoSpawnBots.
    private GeneradorAranas GeneradorActivo =>
        generadorAranas != null && generadorAranas.isActiveAndEnabled
            ? generadorAranas
            : null;


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

        // Incluye inactivos: la máquina puede estar oculta
        // hasta que se construye.
        BuscarProcesadora();


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


        if (generadorAranas == null)
        {
            generadorAranas =
                FindFirstObjectByType<
                    GeneradorAranas
                >();
        }


        // =================================================
        // ACTUALIZAR
        // =================================================

        if (movimientoPersonaje == null)
            movimientoPersonaje = FindFirstObjectByType<MovimientoPersonaje>();

        ActualizarTienda();
    }


    // =====================================================
    // ARMA DE LA LUNA
    // =====================================================

    public void ComprarArmaLuna()
    {
        if (usarEconomiaDemo)
        {
            ComprarFusilDemo();
            return;
        }

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


        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.FusilMonedas, 1, precioArmaLuna);


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
                Ep_precioTorbellino))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Torbellino."
            );

            return;
        }


        gestorEquipamiento
            .DesbloquearTorbellino();


        if (herramientaTorbellino != null) herramientaTorbellino.RestaurarNivelCapacidad(1);


        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Torbellino, 1, Ep_precioTorbellino);


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
        if (usarEconomiaDemo)
        {
            ComprarNivelTorbellinoDemo();
            return;
        }

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
        else if (
            herramientaTorbellino
                .NivelRadio == 3)
        {
            precio =
                precioRadioNivel4;
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
                Ep_precioAlcanceNivel2;
        }
        else if (
            herramientaTorbellino
                .NivelAlcance == 2)
        {
            precio =
                Ep_precioAlcanceNivel3;
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


        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.TorbellinoAlcance, herramientaTorbellino.NivelAlcance, precio);


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
                Ep_precioMovilidadNivel2;
        }
        else if (
            herramientaTorbellino
                .NivelMovilidad == 2)
        {
            precio =
                Ep_precioMovilidadNivel3;
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


        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.TorbellinoMovilidad, herramientaTorbellino.NivelMovilidad, precio);


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
                Ep_precioIman))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Imán."
            );

            return;
        }


        recolectorMagnetico
            .DesbloquearIman();


        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Iman, 1, Ep_precioIman);


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
                Ep_precioImanNivel2;
        }
        else if (
            recolectorMagnetico
                .NivelAlcance == 2)
        {
            precio =
                Ep_precioImanNivel3;
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


        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.ImanAlcance, recolectorMagnetico.NivelAlcance, precio);


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


        if (!ProcesadoraDisponible)
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
                Ep_precioMartillo))
        {
            Debug.Log(
                "No tienes monedas suficientes " +
                "para comprar el Martillo."
            );

            return;
        }


        gestorEquipamiento
            .DesbloquearMartillo();


        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Martillo, 1, Ep_precioMartillo);


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


        if (puntoSpawnBots == null &&
            GeneradorActivo == null)
        {
            Debug.LogError(
                "TiendaTrabajo: falta asignar Punto Spawn Bots."
            );

            return;
        }


        // =================================================
        // REQUIERE PROCESADORA
        // =================================================

        BuscarProcesadora();

        if (BotBloqueadoPorProcesadora)
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

        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Bot, cantidadActual + 1, precio);


        // =================================================
        // CREAR BOT
        // =================================================

        // Con generador, el Bot se crea dentro de él
        // (inactivo) y sale por su puerta.
        GeneradorAranas generador = GeneradorActivo;

        GameObject nuevoBot =
            generador != null
            ? generador.CrearBot(
                prefabBot
            )
            : Instantiate(
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
        // SALIR POR LA PUERTA DEL GENERADOR
        // =================================================

        if (generador != null)
        {
            generador.SacarBot(
                nuevoBot
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
    // ¿SE PUEDE COMPRAR UN BOT? (para mostrarlo en la UI)
    // =====================================================

    private void BuscarProcesadora()
    {
        if (maquinaErosion != null)
            return;

        maquinaErosion =
            FindFirstObjectByType<MaquinaErosion>(
                FindObjectsInactive.Include
            );
    }


    // Mismas comprobaciones que ComprarBot, pero sin comprar:
    // devuelve el texto que debe mostrar el botón.
    public bool PuedeComprarBot(
        out string texto)
    {
        BuscarProcesadora();

        if (cartera == null)
            cartera = FindFirstObjectByType<Cartera>();

        if (gestorBots == null)
            gestorBots = GestorBots.Instancia;

        if (gestorBots == null)
            gestorBots = FindFirstObjectByType<GestorBots>();

        if (BotBloqueadoPorProcesadora)
        {
            texto = "BLOQUEADO\nREQUIERE PROCESADORA";
            return false;
        }

        if (cartera == null ||
            gestorBots == null ||
            prefabBot == null)
        {
            texto = "NO DISPONIBLE";
            return false;
        }

        int cantidadActual =
            gestorBots.ObtenerCantidadBots();

        if (cantidadActual >= maximoBots)
        {
            texto =
                "AL MÁXIMO (" + cantidadActual +
                "/" + maximoBots + ")";
            return false;
        }

        int precio =
            ObtenerPrecioSiguienteBot(cantidadActual);

        if (!cartera.PuedeGastar(precio))
        {
            texto =
                "FALTAN MONEDAS\n" +
                cartera.Monedas + " / " + precio;
            return false;
        }

        texto =
            "COMPRAR BOT-" +
            (cantidadActual + 1).ToString("00") +
            " - " + precio + " monedas";
        return true;
    }


    // =====================================================
    // PRECIO SIGUIENTE BOT
    // =====================================================

    private int ObtenerPrecioSiguienteBot(
        int cantidadActual)
    {
        if (usarEconomiaDemo && Eco.Tiene(EconomiaDemo.Ids.Bot))
        {
            return Eco.CosteOPorDefecto(EconomiaDemo.Ids.Bot, cantidadActual + 1, precioBotFallback);
        }

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
        ActualizarTiendaBase();
        if (usarEconomiaDemo) ActualizarBotonesDemo();
    }

    private ArmaLanzadora FusilMonedas => gestorEquipamiento != null ? gestorEquipamiento.armaLanzadora : null;

    private void ComprarFusilDemo()
    {
        if (cartera == null || gestorEquipamiento == null) return;
        ArmaLanzadora fusil = FusilMonedas;

        if (!gestorEquipamiento.ArmaLanzadoraDesbloqueada())
        {
            int coste = Eco.Coste(EconomiaDemo.Ids.FusilMonedas, 1);
            if (!cartera.GastarMonedas(coste))
            {
                Debug.Log("No tienes monedas suficientes para el Fusil Monedas.");
                return;
            }
            gestorEquipamiento.DesbloquearArmaLanzadora();
            if (fusil != null) fusil.RestaurarNivelFusil(1);
            EventosEconomia.NotificarCompra(EconomiaDemo.Ids.FusilMonedas, 1, coste);
        }
        else
        {
            if (fusil == null || fusil.NivelFusil >= Eco.NivelesMax(EconomiaDemo.Ids.FusilMonedas)) return;
            int nivel = fusil.NivelFusil + 1;
            int coste = Eco.Coste(EconomiaDemo.Ids.FusilMonedas, nivel);
            if (!cartera.GastarMonedas(coste))
            {
                Debug.Log("No tienes monedas suficientes para mejorar el Fusil Monedas.");
                return;
            }
            fusil.MejorarFusil();
            EventosEconomia.NotificarCompra(EconomiaDemo.Ids.FusilMonedas, nivel, coste);
        }

        ActualizarTienda();
    }

    private void ComprarNivelTorbellinoDemo()
    {
        if (cartera == null || gestorEquipamiento == null || herramientaTorbellino == null) return;
        if (!gestorEquipamiento.TorbellinoDesbloqueado() || herramientaTorbellino.CapacidadAlMaximo()) return;

        int nivel = herramientaTorbellino.NivelCapacidad + 1;
        int coste = Eco.Coste(EconomiaDemo.Ids.Torbellino, nivel);
        if (!cartera.GastarMonedas(coste))
        {
            Debug.Log("No tienes monedas suficientes para mejorar el Torbellino.");
            return;
        }

        herramientaTorbellino.MejorarCapacidad();
        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Torbellino, nivel, coste);
        ActualizarTienda();
    }

    private void ActualizarBotonesDemo()
    {
        if (gestorEquipamiento == null) return;

        bool fusilComprado = gestorEquipamiento.ArmaLanzadoraDesbloqueada();
        ArmaLanzadora fusil = FusilMonedas;
        int nivelFusil = fusilComprado && fusil != null ? fusil.NivelFusil : 0;
        bool fusilMaximo = fusilComprado && (fusil == null || nivelFusil >= Eco.NivelesMax(EconomiaDemo.Ids.FusilMonedas));
        if (textoBotonArma != null)
        {
            if (!fusilComprado) textoBotonArma.text = "COMPRAR FUSIL MONEDAS - " + Eco.Coste(EconomiaDemo.Ids.FusilMonedas, 1) + " monedas";
            else if (fusilMaximo) textoBotonArma.text = "FUSIL NIVEL MÁXIMO";
            else textoBotonArma.text = "MEJORAR FUSIL NIVEL " + (nivelFusil + 1) + " - " + Eco.Coste(EconomiaDemo.Ids.FusilMonedas, nivelFusil + 1) + " monedas";
        }
        if (botonComprarArma != null) botonComprarArma.interactable = !fusilMaximo;

        if (herramientaTorbellino != null && gestorEquipamiento.TorbellinoDesbloqueado())
        {
            int nivel = herramientaTorbellino.NivelCapacidad;
            bool maximo = herramientaTorbellino.CapacidadAlMaximo();
            if (textoBotonRadio != null)
            {
                textoBotonRadio.text = maximo
                    ? "TORBELLINO NIVEL MÁXIMO (5 PIEDRAS)"
                    : "MEJORAR TORBELLINO NIVEL " + (nivel + 1) + " (" + (nivel + 1) + " piedras) - " + Eco.Coste(EconomiaDemo.Ids.Torbellino, nivel + 1) + " monedas";
            }
            if (botonMejorarRadio != null) botonMejorarRadio.interactable = !maximo;
        }
    }

    private void ActualizarTiendaBase()
    {
        ActualizarBotas();
        ActualizarJetpack();

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
                    Ep_precioTorbellino +
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
                      Ep_precioMartillo +
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
                        Ep_precioIman +
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
            // Desactivada en escena (demo) = panel de mejoras oculto.
            bool procesadoraDesbloqueada =
                ProcesadoraDisponible;


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
        else if (
            herramientaTorbellino
                .NivelRadio == 3)
        {
            if (textoBotonRadio != null)
            {
                textoBotonRadio.text =
                    "MEJORAR RADIO NIVEL 4 - " +
                    precioRadioNivel4 +
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
                    Ep_precioAlcanceNivel2 +
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
                    Ep_precioAlcanceNivel3 +
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
                    Ep_precioMovilidadNivel2 +
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
                    Ep_precioMovilidadNivel3 +
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
                    Ep_precioImanNivel2 +
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
                    Ep_precioImanNivel3 +
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
        // PRIMERO: REQUIERE PROCESADORA (desactivado en la demo)
        // =================================================

        if (BotBloqueadoPorProcesadora)
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


    public void ComprarMejoraBotas()
    {
        if (cartera == null || movimientoPersonaje == null)
        {
            Debug.LogWarning("TiendaTrabajo: faltan referencias de Cartera o MovimientoPersonaje.");
            return;
        }

        int nivel = movimientoPersonaje.NivelBotas;
        if (nivel >= 3)
            return;

        int precio = nivel == 0 ? Ep_precioBotasNivel1 :
                     nivel == 1 ? Ep_precioBotasNivel2 : Ep_precioBotasNivel3;

        if (precio <= 0)
        {
            Debug.LogWarning("TiendaTrabajo: configura un precio positivo para las botas.");
            return;
        }

        if (!cartera.GastarMonedas(precio))
        {
            Debug.Log("No tienes monedas suficientes para las botas.");
            ActualizarTienda();
            return;
        }

        movimientoPersonaje.MejorarBotas();
        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Botas, movimientoPersonaje.NivelBotas, precio);
        ActualizarTienda();
    }

    private void ActualizarBotas()
    {
        if (movimientoPersonaje == null)
        {
            if (botonComprarBotas != null)
                botonComprarBotas.interactable = false;
            if (textoBotonBotas != null)
                textoBotonBotas.text = "BOTAS NO DISPONIBLES";
            return;
        }

        int nivel = movimientoPersonaje.NivelBotas;
        int precio = nivel == 0 ? Ep_precioBotasNivel1 :
                     nivel == 1 ? Ep_precioBotasNivel2 : Ep_precioBotasNivel3;

        if (botonComprarBotas != null)
            botonComprarBotas.interactable = nivel < 3 && precio > 0 &&
                cartera != null && cartera.PuedeGastar(precio);

        if (textoBotonBotas == null)
            return;

        if (nivel >= 3)
            textoBotonBotas.text = "BOTAS NIVEL MÁXIMO";
        else if (nivel == 0)
            textoBotonBotas.text = "COMPRAR BOTAS - " + precio + " monedas";
        else
            textoBotonBotas.text = "MEJORAR BOTAS NIVEL " + (nivel + 1) +
                                  " - " + precio + " monedas";
    }


    public void ComprarMejoraJetpack()
    {
        if (cartera == null || movimientoPersonaje == null)
        {
            Debug.LogWarning("TiendaTrabajo: faltan referencias de Cartera o MovimientoPersonaje para el jetpack.");
            return;
        }

        int nivel = movimientoPersonaje.NivelJetpack;
        if (nivel >= 4)
            return;

        int precio = ObtenerPrecioJetpack(nivel);
        if (precio <= 0)
        {
            Debug.LogWarning("TiendaTrabajo: configura un precio positivo para el jetpack.");
            return;
        }

        if (!cartera.GastarMonedas(precio))
        {
            Debug.Log("No tienes monedas suficientes para el jetpack.");
            ActualizarTienda();
            return;
        }

        movimientoPersonaje.MejorarJetpack();
        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Jetpack, movimientoPersonaje.NivelJetpack, precio);
        ActualizarTienda();
    }

    private int ObtenerPrecioJetpack(int nivel)
    {
        switch (nivel)
        {
            case 0: return Ep_precioJetpackNivel1;
            case 1: return Ep_precioJetpackNivel2;
            case 2: return Ep_precioJetpackNivel3;
            case 3: return Ep_precioJetpackNivel4;
            default: return 0;
        }
    }

    private void ActualizarJetpack()
    {
        if (movimientoPersonaje == null)
        {
            if (botonComprarJetpack != null)
                botonComprarJetpack.interactable = false;
            if (textoBotonJetpack != null)
                textoBotonJetpack.text = "JETPACK NO DISPONIBLE";
            return;
        }

        int nivel = movimientoPersonaje.NivelJetpack;
        int precio = ObtenerPrecioJetpack(nivel);

        if (botonComprarJetpack != null)
            botonComprarJetpack.interactable = nivel < 4 && precio > 0 &&
                cartera != null && cartera.PuedeGastar(precio);

        if (textoBotonJetpack == null)
            return;

        if (nivel >= 4)
            textoBotonJetpack.text = "JETPACK NIVEL MAXIMO";
        else if (nivel == 0)
            textoBotonJetpack.text = "COMPRAR JETPACK - " + precio + " monedas";
        else
            textoBotonJetpack.text = "MEJORAR JETPACK NIVEL " + (nivel + 1) +
                                     " - " + precio + " monedas";
    }


    // =====================================================
    // REFRESCAR DESDE OTROS SISTEMAS
    // =====================================================

    public void RefrescarTienda()
    {
        ActualizarTienda();
    }
}
