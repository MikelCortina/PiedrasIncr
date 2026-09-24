using UnityEngine;
using System.Collections;

public class EfectoBloop : MonoBehaviour
{
    [Tooltip("Tiempo en segundos que tarda en hacer el efecto")]
    public float duracion = 0.25f;

    private Vector3 escalaFinal;

    void Start()
    {
        // Guardamos la escala original del prefab para saber a qué tamaño debe llegar
        escalaFinal = transform.localScale;

        // Lo encogemos a 0 instantáneamente para que nazca invisible
        transform.localScale = Vector3.zero;

        StartCoroutine(AnimarBloop());
    }

    IEnumerator AnimarBloop()
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;

            // Fórmula matemática 'Ease Out Back' para el rebote
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            transform.localScale = escalaFinal * ease;

            yield return null;
        }

        // Aseguramos que termine exactamente en su escala original
        transform.localScale = escalaFinal;

        // Destruimos este script (no el objeto) para ahorrar recursos de CPU, ya cumplió su función
        Destroy(this);
    }
}