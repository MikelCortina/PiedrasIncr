using UnityEngine;

public class CameraFollowSword : MonoBehaviour
{
    [SerializeField] private Transform espada;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float suavizado = 8f;

    private void LateUpdate()
    {
        if (espada == null)
            return;

        Vector3 posicionObjetivo = espada.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            posicionObjetivo,
            suavizado * Time.deltaTime
        );

        transform.LookAt(espada.position);
    }
}