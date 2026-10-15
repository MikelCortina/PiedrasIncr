using UnityEngine;

public class BucleNeonParticulas : MonoBehaviour
{
    [Header("Tiempos del Bucle")]
    [Tooltip("Tiempo en segundos que tarda en subir desde el color inicial al máximo")]
    public float tiempoCarga = 3f;
    [Tooltip("Tiempo en segundos que tarda en volver suavemente al estado inicial")]
    public float tiempoDescarga = 1.5f;

    [Header("Neones a Controlar")]
    [Tooltip("Arrastra aquí los objetos con el script ControladorNeon")]
    public ControladorNeon[] neones;

    [Header("1. Estado Inicial (Enfriado)")]
    [ColorUsage(true, true)] public Color colorInicio = Color.green;
    public float intensidadInicio = 1f;
    public float parpadeoInicio = 2f;

    [Header("2. Estado Final (Punto Máximo)")]
    [ColorUsage(true, true)] public Color colorFin = Color.red;
    public float intensidadFin = 5f;
    public float parpadeoFin = 25f;

    [Header("Efectos")]
    [Tooltip("Lista de sistemas de partículas que saltarán justo cuando el bucle llegue al límite")]
    public ParticleSystem[] particulasExplosion;

    private float progresoActual = 0f;
    private bool subiendo = true;

    void Update()
    {
        // Evitamos errores de división por cero si dejas los tiempos a 0 en el Inspector
        if (tiempoCarga <= 0.01f) tiempoCarga = 0.01f;
        if (tiempoDescarga <= 0.01f) tiempoDescarga = 0.01f;

        // FASE 1: Subiendo la intensidad
        if (subiendo)
        {
            progresoActual += Time.deltaTime / tiempoCarga;

            // Comprobamos si ha llegado al máximo (100%)
            if (progresoActual >= 1f)
            {
                progresoActual = 1f;
                subiendo = false; // Cambiamos la dirección para que empiece a bajar

                // Disparamos TODAS las partículas de la lista exactamente al tocar el límite
                if (particulasExplosion != null && particulasExplosion.Length > 0)
                {
                    foreach (ParticleSystem ps in particulasExplosion)
                    {
                        if (ps != null)
                        {
                            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            ps.Play();
                        }
                    }
                }
            }
        }
        // FASE 2: Bajando la intensidad suavemente (Lerp de vuelta)
        else
        {
            progresoActual -= Time.deltaTime / tiempoDescarga;

            // Comprobamos si ha llegado al mínimo (0%)
            if (progresoActual <= 0f)
            {
                progresoActual = 0f;
                subiendo = true; // Volvemos a empezar la carga
            }
        }

        // Calculamos los valores intermedios (ahora progresoActual sube y baja suavemente como una ola)
        Color colorActual = Color.Lerp(colorInicio, colorFin, progresoActual);
        float intensidadActual = Mathf.Lerp(intensidadInicio, intensidadFin, progresoActual);
        float parpadeoActual = Mathf.Lerp(parpadeoInicio, parpadeoFin, progresoActual);

        // Se lo aplicamos en tiempo real a todos los neones de la lista
        if (neones != null)
        {
            for (int i = 0; i < neones.Length; i++)
            {
                if (neones[i] != null)
                {
                    neones[i].colorNeon = colorActual;
                    neones[i].intensidad = intensidadActual;
                    neones[i].velocidadParpadeo = parpadeoActual;
                }
            }
        }
    }
}