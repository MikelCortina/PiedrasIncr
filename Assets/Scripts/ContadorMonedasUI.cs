using TMPro;
using UnityEngine;

public class ContadorMonedasUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TextMeshProUGUI textoMonedas;
    [SerializeField] private Cartera cartera;

    private void Awake()
    {
        // Si no se asigna manualmente, intentamos encontrarla
        if (cartera == null)
        {
            cartera = FindFirstObjectByType<Cartera>();
        }

        if (textoMonedas == null)
        {
            textoMonedas = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        if (cartera != null)
        {
            cartera.OnMonedasCambiadas += ActualizarContador;
            ActualizarContador(cartera.Monedas);
        }
    }

    private void OnDisable()
    {
        if (cartera != null)
        {
            cartera.OnMonedasCambiadas -= ActualizarContador;
        }
    }

    private void ActualizarContador(int cantidad)
    {
        if (textoMonedas != null)
        {
            textoMonedas.text = "Monedas: " + cantidad;
        }
    }
}