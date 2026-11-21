using System;
using TMPro;
using UnityEngine;

public class ContadorGasolinera : MonoBehaviour
{
    public TextMeshProUGUI textoLED;
    [Tooltip("Opcional: texto aparte para el porcentaje pequeño. Si está vacío, va debajo de los litros en textoLED.")]
    public TextMeshProUGUI textoPorcentaje;
    [Tooltip("Opcional: texto del aviso animado \"+100 L\".")]
    public TextMeshProUGUI textoIncremento;

    [Header("Animación")]
    [Tooltip("Litros por segundo que sube el contador en pantalla.")]
    [Min(1f)] public float litrosPorSegundo = 120f;
    [Min(0.1f)] public float duracionIncremento = 1.5f;
    public float subidaIncremento = 40f;

    [Header("Obsoleto")]
    [HideInInspector] public float velocidadAnimacion = 0.5f;

    public static event Action<float, float> OnGasolinaAnadida;

    private float fraccionObjetivo;
    private float litrosMostrados;
    private float tiempoIncremento = -1f;
    private Vector3 posicionIncremento;

    public float FraccionActual { get { return fraccionObjetivo; } }
    public float LitrosActuales { get { return ProveedorEconomia.Actual.FraccionALitros(fraccionObjetivo); } }

    void Start()
    {
        if (textoIncremento != null)
        {
            posicionIncremento = textoIncremento.rectTransform.anchoredPosition;
            textoIncremento.gameObject.SetActive(false);
        }
        litrosMostrados = LitrosActuales;
        ActualizarPantalla();
    }

    void Update()
    {
        float objetivo = LitrosActuales;
        if (!Mathf.Approximately(litrosMostrados, objetivo))
        {
            float velocidad = Mathf.Max(litrosPorSegundo, Mathf.Abs(objetivo - litrosMostrados) * 2f);
            litrosMostrados = Mathf.MoveTowards(litrosMostrados, objetivo, velocidad * Time.deltaTime);
            ActualizarPantalla();
        }

        if (textoIncremento != null && tiempoIncremento >= 0f)
        {
            tiempoIncremento += Time.deltaTime;
            float t = tiempoIncremento / duracionIncremento;
            textoIncremento.rectTransform.anchoredPosition = posicionIncremento + Vector3.up * (subidaIncremento * t);
            textoIncremento.alpha = 1f - t;
            if (t >= 1f)
            {
                tiempoIncremento = -1f;
                textoIncremento.gameObject.SetActive(false);
            }
        }
    }

    public void AgregarFraccion(float fraccion)
    {
        if (fraccion <= 0f) return;
        fraccionObjetivo = Mathf.Clamp01(fraccionObjetivo + fraccion);
        float litros = ProveedorEconomia.Actual.FraccionALitros(fraccion);
        MostrarIncremento(litros);
        if (OnGasolinaAnadida != null) OnGasolinaAnadida(fraccion, fraccionObjetivo);
    }

    [Obsolete("Usa AgregarFraccion (fracción 0-1 del tanque).")]
    public void AgregarFuelo(float cantidadPorcentaje)
    {
        AgregarFraccion(cantidadPorcentaje / 100f);
    }

    public void Restaurar(float fraccion)
    {
        fraccionObjetivo = Mathf.Clamp01(fraccion);
        litrosMostrados = LitrosActuales;
        ActualizarPantalla();
    }

    private void MostrarIncremento(float litros)
    {
        if (textoIncremento == null) return;
        textoIncremento.text = "+" + litros.ToString(litros < 10f ? "0.0" : "0") + " L";
        textoIncremento.alpha = 1f;
        textoIncremento.rectTransform.anchoredPosition = posicionIncremento;
        textoIncremento.gameObject.SetActive(true);
        tiempoIncremento = 0f;
    }

    private void ActualizarPantalla()
    {
        if (textoLED == null) return;
        float porcentaje = ProveedorEconomia.Actual.tanqueLitrosVisual > 0f ? litrosMostrados / ProveedorEconomia.Actual.tanqueLitrosVisual * 100f : 0f;
        string litros = litrosMostrados.ToString("N0") + " L";
        string pct = porcentaje.ToString("0.000") + " %";
        if (textoPorcentaje != null)
        {
            textoLED.text = litros;
            textoPorcentaje.text = pct;
        }
        else
        {
            textoLED.text = litros + "\n<size=45%>" + pct + "</size>";
        }
    }
}
