using UnityEngine;

public class EdadMoneda : MonoBehaviour
{
    private float tiempoCreacion;

    void Awake()
    {
        // Registramos exactamente en qué segundo del juego nació la moneda
        tiempoCreacion = Time.time;
    }

    public bool HaSuperadoInmunidad(float tiempoRequerido)
    {
        return (Time.time - tiempoCreacion) >= tiempoRequerido;
    }
}