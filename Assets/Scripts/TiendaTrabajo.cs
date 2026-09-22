using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TiendaTrabajo : MonoBehaviour
{
    [Header("Referencias")]
    public Cartera cartera;
    public GestorEquipamiento gestorEquipamiento;

    [Header("Arma de la Luna")]
    public int precioArmaLuna = 1;
    public Button botonComprarArma;
    public TextMeshProUGUI textoBotonArma;

    private void Start()
    {
        if (cartera == null)
            cartera = FindFirstObjectByType<Cartera>();

        if (gestorEquipamiento == null)
            gestorEquipamiento = FindFirstObjectByType<GestorEquipamiento>();

        ActualizarTienda();
    }

    public void ComprarArmaLuna()
    {
        if (cartera == null || gestorEquipamiento == null)
            return;

        // Ya comprada
        if (gestorEquipamiento.ArmaLanzadoraDesbloqueada())
        {
            return;
        }

        // Intentamos pagar
        if (!cartera.GastarMonedas(precioArmaLuna))
        {
            Debug.Log("No tienes monedas suficientes.");
            return;
        }

        // Desbloqueamos
        gestorEquipamiento.DesbloquearArmaLanzadora();

        ActualizarTienda();
    }

    private void ActualizarTienda()
    {
        if (gestorEquipamiento == null)
            return;

        bool comprada =
            gestorEquipamiento.ArmaLanzadoraDesbloqueada();

        if (textoBotonArma != null)
        {
            textoBotonArma.text = comprada
                ? "COMPRADO"
                : "COMPRAR - " + precioArmaLuna + " moneda";
        }

        if (botonComprarArma != null)
        {
            botonComprarArma.interactable = !comprada;
        }
    }
}