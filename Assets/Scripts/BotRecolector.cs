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


    // =====================================================
    // ESTADO
    // =====================================================

    [Header("Estado")]
    [SerializeField]
    private EstadoBot estadoActual =
        EstadoBot.Buscando;

    private ConfiguracionBot configuracionBot;

    private bool pausaForzada = false;

    private bool parkingForzado = false;


    // =====================================================
    // REFERENCIAS
    // =====================================================

    [Header("Referencias")]

    public Transform puntoAgarre;


    // =====================================================
    // GESTOR GLOBAL DE PIEDRAS
    // =====================================================

    [Header("Gestor Global de Piedras")]

    public GestorPiedras gestorPiedras;


    // =====================================================
    // INTERACCIÓN CON JUGADOR
    // =====================================================

    [Header("Interacción con Jugador")]

    [Tooltip("Velocidad a la que el Bot gira para mirar al jugador.")]
    public float velocidadGiroInteraccion = 6f;

    private bool enInteraccion = false;

    private Transform jugadorInteraccion;

    public bool EnInteraccion => enInteraccion;

    // =====================================================
    // ENTREGA
    // =====================================================

    [Header("Sistema de Entrega")]

    public GestorPuntosEntregaBots gestorPuntosEntrega;

    public Transform puntoEntradaAgujero;

    public AgujeroSimple agujeroDestino;

    public float distanciaEntrega = 1.5f;

    public float duracionLanzamiento = 0.5f;

    public float alturaArcoLanzamiento = 2f;

    public float velocidadGiroLanzamiento = 360f;


    // =====================================================
    // PARKING / ESPERA
    // =====================================================

    [Header("Parking de Bots")]

    [Tooltip(
        "Gestor que contiene las plazas donde " +
        "los Bots esperan cuando no hay trabajo."
    )]
    public GestorPuntosEsperaBots gestorPuntosEspera;

    [Tooltip(
        "Distancia a la plaza para considerar " +
        "que el Bot ya está aparcado."
    )]
    public float distanciaLlegadaParking = 0.6f;

    [Tooltip(
        "Cada cuánto comprueba si ha aparecido " +
        "trabajo mientras está aparcado."
    )]
    public float intervaloBusquedaEnEspera = 1f;

    [Tooltip("Si está activo, el Bot aparece directamente en una plaza libre del parking al comenzar.")]
    public bool aparecerEnParkingAlIniciar = true;


    // =====================================================
    // BÚSQUEDA
    // =====================================================

    [Header("Búsqueda de Piedras")]

    [Tooltip(
        "Radio de prioridad. NO limita la búsqueda global."
    )]
    public float radioBusqueda = 20f;

    public string tagPiedra = "Piedra";

    public float tiempoEntreBusquedas = 0.5f;


    // =====================================================
    // PIEDRAS INACCESIBLES
    // =====================================================

    [Header("Piedras Inaccesibles")]

    public float tiempoIgnorarPiedraInaccesible = 3f;

    public float distanciaMaximaPiedraANavMesh = 1.5f;

    public float intervaloRevalidacionObjetivo = 0.4f;


    // =====================================================
    // RECOGIDA
    // =====================================================

    [Header("Recogida")]

    public float distanciaRecogida = 1.5f;


    // =====================================================
    // ANTI-ATASCO
    // =====================================================

    [Header("Anti-Atasco")]

    public float intervaloComprobacionAtasco = 0.75f;

    public float distanciaMinimaAvance = 0.15f;

    public float tiempoMaximoAtascado = 2f;

    public int maxReintentosRuta = 2;


    // =====================================================
    // VARIABLES INTERNAS
    // =====================================================

    private NavMeshAgent agente;

    private Rigidbody piedraObjetivo;

    private Transform puntoLlegadaReservado;

    private Transform puntoEsperaReservado;

    private float temporizadorBusqueda;

    private float temporizadorBusquedaEnEspera;

    private float temporizadorRevalidacionObjetivo;


    private readonly Dictionary<Rigidbody, float>
        piedrasIgnoradasHasta =
        new Dictionary<Rigidbody, float>();


    // Anti-atasco
    private Vector3 ultimaPosicionAntiAtasco;

    private float temporizadorAntiAtasco;

    private float tiempoAtascado;

    private int reintentosRuta;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        agente =
            GetComponent<NavMeshAgent>();
        configuracionBot =
    GetComponent<ConfiguracionBot>();

        if (gestorPiedras == null)
        {
            gestorPiedras =
                GestorPiedras.Instancia;
        }


        if (gestorPuntosEntrega == null)
        {
            gestorPuntosEntrega =
                FindFirstObjectByType<GestorPuntosEntregaBots>();
        }


        if (gestorPuntosEspera == null)
        {
            gestorPuntosEspera =
                FindFirstObjectByType<GestorPuntosEsperaBots>();
        }


        if (agujeroDestino == null)
        {
            agujeroDestino =
                FindFirstObjectByType<AgujeroSimple>();
        }


        if (gestorPiedras == null)
        {
            Debug.LogError(
                "BotRecolector: no existe GestorPiedras."
            );
        }


        ResetearAntiAtasco();


        // =================================================
        // APARECER DIRECTAMENTE EN EL PARKING
        // =================================================

        if (aparecerEnParkingAlIniciar)
        {
            if (ColocarEnParkingInicial())
            {
                return;
            }
        }


        // Si no se pudo colocar en el parking,
        // empezamos de manera normal.
        estadoActual =
            EstadoBot.Buscando;
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // =================================================
        // HABLANDO / CONFIGURANDO
        // =================================================

        if (enInteraccion)
        {
            MantenerBotDuranteInteraccion();
            return;
        }


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
    // BUSCAR
    // =====================================================

    private void ComportamientoBuscar()
    {
        temporizadorBusqueda -=
            Time.deltaTime;


        if (temporizadorBusqueda > 0f)
            return;


        temporizadorBusqueda =
            tiempoEntreBusquedas;


        bool encontroPiedra =
            BuscarPiedra();


        if (!encontroPiedra)
        {
            EntrarEnEspera();
        }
    }


    // =====================================================
    // BÚSQUEDA GLOBAL
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
                return false;
        }


        LimpiarPiedrasIgnoradas();


        List<Rigidbody> candidatas =
            gestorPiedras.ObtenerPiedrasOrdenadas(
                transform.position,
                radioBusqueda
            );


        foreach (Rigidbody piedra in candidatas)
        {
            if (piedra == null)
                continue;
            if (configuracionBot != null &&
    configuracionBot.PiedraEstaDemasiadoMovida(
        piedra))
            {
                continue;
            }

            if (!piedra.CompareTag(tagPiedra))
                continue;


            if (piedra.transform.parent != null)
                continue;


            if (EstaPiedraTemporalmenteIgnorada(
                    piedra))
            {
                continue;
            }


            if (!IntentarCalcularRutaPiedra(
                    piedra,
                    out NavMeshHit puntoNavMesh,
                    out NavMeshPath camino))
            {
                MarcarPiedraInaccesible(
                    piedra
                );

                continue;
            }


            if (!gestorPiedras.IntentarReservarPiedra(
                    piedra,
                    this))
            {
                continue;
            }


            // Si estábamos en el parking,
            // dejamos libre nuestra plaza.
            LiberarPuntoEspera();


            piedraObjetivo =
                piedra;


            estadoActual =
                EstadoBot.YendoAPiedra;


            temporizadorRevalidacionObjetivo =
                intervaloRevalidacionObjetivo;


            ResetearAntiAtasco();


            agente.SetDestination(
                puntoNavMesh.position
            );


            return true;
        }


        return false;
    }


    // =====================================================
    // PARKING
    // =====================================================

    private void EntrarEnEspera()
    {
        estadoActual =
            EstadoBot.Esperando;


        temporizadorBusquedaEnEspera =
            intervaloBusquedaEnEspera;


        ResetearAntiAtasco();


        IntentarReservarPuntoEspera();
    }


    private bool IntentarReservarPuntoEspera()
    {
        if (puntoEsperaReservado != null)
            return true;


        if (gestorPuntosEspera == null)
        {
            return false;
        }


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


        // =================================================
        // PARKING ORDENADO MANUALMENTE
        // =================================================

        if (parkingForzado)
        {
            return;
        }


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
        if (puntoEsperaReservado == null)
            return;


        if (agente == null ||
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
            // La plaza dejó de ser válida.
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
            // Ya está aparcado.
            DetenerAgente();

            return;
        }


        NavMeshPath camino =
            new NavMeshPath();


        bool rutaValida =
            agente.CalculatePath(
                hit.position,
                camino
            );


        if (!rutaValida ||
            camino.status !=
            NavMeshPathStatus.PathComplete)
        {
            // No podemos llegar a esta plaza.
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
    // ACCESIBILIDAD DE PIEDRAS
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


        bool pudoCalcular =
            agente.CalculatePath(
                puntoNavMesh.position,
                camino
            );


        if (!pudoCalcular)
            return false;


        if (camino.status !=
            NavMeshPathStatus.PathComplete)
        {
            return false;
        }


        return true;
    }


    // =====================================================
    // PIEDRAS INACCESIBLES
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


        // Inmediatamente intenta buscar otra.
        temporizadorBusqueda =
            0f;


        temporizadorRevalidacionObjetivo =
            0f;


        ResetearAntiAtasco();
    }


    // =====================================================
    // IR HACIA PIEDRA
    // =====================================================

    private void ComportamientoIrAPiedra()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();

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
            piedraObjetivo.GetComponentsInChildren<
                Collider
            >();


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
    // LLEVAR AL AGUJERO
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


        piedraObjetivo.transform.position =
            puntoAgarre.position;


        piedraObjetivo.transform.rotation =
            puntoAgarre.rotation;


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
    // PUNTOS DE ENTREGA
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
        if (puntoLlegadaReservado == null)
            return false;


        if (agente == null ||
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


        float distanciaDirecta =
            Vector3.Distance(
                transform.position,
                puntoLlegadaReservado.position
            );


        if (distanciaDirecta <=
            distanciaEntrega)
        {
            return true;
        }


        if (agente == null ||
            !agente.isOnNavMesh ||
            agente.pathPending ||
            !agente.hasPath)
        {
            return false;
        }


        float distanciaAceptable =
            Mathf.Max(
                distanciaEntrega,
                agente.stoppingDistance +
                0.05f
            );


        return agente.remainingDistance <=
               distanciaAceptable;
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
    // LANZAMIENTO GARANTIZADO
    // =====================================================

    private IEnumerator LanzarPiedraAlAgujero()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();

            yield break;
        }


        if (puntoEntradaAgujero == null ||
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
            piedraEntregada.GetComponentsInChildren<
                Collider
            >();


        foreach (Collider col in colliders)
        {
            col.enabled =
                false;
        }


        Vector3 posicionInicial =
            piedraEntregada.position;


        Vector3 posicionFinal =
            puntoEntradaAgujero.position;


        float tiempo =
            0f;


        while (tiempo <
               duracionLanzamiento)
        
        {
            // Si abrimos el menú justo durante la entrega,
            // congelamos también la animación.
            if (enInteraccion)
            {
                yield return null;
                continue;
            }
            if (piedraEntregada == null)
            {
                LiberarPuntoEntrega();


                estadoActual =
                    EstadoBot.Buscando;


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
                    posicionInicial,
                    posicionFinal,
                    t
                );


            float altura =
                Mathf.Sin(
                    t * Mathf.PI
                )
                *
                alturaArcoLanzamiento;


            posicion.y +=
                altura;


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


            estadoActual =
                EstadoBot.Buscando;


            yield break;
        }


        piedraEntregada.position =
            posicionFinal;


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


        estadoActual =
            EstadoBot.Buscando;


        // Al terminar una entrega buscamos prácticamente
        // inmediatamente si queda trabajo.
        temporizadorBusqueda =
            0f;


        ResetearAntiAtasco();
    }


    // =====================================================
    // CANCELAR
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
    // ANTI-ATASCO
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
        // Esperando NO utiliza este antiatasco.
        //
        // Mientras está aparcando controlamos su ruta
        // directamente desde ComportamientoEsperar().
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
            !agente.isOnNavMesh)
        {
            return;
        }


        if (agente.pathPending)
            return;


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


            tiempoAtascado =
                0f;


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
                    out NavMeshHit puntoNavMesh,
                    out NavMeshPath camino))
            {
                AbandonarPiedraInaccesible();

                return;
            }


            agente.SetDestination(
                puntoNavMesh.position
            );


            return;
        }


        if (estadoActual ==
            EstadoBot.LlevandoPiedra)
        {
            if (!IntentarReservarPuntoEntrega())
                return;


            if (puntoLlegadaReservado == null)
                return;


            if (NavMesh.SamplePosition(
                    puntoLlegadaReservado.position,
                    out NavMeshHit hitDestino,
                    3f,
                    NavMesh.AllAreas))
            {
                agente.SetDestination(
                    hitDestino.position
                );
            }
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
    // CONFIGURACIÓN DESDE MENÚ
    // =====================================================

    public void ConfigurarModoTrabajar()
    {
        pausaForzada =
            false;


        parkingForzado =
            false;


        LiberarPuntoEspera();


        estadoActual =
            EstadoBot.Buscando;


        temporizadorBusqueda =
            0f;


        ResetearAntiAtasco();
    }


    public void ConfigurarModoPausa()
    {
        pausaForzada =
            true;


        parkingForzado =
            false;


        DetenerAgente();
    }


    public void ConfigurarModoParking()
    {
        pausaForzada =
            false;


        parkingForzado =
            true;


        // Si iba simplemente buscando una piedra,
        // la dejamos libre.
        if (estadoActual ==
            EstadoBot.YendoAPiedra)
        {
            CancelarObjetivo();
        }


        estadoActual =
            EstadoBot.Esperando;


        temporizadorBusquedaEnEspera =
            intervaloBusquedaEnEspera;


        IntentarReservarPuntoEspera();


        ResetearAntiAtasco();
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
    // GIZMOS DEL BOT
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        // Radio preferente de piedras.
        Gizmos.color =
            Color.cyan;


        Gizmos.DrawWireSphere(
            transform.position,
            radioBusqueda
        );


        // Punto de entrega actual.
        if (puntoLlegadaReservado != null)
        {
            Gizmos.color =
                Color.green;


            Gizmos.DrawWireSphere(
                puntoLlegadaReservado.position,
                0.5f
            );


            Gizmos.DrawLine(
                transform.position,
                puntoLlegadaReservado.position
            );
        }


        // Plaza del parking actual.
        if (puntoEsperaReservado != null)
        {
            Gizmos.color =
                Color.magenta;


            Gizmos.DrawWireSphere(
                puntoEsperaReservado.position,
                0.55f
            );


            Gizmos.DrawLine(
                transform.position,
                puntoEsperaReservado.position
            );
        }


        // Centro del agujero.
        if (puntoEntradaAgujero != null)
        {
            Gizmos.color =
                Color.yellow;


            Gizmos.DrawWireSphere(
                puntoEntradaAgujero.position,
                0.35f
            );
        }
    }

    private bool ColocarEnParkingInicial()
    {
        if (gestorPuntosEspera == null)
        {
            Debug.LogWarning(
                name +
                ": no existe GestorPuntosEsperaBots."
            );

            return false;
        }


        // =================================================
        // PEDIMOS UNA PLAZA EXCLUSIVA
        // =================================================

        puntoEsperaReservado =
            gestorPuntosEspera.ReservarPunto(
                this
            );


        if (puntoEsperaReservado == null)
        {
            Debug.LogWarning(
                name +
                ": no hay plazas libres en el parking."
            );

            return false;
        }


        // =================================================
        // LOCALIZAR NAVMESH
        // =================================================

        if (!NavMesh.SamplePosition(
                puntoEsperaReservado.position,
                out NavMeshHit hit,
                1f,
                NavMesh.AllAreas))
        {
            Debug.LogWarning(
                name +
                ": la plaza " +
                puntoEsperaReservado.name +
                " no está correctamente sobre NavMesh."
            );


            LiberarPuntoEspera();


            return false;
        }


        // =================================================
        // TELETRANSPORTAR BOT
        // =================================================

        if (agente != null &&
            agente.isOnNavMesh)
        {
            agente.ResetPath();


            agente.Warp(
                hit.position
            );
        }
        else
        {
            transform.position =
                hit.position;
        }


        // La orientación de la plaza controla
        // hacia dónde mira el Bot.
        transform.rotation =
            puntoEsperaReservado.rotation;


        // =================================================
        // COMENZAR APARCADO
        // =================================================

        estadoActual =
            EstadoBot.Esperando;


        temporizadorBusquedaEnEspera =
            intervaloBusquedaEnEspera;


        ResetearAntiAtasco();


        Debug.Log(
            name +
            " comienza en parking: " +
            puntoEsperaReservado.name
        );


        return true;
    }

    // =====================================================
    // INTERACCIÓN CON JUGADOR
    // =====================================================

    public void IniciarInteraccion(Transform jugador)
    {
        enInteraccion = true;

        jugadorInteraccion = jugador;


        // Paramos el NavMeshAgent SIN borrar la ruta.
        if (agente != null &&
            agente.isOnNavMesh)
        {
            agente.isStopped = true;
        }


        // Si está llevando una piedra,
        // permanece pegada a sus manos.
        MantenerPiedraEnAgarre();
    }


    public void FinalizarInteraccion()
    {
        enInteraccion = false;

        jugadorInteraccion = null;


        if (agente != null &&
            agente.isOnNavMesh)
        {
            agente.isStopped = false;
        }


        // Recuperamos su tarea actual.
        ReanudarDespuesDeInteraccion();
    }


    private void MantenerBotDuranteInteraccion()
    {
        // Si llevaba una piedra, no dejamos que se mueva
        // ni se caiga mientras hablamos con el Bot.
        MantenerPiedraEnAgarre();


        // Girar hacia el jugador.
        if (jugadorInteraccion == null)
            return;


        Vector3 direccion =
            jugadorInteraccion.position -
            transform.position;


        // Solo rotación horizontal.
        direccion.y = 0f;


        if (direccion.sqrMagnitude < 0.001f)
            return;


        Quaternion rotacionObjetivo =
            Quaternion.LookRotation(
                direccion.normalized
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                Time.deltaTime *
                velocidadGiroInteraccion
            );
    }


    private void MantenerPiedraEnAgarre()
    {
        if (piedraObjetivo == null ||
            puntoAgarre == null)
        {
            return;
        }


        if (estadoActual != EstadoBot.LlevandoPiedra)
            return;


        piedraObjetivo.transform.position =
            puntoAgarre.position;


        piedraObjetivo.transform.rotation =
            puntoAgarre.rotation;
    }


    private void ReanudarDespuesDeInteraccion()
    {
        // Si iba hacia una piedra, recalculamos.
        if (estadoActual == EstadoBot.YendoAPiedra)
        {
            RecalcularRutaActual();
            return;
        }


        // Si llevaba una piedra, vuelve hacia
        // su plaza de entrega.
        if (estadoActual == EstadoBot.LlevandoPiedra)
        {
            RecalcularRutaActual();
            return;
        }


        // Si estaba esperando, seguirá gestionando
        // normalmente su plaza de parking.
        if (estadoActual == EstadoBot.Esperando)
        {
            return;
        }
    }
}