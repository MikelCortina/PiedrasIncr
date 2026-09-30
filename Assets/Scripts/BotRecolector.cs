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
        Esperando
    }


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


        configuracionBot =
            GetComponent<ConfiguracionBot>();


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


            case EstadoBot.LlevandoPiedra:

                ComportamientoLlevarPiedra();

                break;


            case EstadoBot.EntregandoPiedra:

                break;


            case EstadoBot.Esperando:

                ComportamientoEsperar();

                break;
        }


        ComprobarAntiAtasco();
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
            col.enabled =
                false;
        }


        piedraObjetivo.transform.SetParent(
            puntoAgarre,
            false
        );


        piedraObjetivo.transform.localPosition =
            Vector3.zero;


        piedraObjetivo.transform.localRotation =
            Quaternion.identity;


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
            col.enabled =
                false;
        }


        Vector3 inicio =
            piedraEntregada.position;


        Vector3 final =
            puntoEntradaAgujero.position;


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


            Vector3 posicion =
                Vector3.Lerp(
                    inicio,
                    final,
                    t
                );


            posicion.y +=
                Mathf.Sin(
                    t * Mathf.PI
                )
                *
                alturaArcoLanzamiento;


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


        piedraEntregada.position =
            final;


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
            EstadoBot.YendoAPiedra)
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
            EstadoBot.YendoAPiedra)
        {
            CancelarObjetivo();

            EntrarEnEspera();

            return;
        }


        // Si lleva una piedra la entrega primero.
        if (estadoActual ==
                EstadoBot.LlevandoPiedra ||
            estadoActual ==
                EstadoBot.EntregandoPiedra)
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
            // Si abrimos el menú o pausamos el Bot,
            // congelamos también el lanzamiento.

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
                    piedraEntregada
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
        // ENTREGA CORRECTA
        // =====================================================

        piedraObjetivo =
            null;


        if (gestorPiedras != null)
        {
            gestorPiedras.LiberarReserva(
                piedraEntregada,
                this
            );
        }


        // =====================================================
        // VOLVER A TRABAJAR
        // =====================================================

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
            Debug.Log(
                name +
                ": piedra lanzada correctamente " +
                "a la procesadora."
            );
        }
    }
}