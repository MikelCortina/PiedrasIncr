using UnityEngine;
using System.Collections;

public class EfectoBloopDestruccion : MonoBehaviour
{
    [Tooltip("Tiempo en segundos que tarda en desaparecer")]
    public float duracion = 0.2f;

    private Vector3 escalaInicial;

    void Start()
    {
        escalaInicial = transform.localScale;

        // Apagamos los colisionadores de inmediato para liberar el espacio de construcción.
        // Así el holograma no chocará con este objeto mientras se encoge.
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        StartCoroutine(AnimarDesaparicion());
    }

    IEnumerator AnimarDesaparicion()
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;

            // Fórmula matemática 'Ease In Back':
            // Se infla un poquitito antes de absorberse por completo hacia el tamaño 0.
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float progreso = c3 * t * t * t - c1 * t * t;

            // Como queremos ir al revés (de 1 a 0), restamos el progreso
            transform.localScale = escalaInicial * (1f - progreso);

            yield return null;
        }

        // Al terminar la animación, ahora sí destruimos el objeto real de la escena
        Destroy(gameObject);
    }
}