using UnityEngine;

public class RespiracionNeon : MonoBehaviour
{
    [Header("Referencia al Neón")]
    [Tooltip("Arrastra aquí el script ControladorNeon que va a 'respirar'")]
    public ControladorNeon controladorNeon;

    [Header("Ajustes de Respiración")]
    [Tooltip("Intensidad más baja (Equivalente a Intensidad Listo Min)")]
    public float intensidadMinima = 0.5f;

    [Tooltip("Intensidad más alta (Equivalente a Intensidad Listo Max)")]
    public float intensidadMaxima = 1.5f;

    [Tooltip("Velocidad a la que respira (Equivalente a Velocidad Oscilacion)")]
    public float velocidadOscilacion = 2f;

    void Update()
    {
        if (controladorNeon != null)
        {
            // Genera una onda fluida que va constantemente de 0 a 1
            float onda = (Mathf.Sin(Time.time * velocidadOscilacion) + 1f) / 2f;

            // Calcula la intensidad exacta en este fotograma
            float intensidadActual = Mathf.Lerp(intensidadMinima, intensidadMaxima, onda);

            // Se la inyecta directamente a tu controlador de neón
            controladorNeon.intensidad = intensidadActual;
        }
    }
}