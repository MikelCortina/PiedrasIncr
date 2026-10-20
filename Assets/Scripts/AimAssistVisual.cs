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

    [Header("Elementos de UI (Canvas)")]
    [Tooltip("Arrastra aquí otros paneles, iconos o textos del Canvas que quieras que aparezcan al escanear.")]
    public RectTransform[] elementosUIExtras;
    [Tooltip("Velocidad a la que se inflan (pop in) y desinflan (pop out) los elementos del Canvas.")]
    public float velocidadPopUI = 15f;

    [Header("Efecto de Respiración (Latido)")]
    [Tooltip("Qué tan rápido respira la mirilla cuando apunta a un objetivo.")]
    public float velocidadRespiracion = 4f;
    [Tooltip("Cuánto crece/se encoge en cada latido (0.1 = 10% de su tamaño).")]
    public float amplitudRespiracion = 0.08f;

    [Header("Movimiento y Animación")]
    public float velocidadTransicion = 10f;
    public float velocidadRotacion = 150f;

    [Header("Efecto de Delay (Sway de Cámara)")]
    public float intensidadDelay = 15f;
    public float velocidadRecuperacion = 15f;

    // Variables internas para la UI
    private Vector3 escalaOriginalMirilla;
    private Vector3[] escalasOriginalesExtras;

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
        rotacionOriginal = mirilla != null ? mirilla.localRotation : Quaternion.identity;

        if (camaraPrincipal != null)
        {
            rotacionAnteriorCamara = camaraPrincipal.transform.eulerAngles;
        }

        // ==========================================
        // GUARDAMOS ESCALAS ORIGINALES Y OCULTAMOS
        // ==========================================
        if (mirilla != null)
        {
            escalaOriginalMirilla = mirilla.localScale;
            mirilla.localScale = Vector3.zero;
            mirilla.gameObject.SetActive(false);
        }

        escalasOriginalesExtras = new Vector3[elementosUIExtras.Length];
        for (int i = 0; i < elementosUIExtras.Length; i++)
        {
            if (elementosUIExtras[i] != null)
            {
                escalasOriginalesExtras[i] = elementosUIExtras[i].localScale;
                elementosUIExtras[i].localScale = Vector3.zero;
                elementosUIExtras[i].gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        // Comprobamos si estamos en modo construcción
        bool enModoConstruccion = SistemaConstruccion.Instancia != null && SistemaConstruccion.Instancia.modoConstruccion;

        // Comprobamos si el jugador MANTIENE PULSADA la tecla de escaneo (y NO está construyendo)
        escaneandoActivo = !enModoConstruccion && Input.GetKey(teclaEscanear);

        // Variables maestras para decidir qué hacer con la mirilla al final del frame
        bool mostrarMirilla = escaneandoActivo;
        Vector3 escalaObjetivoMirilla = escalaOriginalMirilla;

        // ==========================================
        // LÓGICA DE ESCANEO Y APUNTADO
        // ==========================================
        if (escaneandoActivo || enModoConstruccion)
        {
            CalcularDelayDeCamara();

            if (enModoConstruccion)
            {
                // En modo construcción, el objetivo es la estructura apuntada (si la hay)
                if (SistemaConstruccion.Instancia != null && SistemaConstruccion.Instancia.edificioApuntado != null)
                {
                    objetivoActual = SistemaConstruccion.Instancia.edificioApuntado.transform;
                }
                else
                {
                    objetivoActual = null;
                }
            }
            else
            {
                // En modo escáner, buscamos la piedra
                BuscarPiedraMasCentrada();
            }

            if (objetivoActual != null)
            {
                Vector3 centroObjetivo = objetivoActual.position; // PIVOTE POR DEFECTO

                if (enModoConstruccion)
                {
                    // Al apuntar a un edificio, usamos SU PIVOTE (transform.position) y forzamos la mirilla grande
                    centroObjetivo = objetivoActual.position;
                    mostrarMirilla = true;
                    escalaObjetivoMirilla = escalaOriginalMirilla * 1.5f; // Un 50% más grande para abrazar la estructura
                }
                else
                {
                    // Al apuntar a una piedra, usamos EL CENTRO DEL COLLIDER
                    Collider col = objetivoActual.GetComponentInChildren<Collider>();
                    if (col != null) centroObjetivo = col.bounds.center;
                }

                Vector3 posPantalla = camaraPrincipal.WorldToScreenPoint(centroObjetivo);

                if (mirilla != null)
                {
                    mirilla.position = Vector3.Lerp(mirilla.position, posPantalla, Time.deltaTime * velocidadTransicion);
                    mirilla.Rotate(0f, 0f, velocidadRotacion * Time.deltaTime);
                }

                if (!enModoConstruccion)
                {
                    // Obtenemos el porcentaje de desgaste hacia la esfera primitiva central (SOLO PIEDRAS)
                    DeformacionPiedra piedraScript = objetivoActual.GetComponent<DeformacionPiedra>();
                    if (piedraScript != null)
                    {
                        porcentajeDesgasteActual = piedraScript.ObtenerPorcentajeDesgasteHaciaEsfera();
                    }
                }
            }
            else
            {
                DevolverMirillaAlCentro();
            }
        }
        else
        {
            // Si suelta la tecla o sale de construcción, limpiamos el objetivo actual
            objetivoActual = null;
            DevolverMirillaAlCentro(); // Para que vuelva al centro mientras se hace pequeña
        }

        // ==========================================
        // ANIMACIÓN "POP" DE LA MIRILLA Y LOS EXTRAS
        // ==========================================

        // 1. Añadimos la respiración orgánica a la mirilla si está enfocando un objetivo
        if (mostrarMirilla && objetivoActual != null)
        {
            float pulso = Mathf.Sin(Time.time * velocidadRespiracion) * amplitudRespiracion;
            escalaObjetivoMirilla *= (1f + pulso); // Multiplicamos por (1 ± amplitud)
        }

        // 2. Aplicamos la animación final a la mirilla con su tamaño (y respiración)
        AnimarElementoUIPersonalizado(mirilla, escalaObjetivoMirilla, mostrarMirilla);

        // 3. Los elementos extra (el Canvas general) se muestran tanto si escaneamos como si construimos
        for (int i = 0; i < elementosUIExtras.Length; i++)
        {
            AnimarElementoUIPersonalizado(elementosUIExtras[i], escalasOriginalesExtras[i], escaneandoActivo || enModoConstruccion);
        }
    }

    // ==========================================
    // MÉTODO PARA INFLAR/DESINFLAR UI
    // ==========================================
    void AnimarElementoUIPersonalizado(RectTransform elemento, Vector3 escalaObjetivoConcreta, bool mostrar)
    {
        if (elemento == null) return;

        Vector3 escalaFinal = mostrar ? escalaObjetivoConcreta : Vector3.zero;

        // Si queremos mostrarlo y está apagado, lo encendemos de inmediato
        if (mostrar && !elemento.gameObject.activeSelf)
        {
            elemento.gameObject.SetActive(true);
        }

        // Interpolamos el tamaño para crear el efecto "Pop" y absorber la respiración suavemente
        elemento.localScale = Vector3.Lerp(elemento.localScale, escalaFinal, Time.deltaTime * velocidadPopUI);

        // Si ya es minúsculo (invisible) y no queremos mostrarlo, lo apagamos para ahorrar rendimiento
        if (!mostrar && elemento.localScale.sqrMagnitude < 0.001f && elemento.gameObject.activeSelf)
        {
            elemento.localScale = Vector3.zero;
            elemento.gameObject.SetActive(false);
        }
    }

    void DevolverMirillaAlCentro()
    {
        if (mirilla == null) return;
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