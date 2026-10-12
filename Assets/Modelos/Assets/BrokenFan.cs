using System.Collections;
using UnityEngine;

public class BrokenFan : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Configuración de Rotación")]
    [Tooltip("El eje sobre el que girará el ventilador.")]
    public Axis ejeDeRotacion = Axis.Y;

    [Tooltip("Velocidad máxima de giro.")]
    public float velocidadMaxima = 500f;

    [Tooltip("Rapidez con la que acelera y frena (simula la inercia).")]
    public float inercia = 2f;

    [Header("Intervalos de Avería (Tiempos aleatorios)")]
    [Tooltip("Tiempo mínimo que funcionará bien antes de fallar.")]
    public float tiempoMinimoGirando = 2f;
    [Tooltip("Tiempo máximo que funcionará bien antes de fallar.")]
    public float tiempoMaximoGirando = 6f;

    [Tooltip("Tiempo mínimo que estará 'atascado' o sin fuerza.")]
    public float tiempoMinimoParado = 0.5f;
    [Tooltip("Tiempo máximo que estará 'atascado' o sin fuerza.")]
    public float tiempoMaximoParado = 2f;

    private float velocidadActual = 0f;
    private float velocidadObjetivo = 0f;

    void Start()
    {
        // Iniciamos la rutina que controla los fallos del ventilador
        StartCoroutine(RutinaAveria());
    }

    void Update()
    {
        // Transición suave entre velocidades usando Lerp para simular inercia real
        velocidadActual = Mathf.Lerp(velocidadActual, velocidadObjetivo, Time.deltaTime * inercia);

        // Determinamos el vector de rotación según el eje elegido
        Vector3 vectorDeRotacion = Vector3.zero;
        switch (ejeDeRotacion)
        {
            case Axis.X: vectorDeRotacion = Vector3.right; break;
            case Axis.Y: vectorDeRotacion = Vector3.up; break;
            case Axis.Z: vectorDeRotacion = Vector3.forward; break;
        }

        // Aplicamos la rotación
        transform.Rotate(vectorDeRotacion * velocidadActual * Time.deltaTime);
    }

    IEnumerator RutinaAveria()
    {
        while (true)
        {
            // ESTADO 1: Funcionando bien
            velocidadObjetivo = velocidadMaxima;
            float tiempoGirando = Random.Range(tiempoMinimoGirando, tiempoMaximoGirando);
            yield return new WaitForSeconds(tiempoGirando);

            // ESTADO 2: Falla / Pierde fuerza
            // Puedes cambiar este 0f por un valor bajo (ej: velocidadMaxima * 0.1f) si quieres que gire muy lento en vez de parar del todo
            velocidadObjetivo = 0f;
            float tiempoParado = Random.Range(tiempoMinimoParado, tiempoMaximoParado);
            yield return new WaitForSeconds(tiempoParado);
        }
    }
}