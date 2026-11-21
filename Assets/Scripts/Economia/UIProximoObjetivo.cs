using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIProximoObjetivo : MonoBehaviour
{
    [Header("Referencias")]
    public TMP_Text texto;
    [Tooltip("Opcional: Image en modo Filled que se llena según las monedas que llevas.")]
    public Image barra;
    public Cartera cartera;

    [Header("Ajustes")]
    [Min(0.05f)] public float intervaloRefresco = 0.25f;
    public string textoSinObjetivos = "¡Lo tienes todo!";

    private float siguienteRefresco;

    private void OnEnable()
    {
        siguienteRefresco = 0f;
    }

    private void Update()
    {
        if (Time.unscaledTime < siguienteRefresco) return;
        siguienteRefresco = Time.unscaledTime + intervaloRefresco;
        Refrescar();
    }

    public void Refrescar()
    {
        EconomiaDemo economia = ProveedorEconomia.Actual;
        if (cartera == null) cartera = FindAnyObjectByType<Cartera>();

        PasoCompra paso = NivelesMejoras.SiguientePaso(economia);
        if (paso == null)
        {
            if (texto != null) texto.text = textoSinObjetivos;
            if (barra != null) barra.fillAmount = 1f;
            return;
        }

        string nombre = EconomiaDemo.NombreVisible(paso.id) + (economia.NivelesMax(paso.id) > 1 ? " " + paso.nivel : string.Empty);
        int coste = economia.Coste(paso.id, paso.nivel);

        if (economia.PagaCon(paso.id) == PagoMejora.EsferasPerfectas)
        {
            if (texto != null) texto.text = "Próximo objetivo: " + nombre + " · " + coste + " esferas perfectas";
            if (barra != null) barra.fillAmount = 0f;
            return;
        }

        int monedas = cartera != null ? cartera.Monedas : 0;
        int faltan = Mathf.Max(0, coste - monedas);
        if (texto != null)
        {
            texto.text = faltan > 0
                ? "Próximo: " + nombre + " · te faltan " + faltan + (faltan == 1 ? " moneda" : " monedas")
                : "¡Ya puedes comprar " + nombre + "! (" + coste + " monedas)";
        }
        if (barra != null) barra.fillAmount = coste > 0 ? Mathf.Clamp01((float)monedas / coste) : 1f;
    }
}
