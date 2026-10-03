using System.Collections;
using UnityEngine;

public class BotonTuberia : MonoBehaviour
{
    [Header("Efectos Visuales")]
    [Tooltip("El sistema de partículas en bucle (Looping) de la absorción")]
    public ParticleSystem particulasAbsorcion;

    [Header("Color de Apagado (Opcional)")]
    [Tooltip("Arrastra aquí el script VariacionAlbedo de este mismo botón")]
    public VariacionAlbedo scriptVariacionAlbedo;
    [Tooltip("Color que tomará el botón cuando esté apagado")]
    public Color colorApagado = Color.gray;

    [Header("Animación del Botón")]
    [Tooltip("Cuánto se mueve el botón al pulsarlo")]
    public float distanciaHundido = 0.15f;
    [Tooltip("La velocidad a la que se presiona")]
    public float velocidadAnimacion = 10f;
    [Tooltip("Dirección local hacia la que se hunde (normalmente Y negativa)")]
    public Vector3 ejeHundimiento = new Vector3(0, -1, 0);

    [HideInInspector] public bool encendido = true;

    private Vector3 posicionEncendido;
    private Vector3 posicionApagado;
    private Coroutine animacionActual;

    private void Awake()
    {
        posicionEncendido = transform.localPosition;
        posicionApagado = posicionEncendido + (ejeHundimiento.normalized * distanciaHundido);

        // Forzamos el estado visual inicial según cómo empiece (normalmente encendido)
        AplicarEstadoVisual(true);
    }

    public void Interaccionar()
    {
        encendido = !encendido; // Alternamos el estado

        // Animamos el hundimiento del botón
        if (animacionActual != null) StopCoroutine(animacionActual);
        animacionActual = StartCoroutine(AnimarBoton(encendido ? posicionEncendido : posicionApagado));

        // Aplicamos apagado de partículas y color
        AplicarEstadoVisual(false);
    }

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
                else particulasAbsorcion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Si se pulsa, corte en seco y borrado
            }
        }

        // 2. Gestionar el Color (si tienes el script VariacionAlbedo asignado)
        if (scriptVariacionAlbedo != null)
        {
            if (encendido)
            {
                scriptVariacionAlbedo.RestaurarColor();
            }
            else
            {
                scriptVariacionAlbedo.ForzarColor(colorApagado);
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