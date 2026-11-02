using System.Collections.Generic;
using UnityEngine;

// Cámara libre para grabar el tráiler: con la tecla (F2) se suelta la cámara principal del jugador y se vuela por la escena.
// Mientras está activa se ocultan los Canvas (HUD) y se desactivan el control del jugador y de su cámara.
// Al desactivarla todo vuelve exactamente como estaba.
// Controles: WASD mover, E / Q subir / bajar, ratón mirar, Shift rápido, Ctrl lento, rueda del ratón = velocidad.
public class CamaraLibreTrailer : MonoBehaviour
{
    [Header("Activación")]
    public KeyCode teclaActivar = KeyCode.F2;
    [Tooltip("Cámara a mover (vacío = la cámara principal).")]
    public Camera camara;

    [Header("Movimiento")]
    public float velocidad = 6f;
    public float multiplicadorRapido = 4f;
    public float multiplicadorLento = 0.25f;
    [Tooltip("Cuánto tarda en acelerar y frenar (más alto = más brusco). Suave queda más cinematográfico.")]
    public float suavizadoMovimiento = 4f;
    public KeyCode teclaSubir = KeyCode.E;
    public KeyCode teclaBajar = KeyCode.Q;

    [Header("Mirada")]
    public float sensibilidadRaton = 2f;
    [Tooltip("Cuánto tarda la mirada en seguir al ratón (más alto = más directo).")]
    public float suavizadoRotacion = 8f;

    [Header("Qué se desactiva")]
    [Tooltip("Oculta todos los Canvas raíz de la escena mientras la cámara libre está activa.")]
    public bool ocultarCanvas = true;
    [Tooltip("Componentes extra a desactivar (otros HUD, scripts de control...). El control del jugador y de su cámara se desactivan solos.")]
    public Behaviour[] componentesADesactivar;

    public bool Activa { get; private set; }

    private readonly List<Behaviour> desactivados = new List<Behaviour>();
    private readonly List<Canvas> canvasOcultos = new List<Canvas>();

    private Transform padreOriginal;
    private Vector3 posicionLocalOriginal;
    private Quaternion rotacionLocalOriginal;
    private CursorLockMode cursorOriginal;
    private bool cursorVisibleOriginal;

    private Vector3 velocidadActual;
    private float yawObjetivo, pitchObjetivo, yaw, pitch;

    void Update()
    {
        if (Input.GetKeyDown(teclaActivar))
        {
            if (Activa) Desactivar();
            else Activar();
        }

        if (Activa) Mover();
    }

    void OnDisable()
    {
        if (Activa) Desactivar();
    }

    void Activar()
    {
        Camera cam = camara != null ? camara : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[CamaraLibreTrailer] No hay cámara que mover.");
            return;
        }
        camara = cam;
        Transform t = camara.transform;

        padreOriginal = t.parent;
        posicionLocalOriginal = t.localPosition;
        rotacionLocalOriginal = t.localRotation;
        cursorOriginal = Cursor.lockState;
        cursorVisibleOriginal = Cursor.visible;

        // Control del jugador y de su cámara.
        DesactivarSiActivo(t.GetComponent<CamaraPrimeraPersona>());
        DesactivarSiActivo(FindAnyObjectByType<MovimientoPersonaje>());
        if (componentesADesactivar != null)
            foreach (Behaviour componente in componentesADesactivar) DesactivarSiActivo(componente);

        if (ocultarCanvas)
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || !canvas.enabled) continue;
                canvas.enabled = false;
                canvasOcultos.Add(canvas);
            }
        }

        t.SetParent(null, true);

        Vector3 angulos = t.eulerAngles;
        yaw = yawObjetivo = angulos.y;
        pitch = pitchObjetivo = angulos.x > 180f ? angulos.x - 360f : angulos.x;
        velocidadActual = Vector3.zero;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Activa = true;
        Debug.Log("[CamaraLibreTrailer] Cámara libre ACTIVADA (" + teclaActivar + " para volver).");
    }

    void Desactivar()
    {
        Activa = false;

        if (camara != null)
        {
            Transform t = camara.transform;
            t.SetParent(padreOriginal, false);
            t.localPosition = posicionLocalOriginal;
            t.localRotation = rotacionLocalOriginal;
        }

        foreach (Canvas canvas in canvasOcultos)
            if (canvas != null) canvas.enabled = true;
        canvasOcultos.Clear();

        foreach (Behaviour componente in desactivados)
            if (componente != null) componente.enabled = true;
        desactivados.Clear();

        Cursor.lockState = cursorOriginal;
        Cursor.visible = cursorVisibleOriginal;

        Debug.Log("[CamaraLibreTrailer] Cámara libre desactivada.");
    }

    void DesactivarSiActivo(Behaviour componente)
    {
        if (componente == null || !componente.enabled) return;
        componente.enabled = false;
        desactivados.Add(componente);
    }

    void Mover()
    {
        // Tiempo de juego: con el GrabadorTrailer a ritmo constante, el movimiento sale igual de fluido en el vídeo.
        // Si el juego está en pausa (timeScale 0) se usa el tiempo real para poder seguir moviéndose.
        float dt = Time.timeScale > 0.01f ? Time.deltaTime : Time.unscaledDeltaTime;
        Transform t = camara.transform;

        // Mirada suavizada.
        yawObjetivo += Input.GetAxis("Mouse X") * sensibilidadRaton;
        pitchObjetivo = Mathf.Clamp(pitchObjetivo - Input.GetAxis("Mouse Y") * sensibilidadRaton, -89f, 89f);
        float factorRotacion = 1f - Mathf.Exp(-suavizadoRotacion * dt);
        yaw = Mathf.LerpAngle(yaw, yawObjetivo, factorRotacion);
        pitch = Mathf.Lerp(pitch, pitchObjetivo, factorRotacion);
        t.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // La rueda ajusta la velocidad base (útil para pasar de planos lentos a vuelos rápidos).
        float rueda = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rueda) > 0.01f) velocidad = Mathf.Clamp(velocidad * Mathf.Pow(1.15f, rueda), 0.1f, 500f);

        Vector3 entrada = new Vector3(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
            (Input.GetKey(teclaSubir) ? 1f : 0f) - (Input.GetKey(teclaBajar) ? 1f : 0f),
            (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        if (entrada.sqrMagnitude > 1f) entrada.Normalize();

        float multiplicador = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) multiplicador = multiplicadorRapido;
        else if (Input.GetKey(KeyCode.LeftControl)) multiplicador = multiplicadorLento;

        Vector3 objetivo = (t.right * entrada.x + Vector3.up * entrada.y + t.forward * entrada.z) * velocidad * multiplicador;
        velocidadActual = Vector3.Lerp(velocidadActual, objetivo, 1f - Mathf.Exp(-suavizadoMovimiento * dt));
        t.position += velocidadActual * dt;
    }
}
