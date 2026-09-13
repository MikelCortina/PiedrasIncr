using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class LanzamientoPiedra : MonoBehaviour
{
    [Header("Ajustes de Lanzamiento")]
    public float multiplicadorFuerza = 5f;
    public float fuerzaMaxima = 20f;

    private Rigidbody rb;
    private LineRenderer linea;
    private Vector3 puntoInicioArrastre;
    private bool arrastrando = false;
    private Plane planoSuelo; // Un plano imaginario para detectar el ratón en 3D

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configuramos la línea visual (puedes añadirle un Material en el Inspector)
        linea = GetComponent<LineRenderer>();
        linea.positionCount = 2;
        linea.enabled = false;
        linea.startWidth = 0.1f;
        linea.endWidth = 0.05f;
    }

    void OnMouseDown()
    {
        // Detenemos la piedra al agarrarla
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Creamos un plano matemático plano a la altura de la piedra
        planoSuelo = new Plane(Vector3.up, transform.position);
        
        // Lanzamos un rayo desde la cámara hacia el ratón
        Ray rayo = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (planoSuelo.Raycast(rayo, out float distancia))
        {
            puntoInicioArrastre = rayo.GetPoint(distancia);
            arrastrando = true;
            linea.enabled = true;
        }
    }

    void OnMouseDrag()
    {
        if (!arrastrando) return;

        Ray rayo = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (planoSuelo.Raycast(rayo, out float distancia))
        {
            Vector3 puntoActual = rayo.GetPoint(distancia);
            
            // La dirección es inversa (como un tirachinas o un taco de billar)
            Vector3 direccionTiro = puntoInicioArrastre - puntoActual;

            // Limitamos la fuerza máxima para que no salga volando al infinito
            if (direccionTiro.magnitude > fuerzaMaxima)
            {
                direccionTiro = direccionTiro.normalized * fuerzaMaxima;
            }

            // Dibujamos la línea de trayectoria
            linea.SetPosition(0, transform.position);
            linea.SetPosition(1, transform.position + (direccionTiro * 0.5f)); // La línea es la mitad de larga que la fuerza visual
        }
    }

    void OnMouseUp()
    {
        if (!arrastrando) return;
        
        arrastrando = false;
        linea.enabled = false; // Ocultamos la línea

        Ray rayo = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (planoSuelo.Raycast(rayo, out float distancia))
        {
            Vector3 puntoActual = rayo.GetPoint(distancia);
            Vector3 direccionTiro = puntoInicioArrastre - puntoActual;

            if (direccionTiro.magnitude > fuerzaMaxima)
            {
                direccionTiro = direccionTiro.normalized * fuerzaMaxima;
            }

            // Aplicamos la fuerza de golpe a la piedra
            rb.AddForce(direccionTiro * multiplicadorFuerza, ForceMode.Impulse);
        }
    }
}