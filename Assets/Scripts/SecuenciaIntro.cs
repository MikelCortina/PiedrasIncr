using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guion de la INTRODUCCIÓN (objetivo ≤ 2 min). Cada paso avanza por EVENTOS del juego, no por tiempo:
///  1 Llegas al planeta            (CambioCamaraIntro.OnNaveAterrizada)
///  2 Abres la puerta              (CambioCamaraIntro.OnPuertaAbierta / OnIntroTerminada)
///  3 Enchufas la manguera         (AgarrarManguera.OnMangueraConectada)
///  4-5 Botro: "¡Hey!" / "¿Ves esa máquina?..."  (cada línea espera a que acabe la anterior)
///  6 Vas hacia la piedra          (jugador cerca de una piedra, o ya la ha pateado)
///  7 Tutorial de patada           (SistemaPatada.OnPiedraPateada)
///  8 Llevas la piedra             (una piedra cerca del agujero)
///  9 Botro: desgaste y trampilla
/// 10 Metes la piedra              (AgujeroSimple.OnPiedraTragada)
/// 11 Zoom a la pantalla de gasolina (+XX L)  (ContadorGasolinera.OnGasolinaAnadida)
/// 12 Botro: "recoge los restos"   → termina al ganar la primera moneda (Cartera.OnMonedasCambiadas)
/// Los eventos se "apuntan" aunque lleguen antes de tiempo: si el jugador se adelanta, el guion no se atasca.
/// </summary>
public class SecuenciaIntro : MonoBehaviour
{
    public enum Paso
    {
        EsperandoLlegada, AbrirPuerta, Manguera, BotroPresentacion, IrAPiedra, Patada,
        LlevarPiedra, BotroTrampilla, MeterPiedra, ZoomGasolina, BotroRestos, RecogerRestos, Terminado
    }

    /// <summary>Cambio de paso (para UI, analíticas o pruebas).</summary>
    public static event Action<Paso> OnPasoCambiado;
    /// <summary>Intro completa: a partir de aquí hablan los hitos del intermedio.</summary>
    public static event Action OnTutorialTerminado;
    public static bool TutorialTerminado { get; private set; }

    [Header("General")]
    [Tooltip("Marca para no hacer el tutorial (partida cargada, pruebas).")]
    public bool omitirTutorial = false;
    [Tooltip("Escribe en la consola cada paso del guion ([GUION]).")]
    public bool registrarEnConsola = true;

    [Header("Líneas de Botro (hoja INTRODUCCIÓN)")]
    [TextArea(1, 3)] public string lineaHey = "¡Hey, qué haces!";
    [TextArea(1, 3)] public string lineaMaquina = "¿Ves esa máquina? Consigue una piedra lunar y tráela hasta aquí.";
    [TextArea(1, 3)] public string lineaTrampilla = "Mira el desgaste, fíjate en esa trampilla: tragará tus piedras y te recompensará por ello.";
    [TextArea(1, 3)] public string lineaRestos = "No te olvides de recoger los restos, te servirán para conseguir monedas.";

    [Header("Objetivos en pantalla")]
    public string objetivoPalanca = "Baja la palanca para salir de la nave";
    public string objetivoManguera = "Coge la manguera [E] y enchúfala a la nave";
    public string objetivoBuscarPiedra = "Busca una piedra lunar";
    public string objetivoPatada = "Patea la piedra  [Clic izquierdo]";
    public string objetivoLlevar = "Lleva la piedra a patadas hasta la máquina";
    public string objetivoMeter = "Métela por la trampilla";
    public string objetivoRestos = "Recoge los restos con clic izquierdo";

    [Header("Distancias")]
    [Tooltip("Distancia del jugador a una piedra para pasar al tutorial de patada.")]
    public float distanciaLlegarPiedra = 6f;
    [Tooltip("Distancia de una piedra al agujero para que Botro hable de la trampilla.")]
    public float distanciaPiedraAgujero = 12f;

    [Header("Zoom a la pantalla de gasolina")]
    [Tooltip("Vacío = se busca sola: el objeto que muestra la imagen de la cámara del cartel de gasoil.")]
    public Transform pantallaGasolina;
    [Tooltip("FOV máximo durante el zoom (se reduce más si hace falta para encuadrar la pantalla).")]
    public float fovZoom = 30f;
    [Tooltip("FOV mínimo del zoom, para que no se acerque demasiado.")]
    public float fovMinimoZoom = 8f;
    [Tooltip("Margen alrededor de la pantalla (1 = justa).")]
    public float margenEncuadre = 1.6f;
    public float duracionIdaVuelta = 0.6f;
    [Tooltip("Segundos mirando el +XX L.")]
    public float segundosMirando = 2.5f;
    [Tooltip("Scripts que se desactivan durante el zoom. Vacío = la mirada (MenuBotUI) y el movimiento.")]
    public MonoBehaviour[] bloquearDuranteZoom;

    [Header("Referencias (vacío = se buscan solas)")]
    public CambioCamaraIntro intro;
    public AgujeroSimple agujero;
    public ContadorGasolinera gasolinera;
    public Cartera cartera;
    public Transform jugador;

    [Header("Estilo (vacío = el del escáner de piedras)")]
    [Tooltip("Fuente con tildes y eñes para el objetivo. Vacío = la fuente por defecto.")]
    public Font fuenteTexto;
    public float escala = 1f;

    public Paso PasoActual { get; private set; } = Paso.EsperandoLlegada;

    private const string EvLlegada = "llegada";
    private const string EvPuerta = "puerta";
    private const string EvIntroFin = "intro_fin";
    private const string EvManguera = "manguera";
    private const string EvPatada = "patada";
    private const string EvTragada = "tragada";
    private const string EvGasolina = "gasolina";
    private const string EvMoneda = "moneda";

    private readonly HashSet<string> ocurrido = new HashSet<string>();
    private string objetivo = "";
    private float objetivoDesde;
    private int monedasPrevias;
    private GUIStyle estiloLED;
    private GUIStyle estiloTexto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEventos()
    {
        OnPasoCambiado = null;
        OnTutorialTerminado = null;
        TutorialTerminado = false;
    }

    private void OnEnable()
    {
        CambioCamaraIntro.OnNaveAterrizada += AlAterrizar;
        CambioCamaraIntro.OnPuertaAbierta += AlAbrirPuerta;
        CambioCamaraIntro.OnIntroTerminada += AlTerminarIntro;
        AgarrarManguera.OnMangueraConectada += AlConectarManguera;
        SistemaPatada.OnPiedraPateada += AlPatear;
        AgujeroSimple.OnPiedraTragada += AlTragar;
        ContadorGasolinera.OnGasolinaAnadida += AlAnadirGasolina;
        BotroDice.OnLineaTerminada += AlTerminarLinea;
    }

    private void OnDisable()
    {
        CambioCamaraIntro.OnNaveAterrizada -= AlAterrizar;
        CambioCamaraIntro.OnPuertaAbierta -= AlAbrirPuerta;
        CambioCamaraIntro.OnIntroTerminada -= AlTerminarIntro;
        AgarrarManguera.OnMangueraConectada -= AlConectarManguera;
        SistemaPatada.OnPiedraPateada -= AlPatear;
        AgujeroSimple.OnPiedraTragada -= AlTragar;
        ContadorGasolinera.OnGasolinaAnadida -= AlAnadirGasolina;
        BotroDice.OnLineaTerminada -= AlTerminarLinea;
        if (cartera != null) cartera.OnMonedasCambiadas -= AlCambiarMonedas;
    }

    private void Awake()
    {
        // Es estático: cada partida empieza con el tutorial sin hacer.
        TutorialTerminado = false;
    }

    private void AlAterrizar() { Marcar(EvLlegada); }
    private void AlAbrirPuerta() { Marcar(EvLlegada); Marcar(EvPuerta); }
    private void AlTerminarIntro() { Marcar(EvLlegada); Marcar(EvPuerta); Marcar(EvIntroFin); }
    private void AlConectarManguera() { Marcar(EvManguera); }
    private void AlPatear(Rigidbody piedra) { Marcar(EvPatada); }
    private void AlTragar(AgujeroSimple a, float pureza, float gasolina) { Marcar(EvTragada); }
    private void AlAnadirGasolina(float fraccion, float litros) { Marcar(EvGasolina); }
    private void AlTerminarLinea(string id) { Marcar("linea:" + id); }

    private void AlCambiarMonedas(int total)
    {
        if (total > monedasPrevias) Marcar(EvMoneda);
        monedasPrevias = total;
    }

    private void Marcar(string evento)
    {
        if (ocurrido.Add(evento)) Registrar("Evento: " + evento);
    }
    private bool Ocurrio(string evento) { return ocurrido.Contains(evento); }

    private void Start()
    {
        if (intro == null) intro = FindFirstObjectByType<CambioCamaraIntro>();
        if (agujero == null) agujero = FindFirstObjectByType<AgujeroSimple>();
        if (gasolinera == null) gasolinera = FindFirstObjectByType<ContadorGasolinera>();
        if (cartera == null) cartera = FindFirstObjectByType<Cartera>();
        if (jugador == null)
        {
            GameObject j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) jugador = j.transform;
        }
        if (cartera != null)
        {
            monedasPrevias = cartera.Monedas;
            cartera.OnMonedasCambiadas += AlCambiarMonedas;
        }

        // Sin intro (o intro saltada) no llegarán sus eventos.
        if (intro == null || !intro.isActiveAndEnabled || intro.OmitirIntro || CambioCamaraIntro.IntroTerminada)
        {
            Marcar(EvLlegada);
            Marcar(EvPuerta);
            if (intro == null || CambioCamaraIntro.IntroTerminada) Marcar(EvIntroFin);
        }

        Registrar("Iniciado. Intro: " + (intro == null ? "no encontrada" : (intro.OmitirIntro ? "saltada" : "normal")) +
                  " | Jugador: " + (jugador != null ? jugador.name : "NO ENCONTRADO") +
                  " | Agujero: " + (agujero != null) + " | Gasolinera: " + (gasolinera != null) +
                  " | Botro: " + (BotroDice.Instancia != null));

        if (omitirTutorial || GuardadoPartidaJuego.InicioActual == GuardadoPartidaJuego.TipoInicio.PartidaCargada)
        {
            Terminar();
            return;
        }

        StartCoroutine(Guion());
    }

    // =====================================================
    // GUION
    // =====================================================

    private IEnumerator Guion()
    {
        // 1 · Llegas al planeta
        IrA(Paso.EsperandoLlegada, "");
        yield return new WaitUntil(() => Ocurrio(EvLlegada) || JugadorTieneControl());

        // 2 · Abres la puerta (bajas la palanca y la intro devuelve el control)
        if (!JugadorTieneControl()) IrA(Paso.AbrirPuerta, objetivoPalanca);
        yield return new WaitUntil(() => Ocurrio(EvPuerta) || JugadorTieneControl());
        IrA(Paso.AbrirPuerta, "");
        yield return new WaitUntil(() => Ocurrio(EvIntroFin) || JugadorTieneControl());

        // 3 · Enchufas la manguera
        IrA(Paso.Manguera, objetivoManguera);
        yield return Esperar(EvManguera);

        // 4-5 · Botro se presenta
        IrA(Paso.BotroPresentacion, "");
        yield return DecirYEsperar("hey", lineaHey);
        yield return DecirYEsperar("maquina", lineaMaquina);

        // 6 · Vas hacia la piedra
        IrA(Paso.IrAPiedra, objetivoBuscarPiedra);
        yield return new WaitUntil(() => Ocurrio(EvPatada) || JugadorCercaDeUnaPiedra());

        // 7 · Tutorial de patada
        IrA(Paso.Patada, objetivoPatada);
        yield return Esperar(EvPatada);

        // 8 · Llevas la piedra
        IrA(Paso.LlevarPiedra, objetivoLlevar);
        yield return new WaitUntil(() => Ocurrio(EvTragada) || PiedraCercaDelAgujero());

        // 9 · Botro: desgaste y trampilla (si aún no la ha metido)
        if (!Ocurrio(EvTragada))
        {
            IrA(Paso.BotroTrampilla, objetivoMeter);
            yield return DecirYEsperar("trampilla", lineaTrampilla);
        }

        // 10 · Meter la piedra en el agujero
        IrA(Paso.MeterPiedra, objetivoMeter);
        yield return Esperar(EvTragada);

        // 11 · Zoom a la pantalla de gasolina (+XX L)
        IrA(Paso.ZoomGasolina, "");
        yield return ZoomGasolina();

        // 12 · Botro: recoge los restos → primera moneda
        IrA(Paso.BotroRestos, "");
        yield return DecirYEsperar("restos", lineaRestos);
        IrA(Paso.RecogerRestos, objetivoRestos);
        yield return Esperar(EvMoneda);

        Terminar();
    }

    // Red de seguridad: si la cámara en primera persona ya está activa, la intro ha acabado
    // aunque no llegara su evento (la intro la apaga mientras dura la cinemática).
    private CamaraPrimeraPersona camaraJugador;
    private bool JugadorTieneControl()
    {
        if (CambioCamaraIntro.IntroTerminada) return true;
        if (camaraJugador == null && Camera.main != null) camaraJugador = Camera.main.GetComponent<CamaraPrimeraPersona>();
        return camaraJugador != null && camaraJugador.isActiveAndEnabled;
    }

    private void Registrar(string mensaje)
    {
        if (registrarEnConsola) Debug.Log("[GUION] " + mensaje, this);
    }

    private void Terminar()
    {
        IrA(Paso.Terminado, "");
        if (TutorialTerminado) return;
        TutorialTerminado = true;
        OnTutorialTerminado?.Invoke();
    }

    private void IrA(Paso paso, string textoObjetivo)
    {
        if (objetivo != textoObjetivo) objetivoDesde = Time.unscaledTime;
        objetivo = textoObjetivo;
        if (PasoActual == paso) return;
        PasoActual = paso;
        Registrar("Paso → " + paso + (string.IsNullOrEmpty(textoObjetivo) ? "" : " | Objetivo: " + textoObjetivo));
        OnPasoCambiado?.Invoke(paso);
    }

    private IEnumerator Esperar(string evento)
    {
        while (!Ocurrio(evento)) yield return null;
    }

    private IEnumerator DecirYEsperar(string id, string texto)
    {
        BotroDice.Decir(id, texto);
        yield return Esperar("linea:" + id);
    }

    private bool JugadorCercaDeUnaPiedra()
    {
        if (jugador == null || GestorPiedras.Instancia == null) return false;
        float d2 = distanciaLlegarPiedra * distanciaLlegarPiedra;
        foreach (Rigidbody piedra in GestorPiedras.Instancia.Piedras)
            if (piedra != null && (piedra.position - jugador.position).sqrMagnitude <= d2)
                return true;
        return false;
    }

    private bool PiedraCercaDelAgujero()
    {
        if (agujero == null || GestorPiedras.Instancia == null) return false;
        float d2 = distanciaPiedraAgujero * distanciaPiedraAgujero;
        Vector3 centro = agujero.transform.position;
        foreach (Rigidbody piedra in GestorPiedras.Instancia.Piedras)
            if (piedra != null && (piedra.position - centro).sqrMagnitude <= d2)
                return true;
        return false;
    }

    // =====================================================
    // ZOOM A LA GASOLINA
    // =====================================================

    private IEnumerator ZoomGasolina()
    {
        if (gasolinera == null) yield break;
        yield return Esperar(EvGasolina);

        Camera cam = Camera.main;
        if (cam == null) yield break;

        Bounds pantalla;
        if (!ResolverPantallaGasolina(out pantalla))
        {
            Registrar("Zoom: no encuentro la pantalla de gasolina; asigna 'Pantalla Gasolina'.");
            yield break;
        }

        // Bloquear la mirada (CamaraPrimeraPersona reescribe la rotación cada frame) y el movimiento.
        CamaraPrimeraPersona camaraFPS = cam.GetComponent<CamaraPrimeraPersona>();
        bool camaraYaBloqueada = camaraFPS != null && camaraFPS.EstaBloqueadaPorUI();
        if (camaraFPS != null) camaraFPS.SetBloqueadaPorUI(true);

        List<MonoBehaviour> bloqueados = new List<MonoBehaviour>();
        if (bloquearDuranteZoom != null && bloquearDuranteZoom.Length > 0)
        {
            bloqueados.AddRange(bloquearDuranteZoom);
        }
        else
        {
            MenuBotUI menu = FindFirstObjectByType<MenuBotUI>();
            if (menu != null && menu.controladorMiradaJugador != null) bloqueados.Add(menu.controladorMiradaJugador);
            MovimientoPersonaje movimiento = FindFirstObjectByType<MovimientoPersonaje>();
            if (movimiento != null) bloqueados.Add(movimiento);
        }

        List<MonoBehaviour> desactivados = new List<MonoBehaviour>();
        foreach (MonoBehaviour script in bloqueados)
        {
            if (script != null && script.enabled && script != camaraFPS)
            {
                script.enabled = false;
                desactivados.Add(script);
            }
        }

        Transform t = cam.transform;
        Quaternion rotacionInicial = t.localRotation;
        float fovInicial = cam.fieldOfView;

        // Mirar al centro de la pantalla y ajustar el FOV para que llene el plano.
        Vector3 haciaPantalla = pantalla.center - t.position;
        Quaternion rotacionZoomMundo = Quaternion.LookRotation(haciaPantalla, Vector3.up);
        Quaternion rotacionZoom = t.parent != null ? Quaternion.Inverse(t.parent.rotation) * rotacionZoomMundo : rotacionZoomMundo;
        float distancia = Mathf.Max(0.5f, haciaPantalla.magnitude);
        float tamano = Mathf.Max(pantalla.extents.x, pantalla.extents.y, pantalla.extents.z) * 2f;
        float fovEncuadre = 2f * Mathf.Atan(tamano * 0.5f * margenEncuadre / distancia) * Mathf.Rad2Deg;
        float fovObjetivo = Mathf.Clamp(fovEncuadre, fovMinimoZoom, Mathf.Min(fovZoom, fovInicial));

        Registrar("Zoom a '" + nombrePantalla + "' a " + distancia.ToString("0.0") + " m, FOV " + fovObjetivo.ToString("0"));

        yield return Interpolar(cam, rotacionInicial, rotacionZoom, fovInicial, fovObjetivo);
        yield return new WaitForSeconds(segundosMirando);
        yield return Interpolar(cam, rotacionZoom, rotacionInicial, fovObjetivo, fovInicial);

        t.localRotation = rotacionInicial;
        cam.fieldOfView = fovInicial;
        if (camaraFPS != null) camaraFPS.SetBloqueadaPorUI(camaraYaBloqueada);
        foreach (MonoBehaviour script in desactivados)
            if (script != null) script.enabled = true;
    }

    private string nombrePantalla = "";

    // La pantalla física es la que muestra la RenderTexture de la cámara del cartel de gasoil.
    private bool ResolverPantallaGasolina(out Bounds limites)
    {
        limites = new Bounds();

        if (pantallaGasolina != null)
        {
            Renderer propio = pantallaGasolina.GetComponentInChildren<Renderer>();
            limites = propio != null ? propio.bounds : new Bounds(pantallaGasolina.position, Vector3.one);
            nombrePantalla = pantallaGasolina.name;
            return true;
        }

        Canvas lienzo = gasolinera.textoLED != null ? gasolinera.textoLED.canvas : gasolinera.GetComponentInParent<Canvas>();
        if (lienzo != null) lienzo = lienzo.rootCanvas;

        if (lienzo != null && lienzo.renderMode == RenderMode.WorldSpace)
        {
            limites = new Bounds(lienzo.transform.position, Vector3.one);
            nombrePantalla = lienzo.name;
            return true;
        }

        RenderTexture textura = lienzo != null && lienzo.worldCamera != null ? lienzo.worldCamera.targetTexture : null;
        if (textura == null) return false;

        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) continue;
                foreach (int id in m.GetTexturePropertyNameIDs())
                {
                    if (m.GetTexture(id) == textura)
                    {
                        limites = r.bounds;
                        nombrePantalla = r.name;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator Interpolar(Camera cam, Quaternion desde, Quaternion hasta, float fovDesde, float fovHasta)
    {
        float tiempo = 0f;
        while (tiempo < duracionIdaVuelta)
        {
            tiempo += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / duracionIdaVuelta));
            cam.transform.localRotation = Quaternion.Slerp(desde, hasta, k);
            cam.fieldOfView = Mathf.Lerp(fovDesde, fovHasta, k);
            yield return null;
        }
    }

    // =====================================================
    // OBJETIVO EN PANTALLA (estilo cartel LED)
    // =====================================================

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(objetivo) || Event.current.type != EventType.Repaint) return;

        AimAssistVisual estilo = AimAssistVisual.Instancia;
        Color placa = estilo != null ? estilo.colorPlaca : new Color(0.035f, 0.025f, 0.09f, 0.88f);
        Color neon = estilo != null ? estilo.colorNeon : new Color(0.926f, 0.887f, 1f, 1f);
        Color alto = estilo != null ? estilo.colorNeonAlto : new Color(0.914f, 0.616f, 0.992f, 1f);
        Font fuenteLED = estilo != null ? estilo.fuentePanel : null;

        if (estiloLED == null || estiloLED.font != fuenteLED)
        {
            estiloLED = new GUIStyle { font = fuenteLED, clipping = TextClipping.Overflow };
            estiloLED.normal.textColor = Color.white;
        }
        if (estiloTexto == null || estiloTexto.font != fuenteTexto)
        {
            estiloTexto = new GUIStyle { font = fuenteTexto, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
            estiloTexto.normal.textColor = Color.white;
        }

        float e = Screen.height / 1080f * escala;
        float aparicion = Mathf.Clamp01((Time.unscaledTime - objetivoDesde) / 0.25f);
        float pop = 1f + (1f - aparicion) * 0.12f;
        float k = e * pop;

        float ancho = 560f * k, altoPanel = 62f * k;
        Rect r = new Rect((Screen.width - ancho) * 0.5f, 28f * e, ancho, altoPanel);

        AimAssistVisual.Rectangulo(r, AimAssistVisual.ConAlfa(placa, placa.a * aparicion), 4f * k);
        AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(neon, 0.7f * aparicion), k, 4f * k);
        AimAssistVisual.EsquinasHUD(r, AimAssistVisual.ConAlfa(alto, aparicion), k);

        float m = 16f * k;
        AimAssistVisual.TextoNeon(estiloLED, new Rect(r.x + m, r.y + 6f * k, r.width - 2f * m, 16f * k), "OBJETIVO", 14f * k, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(alto, aparicion), 0.5f);
        AimAssistVisual.TextoNeon(estiloTexto, new Rect(r.x + m, r.y + 24f * k, r.width - 2f * m, 30f * k), objetivo, 21f * k, TextAnchor.MiddleCenter, AimAssistVisual.ConAlfa(neon, aparicion), 0.3f);
    }
}
