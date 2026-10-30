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
    [Tooltip("Fuente del panel. Por defecto, la de los carteles LED (DS-DIGI).")]
    public Font fuentePanel;
    [Tooltip("Tamaño del panel. Además se adapta a la resolución (referencia 1080p).")]
    public float escalaPanel = 1f;
    [Tooltip("Desgaste a partir del cual la piedra se considera lista.")]
    public float porcentajeListo = 95f;
    [Tooltip("Placa del cartel (violeta espacial muy oscuro).")]
    public Color colorPlaca = new Color(0.035f, 0.025f, 0.09f, 0.88f);
    [Tooltip("Neón del borde y los textos fijos (el lavanda de los carteles).")]
    public Color colorNeon = new Color(0.926f, 0.887f, 1f, 1f);
    public Color colorNeonBajo = new Color(0.926f, 0.075f, 0.102f, 1f);
    public Color colorNeonMedio = new Color(0.579f, 0.387f, 0.943f, 1f);
    public Color colorNeonAlto = new Color(0.914f, 0.616f, 0.992f, 1f);
    [Tooltip("Color al llegar al núcleo (parpadea).")]
    public Color colorNeonListo = new Color(0.926f, 0.887f, 1f, 1f);
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

#if UNITY_EDITOR
    // Si no hay fuente asignada, se pone la de los carteles LED. Guarda la escena para conservarla.
    void OnValidate()
    {
        if (fuentePanel == null)
            fuentePanel = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Materials/Led/DS-DIGI.TTF");
    }
#endif

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

    // Panel con estilo de cartel LED (como los de la gasolinera): placa oscura, borde de neón con halo,
    // esquinas de HUD, dígitos de 7 segmentos con los "8" apagados de fondo y barra de segmentos.
    void DibujarPanel(float e)
    {
        // Aparición con un pequeño "pop" (ease out).
        float a = 1f - Mathf.Pow(1f - alfaPanel, 3f);
        float pop = Mathf.Lerp(0.85f, 1f, a);

        float ancho = 270f * e * pop;
        float alto = 104f * e * pop;
        float x = Mathf.Clamp(posicionPanel.x - ancho * 0.5f, 14f, Screen.width - ancho - 14f);
        float y = Mathf.Clamp(posicionPanel.y - alto - 34f * e, 14f, Screen.height - alto - 14f);
        Rect r = new Rect(x, y, ancho, alto);
        float k = e * pop;

        float porcentaje = Mathf.Clamp(porcentajeMostrado, 0f, 100f);
        bool listo = porcentaje >= porcentajeListo;
        Color estado = ColorDesgaste(porcentaje);
        // Parpadeo de LED al estar lista.
        float parpadeo = listo ? (Mathf.Repeat(Time.unscaledTime * 1.6f, 1f) < 0.65f ? 1f : 0.35f) : 1f;

        // Placa, líneas de barrido y borde de neón con halo.
        Rectangulo(new Rect(r.x + 4f * k, r.y + 6f * k, r.width, r.height), new Color(0f, 0f, 0f, 0.4f * a), 4f * k);
        Rectangulo(r, ConAlfa(colorPlaca, colorPlaca.a * a), 4f * k);
        for (float ly = r.y + 3f * k; ly < r.yMax - 2f * k; ly += 3f * k)
            Rectangulo(new Rect(r.x + 2f * k, ly, r.width - 4f * k, Mathf.Max(1f, 0.6f * k)), ConAlfa(colorNeon, 0.035f * a), 0f);
        BordeNeon(r, ConAlfa(colorNeon, 0.75f * a), k, 4f * k);
        EsquinasHUD(r, ConAlfa(estado, a), k);

        float m = 14f * k;

        // Cabecera.
        TextoNeon(new Rect(r.x + m, r.y + 8f * k, r.width - 2f * m, 16f * k), "EROSION", 15f * k, TextAnchor.UpperLeft, ConAlfa(colorNeon, 0.85f * a), 0.5f);
        TextoNeon(new Rect(r.x + m, r.y + 8f * k, r.width - 2f * m, 16f * k), "SCAN", 15f * k, TextAnchor.UpperRight, ConAlfa(colorNeon, 0.45f * a), 0f);

        // Porcentaje en dígitos de 7 segmentos, con los segmentos apagados de fondo.
        Rect cifras = new Rect(r.x + m, r.y + 20f * k, r.width - 2f * m - 26f * k, 46f * k);
        TextoNeon(cifras, "888.8", 44f * k, TextAnchor.MiddleRight, ConAlfa(estado, 0.09f * a), 0f);
        TextoNeon(cifras, porcentaje.ToString("0.0", CultureInfo.InvariantCulture), 44f * k, TextAnchor.MiddleRight, ConAlfa(estado, a * parpadeo), 1f);
        TextoNeon(new Rect(cifras.xMax + 4f * k, cifras.y + 6f * k, 24f * k, 40f * k), "%", 22f * k, TextAnchor.MiddleLeft, ConAlfa(estado, 0.9f * a), 0.6f);

        // Barra de segmentos LED, cada uno con el color de su tramo.
        Rect barra = new Rect(r.x + m, r.y + 70f * k, r.width - 2f * m, 8f * k);
        BarraSegmentos(barra, porcentaje, 20, 2f * k, a);
        float marca = barra.x + barra.width * porcentajeListo / 100f;
        Rectangulo(new Rect(marca - 0.75f * k, barra.y - 3f * k, 1.5f * k, barra.height + 6f * k), ConAlfa(colorNeon, 0.7f * a), 0f);

        // Estado.
        string textoEstado = listo ? "CORE REACHED - READY" : "EROSION REQUIRED";
        TextoNeon(new Rect(r.x + m, barra.yMax + 5f * k, r.width - 2f * m, 16f * k), textoEstado, 14f * k, TextAnchor.UpperLeft, ConAlfa(estado, a * parpadeo), 0.6f);
    }

    void DibujarEtiqueta(EtiquetaPiedra etiqueta, float e)
    {
        float porcentaje = Mathf.Clamp(etiqueta.porcentaje, 0f, 100f);
        bool listo = porcentaje >= porcentajeListo;
        Color estado = ColorDesgaste(porcentaje);
        float parpadeo = listo ? (Mathf.Repeat(Time.unscaledTime * 1.6f, 1f) < 0.65f ? 1f : 0.35f) : 1f;

        float ancho = 66f * e, alto = 32f * e;
        Rect r = new Rect(etiqueta.posicion.x - ancho * 0.5f, etiqueta.posicion.y - alto - 14f * e, ancho, alto);

        Rectangulo(r, colorPlaca, 3f * e);
        BordeNeon(r, ConAlfa(colorNeon, 0.6f), e, 3f * e);

        Rect cifras = new Rect(r.x, r.y + 2f * e, r.width, r.height - 9f * e);
        TextoNeon(cifras, porcentaje.ToString("0", CultureInfo.InvariantCulture) + "%", 20f * e, TextAnchor.MiddleCenter, ConAlfa(estado, parpadeo), 0.8f);

        BarraSegmentos(new Rect(r.x + 7f * e, r.yMax - 7f * e, r.width - 14f * e, 3f * e), porcentaje, 10, 1.5f * e, 1f);

        // Línea que une la etiqueta con la piedra.
        Rectangulo(new Rect(etiqueta.posicion.x - 0.75f * e, r.yMax, 1.5f * e, 10f * e), ConAlfa(estado, 0.7f), 0f);
    }

    void BarraSegmentos(Rect barra, float porcentaje, int segmentos, float hueco, float alfa)
    {
        float anchoSegmento = (barra.width - hueco * (segmentos - 1)) / segmentos;
        float encendidos = porcentaje / 100f * segmentos;

        for (int i = 0; i < segmentos; i++)
        {
            Rect segmento = new Rect(barra.x + i * (anchoSegmento + hueco), barra.y, anchoSegmento, barra.height);
            Color color = ColorDesgaste((i + 1f) / segmentos * 100f);

            // El último segmento se enciende a medias según lo que falte.
            float encendido = Mathf.Clamp01(encendidos - i);
            Rectangulo(segmento, ConAlfa(color, Mathf.Lerp(0.1f, 1f, encendido) * alfa), 0.5f * barra.height * 0.3f);
            if (encendido > 0.5f)
                Rectangulo(new Rect(segmento.x - 1f, segmento.y - 1f, segmento.width + 2f, segmento.height + 2f), ConAlfa(color, 0.18f * encendido * alfa), 1f);
        }
    }

    // Borde de neón: línea nítida y dos capas de halo por fuera.
    void BordeNeon(Rect r, Color color, float e, float radio)
    {
        for (int i = 2; i >= 1; i--)
        {
            float expansion = i * 2f * e;
            Rect halo = new Rect(r.x - expansion, r.y - expansion, r.width + expansion * 2f, r.height + expansion * 2f);
            Borde(halo, ConAlfa(color, color.a * (i == 2 ? 0.08f : 0.18f)), 2f * e, radio + expansion);
        }
        Borde(r, color, 1.2f * e, radio);
    }

    // Esquinas en "L" por fuera de la placa, como un visor.
    void EsquinasHUD(Rect r, Color color, float e)
    {
        float largo = 12f * e, grosor = 2f * e, separacion = 5f * e;
        float izquierda = r.x - separacion, derecha = r.xMax + separacion;
        float arriba = r.y - separacion, abajo = r.yMax + separacion;

        Rectangulo(new Rect(izquierda, arriba, largo, grosor), color, 0f);
        Rectangulo(new Rect(izquierda, arriba, grosor, largo), color, 0f);
        Rectangulo(new Rect(derecha - largo, arriba, largo, grosor), color, 0f);
        Rectangulo(new Rect(derecha - grosor, arriba, grosor, largo), color, 0f);
        Rectangulo(new Rect(izquierda, abajo - grosor, largo, grosor), color, 0f);
        Rectangulo(new Rect(izquierda, abajo - largo, grosor, largo), color, 0f);
        Rectangulo(new Rect(derecha - largo, abajo - grosor, largo, grosor), color, 0f);
        Rectangulo(new Rect(derecha - grosor, abajo - largo, grosor, largo), color, 0f);
    }

    Color ColorDesgaste(float porcentaje)
    {
        if (porcentaje >= porcentajeListo) return colorNeonListo;

        float t = Mathf.Clamp01(porcentaje / porcentajeListo);
        return t < 0.5f
            ? Color.Lerp(colorNeonBajo, colorNeonMedio, t * 2f)
            : Color.Lerp(colorNeonMedio, colorNeonAlto, (t - 0.5f) * 2f);
    }

    void PrepararEstilo()
    {
        if (estiloTexto != null && estiloTexto.font == fuentePanel) return;

        estiloTexto = new GUIStyle { font = fuentePanel, fontStyle = FontStyle.Normal, clipping = TextClipping.Overflow };
        estiloTexto.normal.textColor = Color.white;
    }

    // Texto luminoso: copias desplazadas y tenues alrededor (halo) y el texto nítido encima.
    void TextoNeon(Rect r, string texto, float tamano, TextAnchor anclaje, Color color, float halo)
    {
        estiloTexto.fontSize = Mathf.Max(8, Mathf.RoundToInt(tamano));
        estiloTexto.alignment = anclaje;

        Color guardado = GUI.color;
        if (halo > 0f)
        {
            float d = Mathf.Max(1f, tamano * 0.05f);
            GUI.color = ConAlfa(color, color.a * 0.22f * halo);
            GUI.Label(new Rect(r.x - d, r.y, r.width, r.height), texto, estiloTexto);
            GUI.Label(new Rect(r.x + d, r.y, r.width, r.height), texto, estiloTexto);
            GUI.Label(new Rect(r.x, r.y - d, r.width, r.height), texto, estiloTexto);
            GUI.Label(new Rect(r.x, r.y + d, r.width, r.height), texto, estiloTexto);
        }
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
