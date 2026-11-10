using UnityEngine;

public class RotacionPreview : MonoBehaviour
{
    [SerializeField] private Vector3 velocidadRotacion = new Vector3(0f, 30f, 0f);
    [SerializeField] private Space espacio = Space.Self;

    private void Update()
    {
        transform.Rotate(velocidadRotacion * Time.deltaTime, espacio);
    }
}