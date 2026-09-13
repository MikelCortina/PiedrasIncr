using UnityEngine;

public class CamaraPrimeraPersona : MonoBehaviour
{
    [Header("Referencias Externas")] // ¡NUEVO!
    [Tooltip("Arrastra aquí el objeto que tiene el script SistemaConstruccion")]
    public SistemaConstruccion sistemaConstruccion;

    [Header("Ajustes de Cámara")]
    public float sensibilidadRaton = 2f;
    public Transform cuerpoJugador;

    [Header("Balanceo de Cabeza (Head Bobbing)")]
    public float velocidadBobbing = 12f;
    public float amplitudBobbing = 0.05f;
    public float velocidadRetorno = 5f;

    [Header("Inclinación Lateral (Tilt)")]
    [Tooltip("Grados máximos que se inclinará la cámara al caminar de lado")]
    public float amplitudInclinacion = 2.5f;
    [Tooltip("Qué tan rápido se inclina y vuelve a su sitio")]
    public float velocidadInclinacion = 6f;

    private float rotacionX = 0f;
    private float posicionYOriginal;
    private float temporizadorBobbing = 0f;
    private float inclinacionZActual = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        posicionYOriginal = transform.localPosition.y;
    }

    void Update()
    {
        // --- 1. LECTURA DE CONTROLES DE MOVIMIENTO ---
        float h = Input.GetAxis("Horizontal"); // -1 (Izquierda) a 1 (Derecha)
        float v = Input.GetAxis("Vertical");

        // --- ¡NUEVO! 2. LÓGICA DE BLOQUEO DE CÁMARA ---
        // Comprobamos si el modo construcción está abierto Y estamos pulsando click derecho
        bool bloquearCamara = (sistemaConstruccion != null && sistemaConstruccion.modoConstruccion && Input.GetMouseButton(1));

        // Solo permitimos girar la cámara si no está bloqueada
        if (!bloquearCamara)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensibilidadRaton;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidadRaton;

            rotacionX -= mouseY;
            rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);

            // Giramos el cuerpo del personaje con el ratón
            cuerpoJugador.Rotate(Vector3.up * mouseX);
        }

        // --- 3. INCLINACIÓN (TILT) ---
        float inclinacionZObjetivo = -h * amplitudInclinacion;
        inclinacionZActual = Mathf.Lerp(inclinacionZActual, inclinacionZObjetivo, Time.deltaTime * velocidadInclinacion);

        // Aplicamos la rotación a la cámara (rotacionX se quedará congelada si está bloqueada)
        transform.localRotation = Quaternion.Euler(rotacionX, 0f, inclinacionZActual);

        // --- 4. BALANCEO DE CABEZA ---
        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
            temporizadorBobbing += Time.deltaTime * velocidadBobbing;
            float nuevaY = posicionYOriginal + (Mathf.Sin(temporizadorBobbing) * amplitudBobbing);
            transform.localPosition = new Vector3(transform.localPosition.x, nuevaY, transform.localPosition.z);
        }
        else
        {
            temporizadorBobbing = 0f;
            float nuevaY = Mathf.Lerp(transform.localPosition.y, posicionYOriginal, Time.deltaTime * velocidadRetorno);
            transform.localPosition = new Vector3(transform.localPosition.x, nuevaY, transform.localPosition.z);
        }
    }
}