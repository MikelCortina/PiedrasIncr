using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EsbirroIA : MonoBehaviour
{
    // Hemos eliminado el estado "Llevando"
    public enum EstadoIA { Buscando, PersiguiendoYPateando, Patrullando }

    [Header("Estado Actual")]
    public EstadoIA estado = EstadoIA.Buscando;

    [Header("Referencias")]
    private AgujeroSimple agujeroDestino;

    [Header("Ajustes de Trabajo")]
    public float distanciaParaPatear = 1.5f;
    public float fuerzaPatada = 8f;
    public float tiempoEntrePatadas = 1f;
    public float elevacionPatada = 0.1f;

    [Header("Inteligencia Espacial")]
    public float distanciaSeguridadHoyo = 6f;

    [Header("Ajustes de Patrulla")]
    public float radioPatrulla = 10f;
    public float tiempoEsperaPatrulla = 2f;

    private NavMeshAgent agente;
    private Transform piedraObjetivo;
    private DeformacionPiedra scriptPiedraObjetivo;
    private Rigidbody rbPiedraObjetivo;

    private float temporizadorPatada = 0f;
    private float temporizadorEsperaPatrulla = 0f;
    private float temporizadorBusqueda = 0f;

    private Vector3 direccionPatadaActual;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        agujeroDestino = FindObjectOfType<AgujeroSimple>();
    }

    void Update()
    {
        switch (estado)
        {
            case EstadoIA.Buscando:
                BuscarPiedraAdecuada();
                break;

            case EstadoIA.PersiguiendoYPateando:
                ComportamientoPatear();
                break;

            case EstadoIA.Patrullando:
                ComportamientoPatrullar();
                break;
        }
    }

    void BuscarPiedraAdecuada()
    {
        GameObject[] todasLasPiedras = GameObject.FindGameObjectsWithTag("Piedra");
        float distanciaMinima = Mathf.Infinity;
        Transform mejorPiedra = null;

        NavMeshPath camino = new NavMeshPath();

        foreach (GameObject p in todasLasPiedras)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist < distanciaMinima)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(p.transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    agente.CalculatePath(hit.position, camino);

                    if (camino.status == NavMeshPathStatus.PathComplete)
                    {
                        distanciaMinima = dist;
                        mejorPiedra = p.transform;
                    }
                }
            }
        }

        if (mejorPiedra != null)
        {
            piedraObjetivo = mejorPiedra;
            scriptPiedraObjetivo = piedraObjetivo.GetComponent<DeformacionPiedra>();
            rbPiedraObjetivo = piedraObjetivo.GetComponent<Rigidbody>();

            ElegirNuevaDireccionPatada();
            estado = EstadoIA.PersiguiendoYPateando;
        }
        else
        {
            estado = EstadoIA.Patrullando;
            AsignarNuevoPuntoPatrulla();
        }
    }

    // --- EL CEREBRO DE GOLFISTA ---
    void ElegirNuevaDireccionPatada()
    {
        if (agujeroDestino != null && piedraObjetivo != null && scriptPiedraObjetivo != null)
        {
            Vector3 vectorHaciaHoyo = agujeroDestino.transform.position - piedraObjetivo.position;
            vectorHaciaHoyo.y = 0;

            float pureza = scriptPiedraObjetivo.ObtenerPorcentajeDesgasteHaciaEsfera();

            // Si ya está perfecta, apuntamos DIRECTAMENTE al agujero
            if (pureza >= 95f)
            {
                direccionPatadaActual = vectorHaciaHoyo.normalized;
            }
            else
            {
                // Si le falta desgaste, seguimos alejándola o jugando con ella
                float distanciaAlHoyo = vectorHaciaHoyo.magnitude;

                if (distanciaAlHoyo < distanciaSeguridadHoyo)
                {
                    Vector3 direccionAlejar = -vectorHaciaHoyo.normalized;
                    direccionPatadaActual = Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * direccionAlejar;
                }
                else
                {
                    Vector3 direccionCentro = vectorHaciaHoyo.normalized;
                    float anguloDesvio = Random.Range(50f, 90f);
                    if (Random.value > 0.5f) anguloDesvio = -anguloDesvio;
                    direccionPatadaActual = Quaternion.Euler(0, anguloDesvio, 0) * direccionCentro;
                }
            }
        }
    }

    void ComportamientoPatrullar()
    {
        temporizadorBusqueda -= Time.deltaTime;
        if (temporizadorBusqueda <= 0f)
        {
            temporizadorBusqueda = 1f;
            BuscarPiedraAdecuada();
            if (estado != EstadoIA.Patrullando) return;
        }

        if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance)
        {
            temporizadorEsperaPatrulla -= Time.deltaTime;
            if (temporizadorEsperaPatrulla <= 0f)
            {
                AsignarNuevoPuntoPatrulla();
            }
        }
    }

    void AsignarNuevoPuntoPatrulla()
    {
        Vector3 puntoAleatorio = transform.position + Random.insideUnitSphere * radioPatrulla;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, radioPatrulla, NavMesh.AllAreas))
        {
            agente.SetDestination(hit.position);
        }
        temporizadorEsperaPatrulla = tiempoEsperaPatrulla;
    }

    void ComportamientoPatear()
    {
        if (piedraObjetivo == null)
        {
            estado = EstadoIA.Buscando;
            return;
        }

        NavMeshHit hitPiedra;
        bool piedraEnNavMesh = NavMesh.SamplePosition(piedraObjetivo.position, out hitPiedra, 2.0f, NavMesh.AllAreas);

        // Si alguien vende la piedra, cae al vacío, o desaparece de la zona jugable
        if (!piedraEnNavMesh)
        {
            piedraObjetivo = null;
            estado = EstadoIA.Buscando;
            return;
        }

        if (!agente.pathPending && agente.pathStatus != NavMeshPathStatus.PathComplete && agente.remainingDistance > 2f)
        {
            piedraObjetivo = null;
            estado = EstadoIA.Buscando;
            return;
        }

        // --- ADAPTACIÓN INSTANTÁNEA ---
        // Si mientras la piedra rodaba acaba de alcanzar el 95%, cambiamos la estrategia de tiro al momento
        float pureza = scriptPiedraObjetivo.ObtenerPorcentajeDesgasteHaciaEsfera();
        if (pureza >= 95f)
        {
            Vector3 vectorHaciaHoyo = agujeroDestino.transform.position - piedraObjetivo.position;
            vectorHaciaHoyo.y = 0;
            direccionPatadaActual = vectorHaciaHoyo.normalized;
        }

        Vector3 posicionIdeal = hitPiedra.position - (direccionPatadaActual * distanciaParaPatear);

        NavMeshHit hitPosicionIdeal;
        if (NavMesh.SamplePosition(posicionIdeal, out hitPosicionIdeal, 1.5f, NavMesh.AllAreas))
        {
            agente.SetDestination(hitPosicionIdeal.position);
        }
        else
        {
            ElegirNuevaDireccionPatada();
            agente.SetDestination(hitPiedra.position);
        }

        temporizadorPatada -= Time.deltaTime;

        Vector3 miDireccionHaciaPiedra = (piedraObjetivo.position - transform.position).normalized;
        float anguloPosicionamiento = Vector3.Angle(miDireccionHaciaPiedra, direccionPatadaActual);
        float distanciaAPiedra = Vector3.Distance(transform.position, piedraObjetivo.position);

        if (distanciaAPiedra <= distanciaParaPatear + 0.8f && temporizadorPatada <= 0f)
        {
            if (anguloPosicionamiento < 45f || agente.velocity.sqrMagnitude < 0.1f)
            {
                transform.LookAt(new Vector3(piedraObjetivo.position.x, transform.position.y, piedraObjetivo.position.z));
                if (scriptPiedraObjetivo != null) scriptPiedraObjetivo.Despertar();

                Vector3 fuerzaPatadaVector = direccionPatadaActual;
                fuerzaPatadaVector.y = elevacionPatada;

                fuerzaPatadaVector = fuerzaPatadaVector.normalized;

                rbPiedraObjetivo.AddForce(fuerzaPatadaVector * fuerzaPatada, ForceMode.Impulse);
                rbPiedraObjetivo.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

                temporizadorPatada = tiempoEntrePatadas;
                ElegirNuevaDireccionPatada();
            }
        }
    }
}