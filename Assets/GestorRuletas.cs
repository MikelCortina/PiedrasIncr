using UnityEngine;

public class GestorRuletasFPS : MonoBehaviour
{
    public Camera camaraPrincipal;
    public LayerMask capaRuletas;
    public float distanciaInteraccion = 3f;

    [Tooltip("Multiplicador para ajustar la sensibilidad del giro circular")]
    public float sensibilidadGiro = 1f;

    private RuletaInteractiva ruletaActiva = null;
    private float anguloPantallaAnterior;

    void Update()
    {
        // El centro absoluto de tu pantalla (La mirilla)
        Vector2 mirilla = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray rayo = camaraPrincipal.ScreenPointToRay(mirilla);

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(rayo, out RaycastHit hit, distanciaInteraccion, capaRuletas))
            {
                ruletaActiva = hit.collider.GetComponent<RuletaInteractiva>();
                anguloPantallaAnterior = CalcularAnguloCircular(ruletaActiva, mirilla);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ruletaActiva = null;
        }

        if (ruletaActiva != null && Input.GetMouseButton(0))
        {
            float anguloActual = CalcularAnguloCircular(ruletaActiva, mirilla);

            // Calculamos la diferencia de giro (Ej: Pasó de 90 grados a 45 grados = giró -45)
            float deltaAngulo = Mathf.DeltaAngle(anguloPantallaAnterior, anguloActual);

            if (Mathf.Abs(deltaAngulo) > 0.01f) // Filtro para evitar vibraciones minúsculas
            {
                ruletaActiva.ModificarDesdeGiroVisual(-deltaAngulo * sensibilidadGiro);
                anguloPantallaAnterior = anguloActual;
            }
        }
    }

    // Calcula el ángulo de la mirilla respecto al centro del objeto 3D proyectado en tu monitor
    float CalcularAnguloCircular(RuletaInteractiva ruleta, Vector2 mirilla)
    {
        Vector3 centroRuletaEnPantalla = camaraPrincipal.WorldToScreenPoint(ruleta.transform.position);

        // Vector direccional desde el centro de la ruleta hasta donde estás apuntando
        Vector2 direccion = mirilla - new Vector2(centroRuletaEnPantalla.x, centroRuletaEnPantalla.y);

        return Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
    }
}