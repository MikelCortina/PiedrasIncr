using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VelocidadDebug : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        // Cogemos el Rigidbody de la piedra
        rb = GetComponent<Rigidbody>();
    }

    // OnGUI dibuja elementos en la pantalla de forma s�per sencilla (Ideal para pruebas)
    void OnGUI()
    {
        // Obtenemos la velocidad actual
        float velocidad = rb.linearVelocity.magnitude;

        // Le damos un poco de estilo al texto para que se lea bien
        GUIStyle estiloTexto = new GUIStyle();
        estiloTexto.fontSize = 30; // Tama�o grande
        estiloTexto.normal.textColor = Color.green; // Color verde fosforito
        estiloTexto.fontStyle = FontStyle.Bold;

        // Dibuja una sombra negra para que se lea sobre fondos claros
        GUIStyle estiloSombra = new GUIStyle(estiloTexto);
        estiloSombra.normal.textColor = Color.black;

        // Imprimimos el texto en la esquina superior izquierda
        GUI.Label(new Rect(22, 22, 400, 50), "Velocidad: " + velocidad.ToString("F2"), estiloSombra);
        GUI.Label(new Rect(20, 20, 400, 50), "Velocidad: " + velocidad.ToString("F2"), estiloTexto);
    }
}