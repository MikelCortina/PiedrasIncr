using UnityEngine;

public class CamaraPrimeraPersona : MonoBehaviour
{
    [Header("Referencias Externas")]
    [Tooltip("Arrastra aquí el objeto que tiene el script SistemaConstruccion")]
    public SistemaConstruccion sistemaConstruccion;


    [Header("Ajustes de Cámara")]
    public float sensibilidadRaton = 2f;
    public Transform cuerpoJugador;


    [Header("Balanceo de Cabeza")]
    public float velocidadBobbing = 12f;
    public float amplitudBobbing = 0.05f;
    public float velocidadRetorno = 5f;


    [Header("Inclinación Lateral")]
    public float amplitudInclinacion = 2.5f;
    public float velocidadInclinacion = 6f;


    // =====================================================
    // BLOQUEO EXTERNO
    // =====================================================

    [Header("Bloqueo")]

    [SerializeField]
    private bool camaraBloqueadaPorUI = false;


    // =====================================================
    // INTERNAS
    // =====================================================

    private float rotacionX = 0f;
    private float posicionYOriginal;
    private float temporizadorBobbing = 0f;
    private float inclinacionZActual = 0f;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;

        posicionYOriginal =
            transform.localPosition.y;
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // =================================================
        // BLOQUEO TOTAL POR UI
        // =================================================

        if (camaraBloqueadaPorUI)
        {
            return;
        }


        // =================================================
        // MOVIMIENTO
        // =================================================

        float h =
            Input.GetAxis("Horizontal");

        float v =
            Input.GetAxis("Vertical");


        // =================================================
        // BLOQUEO POR SISTEMA DE CONSTRUCCIÓN
        // =================================================

        bool bloquearPorConstruccion =
            sistemaConstruccion != null &&
            sistemaConstruccion.modoConstruccion &&
            Input.GetMouseButton(1);


        // =================================================
        // RATÓN
        // =================================================

        if (!bloquearPorConstruccion)
        {
            float mouseX =
                Input.GetAxis("Mouse X") *
                sensibilidadRaton;

            float mouseY =
                Input.GetAxis("Mouse Y") *
                sensibilidadRaton;


            rotacionX -=
                mouseY;


            rotacionX =
                Mathf.Clamp(
                    rotacionX,
                    -90f,
                    90f
                );


            if (cuerpoJugador != null)
            {
                cuerpoJugador.Rotate(
                    Vector3.up *
                    mouseX
                );
            }
        }


        // =================================================
        // INCLINACIÓN
        // =================================================

        float inclinacionZObjetivo =
            -h *
            amplitudInclinacion;


        inclinacionZActual =
            Mathf.Lerp(
                inclinacionZActual,
                inclinacionZObjetivo,
                Time.deltaTime *
                velocidadInclinacion
            );


        transform.localRotation =
            Quaternion.Euler(
                rotacionX,
                0f,
                inclinacionZActual
            );


        // =================================================
        // HEAD BOBBING
        // =================================================

        if (Mathf.Abs(h) > 0.1f ||
            Mathf.Abs(v) > 0.1f)
        {
            temporizadorBobbing +=
                Time.deltaTime *
                velocidadBobbing;


            float nuevaY =
                posicionYOriginal +
                Mathf.Sin(
                    temporizadorBobbing
                ) *
                amplitudBobbing;


            transform.localPosition =
                new Vector3(
                    transform.localPosition.x,
                    nuevaY,
                    transform.localPosition.z
                );
        }
        else
        {
            temporizadorBobbing =
                0f;


            float nuevaY =
                Mathf.Lerp(
                    transform.localPosition.y,
                    posicionYOriginal,
                    Time.deltaTime *
                    velocidadRetorno
                );


            transform.localPosition =
                new Vector3(
                    transform.localPosition.x,
                    nuevaY,
                    transform.localPosition.z
                );
        }
    }


    // =====================================================
    // BLOQUEAR DESDE OTROS SCRIPTS
    // =====================================================

    public void SetBloqueadaPorUI(
        bool bloqueada)
    {
        camaraBloqueadaPorUI =
            bloqueada;
    }


    public bool EstaBloqueadaPorUI()
    {
        return camaraBloqueadaPorUI;
    }
}