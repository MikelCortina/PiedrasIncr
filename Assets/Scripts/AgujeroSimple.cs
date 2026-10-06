using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- RENOMBRADO PARA FORZAR A UNITY A BORRAR LA CACHÉ ---
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
// -------------------------------------------------

public class AgujeroSimple : MonoBehaviour
{
    [Header("Recompensas (Tramos de Pureza)")]
    public GameObject prefabDinero;
    public Transform puntoDeExpulsion;

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
    [Tooltip("Multiplicador de fuerza extra para asegurar que las monedas rebotadas salgan del hoyo")]
    public float multiplicadorRebote = 1.2f;

    [Header("Flujo de Tuberías (Deformación)")]
    [Tooltip("Añade aquí las tuberías en orden. Cada una puede tener su propia velocidad y límites.")]
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

    void Start()
    {
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
    }

    public void RecibirPiedra(GameObject piedra)
    {
        if (piedra == null) return;

        DeformacionPiedra scriptPiedra = piedra.GetComponentInParent<DeformacionPiedra>();
        int cantidadMonedas = premioMalo;
        float pureza = 0f;

        if (scriptPiedra != null)
        {
            pureza = scriptPiedra.ObtenerPorcentajeDesgasteHaciaEsfera();
            cantidadMonedas = CalcularRecompensa(pureza);
        }

        Destroy(piedra);

        if (particulasTragar != null) particulasTragar.Play();
        if (audioSource != null && sonidoTragar != null) audioSource.PlayOneShot(sonidoTragar);

        HacerBloop();

        if (recorridoTuberias != null && recorridoTuberias.Count > 0)
        {
            StartCoroutine(RutinaViajePorTuberias());
        }

        if (cantidadMonedas > 0)
        {
            StartCoroutine(EscupirMonedas(cantidadMonedas, pureza));
        }
    }

    // ==========================================
    // LÓGICA DE VIAJE DE TUBERÍAS (PIPELINE)
    // ==========================================
    private IEnumerator RutinaViajePorTuberias()
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

            // Mathf.MoveTowards se encarga de ir hacia arriba o hacia abajo automáticamente
            while (progresoActual != paso.pannerFin)
            {
                // Usamos Mathf.Abs para que la velocidad siempre sume (hacia el objetivo), 
                // así no tienes que preocuparte de poner velocidades negativas en el Inspector.
                progresoActual = Mathf.MoveTowards(progresoActual, paso.pannerFin, Time.deltaTime * Mathf.Abs(paso.velocidadDeformacion));
                paso.tuberia.SetPannerGlobal(progresoActual);
                yield return null;
            }

            // Al salir, lo dejamos invisible en su punto de inicio original
            paso.tuberia.SetPannerGlobal(paso.pannerInicio);
            tuberiaOcupada[i] = false;
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

        for (int i = 0; i < cantidad; i++)
        {
            if (prefabDinero != null && puntoDeExpulsion != null)
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
        if (rb != null)
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