using UnityEngine;

public class GestorRuletasFPS : MonoBehaviour
{
    public Camera camaraPrincipal;

    [Tooltip("Selecciona AQUÍ la capa de Ruletas/Botones Y TAMBIÉN las capas de los edificios/suelo (Default, Edificios, etc.)")]
    public LayerMask capasDetectables;

    public float distanciaInteraccion = 3f;
    public float sensibilidadGiro = 1f;

    private RuletaInteractiva ruletaActiva = null;
    private float anguloPantallaAnterior;

    void Update()
    {
        Vector2 mirilla = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray rayo = camaraPrincipal.ScreenPointToRay(mirilla);

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(rayo, out RaycastHit hit, distanciaInteraccion, capasDetectables))
            {
                // 1. Intentamos coger una ruleta
                ruletaActiva = hit.collider.GetComponent<RuletaInteractiva>();

                if (ruletaActiva != null)
                {
                    anguloPantallaAnterior = CalcularAnguloCircular(ruletaActiva, mirilla);
                }
                else
                {
                    // 2. Si no era ruleta, miramos a ver si es un Botón de Absorción
                    BotonTuberia boton = hit.collider.GetComponent<BotonTuberia>();
                    if (boton != null)
                    {
                        boton.Interaccionar(); // Click!
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ruletaActiva = null;
        }

        if (ruletaActiva != null && Input.GetMouseButton(0))
        {
            float anguloActual = CalcularAnguloCircular(ruletaActiva, mirilla);
            float deltaAngulo = Mathf.DeltaAngle(anguloPantallaAnterior, anguloActual);

            if (Mathf.Abs(deltaAngulo) > 0.01f)
            {
                ruletaActiva.ModificarDesdeGiroVisual(deltaAngulo * sensibilidadGiro);
                anguloPantallaAnterior = anguloActual;
            }
        }
    }

    float CalcularAnguloCircular(RuletaInteractiva ruleta, Vector2 mirilla)
    {
        Vector3 centroRuletaEnPantalla = camaraPrincipal.WorldToScreenPoint(ruleta.transform.position);
        Vector2 direccion = mirilla - new Vector2(centroRuletaEnPantalla.x, centroRuletaEnPantalla.y);
        return Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
    }
}