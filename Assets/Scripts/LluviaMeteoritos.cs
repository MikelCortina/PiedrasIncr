using System.Collections;
using UnityEngine;

public class LluviaMeteoritos : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject prefabPiedra;

    [Tooltip("Si asignas el Cintur�n de Asteroides, los meteoritos orbitar�n en �l hasta estar en la zona de ca�da.")]
    public CinturonAsteroides cinturonOrigen;

    [Header("Inicio de partida")]
    public bool generarMeteoritoInicial = true;

    [Header("Ajustes de Altura y Trayectoria")]
    public float alturaCielo = 100f;
    public float alturaArco = 30f;

    [Header("Ajustes de Aparici�n")]
    public string tagSuelo = "Floor";

    [Header("Cadencia")]
    public float tiempoEntreSpawns = 2f;
    [Min(1)] public int cantidadPorOleada = 1;
    public float tiempoEntreMeteoritosOleada = 0.15f;

    [Header("Aceleraci�n por Curva")]
    public AnimationCurve curvaAceleracion = AnimationCurve.Linear(0f, 1f, 1f, 3f);
    public float velocidadMaximaImpacto = 50f;

    [Header("Seguridad de Colisi�n")]
    public LayerMask capaColisionSuelo;

    [Header("Estado")]
    [SerializeField] private bool sistemaActivo = true;

    private Coroutine rutinaLluvia;
    void Start()
    {
        // Siempre lanzamos un �nico meteorito inicial
        // para poder arrancar la econom�a.
        if (generarMeteoritoInicial)
        {
            SpawnearMeteorito();
        }

        // La lluvia continua solo empieza si est� activa.
        if (sistemaActivo)
        {
            IniciarRutinaLluvia();
        }
    }

    private void IniciarRutinaLluvia()
    {
        if (rutinaLluvia == null) rutinaLluvia = StartCoroutine(RutinaLluviaMeteoritos());
    }

    IEnumerator RutinaLluviaMeteoritos()
    {
        while (sistemaActivo)
        {
            yield return new WaitForSeconds(tiempoEntreSpawns);

            for (int i = 0; i < cantidadPorOleada; i++)
            {
                if (!sistemaActivo) break;

                SpawnearMeteorito();

                if (i < cantidadPorOleada - 1)
                {
                    yield return new WaitForSeconds(tiempoEntreMeteoritosOleada);
                }
            }
        }
        rutinaLluvia = null;
    }

    void SpawnearMeteorito()
    {
        if (prefabPiedra == null) return;

        GameObject[] suelos = GameObject.FindGameObjectsWithTag(tagSuelo);
        if (suelos.Length == 0) return;

        GameObject sueloElegido = suelos[Random.Range(0, suelos.Length)];
        Collider colSuelo = sueloElegido.GetComponent<Collider>();
        if (colSuelo == null) return;

        Bounds limites = colSuelo.bounds;
        float objetivoX = Random.Range(limites.min.x, limites.max.x);
        float objetivoZ = Random.Range(limites.min.z, limites.max.z);
        float alturaImpactoReal = colSuelo.bounds.max.y;

        Vector3 puntoImpacto = new Vector3(objetivoX, alturaImpactoReal, objetivoZ);

        if (cinturonOrigen != null)
        {
            // Pedimos las matem�ticas de un asteroide orbital
            CinturonAsteroides.DatosSpawnMeteorito datosOrbita = cinturonOrigen.ObtenerDatosSpawnMeteorito();
            Vector3 posicionInicial = cinturonOrigen.ObtenerPosicionEnCinturon(datosOrbita.anguloInicial, datosOrbita.distanciaAlCentro, datosOrbita.alturaY);

            GameObject meteorito = InstanciarYPrepararMeteorito(posicionInicial, posicionInicial.y, alturaImpactoReal);
            Rigidbody rb = meteorito.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.useGravity = false;
                // Iniciamos la rutina de espera en �rbita
                StartCoroutine(RutinaOrbitaPrevia(rb, datosOrbita, puntoImpacto, meteorito));
            }
        }
        else
        {
            Vector3 posicionSpawn = new Vector3(objetivoX, alturaCielo, objetivoZ);
            GameObject meteorito = InstanciarYPrepararMeteorito(posicionSpawn, alturaCielo, alturaImpactoReal);
            Rigidbody rb = meteorito.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.useGravity = false;
                StartCoroutine(RutinaArcoMeteorito(rb, posicionSpawn, puntoImpacto, meteorito));
                rb.AddTorque(Random.insideUnitSphere * 40f, ForceMode.Impulse);
            }
        }
    }

    GameObject InstanciarYPrepararMeteorito(Vector3 posSpawn, float alturaCieloVisual, float alturaSueloVisual)
    {
        GameObject meteorito = Instantiate(prefabPiedra, posSpawn, Random.rotation);

        MeteoritoVisual visualMeteorito = meteorito.GetComponent<MeteoritoVisual>();
        if (visualMeteorito != null) visualMeteorito.ConfigurarAlturas(alturaCieloVisual, alturaSueloVisual);

        GeneradorPiedra generador = meteorito.GetComponent<GeneradorPiedra>();
        if (generador != null) generador.Generar();

        return meteorito;
    }

    // --- NUEVO: Rutina para quedarse orbitando hasta entrar en la Zona de Ca�da ---
    IEnumerator RutinaOrbitaPrevia(Rigidbody rb, CinturonAsteroides.DatosSpawnMeteorito datos, Vector3 puntoImpacto, GameObject meteorito)
    {
        float anguloActual = datos.anguloInicial;

        // Mientras siga vivo y NO est� dentro de la zona roja del cintur�n, seguimos orbitando
        while (rb != null && !rb.isKinematic && !cinturonOrigen.EstaEnZonaDeCaida(anguloActual))
        {
            anguloActual += datos.velocidadOrbita * Time.fixedDeltaTime;

            Vector3 nuevaPosicion = cinturonOrigen.ObtenerPosicionEnCinturon(anguloActual, datos.distanciaAlCentro, datos.alturaY);
            Vector3 direccionOrbita = (nuevaPosicion - rb.position).normalized;

            rb.MovePosition(nuevaPosicion);
            rb.linearVelocity = direccionOrbita * Mathf.Abs(datos.velocidadOrbita);

            yield return new WaitForFixedUpdate();
        }

        // �Ha entrado en la zona de ca�da! Empezamos el arco de picado.
        if (rb != null && !rb.isKinematic)
        {
            Vector3 posicionCaida = rb.position;
            StartCoroutine(RutinaArcoMeteorito(rb, posicionCaida, puntoImpacto, meteorito));
            rb.AddTorque(Random.insideUnitSphere * 40f, ForceMode.Impulse);
        }
    }

    IEnumerator RutinaArcoMeteorito(Rigidbody rb, Vector3 posInicial, Vector3 posFinal, GameObject meteorito)
    {
        float distanciaTotal = Vector3.Distance(posInicial, posFinal);
        Vector3 puntoControl = posInicial + (posFinal - posInicial) / 2f + (Vector3.up * alturaArco);
        float t = 0f;

        // --- SOLUCI�N 2: Obtenemos el volumen real del meteorito ---
        Collider col = rb.GetComponent<Collider>();
        float radioPiedra = col != null ? col.bounds.extents.y : 1f;

        while (rb != null && !rb.isKinematic && t < 1f)
        {
            float multiplicadorCurva = curvaAceleracion.Evaluate(t);
            float velocidadFotograma = velocidadMaximaImpacto * multiplicadorCurva;

            t += (velocidadFotograma / distanciaTotal) * Time.fixedDeltaTime;

            bool impactoInminente = false;
            if (t >= 1f)
            {
                t = 1f;
                impactoInminente = true;
            }

            float u = 1f - t;
            Vector3 siguientePosicion = (u * u * posInicial) + (2f * u * t * puntoControl) + (t * t * posFinal);

            Vector3 direccionMovimiento = (siguientePosicion - rb.position).normalized;
            float distanciaAlSiguientePunto = Vector3.Distance(rb.position, siguientePosicion);

            RaycastHit hit;

            // Usamos un SphereCast simulando el volumen del meteorito en vez de un l�ser delgado
            if (Physics.SphereCast(rb.position, radioPiedra, direccionMovimiento, out hit, distanciaAlSiguientePunto + 0.2f))
            {
                if (hit.collider.CompareTag(tagSuelo))
                {
                    // Frenamos el centro exactamente donde la superficie de la piedra toc� el suelo
                    Vector3 puntoFrenado = rb.position + (direccionMovimiento * hit.distance);
                    EjecutarImpactoSuelo(rb, puntoFrenado, posInicial.y, meteorito);
                    yield break;
                }
            }

            rb.MovePosition(siguientePosicion);
            rb.linearVelocity = direccionMovimiento * velocidadFotograma;

            if (impactoInminente)
            {
                EjecutarImpactoSuelo(rb, posFinal, posInicial.y, meteorito);
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    void EjecutarImpactoSuelo(Rigidbody rb, Vector3 puntoImpacto, float yCielo, GameObject meteorito)
    {
        rb.MovePosition(puntoImpacto);
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;

        MeteoritoVisual visual = meteorito.GetComponent<MeteoritoVisual>();
        if (visual != null) visual.ConfigurarAlturas(yCielo, puntoImpacto.y);
    }

    public void ConfigurarLluvia(float nuevoTiempoEntreSpawns, int nuevaCantidadPorOleada)
    {
        tiempoEntreSpawns = Mathf.Max(0.1f, nuevoTiempoEntreSpawns);
        cantidadPorOleada = Mathf.Max(1, nuevaCantidadPorOleada);
    }

    public void ActivarLluvia(bool activar)
    {
        if (sistemaActivo == activar) return;
        sistemaActivo = activar;

        if (sistemaActivo) IniciarRutinaLluvia();
        else
        {
            if (rutinaLluvia != null)
            {
                StopCoroutine(rutinaLluvia);
                rutinaLluvia = null;
            }
        }
    }
}