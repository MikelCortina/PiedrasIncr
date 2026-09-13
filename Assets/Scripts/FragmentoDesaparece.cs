using UnityEngine;

public class FragmentoDesaparece : MonoBehaviour
{
    private Vector3 escalaInicial;
    private float duracion;
    private float tiempoTranscurrido;
    private bool desapareciendo;

    public void IniciarDesaparicion(
        float duracionDesaparicion)
    {
        escalaInicial =
            transform.localScale;

        duracion =
            Mathf.Max(
                0.01f,
                duracionDesaparicion
            );

        tiempoTranscurrido = 0f;
        desapareciendo = true;
    }

    private void Update()
    {
        if (!desapareciendo)
            return;

        tiempoTranscurrido +=
            Time.deltaTime;

        float porcentaje =
            tiempoTranscurrido / duracion;

        porcentaje =
            Mathf.Clamp01(porcentaje);

        transform.localScale =
            Vector3.Lerp(
                escalaInicial,
                Vector3.zero,
                porcentaje
            );

        if (porcentaje >= 1f)
        {
            Destroy(gameObject);
        }
    }
}