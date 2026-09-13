using UnityEngine;

public class MonitorRendimiento : MonoBehaviour
{
    private float deltaTime = 0.0f;

    private int totalPiedras = 0;
    private int piedrasDormidas = 0;
    private float porcentajeDormidas = 0f;

    void Start()
    {
        // Llamamos a la función de contar piedras cada 0.5 segundos.
        // Hacerlo en cada fotograma (Update) arruinaría el rendimiento de tu PC.
        InvokeRepeating(nameof(ContarPiedras), 0.5f, 0.5f);
    }

    void Update()
    {
        // Calculamos el tiempo real que tarda en procesarse cada fotograma
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void ContarPiedras()
    {
        // Buscamos todas las piedras en el mapa
        DeformacionPiedra[] piedras = FindObjectsOfType<DeformacionPiedra>();
        totalPiedras = piedras.Length;
        piedrasDormidas = 0;

        if (totalPiedras == 0) return;

        // Comprobamos cuántas tienen sus físicas apagadas (Kinematic)
        foreach (DeformacionPiedra piedra in piedras)
        {
            Rigidbody rb = piedra.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic)
            {
                piedrasDormidas++;
            }
        }

        // Calculamos el porcentaje
        porcentajeDormidas = ((float)piedrasDormidas / totalPiedras) * 100f;
    }

    // OnGUI nos permite dibujar texto en pantalla sin tener que crear un Canvas complejo
    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle estilo = new GUIStyle();
        Rect rect = new Rect(15, 15, 350, 100);

        // Ajustamos el tamaño de la letra para que se lea bien
        estilo.alignment = TextAnchor.UpperLeft;
        estilo.fontSize = Mathf.Max(20, h * 2 / 100);
        estilo.normal.textColor = Color.white;

        float milisegundos = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;

        // Preparamos el texto
        string texto = string.Format("Rendimiento: {0:0.0} ms ({1:0.} FPS)\n", milisegundos, fps);
        texto += string.Format("Piedras Totales: {0}\n", totalPiedras);

        // Ponemos el porcentaje en Verde si está por encima del 90%, y en Rojo si está por debajo
        string colorDormidas = porcentajeDormidas > 90f ? "green" : "red";
        texto += string.Format("Piedras Dormidas: <color={0}>{1:0.0}%</color>", colorDormidas, porcentajeDormidas);

        // Dibujamos un fondo negro semitransparente para poder leer las letras blancas
        GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(5, 5, 350, 100), "");

        // Imprimimos el texto (permitiendo usar colores en el string)
        estilo.richText = true;
        GUI.Label(rect, texto, estilo);
    }
}