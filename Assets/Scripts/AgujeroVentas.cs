using UnityEngine;
using System.Collections;

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

    void Start()
    {
        if (modeloVisual != null) escalaOriginal = modeloVisual.localScale;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Aseguramos que el sistema de partículas empiece apagado por seguridad
        if (particulasEscupir != null)
        {
            var emision = particulasEscupir.emission;
            emision.rateOverTime = 0f;
            particulasEscupir.Stop();
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

        if (cantidadMonedas > 0)
        {
            StartCoroutine(EscupirMonedas(cantidadMonedas, pureza));
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
        // 1. Configurar emisión según tramos de pureza
        float emisionExtra = 0f;

        if (pureza >= umbralPerfecto)
        {
            emisionExtra = 0f; // Tramo 1: Piedra perfecta
        }
        else if (pureza >= umbralBueno)
        {
            emisionExtra = 15f; // Tramo 2: Piedra buena/aceptable
        }
        else
        {
            emisionExtra = 30f; // Tramo 3: Piedra mala/sucia
        }

        if (particulasEscupir != null)
        {
            var emision = particulasEscupir.emission;
            emision.rateOverTime = emisionExtra;
            if (!particulasEscupir.isPlaying) particulasEscupir.Play();
        }

        // 2. Bucle de expulsión de monedas
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

        // --- 3. PROTECCIÓN AVALANCHAS: APAGADO OBLIGATORIO ---
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