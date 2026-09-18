using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instancia;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(this);
    }

    // Método público para llamar al temblor desde cualquier script
    public void Temblar(float duracion, float magnitud)
    {
        StartCoroutine(RutinaShake(duracion, magnitud));
    }

    IEnumerator RutinaShake(float duracion, float magnitud)
    {
        Vector3 posicionOriginal = transform.localPosition;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            // Generamos un desplazamiento aleatorio dentro de una esfera en los ejes X y Y
            float offsetX = Random.Range(-1f, 1f) * magnitud;
            float offsetY = Random.Range(-1f, 1f) * magnitud;

            transform.localPosition = new Vector3(posicionOriginal.x + offsetX, posicionOriginal.y + offsetY, posicionOriginal.z);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Devolvemos la cámara exactamente a su sitio original
        transform.localPosition = posicionOriginal;
    }
}