using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TramoTuberia
{
    [Tooltip("El controlador de la tubería por la que pasará la piedra.")]
    public ControladorDeformacionTuberia tuberia;

    [Tooltip("Velocidad a la que la panza viaja por ESTA tubería en concreto.")]
    public float velocidadDeformacion = 2f;

    [Tooltip("Desde dónde empieza (Panner) en ESTA tubería.")]
    public float pannerInicio = -0.5f;

    [Tooltip("Hasta dónde llega (Panner) en ESTA tubería.")]
    public float pannerFin = 1.5f;
}

public class AgujeroSimple : MonoBehaviour
{
    [Header("Conexión con la Pantalla")]
    public ContadorGasolinera contadorFuelo;

    [Header("Monedas")]
    [Tooltip("Al tragar una piedra da monedas según su pureza (tramos de abajo). Salen por la ranura del PayZone.")]
    public bool darMonedasPorTramos = true;

    public static event System.Action<AgujeroSimple, float, float> OnPiedraTragada;

    [Header("Recompensas (Tramos de Pureza)")]
    public GameObject prefabDinero;
    [Tooltip("Salida antigua, encima del agujero. Solo se usa si no se encuentra el PayZone.")]
    public Transform puntoDeExpulsion;

    [Header("Salida por la ranura del PayZone")]
    [Tooltip("Punto por el que salen las monedas: su eje Z (azul) es hacia dónde salen. " +
             "Vacío = se crea solo en la ranura del objeto llamado como 'nombrePayzone'.")]
    public Transform ranuraPayzone;
    public string nombrePayzone = "Payzone";
    [Tooltip("Posición de la ranura dentro de la caja del modelo Payzone.fbx (0..1 en X, Y, Z locales). " +
             "Medida en el modelo: centrada, en la cara -Y (la del cartel PAY ZONE) y al 72% de la altura.")]
    public Vector3 fraccionRanura = new Vector3(0.5f, 0.061f, 0.72f);
    [Tooltip("Distancia delante de la ranura a la que aparecen las monedas (para no chocar con el PayZone).")]
    public float separacionRanura = 0.12f;
    [Tooltip("Velocidad hacia delante con la que salen por la ranura (m/s).")]
    public float velocidadSalidaRanura = 3f;
    [Tooltip("Velocidad hacia arriba al salir: hace una pequeña parábola (m/s).")]
    public float elevacionSalidaRanura = 1.2f;
    [Tooltip("Desvío lateral aleatorio al salir (m/s).")]
    public float dispersionRanura = 0.5f;

    [Header("Game Feel del PayZone (Efecto Bloop al escupir)")]
    [Tooltip("Modelo del PayZone que se infla al escupir cada moneda. Vacío = el objeto 'nombrePayzone'.")]
    public Transform modeloPayzone;
    [Tooltip("Cuánto se ensancha el PayZone en cada moneda (0.15 = un 15%).")]
    public float fuerzaBloopPayzone = 0.15f;
    public float velocidadRecuperacionBloopPayzone = 15f;

    [Space(10)]
    public float umbralPerfecto = 95f;
    public int premioPerfecto = 15;
    public float umbralBueno = 70f;
    public int premioBueno = 5;
    public float umbralAceptable = 40f;
    public int premioAceptable = 2;
    public int premioMalo = 0;

    [Header("Expulsión (Hacia Arriba)")]
    public float tiempoEntreMonedas = 0.05f;
    public float fuerzaHaciaArriba = 8f;
    public float dispersionLateral = 1.5f;
    public float rotacionMax = 20f;
    public float multiplicadorRebote = 1.2f;

    [Header("Flujo de Tuberías (Deformación)")]
    public List<TramoTuberia> recorridoTuberias = new List<TramoTuberia>();

    [Header("Efectos Visuales y Sonido")]
    public ParticleSystem particulasTragar;
    public ParticleSystem particulasEscupir;
    public AudioSource audioSource;
    public AudioClip sonidoTragar;
    public AudioClip sonidoEscupir;

    [Header("Game Feel (Efecto Bloop X/Z)")]
    public Transform modeloVisual;
    public float fuerzaBloop = 0.3f;
    public float velocidadRecuperacionBloop = 15f;

    private Vector3 escalaOriginal;
    private float impulsoBloopActual = 0f;

    private bool[] tuberiaOcupada;
    private bool ranuraBuscada;
    private bool payzoneBuscado;
    private Vector3 escalaOriginalPayzone;
    private float impulsoBloopPayzone;

    void Start()
    {
        if (contadorFuelo == null) contadorFuelo = FindAnyObjectByType<ContadorGasolinera>();
        if (modeloVisual != null) escalaOriginal = modeloVisual.localScale;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (particulasEscupir != null)
        {
            var emision = particulasEscupir.emission;
            emision.rateOverTime = 0f;
            particulasEscupir.Stop();
        }

        if (recorridoTuberias != null)
        {
            tuberiaOcupada = new bool[recorridoTuberias.Count];
        }
    }

    void Update()
    {
        if (modeloVisual != null)
        {
            impulsoBloopActual = Mathf.Lerp(impulsoBloopActual, 0f, Time.deltaTime * velocidadRecuperacionBloop);

            modeloVisual.localScale = new Vector3(
                escalaOriginal.x * (1f + impulsoBloopActual),
                escalaOriginal.y,
                escalaOriginal.z * (1f + impulsoBloopActual)
            );
        }

        // Bloop del PayZone: se ensancha en horizontal (X/Y locales; su "arriba" es Z, modelo de Blender).
        if (modeloPayzone != null && payzoneBuscado)
        {
            impulsoBloopPayzone = Mathf.Lerp(impulsoBloopPayzone, 0f, Time.deltaTime * velocidadRecuperacionBloopPayzone);

            modeloPayzone.localScale = new Vector3(
                escalaOriginalPayzone.x * (1f + impulsoBloopPayzone),
                escalaOriginalPayzone.y * (1f + impulsoBloopPayzone),
                escalaOriginalPayzone.z
            );
        }
    }

    public void RecibirPiedra(GameObject piedra)
    {
        if (piedra == null) return;

        DeformacionPiedra scriptPiedra = piedra.GetComponentInParent<DeformacionPiedra>();
        int cantidadMonedas = 0;
        float pureza = 0f;

        bool esPiedraAceptable = false;
        float incrementoCombustible = 0f;

        if (scriptPiedra != null)
        {
            pureza = scriptPiedra.ObtenerPorcentajeDesgasteHaciaEsfera();
            if (darMonedasPorTramos) cantidadMonedas = CalcularRecompensa(pureza);

            incrementoCombustible = ProveedorEconomia.Actual.GasolinaPorPiedra(pureza / 100f);
            esPiedraAceptable = incrementoCombustible > 0f;
        }

        if (OnPiedraTragada != null) OnPiedraTragada(this, pureza / 100f, incrementoCombustible);

        Destroy(piedra);

        if (particulasTragar != null) particulasTragar.Play();
        if (audioSource != null && sonidoTragar != null) audioSource.PlayOneShot(sonidoTragar);
        HacerBloop();

        if (esPiedraAceptable)
        {
            if (recorridoTuberias != null && recorridoTuberias.Count > 0)
            {
                // Pasamos la cantidad de fuel a la rutina de las tuberías para que la sume al final
                StartCoroutine(RutinaViajePorTuberias(incrementoCombustible));
            }
            else
            {
                // Si por algún motivo no hay tuberías configuradas, sumamos el fuel directamente
                if (contadorFuelo != null) contadorFuelo.AgregarFraccion(incrementoCombustible);
            }
        }

        if (cantidadMonedas > 0)
        {
            StartCoroutine(EscupirMonedas(cantidadMonedas, pureza));
        }
    }

    // --- NUEVO: La rutina ahora recibe la cantidad de combustible a sumar ---
    private IEnumerator RutinaViajePorTuberias(float fuelASumar)
    {
        for (int i = 0; i < recorridoTuberias.Count; i++)
        {
            TramoTuberia paso = recorridoTuberias[i];
            if (paso == null || paso.tuberia == null) continue;

            while (tuberiaOcupada[i])
            {
                yield return null;
            }

            tuberiaOcupada[i] = true;

            float progresoActual = paso.pannerInicio;

            while (progresoActual != paso.pannerFin)
            {
                progresoActual = Mathf.MoveTowards(progresoActual, paso.pannerFin, Time.deltaTime * Mathf.Abs(paso.velocidadDeformacion));
                paso.tuberia.SetPannerGlobal(progresoActual);
                yield return null;
            }

            paso.tuberia.SetPannerGlobal(paso.pannerInicio);
            tuberiaOcupada[i] = false;
        }

        // --- NUEVO: Una vez que el bucle 'for' ha terminado (todas las tuberías han hecho su animación), sumamos el fuel ---
        if (contadorFuelo != null && fuelASumar > 0f)
        {
            contadorFuelo.AgregarFraccion(fuelASumar);
        }
    }

    private int CalcularRecompensa(float pureza)
    {
        if (pureza >= umbralPerfecto) return premioPerfecto;
        else if (pureza >= umbralBueno) return premioBueno;
        else if (pureza >= umbralAceptable) return premioAceptable;
        else return premioMalo;
    }

    private IEnumerator EscupirMonedas(int cantidad, float pureza)
    {
        float emisionExtra = 0f;

        if (pureza >= umbralPerfecto) emisionExtra = 0f;
        else if (pureza >= umbralBueno) emisionExtra = 15f;
        else emisionExtra = 30f;

        if (particulasEscupir != null)
        {
            var emision = particulasEscupir.emission;
            emision.rateOverTime = emisionExtra;
            if (!particulasEscupir.isPlaying) particulasEscupir.Play();
        }

        Transform ranura = RanuraPayzone();

        for (int i = 0; i < cantidad; i++)
        {
            if (prefabDinero != null && ranura != null)
            {
                // Sale por la ranura del PayZone: el agujero ya no hace "bloop".
                GameObject nuevaMoneda = Instantiate(prefabDinero, ranura.position, Random.rotation);
                SacarPorRanura(nuevaMoneda.GetComponent<Rigidbody>(), ranura);
            }
            else if (prefabDinero != null && puntoDeExpulsion != null)
            {
                GameObject nuevaMoneda = Instantiate(prefabDinero, puntoDeExpulsion.position, Random.rotation);
                AplicarFuerzaMoneda(nuevaMoneda.GetComponent<Rigidbody>(), 1f);

                ReproducirSonidoEscupir();
                HacerBloop();
            }

            yield return new WaitForSeconds(tiempoEntreMonedas);
        }

        if (particulasEscupir != null)
        {
            var emision = particulasEscupir.emission;
            emision.rateOverTime = 0f;
            particulasEscupir.Stop();
        }
    }

    public void RebotarMoneda(GameObject moneda)
    {
        Rigidbody rb = moneda.GetComponent<Rigidbody>();

        // Una moneda que cae al agujero vuelve a salir por la ranura del PayZone.
        Transform ranura = RanuraPayzone();
        if (rb != null && ranura != null)
        {
            moneda.transform.position = ranura.position;
            SacarPorRanura(rb, ranura);
            return;
        }

        if (rb != null && puntoDeExpulsion != null)
        {
            rb.linearVelocity = Vector3.zero;
            moneda.transform.position = puntoDeExpulsion.position;

            AplicarFuerzaMoneda(rb, multiplicadorRebote);

            ReproducirSonidoEscupir();
            HacerBloop();
        }
    }

    private void AplicarFuerzaMoneda(Rigidbody rb, float multiplicadorFuerza)
    {
        if (rb == null) return;

        Vector3 direccionSalida = puntoDeExpulsion.up * (fuerzaHaciaArriba * multiplicadorFuerza);
        direccionSalida += new Vector3(
            Random.Range(-dispersionLateral, dispersionLateral),
            0f,
            Random.Range(-dispersionLateral, dispersionLateral)
        );

        rb.AddForce(direccionSalida, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * Random.Range(5f, rotacionMax), ForceMode.Impulse);
    }

    // ---------------------------------------------------------------- Ranura del PayZone

    private Transform RanuraPayzone()
    {
        if (ranuraPayzone != null || ranuraBuscada) return ranuraPayzone;
        ranuraBuscada = true;

        GameObject payzone = GameObject.Find(nombrePayzone);
        MeshFilter malla = payzone != null ? payzone.GetComponentInChildren<MeshFilter>() : null;
        if (malla == null || malla.sharedMesh == null)
        {
            Debug.LogWarning("[AgujeroSimple] No encuentro el PayZone '" + nombrePayzone + "': las monedas salen por el agujero.", this);
            return null;
        }

        // La ranura está en la cara -Y local del modelo y su "arriba" es +Z local (modelo exportado de Blender).
        Transform modelo = malla.transform;
        Bounds caja = malla.sharedMesh.bounds;
        Vector3 puntoLocal = caja.min + Vector3.Scale(caja.size, fraccionRanura);
        Vector3 haciaFuera = modelo.TransformDirection(Vector3.down).normalized;
        Vector3 arriba = modelo.TransformDirection(Vector3.forward).normalized;

        GameObject punto = new GameObject("RanuraMonedas");
        punto.transform.SetParent(modelo.parent, false);
        punto.transform.position = modelo.TransformPoint(puntoLocal) + haciaFuera * separacionRanura;
        punto.transform.rotation = Quaternion.LookRotation(haciaFuera, arriba);
        ranuraPayzone = punto.transform;
        return ranuraPayzone;
    }

    private void SacarPorRanura(Rigidbody rb, Transform ranura)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Vector3 velocidad = ranura.forward * velocidadSalidaRanura +
                                ranura.up * elevacionSalidaRanura +
                                ranura.right * Random.Range(-dispersionRanura, dispersionRanura);
            rb.AddForce(velocidad, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * Random.Range(1f, 4f), ForceMode.VelocityChange);
        }

        if (sonidoEscupir != null)
            AudioSource.PlayClipAtPoint(sonidoEscupir, ranura.position, 0.5f);

        HacerBloopPayzone();
    }

    private void HacerBloopPayzone()
    {
        if (!payzoneBuscado)
        {
            payzoneBuscado = true;
            if (modeloPayzone == null)
            {
                GameObject payzone = GameObject.Find(nombrePayzone);
                MeshFilter malla = payzone != null ? payzone.GetComponentInChildren<MeshFilter>() : null;
                if (malla != null) modeloPayzone = malla.transform;
            }
            if (modeloPayzone != null) escalaOriginalPayzone = modeloPayzone.localScale;
        }

        impulsoBloopPayzone = fuerzaBloopPayzone;
    }

    private void ReproducirSonidoEscupir()
    {
        if (audioSource != null && sonidoEscupir != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sonidoEscupir, 0.5f);
        }
    }

    private void HacerBloop()
    {
        impulsoBloopActual = fuerzaBloop;
    }
}