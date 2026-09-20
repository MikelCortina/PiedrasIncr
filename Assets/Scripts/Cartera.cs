using System;
using UnityEngine;

public class Cartera : MonoBehaviour
{
    [Header("Dinero")]
    [SerializeField] private int monedas = 0;

    public int Monedas => monedas;

    public event Action<int> OnMonedasCambiadas;

    public void AnadirMonedas(int cantidad)
    {
        if (cantidad <= 0)
            return;

        monedas += cantidad;

        OnMonedasCambiadas?.Invoke(monedas);

        Debug.Log("Monedas: " + monedas);
    }

    public bool PuedeGastar(int cantidad)
    {
        return monedas >= cantidad;
    }

    public bool GastarMonedas(int cantidad)
    {
        if (cantidad <= 0)
            return false;

        if (monedas < cantidad)
            return false;

        monedas -= cantidad;

        OnMonedasCambiadas?.Invoke(monedas);

        Debug.Log("Monedas: " + monedas);

        return true;
    }
}