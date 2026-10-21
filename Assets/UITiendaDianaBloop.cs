using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz opcional para pagar la construccion y las mejoras desde tienda.
/// Los disparos y la tienda comparten el progreso y nunca cobran dos veces.
/// </summary>
public class UITiendaDianaBloop : MonoBehaviour
{
    public ConstruccionDianaBloopMonedas construccion;

    [Header("Construir")]
    public Button botonConstruir;
    public TextMeshProUGUI textoBotonConstruir;

    [Header("Mejorar")]
    public Button botonMejorar;
    public TextMeshProUGUI textoBotonMejorar;

    private void OnEnable()
    {
        if (botonConstruir != null)
            botonConstruir.onClick.AddListener(ComprarConstruccion);
        if (botonMejorar != null)
            botonMejorar.onClick.AddListener(ComprarMejora);
        if (construccion != null)
            construccion.ProgresoActualizado += Refrescar;
        Refrescar();
    }

    private void OnDisable()
    {
        if (botonConstruir != null)
            botonConstruir.onClick.RemoveListener(ComprarConstruccion);
        if (botonMejorar != null)
            botonMejorar.onClick.RemoveListener(ComprarMejora);
        if (construccion != null)
            construccion.ProgresoActualizado -= Refrescar;
    }

    private void LateUpdate()
    {
        Refrescar();
    }

    private void ComprarConstruccion()
    {
        if (construccion != null)
            construccion.ComprarConstruccionDesdeTienda();
    }

    private void ComprarMejora()
    {
        if (construccion != null)
            construccion.ComprarMejoraDesdeTienda();
    }

    public void Refrescar()
    {
        if (construccion == null) return;

        bool construida = construccion.EstaConstruida;
        bool maximo = construccion.NivelMaximo;

        if (botonConstruir != null)
        {
            botonConstruir.gameObject.SetActive(!construida);
            botonConstruir.interactable = !construida;
        }
        if (textoBotonConstruir != null)
            textoBotonConstruir.text = "CONSTRUIR DIANA BLOOP - " +
                construccion.MonedasRestantes + " MONEDAS";

        if (botonMejorar != null)
        {
            botonMejorar.gameObject.SetActive(construida);
            botonMejorar.interactable = construida && !maximo;
        }
        if (textoBotonMejorar != null)
            textoBotonMejorar.text = maximo
                ? "DIANA BLOOP NIVEL MAXIMO"
                : "DIANA BLOOP NIVEL " + (construccion.NivelActual + 1) +
                  " - " + construccion.MonedasRestantes + " MONEDAS";
    }
}
