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
        EntregandoPiedra
    }

    // =====================================================
    // ANTI-ATASCO
    // =====================================================

    [Header("Anti-Atasco")]

    [Tooltip("Cada cuánto comprobamos si el Bot realmente se está moviendo.")]
    public float intervaloComprobacionAtasco = 0.75f;

    [Tooltip("Distancia mínima que debe avanzar entre comprobaciones.")]
    public float distanciaMinimaAvance = 0.15f;

    [Tooltip("Tiempo que puede permanecer prácticamente quieto antes de intervenir.")]
    public float tiempoMaximoAtascado = 2f;

    [Tooltip("Número de veces que intentará recalcular la ruta antes de tomar una medida mayor.")]
    public int maxReintentosRuta = 2;


    private Vector3 ultimaPosicionAntiAtasco;

    private float temporizadorAntiAtasco;

    private float tiempoAtascado;

    private int reintentosRuta;

    // =====================================================
    // ESTADO
    // =====================================================

    [Header("Estado")]
    [SerializeField]
    private EstadoBot estadoActual = EstadoBot.Buscando;


    // =====================================================
    // REFERENCIAS
    // =====================================================

    [Header("Referencias")]
    public Transform puntoAgarre;


    // =====================================================
    // ENTREGA AL AGUJERO
    // =====================================================

    [Header("Entrega al Agujero")]

    [Tooltip("Punto sobre el NavMesh donde se coloca el Bot para realizar la entrega.")]
    public Transform puntoLlegadaBot;

    [Tooltip("Centro exacto de la boca del agujero. La piedra termina aquí.")]
    public Transform puntoEntradaAgujero;

    [Tooltip("Agujero que procesa la piedra y genera las monedas.")]
    public AgujeroSimple agujeroDestino;

    [Tooltip("Distancia a PuntoLlegadaBot necesaria para comenzar el lanzamiento.")]
    public float distanciaEntrega = 1.5f;

    [Tooltip("Duración visual del lanzamiento.")]
    public float duracionLanzamiento = 0.5f;

    [Tooltip("Altura máxima del arco que hace la piedra.")]
    public float alturaArcoLanzamiento = 2f;

    [Tooltip("Velocidad de giro visual de la piedra durante el lanzamiento.")]
    public float velocidadGiroLanzamiento = 360f;


    // =====================================================
    // BÚSQUEDA
    // =====================================================

    [Header("Búsqueda de Piedras")]

    public float radioBusqueda = 30f;

    public LayerMask capaPiedras;

    public string tagPiedra = "Piedra";

    public float tiempoEntreBusquedas = 0.5f;


    // =====================================================
    // RECOGIDA
    // =====================================================

    [Header("Recogida")]

    public float distanciaRecogida = 1.5f;


    // =====================================================
    // VARIABLES INTERNAS
    // =====================================================

    private NavMeshAgent agente;

    private Rigidbody piedraObjetivo;

    private float temporizadorBusqueda = 0f;

    private AgarreLanzamiento agarreJugador;

    // Evita que dos Bots intenten coger la misma piedra.
    private static readonly HashSet<Rigidbody> piedrasReservadas =
        new HashSet<Rigidbody>();


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        estadoActual = EstadoBot.Buscando;

        ResetearAntiAtasco();
    }
    

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
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

                // Durante el lanzamiento controla todo la Coroutine.
                break;
        }

        ComprobarAntiAtasco();
    }


    // =====================================================
    // BUSCAR PIEDRA
    // =====================================================

    private void ComportamientoBuscar()
    {
        temporizadorBusqueda -= Time.deltaTime;

        if (temporizadorBusqueda > 0f)
            return;

        temporizadorBusqueda = tiempoEntreBusquedas;

        BuscarPiedra();
    }


    private void BuscarPiedra()
    {
        Collider[] encontrados =
            Physics.OverlapSphere(
                transform.position,
                radioBusqueda,
                capaPiedras
            );


        Rigidbody mejorPiedra = null;

        float mejorDistancia =
            Mathf.Infinity;


        foreach (Collider col in encontrados)
        {
            Rigidbody rb =
                col.attachedRigidbody;


            if (rb == null)
                continue;


            if (!rb.CompareTag(tagPiedra))
                continue;
            // Si el jugador está sujetando esta piedra,
            // el Bot ni siquiera la considera candidata.
            if (agarreJugador != null &&
                agarreJugador.EstaSosteniendoPiedra(rb))
            {
                continue;
            }


            // Ya está reservada por otro Bot.
            if (piedrasReservadas.Contains(rb))
                continue;


            // Está siendo transportada o agarrada.
            if (rb.transform.parent != null)
                continue;


            float distancia =
                Vector3.Distance(
                    transform.position,
                    rb.position
                );


            if (distancia >= mejorDistancia)
                continue;


            // Buscamos un punto válido del NavMesh
            // cerca de la piedra.
            if (!NavMesh.SamplePosition(
                    rb.position,
                    out NavMeshHit puntoNavMesh,
                    3f,
                    NavMesh.AllAreas))
            {
                continue;
            }


            // Comprobamos que exista un camino completo.
            NavMeshPath camino =
                new NavMeshPath();


            agente.CalculatePath(
                puntoNavMesh.position,
                camino
            );


            if (camino.status !=
                NavMeshPathStatus.PathComplete)
            {
                continue;
            }


            mejorDistancia =
                distancia;

            mejorPiedra =
                rb;
        }


        if (mejorPiedra == null)
            return;


        piedraObjetivo =
            mejorPiedra;


        piedrasReservadas.Add(
            piedraObjetivo
        );


        estadoActual =
            EstadoBot.YendoAPiedra;
        ResetearAntiAtasco();


        IrHaciaPiedra();
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

        // =================================================
        // EL JUGADOR NOS HA QUITADO LA PIEDRA
        // =================================================

        if (agarreJugador != null &&
            agarreJugador.EstaSosteniendoPiedra(
                piedraObjetivo))
        {
            // Liberamos esa piedra.
            // El Bot deja de seguirla inmediatamente.
            CancelarObjetivo();
            return;
        }


        IrHaciaPiedra();


        float distancia =
            Vector3.Distance(
                transform.position,
                piedraObjetivo.position
            );


        if (distancia <= distanciaRecogida)
        {
            RecogerPiedra();
        }
    }


    private void IrHaciaPiedra()
    {
        if (piedraObjetivo == null)
            return;


        if (NavMesh.SamplePosition(
                piedraObjetivo.position,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
        {
            agente.SetDestination(
                hit.position
            );
        }
    }


    // =====================================================
    // RECOGER PIEDRA
    // =====================================================

    private void RecogerPiedra()
    {
        if (piedraObjetivo == null ||
            puntoAgarre == null)
        {
            CancelarObjetivo();
            return;
        }


        agente.ResetPath();


        // IMPORTANTE:
        // quitamos velocidad antes de hacerla kinematic.
        piedraObjetivo.linearVelocity =
            Vector3.zero;

        piedraObjetivo.angularVelocity =
            Vector3.zero;


        // Mientras el Bot la transporta,
        // desactivamos la lógica física de la piedra.
        DeformacionPiedra deformacion =
            piedraObjetivo.GetComponent<DeformacionPiedra>();


        if (deformacion != null)
        {
            deformacion.enabled = false;
        }


        piedraObjetivo.isKinematic =
            true;


        // Evitamos colisiones con el Bot y el escenario
        // durante el transporte.
        Collider[] colliders =
            piedraObjetivo.GetComponentsInChildren<Collider>();


        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }


        // Pegamos la piedra al punto de agarre.
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
    // LLEVAR PIEDRA AL AGUJERO
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


        if (puntoLlegadaBot == null)
        {
            Debug.LogWarning(
                "BotRecolector: falta PuntoLlegadaBot."
            );

            return;
        }


        // Seguridad extra:
        // mantenemos la piedra exactamente en el agarre.
        piedraObjetivo.transform.position =
            puntoAgarre.position;

        piedraObjetivo.transform.rotation =
            puntoAgarre.rotation;


        // Buscamos el punto navegable cercano
        // a donde debe colocarse el Bot.
        if (NavMesh.SamplePosition(
                puntoLlegadaBot.position,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
        {
            agente.SetDestination(
                hit.position
            );
        }


        float distancia =
            Vector3.Distance(
                transform.position,
                puntoLlegadaBot.position
            );


        if (distancia <= distanciaEntrega)
        {
            estadoActual =
                EstadoBot.EntregandoPiedra;
            ResetearAntiAtasco();


            agente.ResetPath();


            StartCoroutine(
                LanzarPiedraAlAgujero()
            );
        }
    }


    // =====================================================
    // LANZAMIENTO GARANTIZADO AL AGUJERO
    // =====================================================

    private IEnumerator LanzarPiedraAlAgujero()
    {
        if (piedraObjetivo == null)
        {
            CancelarObjetivo();
            yield break;
        }


        if (puntoEntradaAgujero == null)
        {
            Debug.LogWarning(
                "BotRecolector: falta PuntoEntradaAgujero."
            );

            CancelarObjetivo();
            yield break;
        }


        if (agujeroDestino == null)
        {
            Debug.LogWarning(
                "BotRecolector: falta AgujeroDestino."
            );

            CancelarObjetivo();
            yield break;
        }


        Rigidbody piedraEntregada =
            piedraObjetivo;


        // El Bot deja de sujetarla.
        piedraEntregada.transform.SetParent(
            null
        );


        // Sigue siendo kinematic.
        // El vuelo es completamente controlado.
        piedraEntregada.isKinematic =
            true;


        Collider[] colliders =
            piedraEntregada.GetComponentsInChildren<Collider>();


        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }


        Vector3 posicionInicial =
            piedraEntregada.position;


        Vector3 posicionFinal =
            puntoEntradaAgujero.position;


        float tiempo =
            0f;


        // =============================================
        // ANIMACIÓN DEL LANZAMIENTO
        // =============================================

        while (tiempo < duracionLanzamiento)
        {
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


            // Movimiento desde el Bot hasta el agujero.
            Vector3 posicion =
                Vector3.Lerp(
                    posicionInicial,
                    posicionFinal,
                    t
                );


            // Arco:
            //
            // 0 al empezar
            // máximo en mitad
            // 0 al terminar
            float alturaArco =
                Mathf.Sin(
                    t * Mathf.PI
                )
                *
                alturaArcoLanzamiento;


            posicion.y +=
                alturaArco;


            piedraEntregada.position =
                posicion;


            // Giro únicamente visual.
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
            yield break;
        }


        // Garantizamos la posición final.
        piedraEntregada.position =
            posicionFinal;


        // Ya no está reservada.
        piedrasReservadas.Remove(
            piedraEntregada
        );


        // Quitamos nuestra referencia ANTES
        // de que AgujeroSimple destruya la piedra.
        piedraObjetivo =
            null;


        // =================================================
        // AQUÍ ESTÁ LA CLAVE:
        //
        // NO confiamos en la física.
        // Llamamos directamente al mismo método que utiliza
        // el agujero para procesar una piedra.
        //
        // Esto calcula pureza, recompensa y genera monedas.
        // =================================================

        agujeroDestino.RecibirPiedra(
            piedraEntregada.gameObject
        );


        // Volvemos al trabajo.
        estadoActual =
            EstadoBot.Buscando;


        temporizadorBusqueda =
            tiempoEntreBusquedas;
    }


    // =====================================================
    // CANCELAR OBJETIVO
    // =====================================================

    private void CancelarObjetivo()
    {
        if (piedraObjetivo != null)
        {
            piedrasReservadas.Remove(
                piedraObjetivo
            );
        }


        piedraObjetivo =
            null;


        if (agente != null &&
            agente.isOnNavMesh)
        {
            agente.ResetPath();
        }


        estadoActual =
            EstadoBot.Buscando;


        temporizadorBusqueda =
            tiempoEntreBusquedas;
    }


    // =====================================================
    // SEGURIDAD
    // =====================================================

    private void OnDestroy()
    {
        if (piedraObjetivo != null)
        {
            piedrasReservadas.Remove(
                piedraObjetivo
            );
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
        // Solo nos importa mientras el Bot debería
        // estar desplazándose.
        if (estadoActual != EstadoBot.YendoAPiedra &&
            estadoActual != EstadoBot.LlevandoPiedra)
        {
            temporizadorAntiAtasco = 0f;
            tiempoAtascado = 0f;
            ultimaPosicionAntiAtasco = transform.position;

            return;
        }


        if (agente == null ||
            !agente.isOnNavMesh)
        {
            return;
        }


        // Si Unity todavía está calculando la ruta,
        // no consideramos que esté atascado.
        if (agente.pathPending)
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


        // =================================================
        // HA AVANZADO CORRECTAMENTE
        // =================================================

        if (distanciaMovida >= distanciaMinimaAvance)
        {
            tiempoAtascado =
                0f;

            reintentosRuta =
                0;
        }
        else
        {
            // Solo contamos como atasco si todavía
            // debería tener recorrido por delante.
            bool deberiaMoverse =
                agente.hasPath &&
                agente.remainingDistance >
                agente.stoppingDistance + 0.2f;


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


        // =================================================
        // RUTA INVÁLIDA O DESACTUALIZADA
        // =================================================

        if (agente.isPathStale ||
            agente.pathStatus != NavMeshPathStatus.PathComplete)
        {
            ResolverAtasco();

            return;
        }


        // =================================================
        // DEMASIADO TIEMPO SIN MOVERSE
        // =================================================

        if (tiempoAtascado >= tiempoMaximoAtascado)
        {
            ResolverAtasco();
        }
    }


    private void ResolverAtasco()
    {
        tiempoAtascado =
            0f;

        reintentosRuta++;


        // =================================================
        // PRIMEROS INTENTOS:
        // simplemente recalculamos el camino.
        // =================================================

        if (reintentosRuta <= maxReintentosRuta)
        {
            Debug.Log(
                "Bot: recalculando ruta. Intento "
                + reintentosRuta
            );


            RecalcularRutaActual();

            return;
        }


        // =================================================
        // SIGUE ATASCADO DESPUÉS DE VARIOS INTENTOS
        // =================================================

        if (estadoActual == EstadoBot.YendoAPiedra)
        {
            Debug.Log(
                "Bot: no puede llegar a la piedra. Busca otra."
            );


            CancelarObjetivo();

            ResetearAntiAtasco();

            return;
        }


        if (estadoActual == EstadoBot.LlevandoPiedra)
        {
            Debug.Log(
                "Bot: atascado transportando. Intentando recolocarse."
            );


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


        agente.ResetPath();


        // =================================================
        // VAMOS A POR UNA PIEDRA
        // =================================================

        if (estadoActual == EstadoBot.YendoAPiedra)
        {
            if (piedraObjetivo == null)
            {
                CancelarObjetivo();
                return;
            }


            if (NavMesh.SamplePosition(
                    piedraObjetivo.position,
                    out NavMeshHit hitPiedra,
                    3f,
                    NavMesh.AllAreas))
            {
                agente.SetDestination(
                    hitPiedra.position
                );
            }

            return;
        }


        // =================================================
        // LLEVAMOS UNA PIEDRA AL AGUJERO
        // =================================================

        if (estadoActual == EstadoBot.LlevandoPiedra)
        {
            if (puntoLlegadaBot == null)
                return;


            if (NavMesh.SamplePosition(
                    puntoLlegadaBot.position,
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


        // Buscamos una posición válida muy cercana al Bot.
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
    // DEBUG
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.cyan;


        Gizmos.DrawWireSphere(
            transform.position,
            radioBusqueda
        );


        // Marcamos el punto donde espera el Bot.
        if (puntoLlegadaBot != null)
        {
            Gizmos.color =
                Color.green;

            Gizmos.DrawWireSphere(
                puntoLlegadaBot.position,
                0.4f
            );
        }


        // Marcamos el objetivo exacto del lanzamiento.
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
}