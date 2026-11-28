using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lógica de todas las compras (precios del Excel, guardado, eventos). No tiene interfaz propia:
/// la tienda principal se ve con TiendaHolografica y las arañas con el panel del GeneradorAranas.
/// </summary>
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
    [HideInInspector] [Min(1)] public int precioBotasNivel1 = 5;
    [HideInInspector] [Min(1)] public int precioBotasNivel2 = 10;
    [HideInInspector] [Min(1)] public int precioBotasNivel3 = 20;



    [Header("Jetpack")]
    [HideInInspector] [Min(1)] public int precioJetpackNivel1 = 5;
    [HideInInspector] [Min(1)] public int precioJetpackNivel2 = 10;
    [HideInInspector] [Min(1)] public int precioJetpackNivel3 = 20;
    [HideInInspector] [Min(1)] public int precioJetpackNivel4 = 40;


    // =====================================================
    // ARMA DE LA LUNA
    // =====================================================

    [Header("Arma de la Luna")]

    public int precioArmaLuna = 1;


    // =====================================================
    // TORBELLINO - COMPRA
    // =====================================================

    [Header("Torbellino - Compra")]

    [HideInInspector] public int precioTorbellino = 1;

    public HerramientaTorbellino herramientaTorbellino;


    // =====================================================
    // TORBELLINO - RADIO
    // =====================================================

    [Header("Torbellino - Mejora Radio")]

    public int precioRadioNivel2 = 5;
    public int precioRadioNivel3 = 10;
    public int precioRadioNivel4 = 20;


    // =====================================================
    // TORBELLINO - ALCANCE
    // =====================================================

    [Header("Torbellino - Mejora Alcance")]

    [HideInInspector] public int precioAlcanceNivel2 = 5;
    [HideInInspector] public int precioAlcanceNivel3 = 10;


    // =====================================================
    // TORBELLINO - MOVILIDAD
    // =====================================================

    [Header("Torbellino - Mejora Movilidad")]

    [HideInInspector] public int precioMovilidadNivel2 = 5;
    [HideInInspector] public int precioMovilidadNivel3 = 10;


    // =====================================================
    // IMÁN
    // =====================================================

    [Header("Imán - Mejora Alcance")]

    public RecolectorMagnetico recolectorMagnetico;

    [HideInInspector] public int precioImanNivel2 = 5;
    [HideInInspector] public int precioImanNivel3 = 10;


    [Header("Imán - Compra")]

    [HideInInspector] public int precioIman = 3;


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


    // =====================================================
    // PROCESADORA - VELOCIDAD
    // =====================================================

    [Header("Procesadora - Velocidad")]

    public int precioProcesadoraNivel2 = 10;
    public int precioProcesadoraNivel3 = 20;


    // =====================================================
    // MARTILLO
    // =====================================================

    [Header("Martillo - Compra")]

    [HideInInspector] public int precioMartillo = 10;


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

    // Con el generador en la escena pero bloqueado (cubo) no se venden arañas en ningún sitio.
    public bool GeneradorBloqueado =>
        generadorAranas != null &&
        generadorAranas.isActiveAndEnabled &&
        !generadorAranas.Desbloqueado;

    // Generador de Arañas desactivado = fuera de la demo: se usa puntoSpawnBots.
    private GeneradorAranas GeneradorActivo =>
        generadorAranas != null && generadorAranas.isActiveAndEnabled
            ? generadorAranas
            : null;


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
        Iniciar();
    }

    private bool iniciada;

    /// <summary>
    /// Busca las referencias y prepara la tienda. Se llama en Start y también desde la tienda
    /// holográfica, por si este objeto estuviera desactivado y su Start no llegara a ejecutarse.
    /// </summary>
    public void Iniciar()
    {
        if (iniciada) return;
        iniciada = true;

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

        if (GeneradorBloqueado)
        {
            Debug.Log(
                "El generador de arañas está bloqueado."
            );


            ActualizarTienda();

            return;
        }


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

        if (GeneradorBloqueado)
        {
            texto = "GENERADOR BLOQUEADO";
            return false;
        }

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

    /// <summary>Algo de la tienda ha cambiado (compra, desbloqueo...). La interfaz (TiendaHolografica) puede escucharlo.</summary>
    public event System.Action OnTiendaCambiada;

    private void ActualizarTienda()
    {
        OnTiendaCambiada?.Invoke();
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


    // =====================================================
    // REFRESCAR DESDE OTROS SISTEMAS
    // =====================================================

    public void RefrescarTienda()
    {
        ActualizarTienda();
    }

    // =====================================================
    // API PARA LA TIENDA HOLOGRÁFICA
    // Usa las mismas compras de arriba (precios del Excel, eventos, guardado):
    // la tienda holográfica solo es otra forma de enseñarlas.
    // =====================================================

    public enum Articulo
    {
        Fusil, Torbellino, TorbellinoAlcance, TorbellinoMovilidad,
        Iman, ImanAlcance, Martillo, Botas, Jetpack
    }

    public struct EstadoArticulo
    {
        /// <summary>Falta comprar antes otra cosa (p. ej. el Torbellino para mejorar su alcance).</summary>
        public bool requisitoPendiente;
        /// <summary>No se encuentra el componente del juego (no se puede comprar).</summary>
        public bool noDisponible;
        /// <summary>0 = sin comprar.</summary>
        public int nivel;
        public int nivelMax;
        /// <summary>Precio del siguiente nivel (0 si ya está al máximo).</summary>
        public int precio;

        public bool Maximo => nivel >= nivelMax;
        public bool SePuedeComprar => !requisitoPendiente && !noDisponible && !Maximo && precio > 0;
    }

    public int MonedasDisponibles
    {
        get
        {
            Iniciar();
            return cartera != null ? cartera.Monedas : 0;
        }
    }

    public EstadoArticulo Consultar(Articulo articulo)
    {
        Iniciar();
        EstadoArticulo e = new EstadoArticulo();
        bool equipo = gestorEquipamiento != null;

        switch (articulo)
        {
            case Articulo.Fusil:
            {
                bool comprado = equipo && gestorEquipamiento.ArmaLanzadoraDesbloqueada();
                e.noDisponible = !equipo;
                e.nivelMax = usarEconomiaDemo ? Eco.NivelesMax(EconomiaDemo.Ids.FusilMonedas) : 1;
                e.nivel = comprado ? (FusilMonedas != null ? FusilMonedas.NivelFusil : 1) : 0;
                if (!e.Maximo)
                    e.precio = usarEconomiaDemo ? Eco.Coste(EconomiaDemo.Ids.FusilMonedas, e.nivel + 1) : precioArmaLuna;
                break;
            }
            case Articulo.Torbellino:
            {
                bool comprado = equipo && gestorEquipamiento.TorbellinoDesbloqueado();
                e.noDisponible = !equipo || herramientaTorbellino == null;
                e.nivelMax = usarEconomiaDemo ? 5 : 1;
                e.nivel = comprado ? (herramientaTorbellino != null ? herramientaTorbellino.NivelCapacidad : 1) : 0;
                if (!e.Maximo)
                    e.precio = e.nivel == 0 ? Ep_precioTorbellino : Eco.Coste(EconomiaDemo.Ids.Torbellino, e.nivel + 1);
                break;
            }
            case Articulo.TorbellinoAlcance:
            case Articulo.TorbellinoMovilidad:
            {
                bool alcance = articulo == Articulo.TorbellinoAlcance;
                // Con el nivel unificado ya no se venden por separado.
                e.noDisponible = !equipo || herramientaTorbellino == null || herramientaTorbellino.nivelesUnificados;
                e.requisitoPendiente = !equipo || !gestorEquipamiento.TorbellinoDesbloqueado();
                e.nivelMax = 3;
                e.nivel = herramientaTorbellino == null ? 1 : (alcance ? herramientaTorbellino.NivelAlcance : herramientaTorbellino.NivelMovilidad);
                if (!e.Maximo)
                {
                    if (alcance) e.precio = e.nivel == 1 ? Ep_precioAlcanceNivel2 : Ep_precioAlcanceNivel3;
                    else e.precio = e.nivel == 1 ? Ep_precioMovilidadNivel2 : Ep_precioMovilidadNivel3;
                }
                break;
            }
            case Articulo.Iman:
            {
                e.noDisponible = recolectorMagnetico == null;
                e.nivelMax = 1;
                e.nivel = recolectorMagnetico != null && recolectorMagnetico.EstaImanDesbloqueado() ? 1 : 0;
                if (!e.Maximo) e.precio = Ep_precioIman;
                break;
            }
            case Articulo.ImanAlcance:
            {
                e.noDisponible = recolectorMagnetico == null;
                e.requisitoPendiente = recolectorMagnetico == null || !recolectorMagnetico.EstaImanDesbloqueado();
                e.nivelMax = 3;
                e.nivel = recolectorMagnetico != null ? recolectorMagnetico.NivelAlcance : 1;
                if (!e.Maximo) e.precio = e.nivel == 1 ? Ep_precioImanNivel2 : Ep_precioImanNivel3;
                break;
            }
            case Articulo.Martillo:
            {
                e.noDisponible = !equipo;
                e.nivelMax = 1;
                e.nivel = equipo && gestorEquipamiento.MartilloDesbloqueado() ? 1 : 0;
                if (!e.Maximo) e.precio = Ep_precioMartillo;
                break;
            }
            case Articulo.Botas:
            {
                e.noDisponible = movimientoPersonaje == null;
                e.nivelMax = 3;
                e.nivel = movimientoPersonaje != null ? movimientoPersonaje.NivelBotas : 0;
                if (!e.Maximo)
                    e.precio = e.nivel == 0 ? Ep_precioBotasNivel1 : e.nivel == 1 ? Ep_precioBotasNivel2 : Ep_precioBotasNivel3;
                break;
            }
            case Articulo.Jetpack:
            {
                e.noDisponible = movimientoPersonaje == null;
                e.nivelMax = 4;
                e.nivel = movimientoPersonaje != null ? movimientoPersonaje.NivelJetpack : 0;
                if (!e.Maximo) e.precio = ObtenerPrecioJetpack(e.nivel);
                break;
            }
        }

        if (e.Maximo) e.precio = 0;
        return e;
    }

    /// <summary>Compra el siguiente nivel del artículo. Devuelve true si la compra se ha hecho.</summary>
    public bool Comprar(Articulo articulo)
    {
        EstadoArticulo antes = Consultar(articulo);
        if (!antes.SePuedeComprar) return false;
        int monedasAntes = MonedasDisponibles;

        switch (articulo)
        {
            case Articulo.Fusil: ComprarArmaLuna(); break;
            case Articulo.Torbellino:
                if (antes.nivel == 0) ComprarTorbellino();
                else ComprarMejoraRadioTorbellino();
                break;
            case Articulo.TorbellinoAlcance: ComprarMejoraAlcanceTorbellino(); break;
            case Articulo.TorbellinoMovilidad: ComprarMejoraMovilidadTorbellino(); break;
            case Articulo.Iman: ComprarIman(); break;
            case Articulo.ImanAlcance: ComprarMejoraAlcanceIman(); break;
            case Articulo.Martillo: ComprarMartillo(); break;
            case Articulo.Botas: ComprarMejoraBotas(); break;
            case Articulo.Jetpack: ComprarMejoraJetpack(); break;
        }

        EstadoArticulo despues = Consultar(articulo);
        return despues.nivel > antes.nivel || MonedasDisponibles < monedasAntes;
    }
}
