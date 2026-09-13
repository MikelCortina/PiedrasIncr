using UnityEngine;

/// <summary>
/// CAMBIOS: se añaden las propiedades publicas "CurrentYaw" y "CurrentPitch"
/// para que otros scripts (como PaperPlaneController) puedan leer el giro
/// horizontal y vertical actual de esta camara, y usarlos para inclinar el
/// avion tanto en Z (roll, por yaw de camara) como en X (pitch, por pitch de
/// camara). 
/// 
/// AHORA LA CÁMARA SE MUEVE SIEMPRE Y EL CURSOR ESTÁ OCULTO (Sin click derecho).
/// </summary>
public class ThirdPersonSwordCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform espada;
    [SerializeField] private Vector3 offsetObjetivo = new Vector3(0f, 0.5f, 0f);

    [Header("Distancia")]
    [SerializeField] private float distancia = 6f;
    [SerializeField] private float distanciaMinima = 2f;
    [SerializeField] private float distanciaMaxima = 12f;
    [SerializeField] private float velocidadZoom = 3f;

    [Header("Rotación")]
    [SerializeField] private float sensibilidadHorizontal = 180f;
    [SerializeField] private float sensibilidadVertical = 100f;
    [SerializeField] private float anguloMinimo = -20f;
    [SerializeField] private float anguloMaximo = 70f;

    [Header("Suavizado")]
    [SerializeField] private float suavizadoPosicion = 12f;
    [SerializeField] private float suavizadoRotacion = 15f;

    private float yaw;
    private float pitch = 20f;

    /// <summary>
    /// Yaw actual de la camara en grados (sin normalizar).
    /// </summary>
    public float CurrentYaw => yaw;

    /// <summary>
    /// Pitch actual de la camara en grados (ya recortado entre anguloMinimo y anguloMaximo). 
    /// </summary>
    public float CurrentPitch => pitch;

    private void Start()
    {
        Vector3 angulosIniciales = transform.eulerAngles;

        yaw = angulosIniciales.y;
        pitch = angulosIniciales.x;

        // Bloqueamos el cursor en el centro y lo ocultamos permanentemente
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Se ha eliminado el requisito de pulsar el click derecho.
        // Ahora la cámara lee el ratón en todos los frames.

        yaw += Input.GetAxis("Mouse X") * sensibilidadHorizontal * Time.deltaTime;

        pitch -= Input.GetAxis("Mouse Y") * sensibilidadVertical * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, anguloMinimo, anguloMaximo);

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        distancia -= scroll * velocidadZoom;
        distancia = Mathf.Clamp(
            distancia,
            distanciaMinima,
            distanciaMaxima
        );
    }

    private void LateUpdate()
    {
        if (espada == null)
            return;

        Vector3 objetivo = espada.position + offsetObjetivo;

        Quaternion rotacionOrbital = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 posicionDeseada =
            objetivo + rotacionOrbital * Vector3.back * distancia;

        transform.position = Vector3.Lerp(
            transform.position,
            posicionDeseada,
            suavizadoPosicion * Time.deltaTime
        );

        Quaternion rotacionDeseada = Quaternion.LookRotation(
            objetivo - transform.position,
            Vector3.up
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotacionDeseada,
            suavizadoRotacion * Time.deltaTime
        );
    }
}