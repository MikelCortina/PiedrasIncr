using UnityEngine;

public class PlaneTelemetryUI : MonoBehaviour
{
    [Header("Configuración de UI")]
    public bool mostrarTelemetria = true;
    public int tamanoFuente = 20;
    public Color colorTexto = Color.green;

    [Header("Suavizado y Refresco")]
    [Tooltip("Veces por segundo que se actualiza el texto en pantalla (para que sea legible)")]
    public float refrescosPorSegundo = 10f;
    [Tooltip("Fuerza del suavizado de los datos (menor = más estable, mayor = más sensible)")]
    public float suavizadoLectura = 2f;

    private Vector3 lastPosition;

    // Valores mostrados (suavizados)
    private float displayVelTotal;
    private float displayVelHoriz;
    private float displayVelVert;

    private float displayAccTotal;
    private float displayAccHoriz;
    private float displayAccVert;
    private Vector3 displayDir;

    // Historial para aceleración interna
    private float ultimaVelTotal;
    private float ultimaVelHoriz;
    private float ultimaVelVert;

    // Variables para controlar cuándo se actualiza el texto
    private string textoEnPantalla = "";
    private float timerRefresco = 0f;

    private void Start()
    {
        // Al empezar, tomamos la posición inicial para evitar picos raros en el frame 1
        lastPosition = transform.position;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0) return;

        // 1. Calcular velocidad cruda (Posición actual - Posición anterior)
        Vector3 velCruda = (transform.position - lastPosition) / dt;
        lastPosition = transform.position;

        float velTotalCruda = velCruda.magnitude;
        float velHorizCruda = new Vector3(velCruda.x, 0, velCruda.z).magnitude;
        float velVertCruda = velCruda.y;

        // 2. Calcular aceleración cruda
        float accTotalCruda = (velTotalCruda - ultimaVelTotal) / dt;
        float accHorizCruda = (velHorizCruda - ultimaVelHoriz) / dt;
        float accVertCruda = (velVertCruda - ultimaVelVert) / dt;

        // 3. SUAVIZADO: Evita los picos causados por la rotación de la espada
        displayVelTotal = Mathf.Lerp(displayVelTotal, velTotalCruda, dt * suavizadoLectura);
        displayVelHoriz = Mathf.Lerp(displayVelHoriz, velHorizCruda, dt * suavizadoLectura);
        displayVelVert = Mathf.Lerp(displayVelVert, velVertCruda, dt * suavizadoLectura);

        displayAccTotal = Mathf.Lerp(displayAccTotal, accTotalCruda, dt * suavizadoLectura);
        displayAccHoriz = Mathf.Lerp(displayAccHoriz, accHorizCruda, dt * suavizadoLectura);
        displayAccVert = Mathf.Lerp(displayAccVert, accVertCruda, dt * suavizadoLectura);

        if (velCruda.sqrMagnitude > 0.01f)
        {
            displayDir = Vector3.Lerp(displayDir, velCruda.normalized, dt * suavizadoLectura);
        }

        // Guardar para el siguiente frame
        ultimaVelTotal = velTotalCruda;
        ultimaVelHoriz = velHorizCruda;
        ultimaVelVert = velVertCruda;

        // 4. LIMITADOR DE REFRESCO: Solo cambia el texto X veces por segundo
        timerRefresco -= dt;
        if (timerRefresco <= 0f)
        {
            timerRefresco = 1f / refrescosPorSegundo;
            GenerarTexto();
        }
    }

    private void GenerarTexto()
    {
        // Usamos :F1 para mostrar solo 1 decimal (ej: 45.2 m/s), eliminando el caos visual
        textoEnPantalla = "--- TELEMETRÍA DEL AVIÓN ---\n\n" +
                          $"[VELOCIDAD]\n" +
                          $"Total: {displayVelTotal:F1} m/s\n" +
                          $"Horizontal (XZ): {displayVelHoriz:F1} m/s\n" +
                          $"Vertical (Y): {displayVelVert:F1} m/s\n" +
                          $"Dir: (X: {displayDir.x:F1}, Y: {displayDir.y:F1}, Z: {displayDir.z:F1})\n\n" +
                          $"[ACELERACIÓN]\n" +
                          $"Total: {displayAccTotal:F1} m/s²\n" +
                          $"Horizontal (XZ): {displayAccHoriz:F1} m/s²\n" +
                          $"Vertical (Y): {displayAccVert:F1} m/s²";
    }

    private void OnGUI()
    {
        if (!mostrarTelemetria || string.IsNullOrEmpty(textoEnPantalla)) return;

        GUIStyle estilo = new GUIStyle();
        estilo.fontSize = tamanoFuente;
        estilo.normal.textColor = colorTexto;
        estilo.fontStyle = FontStyle.Bold;

        GUIStyle sombra = new GUIStyle(estilo);
        sombra.normal.textColor = Color.black;

        // Dibujar el texto en pantalla (Primero sombra, luego color)
        GUI.Label(new Rect(22, 22, 500, 500), textoEnPantalla, sombra);
        GUI.Label(new Rect(20, 20, 500, 500), textoEnPantalla, estilo);
    }
}