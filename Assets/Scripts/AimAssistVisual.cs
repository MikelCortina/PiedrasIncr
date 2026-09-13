using UnityEngine;

public class AimAssistVisual : MonoBehaviour
{
    public static AimAssistVisual Instancia;

    [Header("Referencias")]
    public RectTransform mirilla;
    public Camera camaraPrincipal;

    [Header("Ajustes del Escáner")]
    [Tooltip("Tecla que hay que mantener pulsada para activar el visor y mostrar la mirilla de escaneo.")]
    public KeyCode teclaEscanear = KeyCode.Mouse1; // Por defecto: Clic Derecho del ratón
    public string tagPiedra = "Piedra";
    public float distanciaMaximaReal = 15f;

    [Header("Movimiento y Animación")]
    public float velocidadTransicion = 10f;
    public float velocidadRotacion = 150f;

    [Header("Efecto de Delay (Sway de Cámara)")]
    public float intensidadDelay = 15f;
    public float velocidadRecuperacion = 15f;

    private Vector2 posicionOriginal;
    private Quaternion rotacionOriginal;
    private Vector3 rotacionAnteriorCamara;
    private Vector2 offsetSway;

    public Transform objetivoActual;
    private float porcentajeDesgasteActual = 0f;
    private bool escaneandoActivo = false;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else { Destroy(gameObject); }
    }

    void Start()
    {
        posicionOriginal = Vector2.zero;
        rotacionOriginal = mirilla.localRotation;

        if (camaraPrincipal != null)
        {
            rotacionAnteriorCamara = camaraPrincipal.transform.eulerAngles;
        }

        // Al iniciar, nos aseguramos de que la mirilla empiece oculta si no se escanea
        if (mirilla != null)
        {
            mirilla.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Comprobamos si el jugador MANTIENE PULSADA la tecla de escaneo
        escaneandoActivo = Input.GetKey(teclaEscanear);

        // Activamos o desactivamos la visibilidad de la mirilla según se pulse la tecla
        if (mirilla != null && mirilla.gameObject.activeSelf != escaneandoActivo)
        {
            mirilla.gameObject.SetActive(escaneandoActivo);
        }

        if (escaneandoActivo)
        {
            CalcularDelayDeCamara();
            BuscarPiedraMasCentrada();

            if (objetivoActual != null)
            {
                Vector3 centroPiedra = objetivoActual.position;
                Collider col = objetivoActual.GetComponentInChildren<Collider>();
                if (col != null) centroPiedra = col.bounds.center;

                Vector3 posPantalla = camaraPrincipal.WorldToScreenPoint(centroPiedra);

                mirilla.position = Vector3.Lerp(mirilla.position, posPantalla, Time.deltaTime * velocidadTransicion);
                mirilla.Rotate(0f, 0f, velocidadRotacion * Time.deltaTime);

                // Obtenemos el porcentaje de desgaste hacia la esfera primitiva central
                DeformacionPiedra piedraScript = objetivoActual.GetComponent<DeformacionPiedra>();
                if (piedraScript != null)
                {
                    porcentajeDesgasteActual = piedraScript.ObtenerPorcentajeDesgasteHaciaEsfera();
                }
            }
            else
            {
                DevolverMirillaAlCentro();
            }
        }
        else
        {
            // Si suelta la tecla, limpiamos el objetivo actual
            objetivoActual = null;
        }
    }

    void DevolverMirillaAlCentro()
    {
        Vector2 centroConDelay = posicionOriginal + offsetSway;
        mirilla.anchoredPosition = Vector2.Lerp(mirilla.anchoredPosition, centroConDelay, Time.deltaTime * velocidadTransicion);
        mirilla.localRotation = Quaternion.Lerp(mirilla.localRotation, rotacionOriginal, Time.deltaTime * velocidadTransicion);
    }

    void CalcularDelayDeCamara()
    {
        Vector3 rotacionActual = camaraPrincipal.transform.eulerAngles;
        float deltaYaw = Mathf.DeltaAngle(rotacionAnteriorCamara.y, rotacionActual.y);
        float deltaPitch = Mathf.DeltaAngle(rotacionAnteriorCamara.x, rotacionActual.x);
        rotacionAnteriorCamara = rotacionActual;

        Vector2 targetSway = new Vector2(-deltaYaw, deltaPitch) * intensidadDelay;
        if (objetivoActual != null) targetSway = Vector2.zero;

        offsetSway = Vector2.Lerp(offsetSway, targetSway, Time.deltaTime * velocidadRecuperacion);
    }

    void BuscarPiedraMasCentrada()
    {
        GameObject[] piedras = GameObject.FindGameObjectsWithTag(tagPiedra);
        Transform mejorObjetivo = null;
        float menorDistanciaAlCentro = float.MaxValue;

        Vector2 centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f);

        foreach (GameObject piedra in piedras)
        {
            Collider col = piedra.GetComponentInChildren<Collider>();
            if (col == null) continue;

            float distanciaReal = Vector3.Distance(camaraPrincipal.transform.position, col.bounds.center);
            if (distanciaReal > distanciaMaximaReal) continue;

            Vector3 centroMundo = col.bounds.center;
            Vector3 posPantalla = camaraPrincipal.WorldToScreenPoint(centroMundo);

            if (posPantalla.z > 0)
            {
                Vector2 posPiedra2D = new Vector2(posPantalla.x, posPantalla.y);
                float distanciaAlCentro = Vector2.Distance(centroPantalla, posPiedra2D);

                Vector3 puntoExtremoMundo = centroMundo + (col.bounds.extents.magnitude * Vector3.right);
                Vector3 extrePantalla = camaraPrincipal.WorldToScreenPoint(puntoExtremoMundo);
                float radioEnPantalla = Mathf.Abs(extrePantalla.x - posPantalla.x);
                float radioDeCapturaDinamico = Mathf.Max(50f, radioEnPantalla);

                if (distanciaAlCentro <= radioDeCapturaDinamico)
                {
                    if (distanciaAlCentro < menorDistanciaAlCentro)
                    {
                        menorDistanciaAlCentro = distanciaAlCentro;
                        mejorObjetivo = piedra.transform;
                    }
                }
            }
        }
        objetivoActual = mejorObjetivo;
    }

    // Dibujamos el panel HUD en pantalla con la información de la piedra escaneada
    void OnGUI()
    {
        // Solo mostramos la interfaz si estamos manteniendo pulsada la tecla Y apuntando a una piedra
        if (!escaneandoActivo || objetivoActual == null) return;

        int w = Screen.width, h = Screen.height;
        GUIStyle estilo = new GUIStyle();
        estilo.fontSize = Mathf.Max(16, h * 2 / 100);
        estilo.normal.textColor = Color.yellow;
        estilo.alignment = TextAnchor.MiddleCenter;

        // Posicionamos la ventanita de info justo encima de la mirilla
        Rect rect = new Rect(w / 2f - 150, (h / 2f) - 70, 300, 50);

        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUI.Box(rect, "");

        string textoInfo = string.Format("Desgaste a Esfera: {0:0.0}%", porcentajeDesgasteActual);
        if (porcentajeDesgasteActual >= 95f)
        {
            textoInfo += "\n<color=green>¡Núcleo Alcanzado (Listo)!</color>";
        }
        else
        {
            textoInfo += "\n<color=orange>Falta Erosión</color>";
        }

        estilo.richText = true;
        GUI.Label(rect, textoInfo, estilo);
    }
}