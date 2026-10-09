using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BotRecolector : MonoBehaviour
{
    public enum EstadoBot
    {
        Buscando,
        YendoAPiedra,
        LlevandoPiedra,
        EntregandoPiedra,
        Esperando,

        // Escalada:
        PreparandoEscalada,
        Escalando,

        // Cadena especial:
        // Bot -> Procesadora -> espera -> piedra procesada -> Agujero.
        YendoAEsperaSalidaProcesadora,
        EsperandoSalidaProcesadora,
        YendoAPiedraProcesada
    }



    private EstadisticasBot estadisticasBot;

    private ConfiguracionBot.DestinoTrabajo
    destinoPiedraActual =
        ConfiguracionBot.DestinoTrabajo.Agujero;

    // =====================================================
    // ESTADO
    // =====================================================

    [Header("Estado")]

    [SerializeField]
    private EstadoBot estadoActual =
        EstadoBot.Buscando;


    // =====================================================
    // INICIO
    // =====================================================

    [Header("Inicio")]

    public bool aparecerEnParkingAlIniciar =
        true;


    public bool empezarEnPausa =
        true;


    // =====================================================
    // REFERENCIAS
    // =====================================================

    [Header("Referencias")]

    public Transform puntoAgarre;


    [Header("Procesadora")]

    public MaquinaErosion maquinaErosion;

    public float distanciaEntregaProcesadora =
        1.5f;


    [Header("Espera salida procesadora")]

    [Tooltip(
        "Distancia a la que el Bot considera que ha llegado " +
        "al punto de espera de la salida."
    )]
    public float distanciaLlegadaEsperaProcesadora =
        0.8f;


    [Tooltip(
        "Tiempo máximo que el Bot esperará su piedra antes " +
        "de abandonar esta tarea por seguridad."
    )]
    public float tiempoMaximoEsperaProcesadora =
        15f;


    [Tooltip(
        "Pequeño retardo después de que la piedra salga para " +
        "dar tiempo a que la física la deje caer."
    )]
    public float retardoRecogidaProcesada =
        0.35f;


    [Tooltip(
        "Radio usado para buscar NavMesh cerca de la piedra " +
        "recién expulsada."
    )]
    public float radioNavMeshPiedraProcesada =
        3f;


    [Tooltip(
        "Velocidad máxima de la piedra para que el Bot intente recogerla."
    )]
    public float velocidadMaximaRecogerProcesada =
        1.5f;

    // =====================================================
    // GESTOR GLOBAL
    // =====================================================

    [Header("Gestor Global de Piedras")]

    public GestorPiedras gestorPiedras;


    // =====================================================
    // ENTREGA
    // =====================================================

    [Header("Sistema de Entrega")]

    public GestorPuntosEntregaBots gestorPuntosEntrega;

    public Transform puntoEntradaAgujero;

    public AgujeroSimple agujeroDestino;

    public float distanciaEntrega =
        1.5f;

    public float duracionLanzamiento =
        0.5f;

    public float alturaArcoLanzamiento =
        2f;

    public float velocidadGiroLanzamiento =
        360f;


    [Header("Entrega visual al agujero")]

    [Tooltip(
        "Antes de soltar la piedra, el Bot intenta mirar hacia la boca del agujero."
    )]
    public bool orientarAntesDeLanzar =
        true;


    [Tooltip(
        "Velocidad de orientación final hacia el agujero, en grados por segundo."
    )]
    public float velocidadGiroEntrega =
        180f;


    [Tooltip(
        "Tiempo máximo que dedicamos a orientar el cuerpo antes de lanzar."
    )]
    public float tiempoMaximoOrientacionEntrega =
        0.45f;


    [Tooltip(
        "Límite visual de altura del arco. Evita lanzamientos gigantes aunque " +
        "Altura Arco Lanzamiento tenga un valor antiguo muy alto."
    )]
    public float alturaMaximaArcoEntrega =
        0.45f;


    [Tooltip(
        "Tiempo de la pequeña caída visual desde la boca hasta el interior."
    )]
    public float duracionCaidaDentroAgujero =
        0.18f;


    [Tooltip(
        "Cuánto baja visualmente la piedra dentro del agujero antes de contar la entrega."
    )]
    public float profundidadVisualAgujero =
        0.35f;


    [Header("Debug lanzamiento al agujero")]

    [Tooltip(
        "Dibuja en la Scene la trayectoria prevista del lanzamiento. " +
        "Amarillo = arco hasta la boca. Rojo = caída visual dentro del agujero."
    )]
    public bool mostrarDebugTrazadaLanzamiento =
        true;


    [Tooltip(
        "Cantidad de segmentos usados para dibujar la curva de debug."
    )]
    [Range(4, 100)]
    public int segmentosDebugTrazadaLanzamiento =
        30;


    [Tooltip(
        "Segundos que permanece visible la trazada hecha con Debug.DrawLine."
    )]
    public float duracionDebugTrazada =
        4f;


    // =====================================================
    // PARKING
    // =====================================================

    [Header("Parking")]

    public GestorPuntosEsperaBots gestorPuntosEspera;

    public float distanciaLlegadaParking =
        0.6f;

    public float intervaloBusquedaEnEspera =
        1f;


    // =====================================================
    // BÚSQUEDA GLOBAL
    // =====================================================

    [Header("Búsqueda Global")]

    public string tagPiedra =
        "Piedra";


    public float tiempoEntreBusquedas =
        0.5f;


    [Tooltip(
        "Solo se mantiene para compatibilidad. " +
        "La búsqueda actual es global."
    )]
    public float radioBusqueda =
        20f;


    // =====================================================
    // PIEDRAS INACCESIBLES
    // =====================================================

    [Header("Piedras Inaccesibles")]

    public float tiempoIgnorarPiedraInaccesible =
        3f;


    public float distanciaMaximaPiedraANavMesh =
        1.5f;


    public float intervaloRevalidacionObjetivo =
        0.4f;




    // =====================================================
    // RECOGIDA
    // =====================================================

    [Header("Recogida")]

    public float distanciaRecogida =
        1.5f;


    // =====================================================
    // ANTI ATASCO
    // =====================================================

    [Header("Anti-Atasco")]

    public float intervaloComprobacionAtasco =
        0.75f;


    public float distanciaMinimaAvance =
        0.15f;


    public float tiempoMaximoAtascado =
        2f;


    public int maxReintentosRuta =
        2;


    // =====================================================
    // INTERACCIÓN
    // =====================================================

    [Header("Interacción con jugador")]

    public float velocidadGiroInteraccion =
        6f;


    // =====================================================
    // MOVIMIENTO - GIRO PROGRESIVO
    // =====================================================

    [Header("Movimiento - Giro progresivo")]

    [Tooltip(
        "Si está activo, el Bot no hace giros bruscos ni se para para girar. " +
        "Rota de forma progresiva mientras avanza."
    )]
    public bool usarGiroProgresivo =
        true;


    [Tooltip(
        "Velocidad máxima de rotación del Bot en grados por segundo. " +
        "Un valor más bajo produce curvas más amplias y suaves."
    )]
    public float velocidadGiroMovimiento =
        110f;


    [Tooltip(
        "A partir de este ángulo el Bot empieza a reducir su velocidad " +
        "de avance para dar tiempo a las patas a recolocarse."
    )]
    [Range(0f, 180f)]
    public float anguloInicioReducirVelocidad =
        20f;


    [Tooltip(
        "Ángulo a partir del cual se aplica la velocidad mínima de giro."
    )]
    [Range(1f, 180f)]
    public float anguloVelocidadMinima =
        110f;


    [Tooltip(
        "Fracción de la velocidad normal que conserva el Bot durante " +
        "un giro muy grande. 0.20 significa un 20%."
    )]
    [Range(0.05f, 1f)]
    public float factorVelocidadMinimaGiro =
        0.20f;


    [Tooltip(
        "Rapidez con la que la velocidad se adapta al ángulo. " +
        "Valores mayores reaccionan más rápido."
    )]
    public float suavizadoVelocidadPorGiro =
        7f;


    // =====================================================
    // ESCALADA - PASO 1
    // =====================================================

    [Header("Escalada - Preparación")]

    [Tooltip(
        "Detector que avisa al Bot cuando tiene delante una pared escalable. " +
        "Si se deja vacío, se busca automáticamente en este mismo GameObject."
    )]
    public DetectorEscalada detectorEscalada;


    [Tooltip(
        "Gestor de las seis patas. Se usa para cambiar progresivamente " +
        "la normal de apoyo desde el suelo hacia la pared."
    )]
    public GestorPatasArana gestorPatasArana;


    [Tooltip(
        "Tiempo que tardan las patas en cambiar su normal de apoyo " +
        "desde Vector3.up hasta la normal de la pared."
    )]
    [Min(0.05f)]
    public float duracionTransicionPatasPared =
        0.75f;


    [Header("Escalada - Transición visual del cuerpo")]

    [Tooltip(
        "Raíz visual de la araña. Debe ser SpiderVisual, NO el Bot raíz. " +
        "Si queda vacío se intenta obtener automáticamente desde GestorPatasArana."
    )]
    public Transform spiderVisual;


    [Tooltip(
        "Cuánto se acerca visualmente SpiderVisual hacia la pared durante " +
        "la transición. Valores pequeños evitan atravesar la pared."
    )]
    [Min(0f)]
    public float acercamientoVisualPared =
        0.12f;


    [Tooltip(
        "Cuánto sube visualmente SpiderVisual mientras gira hacia la pared. " +
        "Ayuda a que el cuerpo no barra el suelo al rotar."
    )]
    [Min(0f)]
    public float elevacionVisualTransicion =
        0.18f;


    [Tooltip(
        "Si está activo, SpiderVisual rota progresivamente hasta quedar " +
        "con su eje Up alineado con la normal de la pared y su Forward subiendo."
    )]
    public bool rotarVisualHaciaPared =
        true;


    [Header("Escalada - Movimiento por pared")]

    [Tooltip(
        "Velocidad con la que el Bot sube por la pared durante esta primera prueba. " +
        "Conviene empezar despacio para que las patas puedan recolocarse."
    )]
    [Min(0.05f)]
    public float velocidadEscalada =
        0.65f;


    [Tooltip(
        "Distancia máxima que sube en esta fase de prueba. Al alcanzarla se queda " +
        "quieto en la pared; todavía no hacemos la salida por el borde superior."
    )]
    [Min(0.1f)]
    public float distanciaMaximaPruebaEscalada =
        2.0f;


    [Tooltip(
        "Si está activo, la escalada se detiene al alcanzar la distancia de prueba."
    )]
    public bool limitarDistanciaPruebaEscalada =
        true;


    [Tooltip(
        "Distancia del raycast que comprueba que sigue existiendo pared escalable " +
        "delante del cuerpo mientras sube."
    )]
    [Min(0.1f)]
    public float distanciaComprobacionParedEscalada =
        1.5f;


    [Tooltip(
        "Separa el origen del raycast un poco hacia fuera de la pared para evitar " +
        "que empiece dentro del collider."
    )]
    [Min(0f)]
    public float separacionOrigenRaycastEscalada =
        0.35f;


    [Header("Escalada - Detección del borde superior")]

    [Tooltip(
        "Activa la detección del suelo superior cuando deja de verse la pared."
    )]
    public bool detectarBordeSuperior =
        true;


    [Tooltip(
        "Layers válidas para el suelo superior. " +
        "Normalmente Floor y, si la parte superior pertenece al mismo collider, Escalable."
    )]
    public LayerMask capaSueloSalidaEscalada;


    [Tooltip(
        "Cuánto avanzamos el punto de sondeo hacia el interior de la plataforma " +
        "desde la cara de la pared."
    )]
    [Min(0f)]
    public float avanceSondeoBorde =
        0.45f;


    [Tooltip(
        "Cuánto elevamos el origen del raycast por encima del cuerpo visual " +
        "para buscar la superficie superior."
    )]
    [Min(0f)]
    public float alturaSondeoBorde =
        0.80f;


    [Tooltip(
        "Distancia máxima del raycast vertical hacia abajo que busca el suelo superior."
    )]
    [Min(0.1f)]
    public float distanciaRaycastSueloSuperior =
        1.50f;


    [Tooltip(
        "Muestra mensajes cuando el Bot conserva una piedra como objetivo " +
        "y entra en PreparandoEscalada."
    )]
    public bool debugEscalada =
        true;


    // =====================================================
    // DEBUG
    // =====================================================

    [Header("Debug búsqueda")]

    public bool debugBusqueda =
        false;


    // =====================================================
    // INTERNAS
    // =====================================================

    private NavMeshAgent agente;

    private ConfiguracionBot configuracionBot;

    private Rigidbody piedraObjetivo;

    private Transform puntoLlegadaReservado;

    private Transform puntoEsperaReservado;


    private float temporizadorBusqueda;

    private float temporizadorBusquedaEnEspera;

    private float temporizadorRevalidacionObjetivo;


    // =====================================================
    // CADENA PROCESADORA
    // =====================================================

    // Referencia exacta a la piedra que este Bot ha metido
    // en la procesadora y está esperando recuperar.
    private Rigidbody piedraEsperadaProcesadora;

    private float temporizadorEsperaProcesadora;

    private float temporizadorRetardoProcesada;


    private bool pausaForzada =
        false;

    private bool parkingForzado =
        false;


    // =====================================================
    // INTERACCIÓN
    // =====================================================

    private bool enInteraccion =
        false;

    private Transform jugadorInteraccion;


    // =====================================================
    // GIRO PROGRESIVO
    // =====================================================

    private float velocidadBaseAgente =
        0f;

    private float factorVelocidadGiroActual =
        1f;


    // =====================================================
    // ESCALADA INTERNA
    // =====================================================

    private bool transicionPatasEscaladaSolicitada =
        false;

    private bool mensajeTransicionPatasTerminada =
        false;

    private Vector3 normalParedEscalada =
        Vector3.up;


    // =====================================================
    // TRANSICIÓN VISUAL DEL CUERPO
    // =====================================================

    private bool transicionVisualEscaladaPreparada =
        false;

    private Vector3 posicionInicioVisualEscalada;

    private Vector3 posicionObjetivoVisualEscalada;

    private Quaternion rotacionInicioVisualEscalada;

    private Quaternion rotacionObjetivoVisualEscalada;


    // =====================================================
    // MOVIMIENTO POR PARED
    // =====================================================

    private Vector3 posicionInicioMovimientoEscalada;

    [SerializeField]
    private float distanciaRecorridaEscalada =
        0f;

    [SerializeField]
    private bool paredPresenteDuranteEscalada =
        false;

    [SerializeField]
    private bool escaladaDetenidaEnPrueba =
        false;

    private bool mensajeFinPruebaEscaladaMostrado =
        false;


    [SerializeField]
    private bool bordeSuperiorDetectado =
        false;


    [SerializeField]
    private Vector3 puntoSueloSuperior =
        Vector3.zero;


    [SerializeField]
    private Vector3 normalSueloSuperior =
        Vector3.up;


    // =====================================================
    // PIEDRAS IGNORADAS
    // =====================================================

    private readonly Dictionary<Rigidbody, float>
        piedrasIgnoradasHasta =
        new Dictionary<Rigidbody, float>();


    // =====================================================
    // ANTI ATASCO INTERNO
    // =====================================================

    private Vector3 ultimaPosicionAntiAtasco;

    private float temporizadorAntiAtasco;

    private float tiempoAtascado;

    private int reintentosRuta;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        // =====================================================
        // COMPONENTES DEL BOT
        // =====================================================

        agente =
            GetComponent<NavMeshAgent>();


        // =====================================================
        // ROTACIÓN CONTROLADA POR EL BOT
        // =====================================================
        //
        // Con giro progresivo dejamos que NavMesh calcule la ruta,
        // pero controlamos manualmente la orientación. También
        // guardamos la velocidad base para reducirla únicamente
        // mientras la curva sea cerrada.
        // =====================================================

        if (agente != null)
        {
            velocidadBaseAgente =
                agente.speed;

            factorVelocidadGiroActual =
                1f;

            agente.updateRotation =
                !usarGiroProgresivo;
        }


        configuracionBot =
            GetComponent<ConfiguracionBot>();


        // =====================================================
        // DETECTOR DE ESCALADA
        // =====================================================

        if (detectorEscalada == null)
        {
            detectorEscalada =
                GetComponent<DetectorEscalada>();
        }


        // =====================================================
        // GESTOR DE PATAS
        // =====================================================

        if (gestorPatasArana == null)
        {
            gestorPatasArana =
                GetComponentInChildren<GestorPatasArana>(
                    true
                );
        }


        // =====================================================
        // SPIDER VISUAL
        // =====================================================

        if (spiderVisual == null &&
            gestorPatasArana != null)
        {
            // En nuestra jerarquía GestorPatas vive dentro de SpiderVisual.
            // Usamos su padre como raíz visual para no inclinar el Bot raíz
            // ni pelear con el NavMeshAgent.
            spiderVisual =
                gestorPatasArana.transform.parent;
        }


        // =====================================================
        // GESTOR GLOBAL DE PIEDRAS
        // =====================================================

        if (gestorPiedras == null)
        {
            gestorPiedras =
                GestorPiedras.Instancia;
        }


        // =====================================================
        // PUNTOS DE ENTREGA DEL AGUJERO
        // =====================================================

        if (gestorPuntosEntrega == null)
        {
            gestorPuntosEntrega =
                FindAnyObjectByType<
                    GestorPuntosEntregaBots
                >();
        }

        estadisticasBot =
    GetComponent<EstadisticasBot>();

        // =====================================================
        // PARKING
        // =====================================================

        if (gestorPuntosEspera == null)
        {
            gestorPuntosEspera =
                FindAnyObjectByType<
                    GestorPuntosEsperaBots
                >();
        }


        // =====================================================
        // AGUJERO
        // =====================================================

        if (agujeroDestino == null)
        {
            agujeroDestino =
                FindAnyObjectByType<
                    AgujeroSimple
                >();
        }


        // =====================================================
        // PROCESADORA
        // IMPORTANTE: BUSCARLA ANTES DEL RETURN DEL PARKING
        // =====================================================

        if (maquinaErosion == null)
        {
            maquinaErosion =
                FindAnyObjectByType<
                    MaquinaErosion
                >(
                    FindObjectsInactive.Include
                );
        }


        // =====================================================
        // DEBUG DE REFERENCIAS
        // =====================================================

        if (debugBusqueda)
        {
            Debug.Log(
                name +
                " | GestorPiedras: " +
                (gestorPiedras != null) +
                " | Procesadora: " +
                (maquinaErosion != null)
            );
        }


        ResetearAntiAtasco();


        // =====================================================
        // APARECER EN PARKING
        // =====================================================

        if (aparecerEnParkingAlIniciar)
        {
            if (ColocarEnParkingInicial())
            {
                return;
            }
        }


        // =====================================================
        // SI NO PUDO IR AL PARKING
        // =====================================================

        estadoActual =
            EstadoBot.Buscando;


        if (empezarEnPausa)
        {
            pausaForzada =
                true;


            if (configuracionBot != null)
            {
                configuracionBot.modoActual =
                    ConfiguracionBot.ModoBot.Pausa;
            }
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // =================================================
        // HABLANDO CON EL JUGADOR
        // =================================================

        if (enInteraccion)
        {
            MantenerBotDuranteInteraccion();

            return;
        }


        // =================================================
        // PAUSA
        // =================================================

        if (pausaForzada)
        {
            DetenerAgente();

            MantenerPiedraEnAgarre();

            return;
        }


        // =================================================
        // COMPORTAMIENTO
        // =================================================

        switch (estadoActual)
        {
            case EstadoBot.Buscando:

                ComportamientoBuscar();

                break;


            case EstadoBot.YendoAPiedra:

                ComportamientoIrAPiedra();

                break;


            case EstadoBot.PreparandoEscalada:

                ComportamientoPrepararEscalada();

                break;


            case EstadoBot.Escalando:

                ComportamientoEscalando();

                break;


            case EstadoBot.LlevandoPiedra:

                ComportamientoLlevarPiedra();

                break;


            case EstadoBot.EntregandoPiedra:

                break;


            case EstadoBot.Esperando:

                ComportamientoEsperar();

                break;


            case EstadoBot.YendoAEsperaSalidaProcesadora:

                ComportamientoIrAEsperaSalidaProcesadora();

                break;


            case EstadoBot.EsperandoSalidaProcesadora:

                ComportamientoEsperarSalidaProcesadora();

                break;


            case EstadoBot.YendoAPiedraProcesada:

                ComportamientoIrAPiedraProcesada();

                break;
        }


        // =================================================
        // ROTACIÓN / AVANCE DEL NAVMESH
        // =================================================

        GestionarGiroProgresivo();


        ComprobarAntiAtasco();
    }


    // =====================================================
    // GIRO PROGRESIVO
    // =====================================================

    private void GestionarGiroProgresivo()
    {
        if (agente == null ||
            !agente.enabled ||
            !agente.isOnNavMesh)
        {
            return;
        }


        // Si se desactiva desde el Inspector, devolvemos el
        // comportamiento normal de rotación al NavMeshAgent.
        if (!usarGiroProgresivo)
        {
            agente.updateRotation =
                true;

            RestaurarVelocidadMovimiento();

            return;
        }


        agente.updateRotation =
            false;


        // Interacción y pausa tienen sus propios controles.
        if (enInteraccion ||
            pausaForzada)
        {
            RestaurarVelocidadMovimiento();

            return;
        }


        // Sin ruta no hay curva que gestionar.
        if (!agente.hasPath ||
            agente.pathPending)
        {
            RestaurarVelocidadMovimiento();

            return;
        }


        if (agente.remainingDistance <=
            agente.stoppingDistance +
            0.05f)
        {
            RestaurarVelocidadMovimiento();

            return;
        }


        // El steeringTarget representa el siguiente tramo real
        // de la ruta. Es mejor para las curvas que mirar solamente
        // el destino final.
        Vector3 direccion =
            agente.steeringTarget -
            transform.position;


        direccion.y =
            0f;


        if (direccion.sqrMagnitude <
            0.0001f)
        {
            RestaurarVelocidadMovimiento();

            return;
        }


        direccion.Normalize();


        float angulo =
            Vector3.Angle(
                transform.forward,
                direccion
            );


        // =================================================
        // 1. GIRAR SIEMPRE DE FORMA PROGRESIVA
        // =================================================
        //
        // Ya no existe:
        //
        //   giro grande -> STOP -> giro en seco -> avanzar
        //
        // El cuerpo siempre rota como máximo unos grados por
        // segundo. Esto convierte un cambio de 90º/180º en una
        // sucesión de pequeños giros, que es justo donde la
        // locomoción procedural de las patas funciona mejor.
        // =================================================

        RotarHaciaDireccionRuta(
            direccion
        );


        // =================================================
        // 2. REDUCIR AVANCE SEGÚN EL ÁNGULO
        // =================================================
        //
        // Recto / curva suave -> velocidad completa.
        // Giro grande         -> avanza más despacio.
        //
        // Nunca lo detenemos solamente por el giro.
        // =================================================

        float anguloInicio =
            Mathf.Max(
                0f,
                anguloInicioReducirVelocidad
            );


        float anguloMinimo =
            Mathf.Max(
                anguloInicio + 0.01f,
                anguloVelocidadMinima
            );


        float progresoGiro =
            Mathf.InverseLerp(
                anguloInicio,
                anguloMinimo,
                angulo
            );


        float factorObjetivo =
            Mathf.Lerp(
                1f,
                Mathf.Clamp(
                    factorVelocidadMinimaGiro,
                    0.05f,
                    1f
                ),
                progresoGiro
            );


        float factorSuavizado =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    suavizadoVelocidadPorGiro
                ) *
                Time.deltaTime
            );


        factorVelocidadGiroActual =
            Mathf.Lerp(
                factorVelocidadGiroActual,
                factorObjetivo,
                factorSuavizado
            );


        AplicarVelocidadMovimiento();
    }


    private void RotarHaciaDireccionRuta(
        Vector3 direccion)
    {
        direccion.y =
            0f;


        if (direccion.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        Quaternion rotacionObjetivo =
            Quaternion.LookRotation(
                direccion.normalized,
                Vector3.up
            );


        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                rotacionObjetivo,
                Mathf.Max(
                    0f,
                    velocidadGiroMovimiento
                ) *
                Time.deltaTime
            );
    }


    private void AplicarVelocidadMovimiento()
    {
        if (agente == null)
            return;


        // Seguridad por si el componente empezó con speed = 0.
        if (velocidadBaseAgente <= 0f)
        {
            velocidadBaseAgente =
                Mathf.Max(
                    0f,
                    agente.speed
                );
        }


        agente.speed =
            velocidadBaseAgente *
            Mathf.Clamp(
                factorVelocidadGiroActual,
                0.05f,
                1f
            );
    }


    private void RestaurarVelocidadMovimiento()
    {
        if (agente == null)
            return;


        factorVelocidadGiroActual =
            1f;


        if (velocidadBaseAgente > 0f)
        {
            agente.speed =
                velocidadBaseAgente;
        }
    }


    // =====================================================
    // BÚSQUEDA
    // =====================================================

    private void ComportamientoBuscar()
    {
        temporizadorBusqueda -=
            Time.deltaTime;


        if (temporizadorBusqueda > 0f)
            return;


        temporizadorBusqueda =
            tiempoEntreBusquedas;


        bool encontro =
            BuscarPiedra();


        if (!encontro)
        {
            EntrarEnEspera();
        }
    }


    // =====================================================
    // BÚSQUEDA GLOBAL REAL
    // =====================================================

    private bool BuscarPiedra()
    {
        if (agente == null ||
            !agente.isOnNavMesh)
        {
            return false;
        }


        if (gestorPiedras == null)
        {
            gestorPiedras =
                GestorPiedras.Instancia;


            if (gestorPiedras == null)
            {
                return false;
            }
        }


        LimpiarPiedrasIgnoradas();


        // =====================================================
        // TODAS LAS PIEDRAS DISPONIBLES DEL MAPA
        // =====================================================

        List<Rigidbody> piedras =
            gestorPiedras.ObtenerPiedrasDisponibles(
                this
            );


        // Guardaremos aquí solamente las que
        // realmente puede considerar este Bot.
        List<Rigidbody> candidatas =
            new List<Rigidbody>();


        // Además guardamos la pureza una sola vez
        // para no recalcularla continuamente durante Sort.
        Dictionary<Rigidbody, float> purezas =
            new Dictionary<Rigidbody, float>();


        // =====================================================
        // FILTRAR
        // =====================================================

        foreach (Rigidbody piedra in piedras)
        {
            if (piedra == null)
                continue;


            if (!piedra.gameObject.activeInHierarchy)
                continue;


            // =================================================
            // TAG
            // =================================================

            if (!piedra.CompareTag(tagPiedra))
                continue;


            // =================================================
            // YA LA ESTÁ TRANSPORTANDO ALGUIEN
            // =================================================

            if (piedra.transform.parent != null)
                continue;


            // =================================================
            // RESERVADA POR OTRO BOT
            // =================================================

            if (gestorPiedras.EstaReservadaPorOtro(
                    piedra,
                    this))
            {
                continue;
            }


            // =================================================
            // TEMPORALMENTE INACCESIBLE
            // =================================================

            if (EstaPiedraTemporalmenteIgnorada(
                    piedra))
            {
                continue;
            }


            // =================================================
            // MOVIÉNDOSE
            // =================================================

            if (configuracionBot != null &&
                configuracionBot
                    .PiedraEstaDemasiadoMovida(
                        piedra))
            {
                continue;
            }


            // =================================================
            // PUREZA
            // =================================================

            float pureza =
                ObtenerPurezaPiedra(
                    piedra
                );

            // =====================================================
            // ¿LA PROCESADORA PUEDE ACEPTARLA?
            // =====================================================

            if (configuracionBot != null &&
                configuracionBot.destinoTrabajo ==
                    ConfiguracionBot
                        .DestinoTrabajo
                        .Procesadora)
            {
                if (maquinaErosion == null ||
                    !maquinaErosion.PuedeAceptarPiedra(
                        piedra))
                {
                    continue;
                }
            }

            // =================================================
            // FILTRO MÍNIMO / MÁXIMO
            // =================================================

            if (configuracionBot != null &&
                !configuracionBot.PurezaPermitida(
                    pureza))
            {
                if (debugBusqueda)
                {
                    Debug.Log(
                        name +
                        " descarta " +
                        piedra.name +
                        " por pureza: " +
                        pureza.ToString("0.0") +
                        "%"
                    );
                }


                continue;
            }


            purezas[piedra] =
                pureza;


            candidatas.Add(
                piedra
            );
        }


        // =====================================================
        // ORDENAR
        // =====================================================

        OrdenarSegunPrioridad(
            candidatas,
            purezas
        );


        // =====================================================
        // PROBAR LA MEJOR, DESPUÉS LA SIGUIENTE...
        // =====================================================

        foreach (Rigidbody piedra in candidatas)
        {
            if (piedra == null)
                continue;


            // =================================================
            // ¿PODEMOS LLEGAR?
            // =================================================

            if (!IntentarCalcularRutaPiedra(
                    piedra,
                    out NavMeshHit puntoNavMesh,
                    out NavMeshPath camino))
            {
                MarcarPiedraInaccesible(
                    piedra
                );


                if (debugBusqueda)
                {
                    Debug.Log(
                        name +
                        " descarta " +
                        piedra.name +
                        " porque no tiene ruta."
                    );
                }


                continue;
            }


            // =================================================
            // INTENTAR RESERVAR
            // =================================================

            if (!gestorPiedras.IntentarReservarPiedra(
                    piedra,
                    this))
            {
                continue;
            }


            // =================================================
            // PIEDRA ELEGIDA
            // =================================================

            LiberarPuntoEspera();


            piedraObjetivo =
                piedra;

            destinoPiedraActual =
    CalcularDestinoPiedra(
        piedraObjetivo
    );


            estadoActual =
                EstadoBot.YendoAPiedra;


            temporizadorRevalidacionObjetivo =
                intervaloRevalidacionObjetivo;


            ResetearAntiAtasco();


            agente.SetDestination(
                puntoNavMesh.position
            );


            if (debugBusqueda)
            {
                Debug.Log(
                    name +
                    " ELIGE " +
                    piedra.name +
                    " | Pureza: " +
                    purezas[piedra].ToString("0.0") +
                    "%"
                );
            }


            return true;
        }


        return false;
    }


    // =====================================================
    // ORDENAR SEGÚN PRIORIDAD
    // =====================================================
    private void OrdenarSegunPrioridad(
        List<Rigidbody> piedras,
        Dictionary<Rigidbody, float> purezas)
    {
        if (piedras == null ||
            piedras.Count <= 1)
        {
            return;
        }


        ConfiguracionBot.PrioridadRecogida prioridad =
            ConfiguracionBot
                .PrioridadRecogida
                .MasCercana;


        if (configuracionBot != null)
        {
            prioridad =
                configuracionBot.prioridadRecogida;
        }


        // =====================================================
        // ALEATORIA
        // =====================================================

        if (prioridad ==
            ConfiguracionBot.PrioridadRecogida.Aleatoria)
        {
            MezclarLista(
                piedras
            );


            return;
        }


        // =====================================================
        // ORDENAR
        // =====================================================

        piedras.Sort(
            (a, b) =>
            {
                if (a == null &&
                    b == null)
                {
                    return 0;
                }


                if (a == null)
                    return 1;


                if (b == null)
                    return -1;


                float distanciaA =
                    (
                        a.position -
                        transform.position
                    ).sqrMagnitude;


                float distanciaB =
                    (
                        b.position -
                        transform.position
                    ).sqrMagnitude;


                // =============================================
                // MÁS CERCANA
                // =============================================

                if (prioridad ==
                    ConfiguracionBot
                        .PrioridadRecogida
                        .MasCercana)
                {
                    return distanciaA.CompareTo(
                        distanciaB
                    );
                }


                float purezaA =
                    purezas.TryGetValue(
                        a,
                        out float valorA)
                    ?
                    valorA
                    :
                    0f;


                float purezaB =
                    purezas.TryGetValue(
                        b,
                        out float valorB)
                    ?
                    valorB
                    :
                    0f;


                // =============================================
                // MAYOR PUREZA
                // =============================================

                if (prioridad ==
                    ConfiguracionBot
                        .PrioridadRecogida
                        .MayorPureza)
                {
                    int resultado =
                        purezaB.CompareTo(
                            purezaA
                        );


                    // Si tienen exactamente la misma pureza,
                    // escogemos la más cercana.
                    if (resultado == 0)
                    {
                        return distanciaA.CompareTo(
                            distanciaB
                        );
                    }


                    return resultado;
                }


                // =============================================
                // MENOR PUREZA
                // =============================================

                if (prioridad ==
                    ConfiguracionBot
                        .PrioridadRecogida
                        .MenorPureza)
                {
                    int resultado =
                        purezaA.CompareTo(
                            purezaB
                        );


                    if (resultado == 0)
                    {
                        return distanciaA.CompareTo(
                            distanciaB
                        );
                    }


                    return resultado;
                }


                return 0;
            }
        );
    }

    // =====================================================
    // PUREZA
    // =====================================================

    private float ObtenerPurezaPiedra(
        Rigidbody piedra)
    {
        if (piedra == null)
            return 0f;


        DeformacionPiedra deformacion =
            piedra.GetComponent<
                DeformacionPiedra
            >();


        if (deformacion == null)
        {
            deformacion =
                piedra.GetComponentInParent<
                    DeformacionPiedra
                >();
        }


        if (deformacion == null)
            return 0f;


        return deformacion
            .ObtenerPorcentajeDesgasteHaciaEsfera();
    }


    // =====================================================
    // ALEATORIO
    // =====================================================

    private void MezclarLista(
        List<Rigidbody> lista)
    {
        for (int i = lista.Count - 1;
             i > 0;
             i--)
        {
            int j =
                Random.Range(
                    0,
                    i + 1
                );


            Rigidbody temporal =
                lista[i];


            lista[i] =
                lista[j];


            lista[j] =
                temporal;
        }
    }


    // =====================================================
    // NAVMESH
    // =====================================================

    private bool IntentarCalcularRutaPiedra(
        Rigidbody piedra,
        out NavMeshHit puntoNavMesh,
        out NavMeshPath camino)
    {
        puntoNavMesh =
            new NavMeshHit();


        camino =
            new NavMeshPath();


        if (piedra == null)
            return false;


        if (agente == null ||
            !agente.isOnNavMesh)
        {
            return false;
        }


        if (!NavMesh.SamplePosition(
                piedra.position,
                out puntoNavMesh,
                distanciaMaximaPiedraANavMesh,
                NavMesh.AllAreas))
        {
            return false;
        }


        if (!agente.CalculatePath(
                puntoNavMesh.position,
                camino))
        {
            return false;
        }


        return camino.status ==
               NavMeshPathStatus.PathComplete;
    }


    // =====================================================
    // PIEDRAS IGNORADAS
    // =====================================================

    private bool EstaPiedraTemporalmenteIgnorada(
        Rigidbody piedra)
    {
        if (piedra == null)
            return false;


        if (!piedrasIgnoradasHasta.TryGetValue(
                piedra,
                out float tiempoFin))
        {
            return false;
        }


        if (Time.time >= tiempoFin)
        {
            piedrasIgnoradasHasta.Remove(
                piedra
            );

            return false;
        }


        return true;
    }


    private void MarcarPiedraInaccesible(
        Rigidbody piedra)
    {
        if (piedra == null)
            return;


        piedrasIgnoradasHasta[piedra] =
            Time.time +
            tiempoIgnorarPiedraInaccesible;


        if (gestorPiedras != null)
        {
            gestorPiedras.LiberarReserva(
                piedra,
                this
            );
        }
    }


    private void LimpiarPiedrasIgnoradas()
    {
        if (piedrasIgnoradasHasta.Count == 0)
            return;


        List<Rigidbody> eliminar =
            new List<Rigidbody>();


        foreach (
            KeyValuePair<Rigidbody, float> dato
            in piedrasIgnoradasHasta)
        {
            if (dato.Key == null ||
                Time.time >= dato.Value)
            {
                eliminar.Add(
                    dato.Key
                );
            }
        }


        foreach (Rigidbody piedra in eliminar)
        {
            piedrasIgnoradasHasta.Remove(
                piedra
            );
        }
    }


    // =====================================================
    // ABANDONAR INACCESIBLE
    // =====================================================

    private void AbandonarPiedraInaccesible()
    {
        Rigidbody piedraProblematica =
            piedraObjetivo;


        if (piedraProblematica != null)
        {
            MarcarPiedraInaccesible(
                piedraProblematica
            );
        }


        piedraObjetivo =
            null;


        DetenerAgente();


        estadoActual =
            EstadoBot.Buscando;


        temporizadorBusqueda =
            0f;


        temporizadorRevalidacionObjetivo =
            0f;


        ResetearAntiAtasco();
    }


    // =====================================================
    // IR A LA PIEDRA
    // =====================================================

    private void ComportamientoIrAPiedra()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();

            return;
        }


        // =====================================================
        // PARED ESCALABLE DELANTE
        // =====================================================
        //
        // IMPORTANTE:
        // Si ya tenemos una piedra reservada y el detector ve una pared
        // escalable delante, NO liberamos la piedra y NO buscamos otra.
        //
        // Cuando estamos lo bastante cerca, entramos en el estado
        // PreparandoEscalada. El movimiento real por la pared se añadirá
        // en el siguiente paso.
        // =====================================================

        if (DebePrepararEscalada())
        {
            EntrarEnPreparandoEscalada();

            return;
        }


        // Si ahora otro objeto ha hecho que se mueva mucho,
        // podemos abandonarla y elegir otra.
        if (configuracionBot != null &&
            configuracionBot
                .PiedraEstaDemasiadoMovida(
                    piedraObjetivo))
        {
            if (gestorPiedras != null)
            {
                gestorPiedras.LiberarReserva(
                    piedraObjetivo,
                    this
                );
            }


            piedraObjetivo =
                null;


            DetenerAgente();


            estadoActual =
                EstadoBot.Buscando;


            temporizadorBusqueda =
                0f;


            return;
        }


        temporizadorRevalidacionObjetivo -=
            Time.deltaTime;


        if (temporizadorRevalidacionObjetivo <= 0f)
        {
            temporizadorRevalidacionObjetivo =
                intervaloRevalidacionObjetivo;


            if (!IntentarCalcularRutaPiedra(
                    piedraObjetivo,
                    out NavMeshHit puntoNavMesh,
                    out NavMeshPath camino))
            {
                // Si la ruta de suelo falla pero tenemos una pared
                // escalable delante, conservamos el objetivo. No marcamos
                // la piedra como inaccesible porque precisamente vamos a
                // intentar llegar a ella mediante escalada.
                if (detectorEscalada != null &&
                    detectorEscalada.HayParedEscalable)
                {
                    EntrarEnPreparandoEscalada();

                    return;
                }


                AbandonarPiedraInaccesible();

                return;
            }


            agente.SetDestination(
                puntoNavMesh.position
            );
        }


        float distancia =
            Vector3.Distance(
                transform.position,
                piedraObjetivo.position
            );


        if (distancia <=
            distanciaRecogida)
        {
            RecogerPiedra();
        }
    }


    // =====================================================
    // PREPARAR ESCALADA
    // =====================================================

    private bool DebePrepararEscalada()
    {
        if (detectorEscalada == null)
        {
            return false;
        }


        if (!detectorEscalada.HayParedEscalable)
        {
            return false;
        }


        return detectorEscalada.EstaFrenteAPared;
    }


    private void EntrarEnPreparandoEscalada()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();

            return;
        }


        // No liberamos la reserva de la piedra.
        // No cambiamos piedraObjetivo.
        // No la añadimos a piedrasIgnoradasHasta.
        estadoActual =
            EstadoBot.PreparandoEscalada;


        DetenerAgente();


        if (agente != null &&
            agente.enabled &&
            agente.isOnNavMesh)
        {
            agente.isStopped =
                true;
        }


        // Guardamos la normal de la pared AHORA. De esta forma, aunque
        // el detector deje de verla durante la transición, las patas
        // siguen teniendo un objetivo estable.
        if (detectorEscalada != null &&
            detectorEscalada.NormalPared.sqrMagnitude >
                0.0001f)
        {
            normalParedEscalada =
                detectorEscalada.NormalPared.normalized;
        }


        transicionPatasEscaladaSolicitada =
            false;

        mensajeTransicionPatasTerminada =
            false;

        transicionVisualEscaladaPreparada =
            false;


        // Preparamos primero la pose inicial/final del cuerpo y luego
        // arrancamos la transición de las patas. Ambas usarán exactamente
        // el mismo progreso del GestorPatasArana.
        PrepararTransicionVisualEscalada();

        IniciarTransicionPatasHaciaPared();


        ResetearAntiAtasco();


        if (debugEscalada)
        {
            Debug.Log(
                name +
                " | PREPARANDO ESCALADA | mantiene objetivo: " +
                piedraObjetivo.name
            );
        }
    }


    private void IniciarTransicionPatasHaciaPared()
    {
        if (transicionPatasEscaladaSolicitada)
            return;


        if (gestorPatasArana == null)
        {
            gestorPatasArana =
                GetComponentInChildren<GestorPatasArana>(
                    true
                );
        }


        if (gestorPatasArana == null)
        {
            if (debugEscalada)
            {
                Debug.LogWarning(
                    name +
                    " | ESCALADA: no se encontró GestorPatasArana."
                );
            }

            return;
        }


        if (normalParedEscalada.sqrMagnitude <
            0.0001f)
        {
            if (detectorEscalada != null &&
                detectorEscalada.NormalPared.sqrMagnitude >
                    0.0001f)
            {
                normalParedEscalada =
                    detectorEscalada.NormalPared.normalized;
            }
            else
            {
                return;
            }
        }


        // Si al entrar todavía no teníamos una normal válida,
        // la transición visual puede no estar preparada aún.
        if (!transicionVisualEscaladaPreparada)
        {
            PrepararTransicionVisualEscalada();
        }


        gestorPatasArana.IniciarTransicionSuperficie(
            normalParedEscalada,
            duracionTransicionPatasPared
        );


        transicionPatasEscaladaSolicitada =
            true;


        if (debugEscalada)
        {
            Debug.Log(
                name +
                " | ESCALADA: patas cambian normal " +
                "suelo -> pared en " +
                duracionTransicionPatasPared.ToString("0.00") +
                " s."
            );
        }
    }


    // =====================================================
    // PREPARAR TRANSICIÓN VISUAL DEL CUERPO
    // =====================================================

    private void PrepararTransicionVisualEscalada()
    {
        if (transicionVisualEscaladaPreparada)
            return;


        if (spiderVisual == null &&
            gestorPatasArana != null)
        {
            spiderVisual =
                gestorPatasArana.transform.parent;
        }


        if (spiderVisual == null)
        {
            if (debugEscalada)
            {
                Debug.LogWarning(
                    name +
                    " | ESCALADA: no se encontró SpiderVisual."
                );
            }

            return;
        }


        if (normalParedEscalada.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        Vector3 normalPared =
            normalParedEscalada.normalized;


        // Guardamos la pose REAL desde la que parte SpiderVisual.
        posicionInicioVisualEscalada =
            spiderVisual.position;

        rotacionInicioVisualEscalada =
            spiderVisual.rotation;


        // =================================================
        // POSICIÓN FINAL
        // =================================================
        //
        // -normalPared apunta HACIA la pared.
        // Vector3.up hace que el cuerpo gane un poco de altura
        // mientras pivota, evitando barrer el suelo.
        // =================================================

        posicionObjetivoVisualEscalada =
            posicionInicioVisualEscalada
            -
            normalPared *
            Mathf.Max(
                0f,
                acercamientoVisualPared
            )
            +
            Vector3.up *
            Mathf.Max(
                0f,
                elevacionVisualTransicion
            );


        // =================================================
        // ROTACIÓN FINAL
        // =================================================
        //
        // En pared queremos:
        //
        // SpiderVisual.up      = normal de la pared
        // SpiderVisual.forward = dirección de subida
        //
        // La dirección de subida se obtiene proyectando el Up
        // mundial sobre el plano de la pared. Para una pared
        // vertical esto da exactamente Vector3.up.
        // =================================================

        Vector3 direccionSubida =
            Vector3.ProjectOnPlane(
                Vector3.up,
                normalPared
            );


        if (direccionSubida.sqrMagnitude <
            0.0001f)
        {
            // Seguridad para superficies extremas.
            direccionSubida =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    normalPared
                );
        }


        if (direccionSubida.sqrMagnitude <
            0.0001f)
        {
            direccionSubida =
                Vector3.Cross(
                    normalPared,
                    transform.right
                );
        }


        direccionSubida.Normalize();


        rotacionObjetivoVisualEscalada =
            Quaternion.LookRotation(
                direccionSubida,
                normalPared
            );


        transicionVisualEscaladaPreparada =
            true;


        if (debugEscalada)
        {
            Debug.Log(
                name +
                " | ESCALADA: transición visual preparada. " +
                "SpiderVisual rotará y se acercará a la pared."
            );
        }
    }


    // =====================================================
    // ACTUALIZAR TRANSICIÓN VISUAL DEL CUERPO
    // =====================================================

    private void ActualizarTransicionVisualEscalada()
    {
        if (!transicionVisualEscaladaPreparada ||
            spiderVisual == null ||
            gestorPatasArana == null)
        {
            return;
        }


        // Usamos EXACTAMENTE el progreso de la transición de las patas.
        // Así el cambio de normal y la inclinación del cuerpo no pueden
        // desincronizarse.
        float t =
            Mathf.Clamp01(
                gestorPatasArana
                    .ProgresoTransicionSuperficie
            );


        // El Gestor también usa SmoothStep para la normal.
        // Repetimos la misma curva en posición y rotación.
        float tSuave =
            t * t *
            (3f - 2f * t);


        spiderVisual.position =
            Vector3.Lerp(
                posicionInicioVisualEscalada,
                posicionObjetivoVisualEscalada,
                tSuave
            );


        if (rotarVisualHaciaPared)
        {
            spiderVisual.rotation =
                Quaternion.Slerp(
                    rotacionInicioVisualEscalada,
                    rotacionObjetivoVisualEscalada,
                    tSuave
                );
        }


        // Debug de los ejes actuales del cuerpo visual.
        if (debugEscalada)
        {
            Debug.DrawRay(
                spiderVisual.position,
                spiderVisual.up * 0.7f,
                Color.blue
            );

            Debug.DrawRay(
                spiderVisual.position,
                spiderVisual.forward * 0.7f,
                Color.green
            );
        }
    }


    private void ComportamientoPrepararEscalada()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();

            return;
        }


        // Durante toda la preparación el NavMesh permanece quieto.
        if (agente != null &&
            agente.enabled &&
            agente.isOnNavMesh)
        {
            agente.isStopped =
                true;

            if (agente.hasPath)
            {
                agente.ResetPath();
            }
        }


        RestaurarVelocidadMovimiento();


        // Si al entrar todavía no teníamos una normal válida, esperamos
        // hasta que DetectorEscalada la proporcione y arrancamos entonces.
        if (!transicionPatasEscaladaSolicitada)
        {
            if (detectorEscalada != null &&
                detectorEscalada.NormalPared.sqrMagnitude >
                    0.0001f)
            {
                normalParedEscalada =
                    detectorEscalada.NormalPared.normalized;

                PrepararTransicionVisualEscalada();

                IniciarTransicionPatasHaciaPared();
            }
        }


        // =====================================================
        // CUERPO + PATAS SINCRONIZADOS
        // =====================================================
        //
        // El GestorPatasArana cambia la normal de apoyo y nosotros
        // usamos exactamente su mismo progreso para rotar y acercar
        // SpiderVisual hacia la pared. El Bot raíz/NavMesh NO se mueve.
        // =====================================================

        ActualizarTransicionVisualEscalada();


        // =====================================================
        // TRANSICIÓN TERMINADA -> EMPEZAR A SUBIR
        // =====================================================
        //
        // En cuanto cuerpo y patas han llegado al 100% de la pose de pared,
        // dejamos el NavMesh temporalmente y pasamos al movimiento manual
        // sobre la superficie.
        // =====================================================

        if (transicionPatasEscaladaSolicitada &&
            gestorPatasArana != null &&
            !gestorPatasArana.TransicionSuperficieEnCurso &&
            !mensajeTransicionPatasTerminada)
        {
            mensajeTransicionPatasTerminada =
                true;


            if (debugEscalada)
            {
                Debug.Log(
                    name +
                    " | ESCALADA: transición terminada. " +
                    "Empieza el movimiento vertical por la pared. " +
                    "Normal actual = " +
                    gestorPatasArana.NormalSuperficieActual
                );
            }


            EntrarEnEscalando();

            return;
        }


        // Debug visual:
        // cyan    = Bot -> piedra que sigue siendo su objetivo.
        // magenta = Bot -> punto detectado de la pared.
        // azul    = normal de pared / Up actual de SpiderVisual.
        // verde   = Forward actual de SpiderVisual (dirección de subida).
        if (debugEscalada)
        {
            Debug.DrawLine(
                transform.position,
                piedraObjetivo.position,
                Color.cyan
            );


            if (detectorEscalada != null &&
                detectorEscalada.HayParedEscalable)
            {
                Debug.DrawLine(
                    transform.position,
                    detectorEscalada.PuntoPared,
                    Color.magenta
                );


                Debug.DrawRay(
                    detectorEscalada.PuntoPared,
                    normalParedEscalada * 0.8f,
                    Color.blue
                );
            }
        }
    }


    // =====================================================
    // ENTRAR EN ESCALANDO
    // =====================================================

    private void EntrarEnEscalando()
    {
        if (piedraObjetivo == null)
        {
            return;
        }


        // =================================================
        // NAVMESH FUERA DURANTE LA ESCALADA
        // =================================================
        //
        // El NavMesh del escenario está pensado para el suelo. Mientras
        // estamos pegados a una pared vertical movemos el Bot raíz de forma
        // manual. Más adelante, al volver al suelo, reactivaremos el Agent.
        // =================================================

        if (agente != null &&
            agente.enabled)
        {
            if (agente.isOnNavMesh)
            {
                agente.isStopped =
                    true;


                if (agente.hasPath)
                {
                    agente.ResetPath();
                }
            }


            agente.enabled =
                false;
        }


        estadoActual =
            EstadoBot.Escalando;


        posicionInicioMovimientoEscalada =
            transform.position;


        distanciaRecorridaEscalada =
            0f;


        paredPresenteDuranteEscalada =
            true;


        escaladaDetenidaEnPrueba =
            false;


        mensajeFinPruebaEscaladaMostrado =
            false;


        bordeSuperiorDetectado =
            false;

        puntoSueloSuperior =
            Vector3.zero;

        normalSueloSuperior =
            Vector3.up;


        ResetearAntiAtasco();


        if (debugEscalada)
        {
            Debug.Log(
                name +
                " | ESCALANDO | NavMesh desactivado temporalmente. " +
                "Velocidad: " +
                velocidadEscalada.ToString("0.00") +
                " m/s."
            );
        }
    }


    // =====================================================
    // COMPORTAMIENTO ESCALANDO
    // =====================================================

    private void ComportamientoEscalando()
    {
        if (piedraObjetivo == null)
        {
            escaladaDetenidaEnPrueba =
                true;


            if (debugEscalada &&
                !mensajeFinPruebaEscaladaMostrado)
            {
                mensajeFinPruebaEscaladaMostrado =
                    true;


                Debug.LogWarning(
                    name +
                    " | ESCALADA detenida: se perdió la piedra objetivo."
                );
            }


            return;
        }


        if (normalParedEscalada.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        Vector3 normalPared =
            normalParedEscalada.normalized;


        Vector3 direccionSubida =
            ObtenerDireccionSubidaPared(
                normalPared
            );


        if (direccionSubida.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        // =================================================
        // COMPROBAR QUE LA PARED SIGUE EXISTIENDO
        // =================================================

        paredPresenteDuranteEscalada =
            ComprobarParedDuranteEscalada(
                normalPared,
                out RaycastHit hitPared
            );


        if (!paredPresenteDuranteEscalada)
        {
            // =================================================
            // PASO 5A: ¿HEMOS LLEGADO AL BORDE SUPERIOR?
            // =================================================
            //
            // De momento NO hacemos todavía la salida completa.
            // Solo comprobamos que:
            // 1) la pared se ha terminado,
            // 2) existe una superficie válida por encima y hacia
            //    el interior de la pared.
            //
            // Si la encontramos, paramos la araña y dejamos el
            // punto guardado para el siguiente subpaso.
            // =================================================

            if (detectarBordeSuperior &&
                ComprobarSueloSuperiorEscalada(
                    normalPared,
                    direccionSubida,
                    out RaycastHit hitSueloSuperior))
            {
                bordeSuperiorDetectado =
                    true;

                puntoSueloSuperior =
                    hitSueloSuperior.point;

                normalSueloSuperior =
                    hitSueloSuperior.normal.normalized;

                escaladaDetenidaEnPrueba =
                    true;


                if (debugEscalada &&
                    !mensajeFinPruebaEscaladaMostrado)
                {
                    mensajeFinPruebaEscaladaMostrado =
                        true;

                    Debug.Log(
                        name +
                        " | ESCALADA PASO 5A: BORDE SUPERIOR DETECTADO. " +
                        "Punto = " +
                        puntoSueloSuperior +
                        " | Normal = " +
                        normalSueloSuperior
                    );
                }


                return;
            }


            escaladaDetenidaEnPrueba =
                true;


            if (debugEscalada &&
                !mensajeFinPruebaEscaladaMostrado)
            {
                mensajeFinPruebaEscaladaMostrado =
                    true;


                Debug.LogWarning(
                    name +
                    " | ESCALADA: se terminó la pared, pero NO se encontró " +
                    "una superficie superior válida. Revisa Capa Suelo Salida Escalada " +
                    "y los valores de sondeo."
                );
            }


            return;
        }


        // =================================================
        // LÍMITE TEMPORAL DE ESTA PRUEBA
        // =================================================

        if (limitarDistanciaPruebaEscalada &&
            distanciaRecorridaEscalada >=
                Mathf.Max(
                    0.1f,
                    distanciaMaximaPruebaEscalada
                ))
        {
            escaladaDetenidaEnPrueba =
                true;


            if (debugEscalada &&
                !mensajeFinPruebaEscaladaMostrado)
            {
                mensajeFinPruebaEscaladaMostrado =
                    true;


                Debug.Log(
                    name +
                    " | ESCALADA: distancia de prueba completada (" +
                    distanciaRecorridaEscalada.ToString("0.00") +
                    " m). Se queda quieto en la pared."
                );
            }


            return;
        }


        if (escaladaDetenidaEnPrueba)
        {
            return;
        }


        // =================================================
        // SUBIR POR LA PARED
        // =================================================
        //
        // Movemos el Bot raíz en una dirección tangente a la pared.
        // No añadimos componente hacia/desde la pared, por lo que en una
        // pared plana conserva exactamente la separación que acabamos de
        // ajustar durante PreparandoEscalada.
        // =================================================

        float desplazamiento =
            Mathf.Max(
                0f,
                velocidadEscalada
            ) *
            Time.deltaTime;


        transform.position +=
            direccionSubida *
            desplazamiento;


        distanciaRecorridaEscalada =
            Vector3.Distance(
                posicionInicioMovimientoEscalada,
                transform.position
            );


        // =================================================
        // DEBUG
        // =================================================

        if (debugEscalada)
        {
            Vector3 origenDebug =
                spiderVisual != null
                ? spiderVisual.position
                : transform.position;


            Debug.DrawRay(
                origenDebug,
                direccionSubida * 1.0f,
                Color.green
            );


            Debug.DrawRay(
                hitPared.point,
                normalPared * 0.7f,
                Color.cyan
            );


            Debug.DrawLine(
                origenDebug,
                hitPared.point,
                Color.yellow
            );
        }
    }


    // =====================================================
    // DIRECCIÓN DE SUBIDA POR LA PARED
    // =====================================================

    private Vector3 ObtenerDireccionSubidaPared(
        Vector3 normalPared)
    {
        Vector3 direccionSubida =
            Vector3.ProjectOnPlane(
                Vector3.up,
                normalPared
            );


        if (direccionSubida.sqrMagnitude <
            0.0001f)
        {
            if (spiderVisual != null)
            {
                direccionSubida =
                    Vector3.ProjectOnPlane(
                        spiderVisual.forward,
                        normalPared
                    );
            }
        }


        if (direccionSubida.sqrMagnitude <
            0.0001f)
        {
            return Vector3.zero;
        }


        return direccionSubida.normalized;
    }


    // =====================================================
    // COMPROBAR CONTACTO CON LA PARED
    // =====================================================

    private bool ComprobarParedDuranteEscalada(
        Vector3 normalPared,
        out RaycastHit hit)
    {
        Vector3 centroVisual =
            spiderVisual != null
            ? spiderVisual.position
            : transform.position;


        // Sacamos el origen hacia fuera de la superficie y lanzamos
        // el raycast de vuelta hacia la pared.
        Vector3 origen =
            centroVisual +
            normalPared *
            Mathf.Max(
                0f,
                separacionOrigenRaycastEscalada
            );


        Vector3 direccion =
            -normalPared;


        int mascara =
            detectorEscalada != null
            ? detectorEscalada.capaEscalable.value
            : Physics.DefaultRaycastLayers;


        bool encontro =
            Physics.Raycast(
                origen,
                direccion,
                out hit,
                Mathf.Max(
                    0.1f,
                    distanciaComprobacionParedEscalada
                ),
                mascara,
                QueryTriggerInteraction.Ignore
            );


        if (debugEscalada)
        {
            Debug.DrawRay(
                origen,
                direccion *
                Mathf.Max(
                    0.1f,
                    distanciaComprobacionParedEscalada
                ),
                encontro
                    ? Color.yellow
                    : Color.red
            );
        }


        return encontro;
    }


    // =====================================================
    // PASO 5A - BUSCAR SUELO EN EL BORDE SUPERIOR
    // =====================================================

    private bool ComprobarSueloSuperiorEscalada(
        Vector3 normalPared,
        Vector3 direccionSubida,
        out RaycastHit hitSuelo)
    {
        hitSuelo =
            default;


        if (normalPared.sqrMagnitude <
            0.0001f)
        {
            return false;
        }


        Vector3 centroVisual =
            spiderVisual != null
            ? spiderVisual.position
            : transform.position;


        Vector3 haciaInterior =
            -normalPared.normalized;


        // Nos colocamos virtualmente un poco por encima del cuerpo y
        // un poco hacia el interior de la plataforma. Desde ahí
        // lanzamos un rayo vertical hacia abajo.
        Vector3 origen =
            centroVisual +
            Vector3.up *
            Mathf.Max(
                0f,
                alturaSondeoBorde
            ) +
            haciaInterior *
            Mathf.Max(
                0f,
                avanceSondeoBorde
            );


        int mascara =
            capaSueloSalidaEscalada.value != 0
            ? capaSueloSalidaEscalada.value
            : Physics.DefaultRaycastLayers;


        bool encontro =
            Physics.Raycast(
                origen,
                Vector3.down,
                out hitSuelo,
                Mathf.Max(
                    0.1f,
                    distanciaRaycastSueloSuperior
                ),
                mascara,
                QueryTriggerInteraction.Ignore
            );


        // Para esta primera versión solo aceptamos una superficie
        // razonablemente orientada hacia arriba. Así no confundimos
        // otra pared vertical con el suelo de salida.
        if (encontro)
        {
            float verticalidad =
                Vector3.Dot(
                    hitSuelo.normal.normalized,
                    Vector3.up
                );


            if (verticalidad <
                0.45f)
            {
                encontro =
                    false;
            }
        }


        if (debugEscalada)
        {
            Debug.DrawRay(
                origen,
                Vector3.down *
                Mathf.Max(
                    0.1f,
                    distanciaRaycastSueloSuperior
                ),
                encontro
                    ? Color.green
                    : Color.red
            );


            Debug.DrawRay(
                origen,
                haciaInterior *
                0.35f,
                Color.magenta
            );


            if (encontro)
            {
                Debug.DrawRay(
                    hitSuelo.point,
                    hitSuelo.normal *
                    0.6f,
                    Color.blue
                );
            }
        }


        return encontro;
    }


    // =====================================================
    // RECOGER
    // =====================================================

    private void RecogerPiedra()
    {
        if (piedraObjetivo == null ||
            puntoAgarre == null)
        {
            CancelarObjetivo();

            return;
        }


        // =====================================================
        // GUARDAR PUREZA ANTES DE DESACTIVAR LA PIEDRA
        // =====================================================

        float purezaRecogida =
            ObtenerPurezaPiedra(
                piedraObjetivo
            );


        // =====================================================
        // DETENER BOT
        // =====================================================

        DetenerAgente();


        // =====================================================
        // PARAR FÍSICAS
        // =====================================================

        if (!piedraObjetivo.isKinematic)
        {
            piedraObjetivo.linearVelocity =
                Vector3.zero;


            piedraObjetivo.angularVelocity =
                Vector3.zero;
        }


        // =====================================================
        // DESACTIVAR DEFORMACIÓN
        // =====================================================

        DeformacionPiedra deformacion =
            piedraObjetivo.GetComponent<
                DeformacionPiedra
            >();


        if (deformacion == null)
        {
            deformacion =
                piedraObjetivo.GetComponentInParent<
                    DeformacionPiedra
                >();
        }


        if (deformacion != null)
        {
            deformacion.enabled =
                false;
        }


        // =====================================================
        // HACER KINEMATIC
        // =====================================================

        piedraObjetivo.isKinematic =
            true;


        // =====================================================
        // DESACTIVAR COLLIDERS
        // =====================================================

        Collider[] colliders =
            piedraObjetivo
                .GetComponentsInChildren<Collider>();


        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled =
                    false;
            }
        }


        // =====================================================
        // COLOCAR EN EL PUNTO DE AGARRE
        // =====================================================

        piedraObjetivo.transform.SetParent(
            puntoAgarre,
            false
        );


        piedraObjetivo.transform.localPosition =
            Vector3.zero;


        piedraObjetivo.transform.localRotation =
            Quaternion.identity;


        // =====================================================
        // REGISTRAR ESTADÍSTICA
        // =====================================================

        if (estadisticasBot != null)
        {
            estadisticasBot
                .RegistrarPiedraRecogida(
                    purezaRecogida
                );
        }


        // =====================================================
        // CAMBIAR ESTADO
        // =====================================================

        estadoActual =
            EstadoBot.LlevandoPiedra;


        ResetearAntiAtasco();
    }


    // =====================================================
    // LLEVAR PIEDRA
    // =====================================================

    private void ComportamientoLlevarPiedra()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();

            return;
        }


        if (puntoAgarre == null)
        {
            CancelarObjetivo();

            return;
        }


        // Mantener la piedra en las manos.
        piedraObjetivo.transform.position =
            puntoAgarre.position;


        piedraObjetivo.transform.rotation =
            puntoAgarre.rotation;


        // =====================================================
        // DESTINO: PROCESADORA
        // =====================================================

        if (destinoPiedraActual ==
            ConfiguracionBot
                .DestinoTrabajo
                .Procesadora)
        {
            ComportamientoLlevarProcesadora();

            return;
        }


        // =====================================================
        // DESTINO: AGUJERO
        // =====================================================

        if (!IntentarReservarPuntoEntrega())
        {
            DetenerAgente();

            return;
        }


        if (!IrHaciaPuntoEntrega())
            return;


        if (HaLlegadoAlPuntoEntrega())
        {
            estadoActual =
                EstadoBot.EntregandoPiedra;


            DetenerAgente();


            ResetearAntiAtasco();


            StartCoroutine(
                LanzarPiedraAlAgujero()
            );
        }
    }


    // =====================================================
    // ENTREGA
    // =====================================================

    private bool IntentarReservarPuntoEntrega()
    {
        if (puntoLlegadaReservado != null)
            return true;


        if (gestorPuntosEntrega == null)
            return false;


        puntoLlegadaReservado =
            gestorPuntosEntrega.ReservarPunto(
                this
            );


        return puntoLlegadaReservado != null;
    }


    private bool IrHaciaPuntoEntrega()
    {
        if (puntoLlegadaReservado == null ||
            agente == null ||
            !agente.isOnNavMesh)
        {
            return false;
        }


        if (!NavMesh.SamplePosition(
                puntoLlegadaReservado.position,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
        {
            return false;
        }


        agente.SetDestination(
            hit.position
        );


        return true;
    }


    private bool HaLlegadoAlPuntoEntrega()
    {
        if (puntoLlegadaReservado == null)
            return false;


        float distancia =
            Vector3.Distance(
                transform.position,
                puntoLlegadaReservado.position
            );


        if (distancia <= distanciaEntrega)
            return true;


        if (agente == null ||
            !agente.isOnNavMesh ||
            agente.pathPending ||
            !agente.hasPath)
        {
            return false;
        }


        return agente.remainingDistance <=
               Mathf.Max(
                   distanciaEntrega,
                   agente.stoppingDistance +
                   0.05f
               );
    }


    private void LiberarPuntoEntrega()
    {
        if (gestorPuntosEntrega != null)
        {
            gestorPuntosEntrega.LiberarPunto(
                this
            );
        }


        puntoLlegadaReservado =
            null;
    }


    // =====================================================
    // LANZAR AL AGUJERO
    // =====================================================

    private IEnumerator LanzarPiedraAlAgujero()
    {
        if (piedraObjetivo == null ||
            puntoEntradaAgujero == null ||
            agujeroDestino == null)
        {
            CancelarObjetivo();

            yield break;
        }


        Rigidbody piedraEntregada =
            piedraObjetivo;


        // =====================================================
        // 1. ORIENTAR EL BOT HACIA LA BOCA DEL AGUJERO
        // =====================================================
        //
        // La piedra todavía sigue en PuntoAgarre durante esta fase.
        // De esta forma el lanzamiento nace visualmente desde delante
        // del Bot y no desde un lateral extraño.
        // =====================================================

        if (orientarAntesDeLanzar)
        {
            float tiempoOrientando =
                0f;


            while (tiempoOrientando <
                   Mathf.Max(
                       0f,
                       tiempoMaximoOrientacionEntrega
                   ))
            {
                if (enInteraccion ||
                    pausaForzada)
                {
                    yield return null;

                    continue;
                }


                if (piedraEntregada == null)
                {
                    LiberarPuntoEntrega();

                    yield break;
                }


                Vector3 direccionAgujero =
                    puntoEntradaAgujero.position -
                    transform.position;


                direccionAgujero.y =
                    0f;


                if (direccionAgujero.sqrMagnitude <
                    0.0001f)
                {
                    break;
                }


                Quaternion rotacionObjetivo =
                    Quaternion.LookRotation(
                        direccionAgujero.normalized,
                        Vector3.up
                    );


                transform.rotation =
                    Quaternion.RotateTowards(
                        transform.rotation,
                        rotacionObjetivo,
                        Mathf.Max(
                            0f,
                            velocidadGiroEntrega
                        ) *
                        Time.deltaTime
                    );


                // Mientras el cuerpo gira, mantenemos la piedra
                // exactamente en el punto de agarre.
                if (puntoAgarre != null)
                {
                    piedraEntregada.transform.position =
                        puntoAgarre.position;

                    piedraEntregada.transform.rotation =
                        puntoAgarre.rotation;
                }


                float anguloRestante =
                    Quaternion.Angle(
                        transform.rotation,
                        rotacionObjetivo
                    );


                if (anguloRestante <= 3f)
                {
                    break;
                }


                tiempoOrientando +=
                    Time.deltaTime;


                yield return null;
            }
        }


        if (piedraEntregada == null)
        {
            LiberarPuntoEntrega();

            yield break;
        }


        // =====================================================
        // 2. SOLTAR LA PIEDRA Y PREPARAR EL LANZAMIENTO CONTROLADO
        // =====================================================

        piedraEntregada.transform.SetParent(
            null,
            true
        );


        piedraEntregada.isKinematic =
            true;


        Collider[] colliders =
            piedraEntregada
                .GetComponentsInChildren<Collider>();


        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled =
                    false;
            }
        }


        Vector3 inicio =
            piedraEntregada.position;


        // PuntoEntradaAgujero debe estar en el centro real de la boca.
        Vector3 bocaAgujero =
            puntoEntradaAgujero.position;


        // Segundo destino invisible calculado por código.
        // Está por debajo de la boca para que SIEMPRE veamos
        // que la piedra entra antes de contar la entrega.
        Vector3 interiorAgujero =
            bocaAgujero +
            Vector3.down *
            Mathf.Max(
                0f,
                profundidadVisualAgujero
            );


        // Aunque AlturaArcoLanzamiento venga de una configuración
        // antigua con valor 2, limitamos el arco del Bot para que
        // no lance la piedra absurdamente alto.
        float alturaArcoReal =
            Mathf.Min(
                Mathf.Max(
                    0f,
                    alturaArcoLanzamiento
                ),
                Mathf.Max(
                    0f,
                    alturaMaximaArcoEntrega
                )
            );


        // =====================================================
        // DEBUG: DIBUJAR LA TRAZADA QUE VA A HACER LA PIEDRA
        // =====================================================

        DibujarDebugTrazadaLanzamiento(
            inicio,
            bocaAgujero,
            interiorAgujero,
            alturaArcoReal
        );


        // =====================================================
        // 3. ARCO CORTO HASTA EL CENTRO DE LA BOCA
        // =====================================================

        float tiempo =
            0f;


        while (tiempo < duracionLanzamiento)
        {
            // Hablar o pausar congela el lanzamiento.
            if (enInteraccion ||
                pausaForzada)
            {
                yield return null;

                continue;
            }


            if (piedraEntregada == null)
            {
                LiberarPuntoEntrega();

                yield break;
            }


            tiempo +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    tiempo /
                    Mathf.Max(
                        0.01f,
                        duracionLanzamiento
                    )
                );


            // SmoothStep evita que la piedra salga y llegue
            // con una sensación demasiado robótica.
            float tSuave =
                t * t *
                (3f - 2f * t);


            Vector3 posicion =
                Vector3.Lerp(
                    inicio,
                    bocaAgujero,
                    tSuave
                );


            posicion.y +=
                Mathf.Sin(
                    t * Mathf.PI
                ) *
                alturaArcoReal;


            piedraEntregada.position =
                posicion;


            piedraEntregada.transform.Rotate(
                Vector3.one *
                velocidadGiroLanzamiento *
                Time.deltaTime,
                Space.World
            );


            yield return null;
        }


        if (piedraEntregada == null)
        {
            LiberarPuntoEntrega();

            yield break;
        }


        // Garantizamos un frame real en el centro de la boca.
        piedraEntregada.position =
            bocaAgujero;


        yield return null;


        // =====================================================
        // 4. CAÍDA VISIBLE DENTRO DEL AGUJERO
        // =====================================================
        //
        // Aquí está la corrección principal del problema:
        // NO contamos la entrega cuando la piedra simplemente
        // termina el arco. Primero debe verse entrando.
        // =====================================================

        float tiempoCaida =
            0f;


        float duracionCaida =
            Mathf.Max(
                0.01f,
                duracionCaidaDentroAgujero
            );


        while (tiempoCaida < duracionCaida)
        {
            if (enInteraccion ||
                pausaForzada)
            {
                yield return null;

                continue;
            }


            if (piedraEntregada == null)
            {
                LiberarPuntoEntrega();

                yield break;
            }


            tiempoCaida +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    tiempoCaida /
                    duracionCaida
                );


            float tSuave =
                t * t *
                (3f - 2f * t);


            piedraEntregada.position =
                Vector3.Lerp(
                    bocaAgujero,
                    interiorAgujero,
                    tSuave
                );


            piedraEntregada.transform.Rotate(
                Vector3.one *
                velocidadGiroLanzamiento *
                Time.deltaTime,
                Space.World
            );


            yield return null;
        }


        if (piedraEntregada == null)
        {
            LiberarPuntoEntrega();

            yield break;
        }


        piedraEntregada.position =
            interiorAgujero;


        // =====================================================
        // 5. SOLO AHORA CONTAMOS LA ENTREGA
        // =====================================================

        if (gestorPiedras != null)
        {
            gestorPiedras.DesregistrarPiedra(
                piedraEntregada
            );
        }


        piedraObjetivo =
            null;


        agujeroDestino.RecibirPiedra(
            piedraEntregada.gameObject
        );


        if (estadisticasBot != null)
        {
            estadisticasBot
                .RegistrarEntregaAgujero();
        }


        LiberarPuntoEntrega();


        if (parkingForzado)
        {
            EntrarEnEspera();
        }
        else
        {
            estadoActual =
                EstadoBot.Buscando;


            temporizadorBusqueda =
                0f;
        }


        ResetearAntiAtasco();
    }



    // =====================================================
    // PARKING
    // =====================================================

    private bool ColocarEnParkingInicial()
    {
        if (gestorPuntosEspera == null)
            return false;


        puntoEsperaReservado =
            gestorPuntosEspera.ReservarPunto(
                this
            );


        if (puntoEsperaReservado == null)
            return false;


        if (!NavMesh.SamplePosition(
                puntoEsperaReservado.position,
                out NavMeshHit hit,
                1f,
                NavMesh.AllAreas))
        {
            LiberarPuntoEspera();

            return false;
        }


        if (agente != null &&
            agente.enabled)
        {
            if (!agente.Warp(
                    hit.position))
            {
                transform.position =
                    hit.position;
            }
        }
        else
        {
            transform.position =
                hit.position;
        }


        transform.rotation =
            puntoEsperaReservado.rotation;


        estadoActual =
            EstadoBot.Esperando;


        temporizadorBusquedaEnEspera =
            intervaloBusquedaEnEspera;


        if (empezarEnPausa)
        {
            pausaForzada =
                true;


            if (configuracionBot != null)
            {
                configuracionBot.modoActual =
                    ConfiguracionBot.ModoBot.Pausa;
            }
        }


        ResetearAntiAtasco();


        return true;
    }


    private void EntrarEnEspera()
    {
        estadoActual =
            EstadoBot.Esperando;


        temporizadorBusquedaEnEspera =
            intervaloBusquedaEnEspera;


        IntentarReservarPuntoEspera();


        ResetearAntiAtasco();
    }


    private bool IntentarReservarPuntoEspera()
    {
        if (puntoEsperaReservado != null)
            return true;


        if (gestorPuntosEspera == null)
            return false;


        puntoEsperaReservado =
            gestorPuntosEspera.ReservarPunto(
                this
            );


        return puntoEsperaReservado != null;
    }


    private void ComportamientoEsperar()
    {
        if (IntentarReservarPuntoEspera())
        {
            IrHaciaPuntoEspera();
        }
        else
        {
            DetenerAgente();
        }


        if (parkingForzado)
            return;


        temporizadorBusquedaEnEspera -=
            Time.deltaTime;


        if (temporizadorBusquedaEnEspera > 0f)
            return;


        temporizadorBusquedaEnEspera =
            intervaloBusquedaEnEspera;


        BuscarPiedra();
    }


    private void IrHaciaPuntoEspera()
    {
        if (puntoEsperaReservado == null ||
            agente == null ||
            !agente.isOnNavMesh)
        {
            return;
        }


        if (!NavMesh.SamplePosition(
                puntoEsperaReservado.position,
                out NavMeshHit hit,
                1f,
                NavMesh.AllAreas))
        {
            LiberarPuntoEspera();

            return;
        }


        float distancia =
            Vector3.Distance(
                transform.position,
                hit.position
            );


        if (distancia <=
            distanciaLlegadaParking)
        {
            DetenerAgente();

            return;
        }


        NavMeshPath camino =
            new NavMeshPath();


        if (!agente.CalculatePath(
                hit.position,
                camino) ||
            camino.status !=
            NavMeshPathStatus.PathComplete)
        {
            LiberarPuntoEspera();

            return;
        }


        agente.SetDestination(
            hit.position
        );
    }


    private void LiberarPuntoEspera()
    {
        if (gestorPuntosEspera != null)
        {
            gestorPuntosEspera.LiberarPunto(
                this
            );
        }


        puntoEsperaReservado =
            null;
    }


    // =====================================================
    // CONFIGURACIÓN DEL MENÚ
    // =====================================================

    public void ConfigurarModoTrabajar()
    {
        pausaForzada =
            false;


        parkingForzado =
            false;


        LiberarPuntoEspera();


        // Si está en mitad de la cadena de la procesadora,
        // no rompemos esa tarea: debe terminarla primero.
        if (EstaEnCadenaProcesadora())
        {
            ResetearAntiAtasco();

            return;
        }


        if (piedraObjetivo == null)
        {
            estadoActual =
                EstadoBot.Buscando;


            temporizadorBusqueda =
                0f;
        }


        ResetearAntiAtasco();
    }


    public void ConfigurarModoPausa()
    {
        pausaForzada =
            true;


        parkingForzado =
            false;


        // Si iba simplemente hacia una piedra,
        // liberamos esa piedra para los demás Bots.
        if (estadoActual ==
                EstadoBot.YendoAPiedra ||
            estadoActual ==
                EstadoBot.PreparandoEscalada)
        {
            CancelarObjetivo();
        }


        DetenerAgente();
    }


    public void ConfigurarModoParking()
    {
        pausaForzada =
            false;


        parkingForzado =
            true;


        if (estadoActual ==
                EstadoBot.YendoAPiedra ||
            estadoActual ==
                EstadoBot.PreparandoEscalada)
        {
            CancelarObjetivo();

            EntrarEnEspera();

            return;
        }


        // Si lleva una piedra o está completando la cadena
        // de la procesadora, termina esa tarea antes de aparcar.
        if (estadoActual ==
                EstadoBot.Escalando ||
            estadoActual ==
                EstadoBot.LlevandoPiedra ||
            estadoActual ==
                EstadoBot.EntregandoPiedra ||
            estadoActual ==
                EstadoBot.YendoAEsperaSalidaProcesadora ||
            estadoActual ==
                EstadoBot.EsperandoSalidaProcesadora ||
            estadoActual ==
                EstadoBot.YendoAPiedraProcesada)
        {
            return;
        }


        EntrarEnEspera();
    }


    // =====================================================
    // INTERACCIÓN
    // =====================================================

    public void IniciarInteraccion(
        Transform jugador)
    {
        enInteraccion =
            true;


        jugadorInteraccion =
            jugador;


        if (agente != null &&
            agente.isOnNavMesh)
        {
            agente.isStopped =
                true;
        }


        MantenerPiedraEnAgarre();
    }


    public void FinalizarInteraccion()
    {
        enInteraccion =
            false;


        jugadorInteraccion =
            null;


        if (agente != null &&
            agente.isOnNavMesh &&
            !pausaForzada)
        {
            agente.isStopped =
                false;
        }


        if (!pausaForzada)
        {
            ReanudarDespuesDeInteraccion();
        }
    }


    private void MantenerBotDuranteInteraccion()
    {
        MantenerPiedraEnAgarre();


        if (jugadorInteraccion == null)
            return;


        Vector3 direccion =
            jugadorInteraccion.position -
            transform.position;


        direccion.y =
            0f;


        if (direccion.sqrMagnitude <
            0.001f)
        {
            return;
        }


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(
                    direccion.normalized
                ),
                Time.deltaTime *
                velocidadGiroInteraccion
            );
    }


    private void MantenerPiedraEnAgarre()
    {
        if (piedraObjetivo == null ||
            puntoAgarre == null ||
            estadoActual !=
                EstadoBot.LlevandoPiedra)
        {
            return;
        }


        piedraObjetivo.transform.position =
            puntoAgarre.position;


        piedraObjetivo.transform.rotation =
            puntoAgarre.rotation;
    }


    private void ReanudarDespuesDeInteraccion()
    {
        if (estadoActual ==
                EstadoBot.YendoAPiedra ||
            estadoActual ==
                EstadoBot.LlevandoPiedra)
        {
            RecalcularRutaActual();
        }
    }


    // =====================================================
    // CANCELAR OBJETIVO
    // =====================================================

    private void CancelarObjetivo()
    {
        if (piedraObjetivo != null)
        {
            if (gestorPiedras != null)
            {
                gestorPiedras.LiberarReserva(
                    piedraObjetivo,
                    this
                );
            }


            if (estadoActual ==
                    EstadoBot.LlevandoPiedra ||
                estadoActual ==
                    EstadoBot.EntregandoPiedra ||
                piedraObjetivo.transform.parent ==
                    puntoAgarre)
            {
                RestaurarPiedraFisica(
                    piedraObjetivo
                );
            }
        }


        piedraObjetivo =
            null;


        piedraEsperadaProcesadora =
            null;


        temporizadorEsperaProcesadora =
            0f;


        temporizadorRetardoProcesada =
            0f;


        LiberarPuntoEntrega();


        DetenerAgente();


        estadoActual =
            EstadoBot.Buscando;


        temporizadorBusqueda =
            0f;


        ResetearAntiAtasco();
    }


    // =====================================================
    // RESTAURAR PIEDRA
    // =====================================================

    private void RestaurarPiedraFisica(
        Rigidbody piedra)
    {
        if (piedra == null)
            return;


        piedra.transform.SetParent(
            null,
            true
        );


        Collider[] colliders =
            piedra.GetComponentsInChildren<
                Collider
            >();


        foreach (Collider col in colliders)
        {
            col.enabled =
                true;
        }


        piedra.isKinematic =
            false;


        piedra.linearVelocity =
            Vector3.zero;


        piedra.angularVelocity =
            Vector3.zero;


        DeformacionPiedra deformacion =
            piedra.GetComponent<
                DeformacionPiedra
            >();


        if (deformacion != null)
        {
            deformacion.enabled =
                true;


            deformacion.Despertar();
        }
    }


    // =====================================================
    // DETENER
    // =====================================================

    private void DetenerAgente()
    {
        RestaurarVelocidadMovimiento();


        if (agente != null &&
            agente.isOnNavMesh &&
            agente.hasPath)
        {
            agente.ResetPath();
        }
    }


    // =====================================================
    // ANTI ATASCO
    // =====================================================

    private void ResetearAntiAtasco()
    {
        ultimaPosicionAntiAtasco =
            transform.position;


        temporizadorAntiAtasco =
            0f;


        tiempoAtascado =
            0f;


        reintentosRuta =
            0;
    }


    private void ComprobarAntiAtasco()
    {
        if (estadoActual !=
                EstadoBot.YendoAPiedra &&
            estadoActual !=
                EstadoBot.LlevandoPiedra)
        {
            temporizadorAntiAtasco =
                0f;

            tiempoAtascado =
                0f;

            ultimaPosicionAntiAtasco =
                transform.position;

            return;
        }


        if (agente == null ||
            !agente.isOnNavMesh ||
            agente.pathPending)
        {
            return;
        }


        temporizadorAntiAtasco +=
            Time.deltaTime;


        if (temporizadorAntiAtasco <
            intervaloComprobacionAtasco)
        {
            return;
        }


        float distanciaMovida =
            Vector3.Distance(
                transform.position,
                ultimaPosicionAntiAtasco
            );


        if (distanciaMovida >=
            distanciaMinimaAvance)
        {
            tiempoAtascado =
                0f;


            reintentosRuta =
                0;
        }
        else
        {
            bool deberiaMoverse =
                agente.hasPath &&
                agente.remainingDistance >
                agente.stoppingDistance +
                0.2f;


            if (deberiaMoverse)
            {
                tiempoAtascado +=
                    intervaloComprobacionAtasco;
            }
            else
            {
                tiempoAtascado =
                    0f;
            }
        }


        ultimaPosicionAntiAtasco =
            transform.position;


        temporizadorAntiAtasco =
            0f;


        if (agente.hasPath &&
            (agente.isPathStale ||
             agente.pathStatus !=
             NavMeshPathStatus.PathComplete))
        {
            ResolverAtasco();

            return;
        }


        if (tiempoAtascado >=
            tiempoMaximoAtascado)
        {
            ResolverAtasco();
        }
    }


    private void ResolverAtasco()
    {
        tiempoAtascado =
            0f;


        reintentosRuta++;


        if (reintentosRuta <=
            maxReintentosRuta)
        {
            RecalcularRutaActual();

            return;
        }


        if (estadoActual ==
            EstadoBot.YendoAPiedra)
        {
            AbandonarPiedraInaccesible();

            return;
        }


        if (estadoActual ==
            EstadoBot.LlevandoPiedra)
        {
            RecolocarBotEnNavMesh();


            reintentosRuta =
                0;


            RecalcularRutaActual();
        }
    }


    private void RecalcularRutaActual()
    {
        if (agente == null ||
            !agente.isOnNavMesh)
        {
            return;
        }


        DetenerAgente();


        if (estadoActual ==
            EstadoBot.YendoAPiedra)
        {
            if (piedraObjetivo == null)
            {
                CancelarObjetivo();

                return;
            }


            if (!IntentarCalcularRutaPiedra(
                    piedraObjetivo,
                    out NavMeshHit hit,
                    out NavMeshPath camino))
            {
                AbandonarPiedraInaccesible();

                return;
            }


            agente.SetDestination(
                hit.position
            );


            return;
        }


        if (estadoActual ==
            EstadoBot.LlevandoPiedra)
        {
            if (destinoPiedraActual ==
                ConfiguracionBot
                    .DestinoTrabajo
                    .Procesadora)
            {
                if (maquinaErosion == null ||
                    maquinaErosion.puntoEntregaBot == null)
                {
                    destinoPiedraActual =
                        ConfiguracionBot
                            .DestinoTrabajo
                            .Agujero;

                    return;
                }


                if (NavMesh.SamplePosition(
                        maquinaErosion
                            .puntoEntregaBot
                            .position,
                        out NavMeshHit hitProcesadora,
                        2f,
                        NavMesh.AllAreas))
                {
                    agente.SetDestination(
                        hitProcesadora.position
                    );
                }


                return;
            }


            IntentarReservarPuntoEntrega();

            IrHaciaPuntoEntrega();
        }
    }


    private void RecolocarBotEnNavMesh()
    {
        if (agente == null)
            return;


        if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
        {
            agente.Warp(
                hit.position
            );
        }
    }


    // =====================================================
    // DEBUG - TRAZADA LANZAMIENTO AL AGUJERO
    // =====================================================

    private void DibujarDebugTrazadaLanzamiento(
        Vector3 inicio,
        Vector3 bocaAgujero,
        Vector3 interiorAgujero,
        float alturaArco)
    {
        if (!mostrarDebugTrazadaLanzamiento)
            return;


        int segmentos =
            Mathf.Max(
                4,
                segmentosDebugTrazadaLanzamiento
            );


        float duracion =
            Mathf.Max(
                0f,
                duracionDebugTrazada
            );


        Vector3 anterior =
            inicio;


        // Amarillo: arco real hasta la boca.
        for (int i = 1;
             i <= segmentos;
             i++)
        {
            float t =
                i /
                (float)segmentos;


            Vector3 posicion =
                Vector3.Lerp(
                    inicio,
                    bocaAgujero,
                    t
                );


            posicion.y +=
                Mathf.Sin(
                    t * Mathf.PI
                )
                *
                alturaArco;


            Debug.DrawLine(
                anterior,
                posicion,
                Color.yellow,
                duracion,
                false
            );


            anterior =
                posicion;
        }


        // Rojo: pequeña caída visual que hacemos dentro del agujero.
        Debug.DrawLine(
            bocaAgujero,
            interiorAgujero,
            Color.red,
            duracion,
            false
        );
    }


    // =====================================================
    // DESTROY
    // =====================================================

    private void OnDestroy()
    {
        if (gestorPiedras != null)
        {
            gestorPiedras.LiberarReservasDeBot(
                this
            );
        }


        if (piedraObjetivo != null &&
            puntoAgarre != null &&
            piedraObjetivo.transform.parent ==
                puntoAgarre)
        {
            RestaurarPiedraFisica(
                piedraObjetivo
            );
        }


        LiberarPuntoEntrega();

        LiberarPuntoEspera();
    }


    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        // =================================================
        // PREVISUALIZACIÓN PERMANENTE DEL LANZAMIENTO
        // =================================================
        //
        // Amarillo = trayectoria hasta la boca.
        // Rojo     = caída visual dentro del agujero.
        // Verde    = inicio.
        // Blanco   = centro de la boca.
        // =================================================

        if (mostrarDebugTrazadaLanzamiento &&
            puntoAgarre != null &&
            puntoEntradaAgujero != null)
        {
            Vector3 inicioDebug =
                puntoAgarre.position;


            Vector3 bocaDebug =
                puntoEntradaAgujero.position;


            Vector3 interiorDebug =
                bocaDebug +
                Vector3.down *
                Mathf.Max(
                    0f,
                    profundidadVisualAgujero
                );


            float alturaDebug =
                Mathf.Min(
                    Mathf.Max(
                        0f,
                        alturaArcoLanzamiento
                    ),
                    Mathf.Max(
                        0f,
                        alturaMaximaArcoEntrega
                    )
                );


            int segmentos =
                Mathf.Max(
                    4,
                    segmentosDebugTrazadaLanzamiento
                );


            Vector3 anterior =
                inicioDebug;


            Gizmos.color =
                Color.yellow;


            for (int i = 1;
                 i <= segmentos;
                 i++)
            {
                float t =
                    i /
                    (float)segmentos;


                Vector3 posicion =
                    Vector3.Lerp(
                        inicioDebug,
                        bocaDebug,
                        t
                    );


                posicion.y +=
                    Mathf.Sin(
                        t * Mathf.PI
                    )
                    *
                    alturaDebug;


                Gizmos.DrawLine(
                    anterior,
                    posicion
                );


                anterior =
                    posicion;
            }


            Gizmos.color =
                Color.red;


            Gizmos.DrawLine(
                bocaDebug,
                interiorDebug
            );


            Gizmos.color =
                Color.green;


            Gizmos.DrawWireSphere(
                inicioDebug,
                0.08f
            );


            Gizmos.color =
                Color.white;


            Gizmos.DrawWireSphere(
                bocaDebug,
                0.10f
            );


            Gizmos.color =
                Color.red;


            Gizmos.DrawWireSphere(
                interiorDebug,
                0.08f
            );
        }


        if (piedraObjetivo != null)
        {
            Gizmos.color =
                Color.cyan;


            Gizmos.DrawLine(
                transform.position,
                piedraObjetivo.position
            );


            Gizmos.DrawWireSphere(
                piedraObjetivo.position,
                0.4f
            );
        }


        if (puntoLlegadaReservado != null)
        {
            Gizmos.color =
                Color.green;


            Gizmos.DrawWireSphere(
                puntoLlegadaReservado.position,
                0.5f
            );
        }


        if (puntoEsperaReservado != null)
        {
            Gizmos.color =
                Color.magenta;


            Gizmos.DrawWireSphere(
                puntoEsperaReservado.position,
                0.5f
            );
        }
    }
    private ConfiguracionBot.DestinoTrabajo
    CalcularDestinoPiedra(
        Rigidbody piedra)
    {
        if (configuracionBot == null)
        {
            return ConfiguracionBot
                .DestinoTrabajo
                .Agujero;
        }


        // =====================================================
        // FORZADO AL AGUJERO
        // =====================================================

        if (configuracionBot.destinoTrabajo ==
            ConfiguracionBot.DestinoTrabajo.Agujero)
        {
            return ConfiguracionBot
                .DestinoTrabajo
                .Agujero;
        }


        // =====================================================
        // FORZADO A PROCESADORA
        // =====================================================

        if (configuracionBot.destinoTrabajo ==
            ConfiguracionBot.DestinoTrabajo.Procesadora)
        {
            return ConfiguracionBot
                .DestinoTrabajo
                .Procesadora;
        }


        // =====================================================
        // AUTOMÁTICO
        // =====================================================

        float pureza =
            ObtenerPurezaPiedra(
                piedra
            );


        // Ya es suficientemente buena.
        if (pureza >=
            configuracionBot.purezaDirectaAgujero)
        {
            return ConfiguracionBot
                .DestinoTrabajo
                .Agujero;
        }


        // Todavía merece procesarse.
        if (maquinaErosion != null &&
            maquinaErosion
                .PuedeAceptarPiedra(
                    piedra))
        {
            return ConfiguracionBot
                .DestinoTrabajo
                .Procesadora;
        }


        // Si la procesadora no está disponible
        // o no acepta esa piedra, usamos el agujero.
        return ConfiguracionBot
            .DestinoTrabajo
            .Agujero;
    }

    private void ComportamientoLlevarProcesadora()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();
            return;
        }


        // =====================================================
        // COMPROBAR PROCESADORA
        // =====================================================

        if (maquinaErosion == null)
        {
            DetenerAgente();

            Debug.LogError(
                name +
                ": MaquinaErosion no está asignada."
            );

            return;
        }


        if (!maquinaErosion.ProcesadoraDesbloqueada)
        {
            DetenerAgente();

            Debug.LogError(
                name +
                ": la procesadora está bloqueada."
            );

            return;
        }


        if (maquinaErosion.puntoEntregaBot == null)
        {
            DetenerAgente();

            Debug.LogError(
                name +
                ": PuntoEntregaBot no está asignado."
            );

            return;
        }


        if (maquinaErosion.puntoEntrada == null)
        {
            DetenerAgente();

            Debug.LogError(
                name +
                ": PuntoEntrada de la procesadora no está asignado."
            );

            return;
        }


        if (agente == null ||
            !agente.isOnNavMesh)
        {
            return;
        }


        // =====================================================
        // PUNTO NAVMESH DE ENTREGA
        // =====================================================

        if (!NavMesh.SamplePosition(
                maquinaErosion.puntoEntregaBot.position,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
        {
            DetenerAgente();

            Debug.LogError(
                name +
                ": no hay NavMesh cerca del PuntoEntregaBot."
            );

            return;
        }


        agente.isStopped = false;

        agente.SetDestination(
            hit.position
        );


        float distancia =
            Vector3.Distance(
                transform.position,
                hit.position
            );


        if (distancia >
            distanciaEntregaProcesadora)
        {
            return;
        }


        // =====================================================
        // HA LLEGADO A LA PROCESADORA
        // =====================================================

        DetenerAgente();


        estadoActual =
            EstadoBot.EntregandoPiedra;


        ResetearAntiAtasco();


        StartCoroutine(
            LanzarPiedraAProcesadora()
        );
    }


    private IEnumerator LanzarPiedraAProcesadora()
    {
        if (piedraObjetivo == null ||
            maquinaErosion == null ||
            maquinaErosion.puntoEntrada == null)
        {
            CancelarObjetivo();
            yield break;
        }


        Rigidbody piedraEntregada =
            piedraObjetivo;


        // =====================================================
        // SOLTAR DEL BOT
        // =====================================================

        piedraEntregada.transform.SetParent(
            null,
            true
        );


        piedraEntregada.isKinematic =
            true;


        Collider[] colliders =
            piedraEntregada
                .GetComponentsInChildren<Collider>();


        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled =
                    false;
            }
        }


        // =====================================================
        // INICIO Y DESTINO DEL LANZAMIENTO
        // =====================================================

        Vector3 inicio =
            piedraEntregada.position;


        Vector3 final =
            maquinaErosion
                .puntoEntrada
                .position;


        float tiempo =
            0f;


        // =====================================================
        // LANZAMIENTO VISUAL
        // =====================================================

        while (tiempo <
               duracionLanzamiento)
        {
            if (enInteraccion ||
                pausaForzada)
            {
                yield return null;

                continue;
            }


            if (piedraEntregada == null)
            {
                yield break;
            }


            tiempo +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    tiempo /
                    Mathf.Max(
                        0.01f,
                        duracionLanzamiento
                    )
                );


            Vector3 posicion =
                Vector3.Lerp(
                    inicio,
                    final,
                    t
                );


            // =================================================
            // ARCO
            // =================================================

            posicion.y +=
                Mathf.Sin(
                    t * Mathf.PI
                )
                *
                alturaArcoLanzamiento;


            piedraEntregada.position =
                posicion;


            // =================================================
            // GIRO
            // =================================================

            piedraEntregada.transform.Rotate(
                Vector3.one *
                velocidadGiroLanzamiento *
                Time.deltaTime,
                Space.World
            );


            yield return null;
        }


        // =====================================================
        // LLEGADA EXACTA A LA ENTRADA
        // =====================================================

        if (piedraEntregada == null)
        {
            yield break;
        }


        piedraEntregada.position =
            final;


        // =====================================================
        // ENTREGAR A LA PROCESADORA
        // =====================================================

        bool aceptada =
            maquinaErosion
                .RecibirPiedraBot(
                    piedraEntregada,
                    this
                );


        // =====================================================
        // SI LA RECHAZA
        // =====================================================

        if (!aceptada)
        {
            Debug.LogWarning(
                name +
                ": la procesadora rechazó la piedra."
            );


            // Volver a cogerla.
            piedraEntregada.transform.SetParent(
                puntoAgarre,
                false
            );


            piedraEntregada.transform.localPosition =
                Vector3.zero;


            piedraEntregada.transform.localRotation =
                Quaternion.identity;


            piedraObjetivo =
                piedraEntregada;


            estadoActual =
                EstadoBot.LlevandoPiedra;


            yield break;
        }


        // =====================================================
        // ESTADÍSTICA - PROCESADA CORRECTAMENTE
        // =====================================================

        if (estadisticasBot != null)
        {
            estadisticasBot
                .RegistrarPiedraProcesada();
        }


        // =====================================================
        // ENTREGA CORRECTA
        // =====================================================

        // La piedra ya pertenece temporalmente a la máquina.
        // El Bot recuerda exactamente cuál es para recuperarla
        // cuando MaquinaErosion notifique su salida.
        piedraObjetivo =
            null;


        piedraEsperadaProcesadora =
            piedraEntregada;


        temporizadorEsperaProcesadora =
            tiempoMaximoEsperaProcesadora;


        temporizadorRetardoProcesada =
            0f;


        if (gestorPiedras != null)
        {
            gestorPiedras.LiberarReserva(
                piedraEntregada,
                this
            );
        }


        // =====================================================
        // IR AL PUNTO DE ESPERA DE LA SALIDA
        // =====================================================

        estadoActual =
            EstadoBot.YendoAEsperaSalidaProcesadora;


        ResetearAntiAtasco();


        if (debugBusqueda)
        {
            Debug.Log(
                name +
                ": piedra aceptada por la procesadora. " +
                "Esperando su salida."
            );
        }
    }

    // =====================================================
    // CADENA: ESPERAR SALIDA DE PROCESADORA
    // =====================================================

    private bool EstaEnCadenaProcesadora()
    {
        return estadoActual ==
                   EstadoBot.YendoAEsperaSalidaProcesadora ||
               estadoActual ==
                   EstadoBot.EsperandoSalidaProcesadora ||
               estadoActual ==
                   EstadoBot.YendoAPiedraProcesada;
    }


    private bool ConsumirTiempoEsperaProcesadora()
    {
        temporizadorEsperaProcesadora -=
            Time.deltaTime;


        if (temporizadorEsperaProcesadora > 0f)
            return true;


        AbandonarCadenaProcesadora(
            "tiempo máximo de espera superado"
        );


        return false;
    }


    private void ComportamientoIrAEsperaSalidaProcesadora()
    {
        if (piedraEsperadaProcesadora == null)
        {
            AbandonarCadenaProcesadora(
                "se perdió la referencia de la piedra"
            );

            return;
        }


        if (!ConsumirTiempoEsperaProcesadora())
            return;


        if (maquinaErosion == null)
        {
            AbandonarCadenaProcesadora(
                "no existe MaquinaErosion"
            );

            return;
        }


        Transform puntoEspera =
            maquinaErosion.puntoEsperaSalidaBot != null
            ? maquinaErosion.puntoEsperaSalidaBot
            : maquinaErosion.puntoEntregaBot;


        if (puntoEspera == null)
        {
            // No rompemos el trabajo si falta el punto:
            // esperamos quietos en la posición actual.
            DetenerAgente();

            estadoActual =
                EstadoBot.EsperandoSalidaProcesadora;

            return;
        }


        if (agente == null ||
            !agente.isOnNavMesh)
        {
            return;
        }


        if (!NavMesh.SamplePosition(
                puntoEspera.position,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
        {
            DetenerAgente();

            return;
        }


        agente.isStopped =
            false;


        agente.SetDestination(
            hit.position
        );


        float distancia =
            Vector3.Distance(
                transform.position,
                hit.position
            );


        if (distancia <=
            distanciaLlegadaEsperaProcesadora)
        {
            DetenerAgente();

            estadoActual =
                EstadoBot.EsperandoSalidaProcesadora;


            ResetearAntiAtasco();
        }
    }


    private void ComportamientoEsperarSalidaProcesadora()
    {
        DetenerAgente();


        if (piedraEsperadaProcesadora == null)
        {
            AbandonarCadenaProcesadora(
                "se perdió la referencia de la piedra"
            );

            return;
        }


        ConsumirTiempoEsperaProcesadora();
    }


    // MaquinaErosion llama a este método exactamente cuando
    // la MISMA piedra que entregó este Bot termina de salir.
    public void NotificarPiedraProcesadaLista(
        Rigidbody piedra)
    {
        if (piedra == null)
            return;


        if (piedraEsperadaProcesadora == null ||
            piedra != piedraEsperadaProcesadora)
        {
            return;
        }


        if (gestorPiedras == null)
        {
            gestorPiedras =
                GestorPiedras.Instancia;
        }


        // La máquina vuelve a registrar la piedra justo antes
        // de llamar aquí. La reservamos inmediatamente para
        // que ningún otro Bot pueda robársela.
        if (gestorPiedras != null)
        {
            if (!gestorPiedras.IntentarReservarPiedra(
                    piedra,
                    this))
            {
                AbandonarCadenaProcesadora(
                    "no se pudo reservar la piedra procesada"
                );

                return;
            }
        }


        piedraObjetivo =
            piedra;


        destinoPiedraActual =
            ConfiguracionBot
                .DestinoTrabajo
                .Agujero;


        temporizadorRetardoProcesada =
            retardoRecogidaProcesada;


        // Damos un tiempo nuevo para que pueda caer,
        // estabilizarse y ser recogida.
        temporizadorEsperaProcesadora =
            tiempoMaximoEsperaProcesadora;


        estadoActual =
            EstadoBot.YendoAPiedraProcesada;


        ResetearAntiAtasco();


        if (debugBusqueda)
        {
            Debug.Log(
                name +
                ": su piedra ha salido de la procesadora. " +
                "Va a recogerla y llevarla al agujero."
            );
        }
    }


    private void ComportamientoIrAPiedraProcesada()
    {
        if (piedraObjetivo == null)
        {
            AbandonarCadenaProcesadora(
                "la piedra procesada dejó de existir"
            );

            return;
        }


        if (!ConsumirTiempoEsperaProcesadora())
            return;


        // Dejamos un instante para que la expulsión física
        // empiece a asentarse.
        if (temporizadorRetardoProcesada > 0f)
        {
            temporizadorRetardoProcesada -=
                Time.deltaTime;


            DetenerAgente();

            return;
        }


        if (agente == null ||
            !agente.isOnNavMesh)
        {
            return;
        }


        if (!NavMesh.SamplePosition(
                piedraObjetivo.position,
                out NavMeshHit hit,
                radioNavMeshPiedraProcesada,
                NavMesh.AllAreas))
        {
            // La piedra puede seguir en el aire.
            // No la damos por inaccesible todavía.
            DetenerAgente();

            return;
        }


        NavMeshPath camino =
            new NavMeshPath();


        if (!agente.CalculatePath(
                hit.position,
                camino) ||
            camino.status !=
                NavMeshPathStatus.PathComplete)
        {
            DetenerAgente();

            return;
        }


        agente.isStopped =
            false;


        agente.SetDestination(
            hit.position
        );


        float distancia =
            Vector3.Distance(
                transform.position,
                hit.position
            );


        if (distancia >
            distanciaRecogida)
        {
            return;
        }


        // No intentamos agarrarla si todavía sale disparada
        // a demasiada velocidad.
        if (!piedraObjetivo.isKinematic &&
            piedraObjetivo.linearVelocity.magnitude >
                velocidadMaximaRecogerProcesada)
        {
            return;
        }


        RecogerPiedraProcesada();
    }


    private void RecogerPiedraProcesada()
    {
        if (piedraObjetivo == null ||
            puntoAgarre == null)
        {
            AbandonarCadenaProcesadora(
                "no se puede recoger la piedra procesada"
            );

            return;
        }


        DetenerAgente();


        if (!piedraObjetivo.isKinematic)
        {
            piedraObjetivo.linearVelocity =
                Vector3.zero;


            piedraObjetivo.angularVelocity =
                Vector3.zero;
        }


        DeformacionPiedra deformacion =
            piedraObjetivo.GetComponent<
                DeformacionPiedra
            >();


        if (deformacion == null)
        {
            deformacion =
                piedraObjetivo.GetComponentInParent<
                    DeformacionPiedra
                >();
        }


        if (deformacion != null)
        {
            deformacion.enabled =
                false;
        }


        piedraObjetivo.isKinematic =
            true;


        Collider[] colliders =
            piedraObjetivo
                .GetComponentsInChildren<Collider>();


        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled =
                    false;
            }
        }


        piedraObjetivo.transform.SetParent(
            puntoAgarre,
            false
        );


        piedraObjetivo.transform.localPosition =
            Vector3.zero;


        piedraObjetivo.transform.localRotation =
            Quaternion.identity;


        // Ya la hemos recuperado. A partir de aquí se trata
        // como una piedra normal transportada, pero el destino
        // queda FORZADO al agujero para no reprocesarla.
        piedraEsperadaProcesadora =
            null;


        temporizadorEsperaProcesadora =
            0f;


        temporizadorRetardoProcesada =
            0f;


        destinoPiedraActual =
            ConfiguracionBot
                .DestinoTrabajo
                .Agujero;


        estadoActual =
            EstadoBot.LlevandoPiedra;


        ResetearAntiAtasco();
    }


    private void AbandonarCadenaProcesadora(
        string motivo)
    {
        Rigidbody piedraLiberar =
            piedraObjetivo;


        if (piedraLiberar != null &&
            gestorPiedras != null)
        {
            gestorPiedras.LiberarReserva(
                piedraLiberar,
                this
            );
        }


        piedraObjetivo =
            null;


        piedraEsperadaProcesadora =
            null;


        temporizadorEsperaProcesadora =
            0f;


        temporizadorRetardoProcesada =
            0f;


        DetenerAgente();


        if (parkingForzado)
        {
            EntrarEnEspera();
        }
        else
        {
            estadoActual =
                EstadoBot.Buscando;


            temporizadorBusqueda =
                0f;
        }


        ResetearAntiAtasco();


        if (debugBusqueda)
        {
            Debug.LogWarning(
                name +
                ": abandona la espera de procesadora: " +
                motivo
            );
        }
    }

}