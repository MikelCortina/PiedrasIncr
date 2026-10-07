using UnityEngine;
using TMPro;

public class ContadorGasolinera : MonoBehaviour
{
    public TextMeshProUGUI textoLED;

    [Tooltip("Velocidad a la que sube el porcentaje visualmente por segundo")]
    public float velocidadAnimacion = 0.5f;

    // El combustible real que tenemos
    private float combustibleObjetivo = 0f;

    // El combustible falso que usamos solo para la animación en pantalla
    private float combustibleMostrado = 0f;

    void Start()
    {
        ActualizarPantalla();
    }

    void Update()
    {
        // Si el número de la pantalla aún no ha alcanzado al número real, lo subimos poco a poco
        if (combustibleMostrado != combustibleObjetivo)
        {
            // Mathf.MoveTowards acerca el valor progresivamente sin pasarse
            combustibleMostrado = Mathf.MoveTowards(combustibleMostrado, combustibleObjetivo, velocidadAnimacion * Time.deltaTime);
            ActualizarPantalla();
        }
    }

    // El agujero llama a esta función
    public void AgregarFuelo(float cantidad)
    {
        // Sumamos al objetivo real, no al mostrado
        combustibleObjetivo += cantidad;

        // Evitamos que el porcentaje pase de 100%
        if (combustibleObjetivo > 100f)
        {
            combustibleObjetivo = 100f;
        }
    }

    private void ActualizarPantalla()
    {
        textoLED.text = "Fuel: " + combustibleMostrado.ToString("F2") + "%";
    }
}