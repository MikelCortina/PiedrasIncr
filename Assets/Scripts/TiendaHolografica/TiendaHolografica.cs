using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tienda holográfica (sustituye la presentación de la tienda vieja, no su lógica).
///
/// - En el suelo, junto a la MesaTrabajo, hay un PROYECTOR: un disco con anillos cian que laten.
///   Al acercarte se encienden unos rayos en abanico y un haz de luz.
/// - Al pulsar E (MesaTrabajo) el holograma sale del proyector, crece con parpadeo y se queda a
///   pantalla completa. Con X o Esc se encoge y vuelve al proyector.
/// - Panel "TIENDA HOLOGRÁFICA" con las monedas y una tarjeta por objeto: nombre, modelo 3D
///   girando con efecto holograma, nivel, precio y botón (COMPRAR / MEJORAR / SIN FONDOS / MÁX).
/// - Las compras las hace TiendaTrabajo (precios del Excel, guardado, eventos, panel de ritmo).
///
/// No hay que montar nada: se añade sola a la MesaTrabajo de la escena. Si queréis ajustar
/// valores y que se guarden, añadid el componente a mano en el objeto de la MesaTrabajo.
/// Los modelos que faltan (imán, martillo, botas, jetpack) salen como una forma provisional:
/// cuando los tengáis, arrastradlos a "Modelos".
/// </summary>
[DisallowMultipleComponent]
public class TiendaHolografica : MonoBehaviour
{
    [Header("Referencias (vacío = se buscan solas)")]
    public MesaTrabajo mesa;
    public TiendaTrabajo tienda;

    [Header("Textos")]
    public string titulo = "TIENDA HOLOGRÁFICA";
    public string nombreMoneda = "MONEDAS";

    [Header("Modelos (vacío = el del juego o una forma provisional)")]
    public GameObject modeloFusil;
    public GameObject modeloTorbellino;
    public GameObject modeloIman;
    public GameObject modeloMartillo;
    public GameObject modeloBotas;
    public GameObject modeloJetpack;

    [Header("Proyector del suelo")]
    public bool crearProyector = true;
    [Tooltip("Desplazamiento desde el punto del suelo bajo la MesaTrabajo (metros, en el mundo).")]
    public Vector3 desplazamientoProyector = Vector3.zero;
    [Min(0.1f)] public float radioProyector = 0.55f;

    [Header("Tema")]
    [Tooltip("Usa los colores y la fuente del TemaUINeon (estilo del escáner de piedras) en vez de los de abajo.")]
    public bool usarTemaUINeon = true;

    [Header("Colores (si no se usa el tema)")]
    public Color colorHolograma = new Color(0.30f, 0.92f, 1f, 1f);
    public Color colorTexto = new Color(0.86f, 1f, 1f, 1f);
    public Color colorFondo = new Color(0.02f, 0.07f, 0.11f, 0.86f);
    public Color colorListo = new Color(0.45f, 1f, 0.72f, 1f);
    public Color colorAviso = new Color(1f, 0.32f, 0.36f, 1f);

    [Header("Sonidos (opcionales)")]
    public AudioClip sonidoAbrir;
    public AudioClip sonidoCerrar;
    public AudioClip sonidoComprar;
    public AudioClip sonidoError;
    [Range(0f, 1f)] public float volumen = 0.7f;

    // =====================================================
    // ESTADO
    // =====================================================

    private enum Estado { Cerrada, Abriendo, Abierta, Cerrando }
    private Estado estado = Estado.Cerrada;
    private float tEstado;
    private const float DuracionAbrir = 0.5f;
    private const float DuracionCerrar = 0.3f;

    private class Tarjeta
    {
        public string nombre;
        public TiendaTrabajo.Articulo principal;
        public TiendaTrabajo.Articulo? siguiente;     // p. ej. Imán -> Alcance del imán
        public TiendaTrabajo.Articulo[] extras;        // botones pequeños (Torbellino: alcance y movilidad)
        public string[] nombresExtras;

        public RectTransform raiz;
        public Image fondo;
        public Image[] marco;
        public TextMeshProUGUI precio;
        public TextMeshProUGUI etiquetaBoton;
        public Image fondoBoton;
        public Button boton;
        public Image[] pips;
        public Button[] botonesExtra;
        public TextMeshProUGUI[] etiquetasExtra;
        public Image[] fondosExtra;
        public RawImage vista;

        public Transform modelo;
        public float giroExtra;
        public float destello;
        public float temblor;
        public Color colorDestello;
    }

    private readonly List<Tarjeta> tarjetas = new List<Tarjeta>();

    private Canvas lienzo;
    private RectTransform panel;
    private CanvasGroup grupoPanel;
    private RawImage barrido;
    private Image velo;
    private TextMeshProUGUI textoMonedas;
    private TextMeshProUGUI textoAyuda;
    private AudioSource audioSource;

    private Material materialHolo;
    private Camera camaraEstudio;
    private RenderTexture texturaEstudio;
    private Transform estudio;
    private int capaEstudio = -1;

    private Transform proyector;
    private Renderer[] anillos;
    private Renderer rayos;
    private Renderer haz;
    private float intensidadProyector;
    private MaterialPropertyBlock bloque;

    private const int Columnas = 3;
    private const int Filas = 2;
    private static readonly Vector2 TamPanel = new Vector2(1500f, 860f);
    private static readonly Vector2 TamTarjeta = new Vector2(440f, 330f);

    // =====================================================
    // INSTALACIÓN AUTOMÁTICA
    // =====================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        AnadirATodas();
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        AnadirATodas();
    }

    private static void AnadirATodas()
    {
        foreach (MesaTrabajo m in FindObjectsByType<MesaTrabajo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (m != null && m.GetComponent<TiendaHolografica>() == null)
                m.gameObject.AddComponent<TiendaHolografica>();
        }
    }

    // =====================================================
    // CICLO DE VIDA
    // =====================================================

    private void Start()
    {
        AplicarTema();

        if (mesa == null) mesa = GetComponent<MesaTrabajo>();
        if (tienda == null && mesa != null && mesa.panelTienda != null)
            tienda = mesa.panelTienda.GetComponentInChildren<TiendaTrabajo>(true);
        if (tienda == null)
            tienda = FindAnyObjectByType<TiendaTrabajo>(FindObjectsInactive.Include);

        bloque = new MaterialPropertyBlock();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        Shader shader = Shader.Find("PiedrasIncr/HologramaTienda");
        if (shader == null) shader = Resources.Load<Shader>("HologramaTienda");
        if (shader != null)
        {
            materialHolo = new Material(shader) { name = "HologramaTienda (instancia)" };
            materialHolo.SetColor("_Color", colorHolograma);
        }
        else
        {
            Debug.LogWarning("TiendaHolografica: no encuentro el shader PiedrasIncr/HologramaTienda.", this);
        }

        DefinirTarjetas();
        if (crearProyector) CrearProyector();
    }

    private void OnDestroy()
    {
        if (torbellinoEscuchado != null) torbellinoEscuchado.OnNivelCambiado -= AlCambiarNivelTorbellino;
        if (texturaEstudio != null) texturaEstudio.Release();
        if (estudio != null) Destroy(estudio.gameObject);
        if (lienzo != null) Destroy(lienzo.gameObject);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        Shader.SetGlobalFloat("_HoloTiempo", Time.unscaledTime);

        ActualizarProyector(dt);

        if (estado == Estado.Cerrada) return;

        tEstado += dt;
        AnimarPanel();
        AnimarTarjetas(dt);

        if (barrido != null)
        {
            Rect uv = barrido.uvRect;
            uv.y = Mathf.Repeat(uv.y - dt * 0.15f, 1f);
            barrido.uvRect = uv;
        }
    }

    // =====================================================
    // ABRIR / CERRAR (lo llama MesaTrabajo)
    // =====================================================

    public bool EstaVisible => estado != Estado.Cerrada;

    public void Mostrar()
    {
        if (tienda != null) tienda.Iniciar();
        if (lienzo == null) ConstruirUI();
        if (estudio == null) ConstruirEstudio();

        lienzo.gameObject.SetActive(true);
        if (camaraEstudio != null) camaraEstudio.enabled = true;
        estado = Estado.Abriendo;
        tEstado = 0f;
        Refrescar();
        Sonar(sonidoAbrir);
    }

    public void Ocultar()
    {
        if (estado == Estado.Cerrada || estado == Estado.Cerrando) return;
        estado = Estado.Cerrando;
        tEstado = 0f;
        Sonar(sonidoCerrar);
    }

    private void Cerrar()
    {
        if (mesa != null && mesa.EstaTiendaAbierta()) mesa.CerrarTienda();
        else Ocultar();
    }

    private void Sonar(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volumen);
    }

    // =====================================================
    // TARJETAS
    // =====================================================

    private void DefinirTarjetas()
    {
        tarjetas.Clear();
        tarjetas.Add(new Tarjeta { nombre = "Fusil de monedas", principal = TiendaTrabajo.Articulo.Fusil });
        // Con el nivel unificado del Torbellino no hay botones sueltos de alcance y movilidad.
        bool torbellinoUnificado = tienda != null && tienda.herramientaTorbellino != null && tienda.herramientaTorbellino.nivelesUnificados;
        tarjetas.Add(new Tarjeta
        {
            nombre = "Torbellino",
            principal = TiendaTrabajo.Articulo.Torbellino,
            extras = torbellinoUnificado ? null : new[] { TiendaTrabajo.Articulo.TorbellinoAlcance, TiendaTrabajo.Articulo.TorbellinoMovilidad },
            nombresExtras = torbellinoUnificado ? null : new[] { "ALCANCE", "MOVILIDAD" }
        });
        tarjetas.Add(new Tarjeta { nombre = "Imán", principal = TiendaTrabajo.Articulo.Iman, siguiente = TiendaTrabajo.Articulo.ImanAlcance });
        tarjetas.Add(new Tarjeta { nombre = "Martillo", principal = TiendaTrabajo.Articulo.Martillo });
        tarjetas.Add(new Tarjeta { nombre = "Botas", principal = TiendaTrabajo.Articulo.Botas });
        tarjetas.Add(new Tarjeta { nombre = "Jetpack", principal = TiendaTrabajo.Articulo.Jetpack });
    }

    // Artículo que compra el botón grande ahora mismo (el Imán pasa a "alcance" tras comprarlo).
    private TiendaTrabajo.Articulo ArticuloActual(Tarjeta t)
    {
        if (t.siguiente.HasValue && tienda != null && tienda.Consultar(t.principal).Maximo)
            return t.siguiente.Value;
        return t.principal;
    }

    // Nivel total de la tarjeta para los "pips" (Imán: comprar + 2 mejoras de alcance = 3).
    private void NivelTarjeta(Tarjeta t, out int nivel, out int maximo)
    {
        TiendaTrabajo.EstadoArticulo e = tienda.Consultar(t.principal);
        nivel = e.nivel;
        maximo = e.nivelMax;
        if (t.siguiente.HasValue)
        {
            TiendaTrabajo.EstadoArticulo s = tienda.Consultar(t.siguiente.Value);
            maximo += s.nivelMax - 1;
            if (e.Maximo) nivel += s.nivel - 1;
        }
    }

    private void Refrescar()
    {
        if (tienda == null || lienzo == null) return;

        int monedas = tienda.MonedasDisponibles;
        if (textoMonedas != null) textoMonedas.text = nombreMoneda + ": " + monedas;

        foreach (Tarjeta t in tarjetas)
        {
            TiendaTrabajo.Articulo actual = ArticuloActual(t);
            TiendaTrabajo.EstadoArticulo e = tienda.Consultar(actual);

            int nivel, maximo;
            NivelTarjeta(t, out nivel, out maximo);
            bool todoComprado = nivel >= maximo;

            // Pips de nivel.
            for (int i = 0; i < t.pips.Length; i++)
            {
                bool visible = i < maximo && maximo > 1;
                t.pips[i].gameObject.SetActive(visible);
                if (visible) t.pips[i].color = i < nivel ? colorHolograma : ConAlfa(colorHolograma, 0.18f);
            }

            string precio;
            string boton;
            Color colorBoton;
            bool activo;

            if (e.noDisponible)
            {
                precio = "NO DISPONIBLE"; boton = "—"; colorBoton = ConAlfa(colorTexto, 0.35f); activo = false;
            }
            else if (todoComprado)
            {
                precio = maximo > 1 ? "NIVEL MÁXIMO" : "COMPRADO";
                boton = maximo > 1 ? "MÁX" : "COMPRADO";
                colorBoton = colorListo; activo = false;
            }
            else if (e.requisitoPendiente)
            {
                precio = "BLOQUEADO"; boton = "BLOQUEADO"; colorBoton = ConAlfa(colorTexto, 0.35f); activo = false;
            }
            else
            {
                precio = e.precio + " " + nombreMoneda;
                bool falta = e.precio > monedas;
                if (falta) { boton = "SIN FONDOS"; colorBoton = colorAviso; }
                else { boton = nivel == 0 ? "COMPRAR" : (actual == TiendaTrabajo.Articulo.ImanAlcance ? "MEJORAR ALCANCE" : "MEJORAR"); colorBoton = colorHolograma; }
                activo = true;
            }

            t.precio.text = precio;
            t.precio.color = todoComprado ? colorListo : colorTexto;
            t.etiquetaBoton.text = boton;
            t.etiquetaBoton.color = activo && boton != "SIN FONDOS" ? colorFondoOpaco : colorBoton;
            t.fondoBoton.color = activo && boton != "SIN FONDOS" ? ConAlfa(colorBoton, 0.85f) : ConAlfa(colorBoton, 0.12f);
            t.boton.interactable = activo;

            // Botones pequeños (Torbellino).
            if (t.extras != null)
            {
                for (int i = 0; i < t.extras.Length; i++)
                {
                    TiendaTrabajo.EstadoArticulo x = tienda.Consultar(t.extras[i]);
                    string texto;
                    bool puede = false;
                    Color c = colorHolograma;
                    if (x.requisitoPendiente || x.noDisponible) { texto = t.nombresExtras[i] + "\nBLOQUEADO"; c = ConAlfa(colorTexto, 0.35f); }
                    else if (x.Maximo) { texto = t.nombresExtras[i] + "\nMÁX"; c = colorListo; }
                    else
                    {
                        texto = t.nombresExtras[i] + " " + (x.nivel + 1) + "/" + x.nivelMax + "\n" + x.precio + " " + nombreMoneda;
                        puede = true;
                        if (x.precio > monedas) c = colorAviso;
                    }
                    t.etiquetasExtra[i].text = texto;
                    t.etiquetasExtra[i].color = c;
                    t.fondosExtra[i].color = ConAlfa(c, puede ? 0.16f : 0.07f);
                    t.botonesExtra[i].interactable = puede;
                }
            }
        }
    }

    private Color colorFondoOpaco => new Color(colorFondo.r, colorFondo.g, colorFondo.b, 1f);

    private void AlPulsar(Tarjeta t, TiendaTrabajo.Articulo articulo)
    {
        if (tienda == null) return;
        TiendaTrabajo.EstadoArticulo e = tienda.Consultar(articulo);
        if (!e.SePuedeComprar) return;

        if (e.precio > tienda.MonedasDisponibles)
        {
            t.temblor = 1f;
            t.destello = 1f;
            t.colorDestello = colorAviso;
            Sonar(sonidoError);
            return;
        }

        if (tienda.Comprar(articulo))
        {
            t.destello = 1f;
            t.colorDestello = colorListo;
            t.giroExtra = 720f;
            Sonar(sonidoComprar);
        }
        else
        {
            t.temblor = 1f;
            Sonar(sonidoError);
        }
        Refrescar();
    }

    // =====================================================
    // ANIMACIONES
    // =====================================================

    private void AnimarPanel()
    {
        if (panel == null) return;

        float k;
        if (estado == Estado.Abriendo)
        {
            k = Mathf.Clamp01(tEstado / DuracionAbrir);
            if (k >= 1f) estado = Estado.Abierta;
        }
        else if (estado == Estado.Cerrando)
        {
            k = 1f - Mathf.Clamp01(tEstado / DuracionCerrar);
            if (k <= 0f)
            {
                estado = Estado.Cerrada;
                lienzo.gameObject.SetActive(false);
                if (camaraEstudio != null) camaraEstudio.enabled = false;
                return;
            }
        }
        else
        {
            k = 1f;
        }

        // Sale del proyector (su punto en pantalla) y crece hasta el centro.
        Vector2 origen = PuntoProyectorEnLienzo();
        float salida = EaseOutBack(k);
        panel.anchoredPosition = Vector2.LerpUnclamped(origen, Vector2.zero, Mathf.Clamp01(k * 1.15f));
        panel.localScale = new Vector3(Mathf.Lerp(0.04f, 1f, salida), Mathf.Lerp(0.01f, 1f, Mathf.Clamp01(k * 1.6f)) * Mathf.Lerp(0.04f, 1f, salida), 1f);

        // Parpadeo de holograma al encenderse/apagarse y un leve temblor de señal.
        float parpadeo = k < 1f ? (Mathf.PerlinNoise(Time.unscaledTime * 40f, 0f) > 0.35f ? 1f : 0.35f) : 1f;
        float senal = 0.96f + 0.04f * Mathf.Sin(Time.unscaledTime * 31f) * Mathf.Sin(Time.unscaledTime * 7.7f);
        grupoPanel.alpha = Mathf.Clamp01(k * 1.4f) * parpadeo * senal;
        if (velo != null) velo.color = new Color(0f, 0.02f, 0.04f, 0.45f * k);
        grupoPanel.interactable = estado == Estado.Abierta;
        grupoPanel.blocksRaycasts = estado != Estado.Cerrando;
    }

    private void AnimarTarjetas(float dt)
    {
        for (int i = 0; i < tarjetas.Count; i++)
        {
            Tarjeta t = tarjetas[i];
            if (t.modelo != null)
            {
                float velocidad = 35f + t.giroExtra * 2.5f;
                t.modelo.Rotate(0f, velocidad * dt, 0f, Space.Self);
                // Vuelve a su tamaño tras el "bloop" de subir de nivel.
                t.modelo.localScale = Vector3.Lerp(t.modelo.localScale, Vector3.one, dt * 10f);
                t.giroExtra = Mathf.MoveTowards(t.giroExtra, 0f, dt * 600f);
            }

            if (t.temblor > 0f)
            {
                t.temblor = Mathf.MoveTowards(t.temblor, 0f, dt * 3f);
                float desplazamiento = Mathf.Sin(Time.unscaledTime * 60f) * 10f * t.temblor;
                t.raiz.anchoredPosition = PosicionTarjeta(i) + new Vector2(desplazamiento, 0f);
            }

            t.destello = Mathf.MoveTowards(t.destello, 0f, dt * 2f);
            Color marco = Color.Lerp(colorHolograma, t.colorDestello, t.destello);
            foreach (Image borde in t.marco) borde.color = ConAlfa(marco, 0.85f);
            t.fondo.color = Color.Lerp(ConAlfa(colorFondo, 0.55f), ConAlfa(t.colorDestello, 0.35f), t.destello * 0.6f);
        }
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private Vector2 PuntoProyectorEnLienzo()
    {
        Camera cam = Camera.main;
        if (cam == null || lienzo == null) return new Vector2(0f, -400f);
        Vector3 mundo = proyector != null ? proyector.position + Vector3.up * 0.8f : transform.position;
        Vector3 pantalla = cam.WorldToScreenPoint(mundo);
        if (pantalla.z <= 0f) return new Vector2(0f, -400f);

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)lienzo.transform, pantalla, null, out local);
        return local;
    }

    // =====================================================
    // CONSTRUCCIÓN DE LA INTERFAZ
    // =====================================================

    private void ConstruirUI()
    {
        GameObject go = new GameObject("TiendaHolograficaUI", typeof(RectTransform));
        lienzo = go.AddComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzo.sortingOrder = 500;
        CanvasScaler escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Velo oscuro detrás.
        velo = Crear<Image>("Velo", go.transform);
        Estirar(velo.rectTransform);
        velo.color = new Color(0f, 0.02f, 0.04f, 0.45f);
        velo.raycastTarget = true;

        // Panel.
        panel = Crear<RectTransform>("Panel", go.transform);
        panel.sizeDelta = TamPanel;
        grupoPanel = panel.gameObject.AddComponent<CanvasGroup>();

        Image fondo = Crear<Image>("Fondo", panel);
        Estirar(fondo.rectTransform);
        fondo.color = colorFondo;

        barrido = Crear<RawImage>("LineasBarrido", panel);
        Estirar(barrido.rectTransform);
        barrido.texture = TexturaLineas();
        barrido.uvRect = new Rect(0f, 0f, 1f, TamPanel.y / 4f);
        barrido.color = ConAlfa(colorHolograma, 0.07f);
        barrido.raycastTarget = false;

        Marco(panel, TamPanel, 3f, ConAlfa(colorHolograma, 0.9f));
        Marco(panel, TamPanel + new Vector2(18f, 18f), 9f, ConAlfa(colorHolograma, 0.08f)); // halo
        Esquinas(panel, TamPanel, 34f, 5f, colorHolograma, 12f);

        // Cabecera.
        TextMeshProUGUI tituloTexto = Texto("Titulo", panel, titulo, 46f, colorHolograma, TextAlignmentOptions.Left, FontStyles.Bold);
        Colocar(tituloTexto.rectTransform, new Vector2(-TamPanel.x * 0.5f + 60f, TamPanel.y * 0.5f - 60f), new Vector2(900f, 70f), new Vector2(0f, 0.5f));
        tituloTexto.characterSpacing = 6f;

        textoMonedas = Texto("Monedas", panel, "", 36f, colorTexto, TextAlignmentOptions.Right, FontStyles.Bold);
        Colocar(textoMonedas.rectTransform, new Vector2(TamPanel.x * 0.5f - 140f, TamPanel.y * 0.5f - 60f), new Vector2(600f, 60f), new Vector2(1f, 0.5f));

        Button cerrar = Boton("Cerrar", panel, new Vector2(TamPanel.x * 0.5f - 70f, TamPanel.y * 0.5f - 60f), new Vector2(64f, 64f), "X", 38f, out Image fondoCerrar, out TextMeshProUGUI _);
        fondoCerrar.color = ConAlfa(colorHolograma, 0.85f);
        cerrar.onClick.AddListener(Cerrar);

        Image linea = Crear<Image>("LineaCabecera", panel);
        Colocar(linea.rectTransform, new Vector2(0f, TamPanel.y * 0.5f - 108f), new Vector2(TamPanel.x - 120f, 2f), new Vector2(0.5f, 0.5f));
        linea.color = ConAlfa(colorHolograma, 0.6f);

        textoAyuda = Texto("Ayuda", panel, "E / ESC  ·  CERRAR", 18f, ConAlfa(colorHolograma, 0.6f), TextAlignmentOptions.Center, FontStyles.Normal);
        Colocar(textoAyuda.rectTransform, new Vector2(0f, -TamPanel.y * 0.5f + 26f), new Vector2(600f, 30f), new Vector2(0.5f, 0.5f));
        textoAyuda.characterSpacing = 4f;

        for (int i = 0; i < tarjetas.Count; i++) ConstruirTarjeta(tarjetas[i], i);

        go.SetActive(false);
    }

    private Vector2 PosicionTarjeta(int indice)
    {
        int col = indice % Columnas, fila = indice / Columnas;
        float separacionX = TamTarjeta.x + 30f, separacionY = TamTarjeta.y + 26f;
        return new Vector2((col - (Columnas - 1) * 0.5f) * separacionX, -40f - (fila - (Filas - 1) * 0.5f) * separacionY);
    }

    private void ConstruirTarjeta(Tarjeta t, int indice)
    {
        t.raiz = Crear<RectTransform>("Tarjeta_" + t.nombre, panel);
        Colocar(t.raiz, PosicionTarjeta(indice), TamTarjeta, new Vector2(0.5f, 0.5f));

        t.fondo = Crear<Image>("Fondo", t.raiz);
        Estirar(t.fondo.rectTransform);
        t.fondo.color = ConAlfa(colorFondo, 0.55f);
        t.marco = Marco(t.raiz, TamTarjeta, 2f, ConAlfa(colorHolograma, 0.85f));
        t.colorDestello = colorListo;

        float arriba = TamTarjeta.y * 0.5f;
        TextMeshProUGUI nombre = Texto("Nombre", t.raiz, t.nombre.ToUpperInvariant(), 26f, colorTexto, TextAlignmentOptions.Center, FontStyles.Bold);
        Colocar(nombre.rectTransform, new Vector2(0f, arriba - 26f), new Vector2(TamTarjeta.x - 20f, 40f), new Vector2(0.5f, 0.5f));
        nombre.characterSpacing = 3f;

        // Vista 3D (celda de la textura del estudio).
        t.vista = Crear<RawImage>("Vista", t.raiz);
        Colocar(t.vista.rectTransform, new Vector2(0f, arriba - 128f), new Vector2(260f, 150f), new Vector2(0.5f, 0.5f));
        t.vista.raycastTarget = false;
        Esquinas(t.vista.rectTransform, new Vector2(260f, 150f), 14f, 2f, ConAlfa(colorHolograma, 0.6f), 4f);

        // Pips de nivel.
        t.pips = new Image[5];
        for (int i = 0; i < t.pips.Length; i++)
        {
            t.pips[i] = Crear<Image>("Nivel" + (i + 1), t.raiz);
            Colocar(t.pips[i].rectTransform, new Vector2((i - 2f) * 30f, arriba - 214f), new Vector2(24f, 6f), new Vector2(0.5f, 0.5f));
        }

        t.precio = Texto("Precio", t.raiz, "", 26f, colorTexto, TextAlignmentOptions.Center, FontStyles.Bold);
        Colocar(t.precio.rectTransform, new Vector2(0f, arriba - 244f), new Vector2(TamTarjeta.x - 20f, 36f), new Vector2(0.5f, 0.5f));

        float yBoton = -TamTarjeta.y * 0.5f + 36f;
        TiendaTrabajo.Articulo[] extras = t.extras;
        if (extras == null)
        {
            t.boton = Boton("Comprar", t.raiz, new Vector2(0f, yBoton), new Vector2(300f, 50f), "", 22f, out t.fondoBoton, out t.etiquetaBoton);
            t.boton.onClick.AddListener(() => AlPulsar(t, ArticuloActual(t)));
        }
        else
        {
            t.boton = Boton("Comprar", t.raiz, new Vector2(-110f, yBoton), new Vector2(190f, 50f), "", 22f, out t.fondoBoton, out t.etiquetaBoton);
            t.boton.onClick.AddListener(() => AlPulsar(t, ArticuloActual(t)));

            t.botonesExtra = new Button[extras.Length];
            t.etiquetasExtra = new TextMeshProUGUI[extras.Length];
            t.fondosExtra = new Image[extras.Length];
            for (int i = 0; i < extras.Length; i++)
            {
                TiendaTrabajo.Articulo articulo = extras[i];
                t.botonesExtra[i] = Boton("Extra" + i, t.raiz, new Vector2(52f + i * 116f, yBoton), new Vector2(108f, 50f), "", 13f, out t.fondosExtra[i], out t.etiquetasExtra[i]);
                t.botonesExtra[i].onClick.AddListener(() => AlPulsar(t, articulo));
            }
        }
    }

    // ----- utilidades de UI -----

    private static T Crear<T>(string nombre, Transform padre) where T : Component
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return typeof(T) == typeof(RectTransform) ? go.GetComponent<T>() : go.AddComponent<T>();
    }

    private static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void Colocar(RectTransform r, Vector2 posicion, Vector2 tamano, Vector2 pivote)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = pivote;
        r.sizeDelta = tamano;
        r.anchoredPosition = posicion;
    }

    // Toma los colores y la fuente del tema común (los mismos del panel de escaneo de piedras).
    private void AplicarTema()
    {
        if (!usarTemaUINeon) return;

        TemaUINeon tema = TemaUINeon.Actual;
        colorHolograma = tema.colorNeonAlto;
        colorTexto = tema.colorNeon;
        colorFondo = tema.colorPlaca;
        colorListo = tema.colorNeonMedio;
        colorAviso = tema.colorNeonBajo;
    }

    private TextMeshProUGUI Texto(string nombre, Transform padre, string texto, float tamano, Color color, TextAlignmentOptions alineacion, FontStyles estilo)
    {
        TextMeshProUGUI t = Crear<TextMeshProUGUI>(nombre, padre);
        if (usarTemaUINeon)
        {
            TemaUINeon tema = TemaUINeon.Actual;
            if (tema.fuenteTMP != null) t.font = tema.fuenteTMP;
            if (tema.MaterialTexto != null) t.fontSharedMaterial = tema.MaterialTexto;
        }
        t.text = texto;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = alineacion;
        t.fontStyle = estilo;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    private Button Boton(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, string texto, float tamanoTexto,
                         out Image fondo, out TextMeshProUGUI etiqueta)
    {
        fondo = Crear<Image>(nombre, padre);
        Colocar(fondo.rectTransform, posicion, tamano, new Vector2(0.5f, 0.5f));
        fondo.color = ConAlfa(colorHolograma, 0.85f);
        Marco(fondo.rectTransform, tamano, 2f, colorHolograma);

        Button b = fondo.gameObject.AddComponent<Button>();
        b.targetGraphic = fondo;
        ColorBlock colores = b.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colores.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colores.disabledColor = new Color(1f, 1f, 1f, 0.6f);
        colores.colorMultiplier = 1.4f;
        b.colors = colores;

        etiqueta = Texto("Texto", fondo.transform, texto, tamanoTexto, colorFondoOpaco, TextAlignmentOptions.Center, FontStyles.Bold);
        Estirar(etiqueta.rectTransform);
        etiqueta.textWrappingMode = TextWrappingModes.Normal;
        etiqueta.characterSpacing = 2f;
        return b;
    }

    // Marco de 4 líneas alrededor de un rectángulo centrado en su padre.
    private static Image[] Marco(RectTransform padre, Vector2 tamano, float grosor, Color color)
    {
        Image[] lados = new Image[4];
        Vector2[] pos =
        {
            new Vector2(0f, tamano.y * 0.5f), new Vector2(0f, -tamano.y * 0.5f),
            new Vector2(-tamano.x * 0.5f, 0f), new Vector2(tamano.x * 0.5f, 0f)
        };
        Vector2[] tam =
        {
            new Vector2(tamano.x + grosor, grosor), new Vector2(tamano.x + grosor, grosor),
            new Vector2(grosor, tamano.y + grosor), new Vector2(grosor, tamano.y + grosor)
        };
        for (int i = 0; i < 4; i++)
        {
            lados[i] = Crear<Image>("Borde", padre);
            Colocar(lados[i].rectTransform, pos[i], tam[i], new Vector2(0.5f, 0.5f));
            lados[i].color = color;
            lados[i].raycastTarget = false;
        }
        return lados;
    }

    // Esquinas en "L" por fuera, como un visor.
    private static void Esquinas(RectTransform padre, Vector2 tamano, float largo, float grosor, Color color, float separacion)
    {
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                Vector2 esquina = new Vector2(sx * (tamano.x * 0.5f + separacion), sy * (tamano.y * 0.5f + separacion));
                Image h = Crear<Image>("Esquina", padre);
                Colocar(h.rectTransform, esquina + new Vector2(-sx * largo * 0.5f, 0f), new Vector2(largo, grosor), new Vector2(0.5f, 0.5f));
                h.color = color;
                h.raycastTarget = false;
                Image v = Crear<Image>("Esquina", padre);
                Colocar(v.rectTransform, esquina + new Vector2(0f, -sy * largo * 0.5f), new Vector2(grosor, largo), new Vector2(0.5f, 0.5f));
                v.color = color;
                v.raycastTarget = false;
            }
        }
    }

    private static Texture2D TexturaLineas()
    {
        Texture2D t = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        t.SetPixels(new[] { Color.white, new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.4f) });
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Point;
        t.Apply();
        return t;
    }

    private static Color ConAlfa(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // =====================================================
    // ESTUDIO DE PREVISUALIZACIÓN (modelos 3D holográficos)
    // Una sola cámara ortográfica, lejos de todo, dibuja los 6 modelos en una textura
    // (rejilla 3x2) y cada tarjeta enseña su celda.
    // =====================================================

    private void ConstruirEstudio()
    {
        capaEstudio = BuscarCapaLibre();

        estudio = new GameObject("TiendaHolografica_Estudio").transform;
        estudio.position = new Vector3(0f, -4000f, 0f);

        texturaEstudio = new RenderTexture(1200, 600, 16, RenderTextureFormat.ARGB32) { name = "TiendaHolograficaVistas" };
        texturaEstudio.Create();

        GameObject goCamara = new GameObject("CamaraEstudio");
        goCamara.transform.SetParent(estudio, false);
        goCamara.transform.localPosition = new Vector3(0f, 0f, -10f);
        camaraEstudio = goCamara.AddComponent<Camera>();
        camaraEstudio.orthographic = true;
        camaraEstudio.orthographicSize = 0.75f; // rejilla de 3 x 2 celdas de 1 x 0,75
        camaraEstudio.aspect = 2f;
        camaraEstudio.nearClipPlane = 0.1f;
        camaraEstudio.farClipPlane = 20f;
        camaraEstudio.clearFlags = CameraClearFlags.SolidColor;
        camaraEstudio.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camaraEstudio.targetTexture = texturaEstudio;
        camaraEstudio.allowHDR = false; // sin HDR para conservar el fondo transparente
        camaraEstudio.allowMSAA = false;
        camaraEstudio.enabled = false;
        if (capaEstudio >= 0)
        {
            camaraEstudio.cullingMask = 1 << capaEstudio;
            // Que el resto de cámaras no vean el estudio.
            foreach (Camera c in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c != camaraEstudio) c.cullingMask &= ~(1 << capaEstudio);
        }

        for (int i = 0; i < tarjetas.Count; i++)
        {
            Tarjeta t = tarjetas[i];
            int col = i % Columnas, fila = i / Columnas;
            Vector3 centroCelda = new Vector3((col - 1f) * 1f, (0.5f - fila) * 0.75f, 0f);

            // Celda inclinada hacia la cámara y, dentro, un plato giratorio.
            Transform pivote = new GameObject("Vista_" + t.nombre).transform;
            pivote.SetParent(estudio, false);
            pivote.localPosition = centroCelda;
            pivote.localRotation = Quaternion.Euler(-18f, 0f, 0f);

            Transform giro = new GameObject("Giro").transform;
            giro.SetParent(pivote, false);

            t.modelo = giro;
            RellenarModelo(t);

            if (t.vista != null)
            {
                t.vista.texture = texturaEstudio;
                t.vista.uvRect = new Rect(col / 3f, (1 - fila) / 2f, 1f / 3f, 0.5f);
            }
        }
    }

    // Crea (o rehace) la copia holográfica de una tarjeta dentro de su plato giratorio.
    // El Torbellino se ve más grande cuanto mayor es su nivel (de 0,8 a 1,15 veces el tamaño de la celda).
    private void RellenarModelo(Tarjeta t)
    {
        if (t.modelo == null) return;
        for (int i = t.modelo.childCount - 1; i >= 0; i--)
            Destroy(t.modelo.GetChild(i).gameObject);

        GameObject copia = CrearModelo(t);
        if (copia == null) return;

        float tamano = 0.62f;
        if (t.principal == TiendaTrabajo.Articulo.Torbellino && tienda != null && tienda.herramientaTorbellino != null)
            tamano *= Mathf.Lerp(0.8f, 1.15f, (tienda.herramientaTorbellino.NivelCapacidad - 1) / 4f);

        Ajustar(copia.transform, t.modelo, tamano);
        AplicarCapa(copia, capaEstudio);
    }

    // Al subir de nivel el Torbellino, su holograma se rehace (un frame después, cuando el modelo
    // del juego ya ha cambiado de fase) y hace un "bloop".
    private bool rehacerTorbellino;
    private int framePeticion;
    private HerramientaTorbellino torbellinoEscuchado;

    private void EscucharTorbellino()
    {
        HerramientaTorbellino h = tienda != null ? tienda.herramientaTorbellino : null;
        if (h == torbellinoEscuchado) return;
        if (torbellinoEscuchado != null) torbellinoEscuchado.OnNivelCambiado -= AlCambiarNivelTorbellino;
        torbellinoEscuchado = h;
        if (h != null) h.OnNivelCambiado += AlCambiarNivelTorbellino;
    }

    private void AlCambiarNivelTorbellino(int nivel)
    {
        rehacerTorbellino = true;
        framePeticion = Time.frameCount;
    }

    private void LateUpdate()
    {
        EscucharTorbellino();
        if (!rehacerTorbellino || estudio == null) return;
        if (Time.frameCount <= framePeticion) return; // deja pasar un frame para que cambien las mallas
        rehacerTorbellino = false;

        foreach (Tarjeta t in tarjetas)
        {
            if (t.principal != TiendaTrabajo.Articulo.Torbellino) continue;
            RellenarModelo(t);
            if (t.modelo != null) t.modelo.localScale = Vector3.one * 1.25f;
        }
    }

    private static int BuscarCapaLibre()
    {
        for (int i = 31; i >= 8; i--)
            if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
        return -1;
    }

    private static void AplicarCapa(GameObject go, int capa)
    {
        if (capa < 0) return;
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = capa;
    }

    private GameObject CrearModelo(Tarjeta t)
    {
        GameObject fuente = null;
        GestorEquipamiento equipo = tienda != null ? tienda.gestorEquipamiento : null;
        if (equipo == null) equipo = FindAnyObjectByType<GestorEquipamiento>();

        switch (t.principal)
        {
            // Se copia el modelo de la herramienta (no el objeto del script, que puede ser la cámara):
            // así da igual que esté guardada (modelo desactivado) y solo cuentan las piezas de su fase.
            case TiendaTrabajo.Articulo.Fusil:
                fuente = modeloFusil != null ? modeloFusil : (equipo != null && equipo.armaLanzadora != null
                    ? (equipo.armaLanzadora.modeloArma != null ? equipo.armaLanzadora.modeloArma : equipo.armaLanzadora.gameObject) : null);
                break;
            case TiendaTrabajo.Articulo.Torbellino:
                fuente = modeloTorbellino != null ? modeloTorbellino : (equipo != null && equipo.herramientaTorbellino != null
                    ? (equipo.herramientaTorbellino.modeloHerramienta != null ? equipo.herramientaTorbellino.modeloHerramienta : equipo.herramientaTorbellino.gameObject) : null);
                break;
            case TiendaTrabajo.Articulo.Iman: fuente = modeloIman; break;
            case TiendaTrabajo.Articulo.Martillo: fuente = modeloMartillo; break;
            case TiendaTrabajo.Articulo.Botas: fuente = modeloBotas; break;
            case TiendaTrabajo.Articulo.Jetpack: fuente = modeloJetpack; break;
        }

        GameObject copia = fuente != null ? CopiarMallas(fuente) : null;
        if (copia == null) copia = FormaProvisional(t.principal);
        return copia;
    }

    // Copia solo las mallas visibles (las de la fase actual); si no hay ninguna, todas.
    private GameObject CopiarMallas(GameObject fuente)
    {
        return CopiarMallas(fuente, true) ?? CopiarMallas(fuente, false);
    }

    // Copia solo las mallas (sin scripts ni colliders) y les pone el material holográfico.
    private GameObject CopiarMallas(GameObject fuente, bool soloVisibles)
    {
        GameObject raiz = new GameObject("Copia_" + fuente.name);
        Matrix4x4 aLocal = fuente.transform.worldToLocalMatrix;
        int piezas = 0;

        foreach (MeshFilter mf in fuente.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null) continue;
            if (soloVisibles && (!VisibleDentroDe(mf.transform, fuente.transform) || !mf.GetComponent<MeshRenderer>().enabled)) continue;
            Pieza(raiz.transform, mf.sharedMesh, aLocal * mf.transform.localToWorldMatrix);
            piezas++;
        }
        foreach (SkinnedMeshRenderer smr in fuente.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null) continue;
            if (soloVisibles && (!smr.enabled || !VisibleDentroDe(smr.transform, fuente.transform))) continue;
            Mesh horneada = new Mesh();
            smr.BakeMesh(horneada);
            Pieza(raiz.transform, horneada, aLocal * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one));
            piezas++;
        }

        if (piezas == 0)
        {
            Destroy(raiz);
            return null;
        }
        return raiz;
    }

    // Activo respecto a la raíz copiada (la raíz puede estar desactivada si la herramienta no está equipada).
    private static bool VisibleDentroDe(Transform t, Transform raiz)
    {
        for (Transform actual = t; actual != null && actual != raiz; actual = actual.parent)
            if (!actual.gameObject.activeSelf) return false;
        return true;
    }

    private void Pieza(Transform padre, Mesh malla, Matrix4x4 m)
    {
        GameObject go = new GameObject("Pieza");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = m.GetColumn(3);
        go.transform.localRotation = m.rotation;
        go.transform.localScale = m.lossyScale;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = materialHolo;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // Formas provisionales hechas con primitivas hasta que haya modelos.
    private GameObject FormaProvisional(TiendaTrabajo.Articulo articulo)
    {
        GameObject raiz = new GameObject("Provisional_" + articulo);
        switch (articulo)
        {
            case TiendaTrabajo.Articulo.Iman: // herradura
                Primitiva(raiz, PrimitiveType.Cube, new Vector3(-0.22f, 0.12f, 0f), new Vector3(0.16f, 0.5f, 0.16f));
                Primitiva(raiz, PrimitiveType.Cube, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.16f, 0.5f, 0.16f));
                Primitiva(raiz, PrimitiveType.Cylinder, new Vector3(0f, -0.13f, 0f), new Vector3(0.6f, 0.08f, 0.16f), new Vector3(90f, 0f, 0f));
                Primitiva(raiz, PrimitiveType.Cube, new Vector3(-0.22f, 0.42f, 0f), new Vector3(0.17f, 0.12f, 0.17f));
                Primitiva(raiz, PrimitiveType.Cube, new Vector3(0.22f, 0.42f, 0f), new Vector3(0.17f, 0.12f, 0.17f));
                break;
            case TiendaTrabajo.Articulo.Martillo:
                Primitiva(raiz, PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.07f, 0.35f, 0.07f));
                Primitiva(raiz, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.17f, 0.17f));
                break;
            case TiendaTrabajo.Articulo.Botas:
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Primitiva(raiz, PrimitiveType.Cube, new Vector3(lado * 0.15f, 0.12f, 0f), new Vector3(0.2f, 0.38f, 0.2f));
                    Primitiva(raiz, PrimitiveType.Cube, new Vector3(lado * 0.15f, -0.1f, 0.1f), new Vector3(0.21f, 0.12f, 0.42f));
                }
                break;
            case TiendaTrabajo.Articulo.Jetpack:
                Primitiva(raiz, PrimitiveType.Cube, Vector3.zero, new Vector3(0.36f, 0.48f, 0.16f));
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Primitiva(raiz, PrimitiveType.Capsule, new Vector3(lado * 0.22f, 0.02f, -0.06f), new Vector3(0.16f, 0.28f, 0.16f));
                    Primitiva(raiz, PrimitiveType.Cylinder, new Vector3(lado * 0.22f, -0.31f, -0.06f), new Vector3(0.1f, 0.05f, 0.1f));
                }
                break;
            default:
                Primitiva(raiz, PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.4f, new Vector3(20f, 30f, 0f));
                break;
        }
        return raiz;
    }

    private void Primitiva(GameObject padre, PrimitiveType tipo, Vector3 posicion, Vector3 escala, Vector3? rotacion = null)
    {
        GameObject p = GameObject.CreatePrimitive(tipo);
        Collider col = p.GetComponent<Collider>();
        if (col != null) Destroy(col);
        p.transform.SetParent(padre.transform, false);
        p.transform.localPosition = posicion;
        p.transform.localScale = escala;
        if (rotacion.HasValue) p.transform.localRotation = Quaternion.Euler(rotacion.Value);
        Renderer r = p.GetComponent<Renderer>();
        r.sharedMaterial = materialHolo;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Centra el modelo en su celda y lo escala para que quepa.
    private static void Ajustar(Transform modelo, Transform pivote, float tamanoMaximo)
    {
        modelo.position = Vector3.zero;
        modelo.rotation = Quaternion.identity;
        modelo.localScale = Vector3.one;

        Renderer[] rs = modelo.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        float mayor = Mathf.Max(b.size.x, b.size.y, b.size.z);
        float escala = mayor > 0.0001f ? tamanoMaximo / mayor : 1f;

        GameObject centro = new GameObject("Centro");
        centro.transform.SetParent(pivote, false);
        modelo.SetParent(centro.transform, false);
        modelo.localPosition = -b.center;
        modelo.localRotation = Quaternion.identity;
        modelo.localScale = Vector3.one;
        centro.transform.localScale = Vector3.one * escala;
    }

    // =====================================================
    // PROYECTOR DEL SUELO
    // =====================================================

    private void CrearProyector()
    {
        if (materialHolo == null) return;

        Vector3 punto = transform.position;
        Collider propio = GetComponent<Collider>();
        if (propio != null) punto = propio.bounds.center;

        RaycastHit hit;
        if (Physics.Raycast(punto + Vector3.up * 0.5f, Vector3.down, out hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            punto = hit.point;
        else if (propio != null)
            punto = new Vector3(punto.x, propio.bounds.min.y, punto.z);

        proyector = new GameObject("ProyectorTiendaHolografica").transform;
        proyector.position = punto + desplazamientoProyector + Vector3.up * 0.01f;

        float r = radioProyector;

        // Base: disco oscuro.
        GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Collider colDisco = disco.GetComponent<Collider>();
        if (colDisco != null) Destroy(colDisco);
        disco.name = "Base";
        disco.transform.SetParent(proyector, false);
        disco.transform.localScale = new Vector3(r * 2f, 0.025f, r * 2f);
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        if (lit != null)
        {
            Material mBase = new Material(lit);
            mBase.color = new Color(0.06f, 0.08f, 0.1f, 1f);
            disco.GetComponent<Renderer>().sharedMaterial = mBase;
        }

        Material plano = new Material(materialHolo);
        plano.SetFloat("_Plano", 1f);
        plano.SetFloat("_Intensidad", 2f);

        anillos = new Renderer[3];
        anillos[0] = ObjetoMalla("Anillo1", Corona(r * 0.55f, r * 0.68f, 64, 1f, 1f), plano, 0.04f);
        anillos[1] = ObjetoMalla("Anillo2", Corona(r * 0.8f, r * 0.86f, 64, 1f, 1f), plano, 0.04f);
        anillos[2] = ObjetoMalla("Anillo3", Corona(r * 0.95f, r * 1.0f, 64, 1f, 1f), plano, 0.04f);
        rayos = ObjetoMalla("Rayos", Rayos(r * 1.05f, r * 2.6f, 36), plano, 0.03f);
        haz = ObjetoMalla("Haz", Cono(r * 0.9f, r * 0.35f, 1.7f, 48), plano, 0.05f);
    }

    private Renderer ObjetoMalla(string nombre, Mesh malla, Material material, float altura)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(proyector, false);
        go.transform.localPosition = new Vector3(0f, altura, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return mr;
    }

    private void ActualizarProyector(float dt)
    {
        if (proyector == null) return;

        bool cerca = mesa != null && mesa.JugadorCerca;
        float objetivo = estado != Estado.Cerrada ? 1f : (cerca ? 0.55f : 0f);
        intensidadProyector = Mathf.MoveTowards(intensidadProyector, objetivo, dt * 2.5f);
        float t = Time.unscaledTime;

        for (int i = 0; i < anillos.Length; i++)
        {
            float pulso = 0.45f + 0.35f * Mathf.Sin(t * 2.2f - i * 0.9f);
            Alfa(anillos[i], Mathf.Clamp01(pulso + intensidadProyector * 0.5f));
            anillos[i].transform.localRotation = Quaternion.Euler(0f, t * (i % 2 == 0 ? 20f : -14f), 0f);
        }

        Alfa(rayos, intensidadProyector * (0.75f + 0.25f * Mathf.Sin(t * 5f)));
        rayos.transform.localRotation = Quaternion.Euler(0f, t * (8f + 30f * intensidadProyector), 0f);
        float escalaRayos = Mathf.Lerp(0.6f, 1f, intensidadProyector);
        rayos.transform.localScale = new Vector3(escalaRayos, 1f, escalaRayos);

        Alfa(haz, Mathf.Clamp01(intensidadProyector * 2f - 1f) * 0.5f);
    }

    private void Alfa(Renderer r, float a)
    {
        if (r == null) return;
        r.enabled = a > 0.003f;
        r.GetPropertyBlock(bloque);
        bloque.SetFloat("_Alpha", a);
        r.SetPropertyBlock(bloque);
    }

    // ----- mallas procedurales -----

    private static Mesh Corona(float interior, float exterior, int segmentos, float alfaDentro, float alfaFuera)
    {
        Vector3[] v = new Vector3[segmentos * 2 + 2];
        Color[] c = new Color[v.Length];
        int[] tri = new int[segmentos * 6];
        for (int i = 0; i <= segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v[i * 2] = d * interior;
            v[i * 2 + 1] = d * exterior;
            c[i * 2] = new Color(1f, 1f, 1f, alfaDentro);
            c[i * 2 + 1] = new Color(1f, 1f, 1f, alfaFuera);
            if (i < segmentos)
            {
                int k = i * 6, b = i * 2;
                tri[k] = b; tri[k + 1] = b + 1; tri[k + 2] = b + 2;
                tri[k + 3] = b + 1; tri[k + 4] = b + 3; tri[k + 5] = b + 2;
            }
        }
        Mesh m = new Mesh { name = "Corona" };
        m.vertices = v; m.colors = c; m.triangles = tri;
        m.RecalculateNormals();
        return m;
    }

    // Rayos en abanico sobre el suelo: cuñas que se desvanecen hacia fuera.
    private static Mesh Rayos(float interior, float exterior, int cantidad)
    {
        List<Vector3> v = new List<Vector3>();
        List<Color> c = new List<Color>();
        List<int> tri = new List<int>();
        float ancho = Mathf.PI * 2f / cantidad * 0.42f;
        for (int i = 0; i < cantidad; i++)
        {
            float a = i * Mathf.PI * 2f / cantidad;
            int b = v.Count;
            v.Add(new Vector3(Mathf.Cos(a - ancho * 0.5f), 0f, Mathf.Sin(a - ancho * 0.5f)) * interior);
            v.Add(new Vector3(Mathf.Cos(a + ancho * 0.5f), 0f, Mathf.Sin(a + ancho * 0.5f)) * interior);
            v.Add(new Vector3(Mathf.Cos(a - ancho), 0f, Mathf.Sin(a - ancho)) * exterior);
            v.Add(new Vector3(Mathf.Cos(a + ancho), 0f, Mathf.Sin(a + ancho)) * exterior);
            c.Add(new Color(1f, 1f, 1f, 0.9f)); c.Add(new Color(1f, 1f, 1f, 0.9f));
            c.Add(new Color(1f, 1f, 1f, 0f)); c.Add(new Color(1f, 1f, 1f, 0f));
            tri.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
        }
        Mesh m = new Mesh { name = "Rayos" };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        return m;
    }

    // Haz de luz: cono abierto que se desvanece hacia arriba.
    private static Mesh Cono(float radioAbajo, float radioArriba, float alto, int segmentos)
    {
        Vector3[] v = new Vector3[(segmentos + 1) * 2];
        Color[] c = new Color[v.Length];
        int[] tri = new int[segmentos * 6];
        for (int i = 0; i <= segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v[i * 2] = d * radioAbajo;
            v[i * 2 + 1] = d * radioArriba + Vector3.up * alto;
            c[i * 2] = new Color(1f, 1f, 1f, 0.55f);
            c[i * 2 + 1] = new Color(1f, 1f, 1f, 0f);
            if (i < segmentos)
            {
                int k = i * 6, b = i * 2;
                tri[k] = b; tri[k + 1] = b + 1; tri[k + 2] = b + 2;
                tri[k + 3] = b + 1; tri[k + 4] = b + 3; tri[k + 5] = b + 2;
            }
        }
        Mesh m = new Mesh { name = "Haz" };
        m.vertices = v; m.colors = c; m.triangles = tri;
        m.RecalculateNormals();
        return m;
    }
}
