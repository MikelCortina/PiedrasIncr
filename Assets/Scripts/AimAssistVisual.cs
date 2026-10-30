using System.Collections.Generic;
using System.Globalization;
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

    [Header("Panel de desgaste")]
    [Tooltip("Tecla que muestra u oculta el desgaste de todas las piedras a cualquier distancia.")]
    public KeyCode teclaMostrarTodas = KeyCode.P;
    [Tooltip("Muestra una etiqueta con el desgaste sobre todas las piedras, a cualquier distancia (se alterna con la tecla).")]
    public bool mostrarDesgasteTodas = false;
    [Tooltip("Fuente del panel (vacío = la de por defecto).")]
    public Font fuentePanel;
    [Tooltip("Tamaño del panel. Además se adapta a la resolución (referencia 1080p).")]
    public float escalaPanel = 1f;
    [Tooltip("Desgaste a partir del cual la piedra se considera lista.")]
    public float porcentajeListo = 95f;
    public Color colorFondoPanel = new Color(0.04f, 0.06f, 0.1f, 0.82f);
    public Color colorAcento = new Color(0.35f, 0.85f, 1f, 1f);
    public Color colorDesgasteBajo = new Color(1f, 0.45f, 0.2f, 1f);
    public Color colorDesgasteMedio = new Color(1f, 0.82f, 0.25f, 1f);
    public Color colorListo = new Color(0.35f, 1f, 0.55f, 1f);
    [Tooltip("Velocidad de aparición y desaparición del panel.")]
    public float velocidadFundidoPanel = 6f;
    [Tooltip("Velocidad a la que el porcentaje cuenta hasta su valor real.")]
    public float velocidadContador = 6f;
    [Tooltip("Cada cuántos segundos se vuelve a buscar la lista de piedras en el modo de todas las piedras.")]
    public float intervaloBusquedaPiedras = 0.5f;

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

    // Panel de desgaste
    private struct EtiquetaPiedra
    {
        public Vector2 posicion;
        public float porcentaje;
        public float distancia;
    }

    private static readonly System.Comparison<EtiquetaPiedra> OrdenLejanasPrimero = (a, b) => b.distancia.CompareTo(a.distancia);

    private readonly List<DeformacionPiedra> piedrasCacheadas = new List<DeformacionPiedra>();
    private readonly List<Collider> collidersCacheados = new List<Collider>();
    private readonly List<EtiquetaPiedra> etiquetas = new List<EtiquetaPiedra>();
    private float siguienteBusquedaPiedras;
    private Transform objetivoPanel;
    private float porcentajeMostrado;
    private float alfaPanel;
    private Vector2 posicionPanel;
    private GUIStyle estiloTexto;

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
        if (Input.GetKeyDown(teclaMostrarTodas))
        {
            mostrarDesgasteTodas = !mostrarDesgasteTodas;
            siguienteBusquedaPiedras = 0f;
        }

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

        ActualizarPanel();
        ActualizarEtiquetas();
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

    // ==========================================
    // PANEL DE DESGASTE (HUD)
    // ==========================================

    void ActualizarPanel()
    {
        bool visible = escaneandoActivo && objetivoActual != null;
        alfaPanel = Mathf.MoveTowards(alfaPanel, visible ? 1f : 0f, Time.deltaTime * velocidadFundidoPanel);

        if (!visible)
        {
            // Al cerrarse del todo, la próxima piedra vuelve a "contar" desde cero.
            if (alfaPanel <= 0f) objetivoPanel = null;
            return;
        }

        Collider col = objetivoActual.GetComponentInChildren<Collider>();
        bool enPantalla = PosicionSobrePiedra(col, objetivoActual, out Vector2 posicion, out _);

        if (objetivoActual != objetivoPanel)
        {
            objetivoPanel = objetivoActual;
            porcentajeMostrado = 0f;
            if (enPantalla) posicionPanel = posicion;
        }

        porcentajeMostrado = Mathf.Lerp(porcentajeMostrado, porcentajeDesgasteActual, Time.deltaTime * velocidadContador);
        if (enPantalla)
            posicionPanel = Vector2.Lerp(posicionPanel, posicion, Time.deltaTime * velocidadTransicion);
    }

    // Etiquetas de todas las piedras (modo de la tecla P), sin límite de distancia.
    void ActualizarEtiquetas()
    {
        etiquetas.Clear();
        if (!mostrarDesgasteTodas || camaraPrincipal == null) return;

        if (Time.time >= siguienteBusquedaPiedras)
        {
            BuscarTodasLasPiedras();
            siguienteBusquedaPiedras = Time.time + intervaloBusquedaPiedras;
        }

        float margen = 100f;
        for (int i = 0; i < piedrasCacheadas.Count; i++)
        {
            DeformacionPiedra piedra = piedrasCacheadas[i];
            if (piedra == null || !piedra.isActiveAndEnabled) continue;

            // La piedra escaneada ya tiene el panel grande.
            if (alfaPanel > 0f && piedra.transform == objetivoPanel) continue;

            if (!PosicionSobrePiedra(collidersCacheados[i], piedra.transform, out Vector2 posicion, out float distancia)) continue;
            if (posicion.x < -margen || posicion.x > Screen.width + margen || posicion.y < -margen || posicion.y > Screen.height + margen) continue;

            etiquetas.Add(new EtiquetaPiedra
            {
                posicion = posicion,
                porcentaje = piedra.ObtenerPorcentajeDesgasteHaciaEsfera(),
                distancia = distancia
            });
        }

        // Las lejanas se dibujan primero para que las cercanas queden encima.
        etiquetas.Sort(OrdenLejanasPrimero);
    }

    void BuscarTodasLasPiedras()
    {
        piedrasCacheadas.Clear();
        collidersCacheados.Clear();

        foreach (GameObject objeto in GameObject.FindGameObjectsWithTag(tagPiedra))
        {
            DeformacionPiedra piedra = objeto.GetComponent<DeformacionPiedra>();
            if (piedra == null) continue;

            piedrasCacheadas.Add(piedra);
            collidersCacheados.Add(objeto.GetComponentInChildren<Collider>());
        }
    }

    // Punto justo encima de la piedra, en coordenadas de GUI (origen arriba a la izquierda).
    bool PosicionSobrePiedra(Collider col, Transform piedra, out Vector2 posicion, out float distancia)
    {
        Vector3 punto = col != null ? col.bounds.center + Vector3.up * col.bounds.extents.y : piedra.position;
        Vector3 pantalla = camaraPrincipal.WorldToScreenPoint(punto);

        posicion = new Vector2(pantalla.x, Screen.height - pantalla.y);
        distancia = pantalla.z;
        return pantalla.z > 0f;
    }

    void OnGUI()
    {
        // Todo se dibuja en el evento Repaint: el resto de eventos de OnGUI no pintan nada.
        if (Event.current.type != EventType.Repaint) return;
        if (alfaPanel <= 0f && etiquetas.Count == 0) return;

        PrepararEstilo();
        float escala = Screen.height / 1080f * escalaPanel;

        foreach (EtiquetaPiedra etiqueta in etiquetas)
            DibujarEtiqueta(etiqueta, escala);

        if (alfaPanel > 0f)
            DibujarPanel(escala);
    }

    void DibujarPanel(float e)
    {
        // Aparición con un pequeño "pop" (ease out).
        float a = 1f - Mathf.Pow(1f - alfaPanel, 3f);
        float pop = Mathf.Lerp(0.85f, 1f, a);

        float ancho = 300f * e * pop;
        float alto = 92f * e * pop;
        float x = Mathf.Clamp(posicionPanel.x - ancho * 0.5f, 10f, Screen.width - ancho - 10f);
        float y = Mathf.Clamp(posicionPanel.y - alto - 30f * e, 10f, Screen.height - alto - 10f);
        Rect r = new Rect(x, y, ancho, alto);

        float porcentaje = Mathf.Clamp(porcentajeMostrado, 0f, 100f);
        bool listo = porcentaje >= porcentajeListo;
        Color estado = ColorDesgaste(porcentaje);
        float pulso = listo ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
        float m = 16f * e * pop;

        // Fondo: sombra, tarjeta, borde y franja de acento a la izquierda.
        Rectangulo(new Rect(r.x + 4f * e, r.y + 5f * e, r.width, r.height), new Color(0f, 0f, 0f, 0.35f * a), 12f * e);
        Rectangulo(r, ConAlfa(colorFondoPanel, colorFondoPanel.a * a), 12f * e);
        Borde(r, ConAlfa(estado, 0.85f * a * pulso), 1.5f * e, 12f * e);
        Rectangulo(new Rect(r.x + 5f * e, r.y + 14f * e, 3f * e, r.height - 28f * e), ConAlfa(estado, a), 1.5f * e);

        // Título y porcentaje.
        Texto(new Rect(r.x + m, r.y + 10f * e, r.width - 2f * m, 18f * e), "EROSION SCAN", 13f * e, TextAnchor.UpperLeft, ConAlfa(colorAcento, 0.9f * a));
        Texto(new Rect(r.x + m, r.y + 4f * e, r.width - 2f * m, 40f * e), porcentaje.ToString("0.0", CultureInfo.InvariantCulture) + "%", 30f * e, TextAnchor.UpperRight, ConAlfa(Color.white, a));

        // Barra de progreso con la marca del punto en el que la piedra está lista.
        Rect barra = new Rect(r.x + m, r.y + 50f * e * pop, r.width - 2f * m, 8f * e);
        Rectangulo(barra, new Color(1f, 1f, 1f, 0.12f * a), 4f * e);
        float relleno = barra.width * porcentaje / 100f;
        if (relleno > 1f)
            Rectangulo(new Rect(barra.x, barra.y, relleno, barra.height), ConAlfa(estado, a * pulso), 4f * e);
        float marca = barra.x + barra.width * porcentajeListo / 100f;
        Rectangulo(new Rect(marca - e, barra.y - 3f * e, 2f * e, barra.height + 6f * e), new Color(1f, 1f, 1f, 0.6f * a), 0f);

        // Estado.
        float yEstado = barra.yMax + 8f * e;
        Rectangulo(new Rect(r.x + m, yEstado + 5f * e, 8f * e, 8f * e), ConAlfa(estado, a * pulso), 4f * e);
        Texto(new Rect(r.x + m + 14f * e, yEstado, r.width - 2f * m, 18f * e),
              listo ? "CORE REACHED  ·  READY" : "EROSION REQUIRED", 13f * e, TextAnchor.MiddleLeft, ConAlfa(estado, a));
    }

    void DibujarEtiqueta(EtiquetaPiedra etiqueta, float e)
    {
        float porcentaje = Mathf.Clamp(etiqueta.porcentaje, 0f, 100f);
        bool listo = porcentaje >= porcentajeListo;
        Color estado = ColorDesgaste(porcentaje);

        float ancho = 64f * e, alto = 28f * e;
        Rect r = new Rect(etiqueta.posicion.x - ancho * 0.5f, etiqueta.posicion.y - alto - 12f * e, ancho, alto);

        Rectangulo(new Rect(r.x + 2f * e, r.y + 3f * e, r.width, r.height), new Color(0f, 0f, 0f, 0.3f), 8f * e);
        Rectangulo(r, colorFondoPanel, 8f * e);
        Borde(r, ConAlfa(estado, 0.8f), 1f * e, 8f * e);

        Texto(new Rect(r.x, r.y + 1f * e, r.width, r.height - 7f * e), porcentaje.ToString("0", CultureInfo.InvariantCulture) + "%",
              14f * e, TextAnchor.MiddleCenter, listo ? estado : Color.white);

        Rect barra = new Rect(r.x + 9f * e, r.yMax - 8f * e, r.width - 18f * e, 3f * e);
        Rectangulo(barra, new Color(1f, 1f, 1f, 0.15f), 1.5f * e);
        float relleno = barra.width * porcentaje / 100f;
        if (relleno > 1f)
            Rectangulo(new Rect(barra.x, barra.y, relleno, barra.height), estado, 1.5f * e);

        // Punto que señala la piedra.
        Rectangulo(new Rect(etiqueta.posicion.x - 2.5f * e, r.yMax + 4f * e, 5f * e, 5f * e), estado, 2.5f * e);
    }

    Color ColorDesgaste(float porcentaje)
    {
        if (porcentaje >= porcentajeListo) return colorListo;

        float t = Mathf.Clamp01(porcentaje / porcentajeListo);
        return t < 0.5f
            ? Color.Lerp(colorDesgasteBajo, colorDesgasteMedio, t * 2f)
            : Color.Lerp(colorDesgasteMedio, colorListo, (t - 0.5f) * 2f);
    }

    void PrepararEstilo()
    {
        if (estiloTexto != null) return;

        estiloTexto = new GUIStyle { font = fuentePanel, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
        estiloTexto.normal.textColor = Color.white;
    }

    // Texto con sombra para que se lea sobre cualquier fondo.
    void Texto(Rect r, string texto, float tamano, TextAnchor anclaje, Color color)
    {
        estiloTexto.fontSize = Mathf.Max(8, Mathf.RoundToInt(tamano));
        estiloTexto.alignment = anclaje;

        Color guardado = GUI.color;
        float sombra = Mathf.Max(1f, tamano * 0.08f);
        GUI.color = new Color(0f, 0f, 0f, 0.6f * color.a);
        GUI.Label(new Rect(r.x + sombra, r.y + sombra, r.width, r.height), texto, estiloTexto);
        GUI.color = color;
        GUI.Label(r, texto, estiloTexto);
        GUI.color = guardado;
    }

    static void Rectangulo(Rect r, Color color, float radio)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radio);
    }

    static void Borde(Rect r, Color color, float grosor, float radio)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, grosor, radio);
    }

    static Color ConAlfa(Color color, float alfa)
    {
        color.a = alfa;
        return color;
    }
}
