using UnityEngine;

public class RuletaInteractiva : MonoBehaviour
{
    public enum TipoControl { RotacionX, RotacionY, FuerzaLanzamiento }
    [Tooltip("¿Qué controla esta ruleta?")]
    public TipoControl tipoControl;

    [Header("Límites del Efecto")]
    public float valorMinimo = -45f;
    public float valorMaximo = 45f;
    [Range(0f, 1f)]
    public float porcentajeInicial = 0.5f;

    [Header("Giro Visual (Eje Z Local)")]
    public float anguloVisualMinimo = -140f;
    public float anguloVisualMaximo = 140f;
    [Tooltip("Marca esta casilla si al mover la cámara la ruleta gira hacia el lado contrario")]
    public bool invertirGiro = false;

    [Header("Referencias a Controlar")]
    public Transform objetoARotar;
    public MaquinaPiedras maquinaPiedras;

    private float porcentajeActual;
    private float rotacionBaseX;
    private float rotacionBaseY;

    void Start()
    {
        porcentajeActual = porcentajeInicial;

        if (objetoARotar != null)
        {
            rotacionBaseX = NormalizarAngulo(objetoARotar.localEulerAngles.x);
            rotacionBaseY = NormalizarAngulo(objetoARotar.localEulerAngles.y);
        }

        AplicarCambios();
    }

    // NUEVO: Método que recibe los grados que has arrastrado circularmente con tu cabeza
    public void ModificarDesdeGiroVisual(float gradosGirados)
    {
        if (invertirGiro) gradosGirados = -gradosGirados;

        // Calculamos cuánto porcentaje representa este giro respecto al tope de nuestra ruleta
        float rangoTotalVisual = anguloVisualMaximo - anguloVisualMinimo;

        if (rangoTotalVisual != 0)
        {
            float incrementoPorcentaje = gradosGirados / rangoTotalVisual;
            porcentajeActual += incrementoPorcentaje;
            porcentajeActual = Mathf.Clamp01(porcentajeActual);

            AplicarCambios();
        }
    }

    void AplicarCambios()
    {
        float anguloZ = Mathf.Lerp(anguloVisualMinimo, anguloVisualMaximo, porcentajeActual);
        transform.localEulerAngles = new Vector3(
            transform.localEulerAngles.x,
            transform.localEulerAngles.y,
            anguloZ
        );

        float valorCalculado = Mathf.Lerp(valorMinimo, valorMaximo, porcentajeActual);

        if (tipoControl == TipoControl.FuerzaLanzamiento && maquinaPiedras != null)
        {
            maquinaPiedras.fuerzaLanzamiento = valorCalculado;
        }
        else if (objetoARotar != null)
        {
            Vector3 rotacionObjetivo = objetoARotar.localEulerAngles;

            if (tipoControl == TipoControl.RotacionX) rotacionObjetivo.x = rotacionBaseX + valorCalculado;
            else if (tipoControl == TipoControl.RotacionY) rotacionObjetivo.y = rotacionBaseY + valorCalculado;

            objetoARotar.localEulerAngles = rotacionObjetivo;
        }
    }

    float NormalizarAngulo(float angulo)
    {
        angulo %= 360f;
        if (angulo > 180f) angulo -= 360f;
        return angulo;
    }
}