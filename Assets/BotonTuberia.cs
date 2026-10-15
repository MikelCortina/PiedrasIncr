using System.Collections;
using UnityEngine;

public class BotonTuberia : MonoBehaviour
{
    [Header("Efectos Visuales")]
    [Tooltip("El sistema de partículas en bucle (Looping) de la absorción")]
    public ParticleSystem particulasAbsorcion;

    [Header("Animación del Botón")]
    [Tooltip("Cuánto se mueve el botón al pulsarlo")]
    public float distanciaHundido = 0.15f;
    [Tooltip("La velocidad a la que se presiona")]
    public float velocidadAnimacion = 10f;
    [Tooltip("Dirección local hacia la que se hunde (normalmente Y negativa)")]
    public Vector3 ejeHundimiento = new Vector3(0, -1, 0);

    // ========================================================
    // SISTEMA DE NEONES
    // ========================================================
    [Header("Indicadores Visuales (Neones)")]
    [Tooltip("Arrastra aquí los objetos con el script ControladorNeon de este botón")]
    public ControladorNeon[] neonesBoton;

    [ColorUsage(true, true)]
    public Color colorNeonEncendido = Color.cyan;
    [ColorUsage(true, true)]
    public Color colorNeonApagado = Color.red;

    [Tooltip("Intensidad extra que se suma al encender el botón")]
    public float aumentoIntensidadEncendido = 2f;
    [Tooltip("Velocidad de parpadeo extra que se suma al encender el botón")]
    public float aumentoParpadeoEncendido = 15f;

    [Tooltip("Velocidad a la que el neón hace la transición (20 es rápido)")]
    public float velocidadTransicionNeon = 20f;

    private float transicionNeonActual = 0f;
    private float[] intensidadesBaseNeones;
    private float[] parpadeosBaseNeones;
    // ========================================================

    [HideInInspector] public bool encendido = true;

    private Vector3 posicionEncendido;
    private Vector3 posicionApagado;
    private Coroutine animacionActual;

    private void Awake()
    {
        posicionEncendido = transform.localPosition;
        posicionApagado = posicionEncendido + (ejeHundimiento.normalized * distanciaHundido);

        // --- INICIALIZAMOS LOS NEONES GUARDANDO SUS VALORES BASE ---
        if (neonesBoton != null && neonesBoton.Length > 0)
        {
            intensidadesBaseNeones = new float[neonesBoton.Length];
            parpadeosBaseNeones = new float[neonesBoton.Length];

            for (int i = 0; i < neonesBoton.Length; i++)
            {
                if (neonesBoton[i] != null)
                {
                    intensidadesBaseNeones[i] = neonesBoton[i].intensidad;
                    parpadeosBaseNeones[i] = neonesBoton[i].velocidadParpadeo;
                    // Forzamos el color inicial según empiece el botón
                    neonesBoton[i].colorNeon = encendido ? colorNeonEncendido : colorNeonApagado;
                }
            }
        }

        // Configuramos la transición matemática inicial
        transicionNeonActual = encendido ? 1f : 0f;
        // -----------------------------------------------------------

        // Forzamos el estado visual inicial de las partículas
        AplicarEstadoVisual(true);
    }

    private void Update()
    {
        ManejarNeones();
    }

    public void Interaccionar()
    {
        encendido = !encendido; // Alternamos el estado

        // Animamos el hundimiento del botón
        if (animacionActual != null) StopCoroutine(animacionActual);
        animacionActual = StartCoroutine(AnimarBoton(encendido ? posicionEncendido : posicionApagado));

        // Aplicamos apagado de partículas
        AplicarEstadoVisual(false);
    }

    // ========================================================
    // LÓGICA DE NEONES: Lerp rápido
    // ========================================================
    private void ManejarNeones()
    {
        if (neonesBoton == null || neonesBoton.Length == 0) return;

        float transicionObjetivo = encendido ? 1f : 0f;

        // Lerp rápido para transición de colores e intensidades
        transicionNeonActual = Mathf.Lerp(transicionNeonActual, transicionObjetivo, Time.deltaTime * velocidadTransicionNeon);

        Color colorActualizado = Color.Lerp(colorNeonApagado, colorNeonEncendido, transicionNeonActual);

        for (int i = 0; i < neonesBoton.Length; i++)
        {
            if (neonesBoton[i] != null)
            {
                neonesBoton[i].colorNeon = colorActualizado;
                neonesBoton[i].intensidad = intensidadesBaseNeones[i] + (aumentoIntensidadEncendido * transicionNeonActual);
                neonesBoton[i].velocidadParpadeo = parpadeosBaseNeones[i] + (aumentoParpadeoEncendido * transicionNeonActual);
            }
        }
    }
    // ========================================================

    private void AplicarEstadoVisual(bool esInicio)
    {
        // 1. Gestionar las Partículas
        if (particulasAbsorcion != null)
        {
            if (encendido)
            {
                particulasAbsorcion.Play();
            }
            else
            {
                if (esInicio) particulasAbsorcion.Stop(); // Si inicia apagado, corte normal
                else particulasAbsorcion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Si se apaga al hacer clic, corte en seco y borrado
            }
        }
    }

    private IEnumerator AnimarBoton(Vector3 destino)
    {
        while (Vector3.Distance(transform.localPosition, destino) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, destino, Time.deltaTime * velocidadAnimacion);
            yield return null;
        }
        transform.localPosition = destino;
    }
}