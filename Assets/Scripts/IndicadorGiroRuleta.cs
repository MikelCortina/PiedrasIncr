using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Indicador holográfico del sentido de giro para las válvulas que se abren girándolas
/// (RuletaInteractivaGasoil).
///
/// Cuando el jugador mira una válvula de cerca aparece pegado a ella, en su mismo plano (en
/// perspectiva), un HUD holográfico de nave espacial:
///  - Anillo segmentado que se enciende con el progreso de la apertura (y se apaga si se suelta).
///  - Corona exterior de marcas técnicas que gira despacio hacia el lado de apertura.
///  - Flechas ">>>" que corren por dentro del anillo marcando el sentido de giro.
///  - Placa con esquinas de visor: "MANTÉN CLIC Y GIRA", "APERTURA 45 %" o "SENTIDO INCORRECTO",
///    con un icono de giro.
///  - Si se gira al revés todo pasa a rojo; al abrirse del todo, onda expansiva y "DESBLOQUEADA".
///
/// No hay que montar nada: se añade solo a todas las RuletaInteractivaGasoil de la escena.
/// El sentido se calcula igual que GestorRuletasFPS (antihorario en pantalla = abrir si la
/// sensibilidad es positiva); si en alguna válvula sale al revés, marca "Invertir Sentido".
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RuletaInteractivaGasoil))]
public class IndicadorGiroRuleta : MonoBehaviour
{
    [Header("Cuándo se ve")]
    [Tooltip("Distancia máxima a la cámara. 0 = la distancia de interacción del GestorRuletasFPS + 1,5 m.")]
    [Min(0f)] public float distanciaMostrar = 0f;
    [Tooltip("La válvula tiene que estar cerca del centro de la pantalla (fracción de la altura).")]
    [Range(0.1f, 1f)] public float cercaniaCentroPantalla = 0.45f;
    [Min(0.1f)] public float velocidadFundido = 5f;

    [Header("Anillo")]
    [Tooltip("Tamaño del anillo respecto a la válvula.")]
    [Min(0.5f)] public float escalaAnillo = 1.2f;
    [Tooltip("Separación extra entre la válvula y el anillo (metros).")]
    public float margenAnillo = 0.05f;
    [Tooltip("Tramos del anillo de progreso.")]
    [Min(4)] public int segmentos = 16;
    [Tooltip("Marcas de la corona exterior.")]
    [Min(12)] public int marcas = 72;

    [Header("Flechas que marcan el sentido")]
    [Tooltip("Grupos de flechas >>> que recorren el anillo.")]
    [Min(1)] public int gruposFlechas = 2;
    [Tooltip("Vueltas por segundo de las flechas en reposo (al girar bien van más rápido).")]
    public float vueltasPorSegundo = 0.25f;
    [Tooltip("Marca esto si en esta válvula la flecha señala el lado contrario al que abre.")]
    public bool invertirSentido = false;

    [Header("Tema")]
    [Tooltip("Usa los colores y la fuente del TemaUINeon (estilo del escáner de piedras) en vez de los de abajo.")]
    public bool usarTemaUINeon = true;

    [Header("Colores (holograma, si no se usa el tema)")]
    public Color colorHolograma = new Color(0.30f, 0.92f, 1f, 1f);
    public Color colorNucleo = new Color(0.86f, 1f, 1f, 1f);
    public Color colorListo = new Color(0.45f, 1f, 0.72f, 1f);
    public Color colorAviso = new Color(1f, 0.30f, 0.34f, 1f);

    [Header("Textos")]
    public bool mostrarTexto = true;
    public string textoCabecera = "VALVE CONTROL";
    public string textoAyuda = "MANTÉN CLIC Y GIRA";
    public string textoProgreso = "APERTURA";
    public string textoAlReves = "SENTIDO INCORRECTO";
    public string textoAbierta = "DESBLOQUEADA";

    private RuletaInteractivaGasoil ruleta;
    private Renderer render;
    private GestorRuletasFPS gestor;
    private Camera cam;
    private float radioMundo = 0.5f;
    private float signo = 1f;

    private float alfa;
    private float fase;
    private float faseMarcas;
    private float giroReciente;
    private float tiempoVisto = -999f;
    private float avisoAlReves;
    private float destello;
    private bool estabaAbierta;
    private bool haGirado;
    private GUIStyle estiloTexto;
    private GUIStyle estiloLED;

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
        foreach (RuletaInteractivaGasoil r in FindObjectsByType<RuletaInteractivaGasoil>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r != null && r.GetComponent<IndicadorGiroRuleta>() == null)
                r.gameObject.AddComponent<IndicadorGiroRuleta>();
        }
    }

    // =====================================================
    // CICLO DE VIDA
    // =====================================================

    private void Start()
    {
        AplicarTema();
        ruleta = GetComponent<RuletaInteractivaGasoil>();
        render = GetComponent<Renderer>();
        if (render == null) render = GetComponentInChildren<Renderer>();
        radioMundo = CalcularRadioMundo();

        gestor = FindAnyObjectByType<GestorRuletasFPS>();
        signo = gestor != null && gestor.sensibilidadGiro < 0f ? -1f : 1f;
        if (invertirSentido) signo = -signo;

        estabaAbierta = ruleta.Abierta;
    }

    private void Update()
    {
        if (ruleta == null) return;
        if (cam == null || !cam.isActiveAndEnabled)
            cam = gestor != null && gestor.camaraPrincipal != null ? gestor.camaraPrincipal : Camera.main;

        float dt = Time.deltaTime;

        // Giros nuevos del jugador (uno por frame mientras arrastra).
        if (ruleta.TiempoUltimoGiro != tiempoVisto)
        {
            tiempoVisto = ruleta.TiempoUltimoGiro;
            giroReciente += ruleta.UltimoDeltaGiro;
        }
        giroReciente *= Mathf.Exp(-dt * 4f);

        bool girando = Time.time - ruleta.TiempoUltimoGiro < 0.2f;
        if (girando && giroReciente < -15f) avisoAlReves = 1f;
        if (ruleta.Progreso > 0.03f) haGirado = true;
        avisoAlReves = Mathf.MoveTowards(avisoAlReves, 0f, dt * 1.5f);

        bool abierta = ruleta.Abierta;
        if (abierta && !estabaAbierta) destello = 1f;
        estabaAbierta = abierta;
        destello = Mathf.MoveTowards(destello, 0f, dt * 0.9f);

        // Las flechas corren más cuando el jugador gira bien.
        float ritmo = girando && giroReciente > 5f ? 2.5f : 1f;
        fase = Mathf.Repeat(fase + dt * vueltasPorSegundo * ritmo, 1f);
        faseMarcas = Mathf.Repeat(faseMarcas + dt * 0.03f * ritmo, 1f);

        bool mostrar = !abierta && (girando || DebeVerse());
        alfa = Mathf.MoveTowards(alfa, mostrar ? 1f : 0f, dt * velocidadFundido);
    }

    private bool DebeVerse()
    {
        if (cam == null || !cam.isActiveAndEnabled) return false;
        if (Cursor.visible && Cursor.lockState == CursorLockMode.None) return false; // hay un menú abierto

        Vector3 centro = Centro();
        float maximo = distanciaMostrar > 0f ? distanciaMostrar : (gestor != null ? gestor.distanciaInteraccion + 1.5f : 6.5f);
        if ((centro - cam.transform.position).sqrMagnitude > maximo * maximo) return false;

        Vector3 enPantalla = cam.WorldToScreenPoint(centro);
        if (enPantalla.z <= 0f) return false;

        Vector2 desdeCentro = new Vector2(enPantalla.x - Screen.width * 0.5f, enPantalla.y - Screen.height * 0.5f);
        return desdeCentro.magnitude <= Screen.height * cercaniaCentroPantalla;
    }

    private Vector3 Centro()
    {
        return render != null ? render.bounds.center : transform.position;
    }

    private float CalcularRadioMundo()
    {
        MeshFilter filtro = render != null ? render.GetComponent<MeshFilter>() : null;
        if (filtro != null && filtro.sharedMesh != null)
        {
            Vector3 e = filtro.sharedMesh.bounds.extents;
            Vector3 s = filtro.transform.lossyScale;
            return Mathf.Max(Mathf.Abs(e.x * s.x), Mathf.Abs(e.y * s.y), Mathf.Abs(e.z * s.z));
        }
        return render != null ? render.bounds.extents.magnitude * 0.7f : 0.5f;
    }

    // =====================================================
    // DIBUJO (holograma)
    // =====================================================

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || cam == null || ruleta == null) return;
        if (alfa <= 0.001f && destello <= 0.001f) return;

        Vector3 centroMundo = Centro();
        Vector3 c = cam.WorldToScreenPoint(centroMundo);
        if (c.z <= 0f) return;

        // El HUD va en el plano de la propia válvula (perpendicular a su eje de giro) y se ve en
        // perspectiva: de frente es un círculo y en una válvula tumbada, una elipse pegada a ella.
        if (!PrepararPlano(centroMundo)) return;

        float e = Screen.height / 1080f;
        float a = alfa * alfa * (3f - 2f * alfa);
        float tiempo = Time.unscaledTime;

        // Parpadeo suave de holograma.
        float holo = 0.9f + 0.1f * Mathf.Sin(tiempo * 37f) * Mathf.Sin(tiempo * 11.3f);
        float r = (radioMundo * escalaAnillo + margenAnillo) * Mathf.Lerp(0.88f, 1f, a); // se "despliega" al aparecer

        bool alReves = avisoAlReves > 0f;
        bool parpadeoAviso = alReves && Mathf.Repeat(tiempo * 5f, 1f) < 0.55f;
        float progreso = ruleta.Progreso;
        Color baseColor = alReves ? colorAviso : colorHolograma;
        Color colorLleno = alReves ? colorAviso : Color.Lerp(colorNucleo, colorListo, progreso * progreso);

        if (a > 0.001f)
        {
            float k = a * holo;

            // 1. Corona exterior de marcas que gira hacia el lado de apertura.
            for (int i = 0; i < marcas; i++)
            {
                float t = (float)i / marcas + faseMarcas;
                bool larga = i % 6 == 0;
                float r1 = r * 1.13f, r2 = r * (larga ? 1.22f : 1.17f);
                Segmento(PuntoAnillo(r1, t), PuntoAnillo(r2, t), (larga ? 2f : 1.2f) * e,
                         AimAssistVisual.ConAlfa(baseColor, (larga ? 0.55f : 0.28f) * k));
            }
            Arco(r * 1.1f, 0f, 1f, 1f * e, AimAssistVisual.ConAlfa(baseColor, 0.18f * k), 96);

            // 2. Anillo de progreso segmentado (se enciende tramo a tramo al aparecer).
            float hueco = 0.012f;
            for (int s = 0; s < segmentos; s++)
            {
                float t0 = (float)s / segmentos;
                float t1 = t0 + 1f / segmentos - hueco;
                if (t0 > a) break;

                Arco(r, t0, t1, 3f * e, AimAssistVisual.ConAlfa(baseColor, 0.22f * k), 48);

                float fin = Mathf.Min(t1, progreso);
                if (fin > t0)
                {
                    Color lleno = parpadeoAviso ? colorAviso : colorLleno;
                    Arco(r, t0, fin, 11f * e, AimAssistVisual.ConAlfa(lleno, 0.16f * k), 48); // halo
                    Arco(r, t0, fin, 4.5f * e, AimAssistVisual.ConAlfa(lleno, 0.95f * k), 48);
                }
            }

            // Marcador del frente del progreso.
            if (progreso > 0.001f)
            {
                Color marcador = alReves ? colorAviso : colorNucleo;
                Segmento(PuntoAnillo(r * 0.9f, progreso), PuntoAnillo(r * 1.12f, progreso), 3f * e, AimAssistVisual.ConAlfa(marcador, k));
            }

            // 3. Flechas ">>>" que corren por dentro del anillo en el sentido de apertura.
            float rf = r * 0.86f;
            Arco(rf, 0f, 1f, 1f * e, AimAssistVisual.ConAlfa(baseColor, 0.12f * k), 96);
            for (int g = 0; g < gruposFlechas; g++)
            {
                float cabeza = fase + (float)g / gruposFlechas;
                for (int j = 0; j < 3; j++)
                {
                    float t = cabeza - j * 0.032f;
                    float brillo = 1f - j * 0.3f;
                    Chevron(rf, t, r * 0.07f, 2.6f * e, AimAssistVisual.ConAlfa(alReves ? colorAviso : colorNucleo, brillo * k));
                }
            }

            // 4. Placa de texto bajo el anillo.
            if (mostrarTexto)
            {
                string texto;
                Color colorTexto;
                if (alReves) { texto = textoAlReves; colorTexto = colorAviso; }
                else if (!haGirado || progreso <= 0.001f) { texto = textoAyuda; colorTexto = colorNucleo; }
                else { texto = textoProgreso + "  " + Mathf.FloorToInt(progreso * 100f) + " %"; colorTexto = colorLleno; }

                Placa(new Vector2(c.x, BordeInferiorGui(r * 1.22f) + 14f * e), texto, colorTexto, baseColor, k, e);
            }
        }

        // 5. Al abrirse: onda expansiva doble y "DESBLOQUEADA".
        if (destello > 0.001f)
        {
            float d = destello;
            for (int o = 0; o < 2; o++)
            {
                float avance = Mathf.Clamp01((1f - d) * 1.4f - o * 0.25f);
                if (avance <= 0f) continue;
                float ro = r * (1f + 0.8f * avance);
                float ao = (1f - avance) * d;
                Arco(ro, 0f, 1f, 12f * e, AimAssistVisual.ConAlfa(colorListo, 0.15f * ao), 96);
                Arco(ro, 0f, 1f, 3f * e, AimAssistVisual.ConAlfa(colorListo, 0.9f * ao), 96);
            }
            if (mostrarTexto)
                Placa(new Vector2(c.x, BordeInferiorGui(r * 1.22f) + 14f * e), textoAbierta, colorListo, colorListo, d, e);
        }
    }

    // =====================================================
    // GEOMETRÍA EN EL PLANO DE LA VÁLVULA
    // =====================================================

    private Vector3 planoCentro, planoU, planoV;
    private float signoPlano = 1f;

    private bool PrepararPlano(Vector3 centroMundo)
    {
        Vector3 eje = transform.TransformDirection(ruleta.ejeGiroLocal);
        if (eje.sqrMagnitude < 0.000001f) eje = transform.forward;
        eje.Normalize();

        // "Arriba" del anillo = lo más alto que se ve de la válvula desde la cámara.
        Vector3 u = Vector3.ProjectOnPlane(cam.transform.up, eje);
        if (u.sqrMagnitude < 0.0001f) u = Vector3.ProjectOnPlane(cam.transform.forward, eje);
        if (u.sqrMagnitude < 0.000001f) return false;
        u.Normalize();

        planoCentro = centroMundo;
        planoU = u;
        planoV = Vector3.Cross(eje, u);

        // El gesto se mide en pantalla (antihorario = abrir): elegimos el sentido de recorrido del
        // plano que, visto desde la cámara, va hacia el mismo lado.
        Vector3 sc = cam.WorldToScreenPoint(centroMundo);
        Vector3 su = cam.WorldToScreenPoint(centroMundo + planoU * 0.1f) - sc;
        Vector3 sv = cam.WorldToScreenPoint(centroMundo + planoV * 0.1f) - sc;
        float cruz = su.x * sv.y - su.y * sv.x;
        if (Mathf.Abs(cruz) < 0.0001f) return false; // válvula vista de canto
        signoPlano = signo * Mathf.Sign(cruz);
        return true;
    }

    // t = 0 arriba; avanza en el sentido de apertura. r en metros. Devuelve coordenadas GUI (Y hacia abajo).
    private Vector2 PuntoAnillo(float r, float t)
    {
        float angulo = signoPlano * t * 2f * Mathf.PI;
        Vector3 mundo = planoCentro + (planoU * Mathf.Cos(angulo) + planoV * Mathf.Sin(angulo)) * r;
        Vector3 p = cam.WorldToScreenPoint(mundo);
        return new Vector2(p.x, Screen.height - p.y);
    }

    // Parte más baja del anillo en pantalla, para colocar la placa debajo.
    private float BordeInferiorGui(float r)
    {
        float maximo = float.MinValue;
        for (int i = 0; i < 32; i++)
            maximo = Mathf.Max(maximo, PuntoAnillo(r, i / 32f).y);
        return maximo;
    }

    private void Arco(float r, float t0, float t1, float grosor, Color color, int muestrasPorVuelta)
    {
        if (color.a <= 0.002f || t1 <= t0) return;
        int n = Mathf.Max(2, Mathf.CeilToInt((t1 - t0) * muestrasPorVuelta));
        Vector2 anterior = PuntoAnillo(r, t0);
        for (int i = 1; i <= n; i++)
        {
            Vector2 p = PuntoAnillo(r, Mathf.Lerp(t0, t1, (float)i / n));
            Segmento(anterior, p, grosor, color);
            anterior = p;
        }
    }

    // Flecha ">" tumbada en el plano de la válvula, apuntando hacia donde hay que girar.
    private void Chevron(float r, float t, float ancho, float grosor, Color color)
    {
        Vector2 punta = PuntoAnillo(r, t + 0.022f);
        Segmento(PuntoAnillo(r + ancho, t), punta, grosor, color);
        Segmento(PuntoAnillo(r - ancho, t), punta, grosor, color);
    }

    // =====================================================
    // PLACA DE TEXTO
    // =====================================================

    // Toma los colores del tema común (los del panel de escaneo de piedras).
    private void AplicarTema()
    {
        if (!usarTemaUINeon) return;

        TemaUINeon tema = TemaUINeon.Actual;
        colorHolograma = tema.colorNeonAlto;
        colorNucleo = tema.colorNeon;
        colorListo = tema.colorNeonMedio;
        colorAviso = tema.colorNeonBajo;
    }

    private Color ColorPlaca(float k)
    {
        if (!usarTemaUINeon) return new Color(0.01f, 0.04f, 0.07f, 0.72f * k);
        Color placa = TemaUINeon.Actual.colorPlaca;
        placa.a *= k;
        return placa;
    }

    private void Placa(Vector2 arribaCentro, string texto, Color colorTexto, Color colorMarco, float k, float e)
    {
        Font fuenteTema = usarTemaUINeon ? TemaUINeon.Actual.fuenteIMGUI : null;
        if (estiloTexto == null)
        {
            estiloTexto = new GUIStyle { font = fuenteTema, fontStyle = fuenteTema != null ? FontStyle.Normal : FontStyle.Bold, clipping = TextClipping.Overflow };
            estiloTexto.normal.textColor = Color.white;
        }
        AimAssistVisual estilo = AimAssistVisual.Instancia;
        Font fuenteLED = fuenteTema != null ? fuenteTema : (estilo != null ? estilo.fuentePanel : null);
        if (estiloLED == null || estiloLED.font != fuenteLED)
        {
            estiloLED = new GUIStyle { font = fuenteLED, clipping = TextClipping.Overflow };
            estiloLED.normal.textColor = Color.white;
        }

        estiloTexto.fontSize = Mathf.Max(8, Mathf.RoundToInt(19f * e));
        float anchoTexto = estiloTexto.CalcSize(new GUIContent(texto)).x;
        float icono = 26f * e;
        float ancho = Mathf.Max(220f * e, anchoTexto + icono + 40f * e);
        Rect r = new Rect(arribaCentro.x - ancho * 0.5f, arribaCentro.y, ancho, 52f * e);

        AimAssistVisual.Rectangulo(r, ColorPlaca(k), 3f * e);
        for (float ly = r.y + 3f * e; ly < r.yMax - 2f * e; ly += 3f * e) // líneas de barrido
            AimAssistVisual.Rectangulo(new Rect(r.x + 2f * e, ly, r.width - 4f * e, Mathf.Max(1f, 0.6f * e)), AimAssistVisual.ConAlfa(colorMarco, 0.05f * k), 0f);
        AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(colorMarco, 0.45f * k), e, 3f * e);
        AimAssistVisual.EsquinasHUD(r, AimAssistVisual.ConAlfa(colorMarco, k), e);

        float m = 12f * e;
        AimAssistVisual.TextoNeon(estiloLED, new Rect(r.x + m, r.y + 5f * e, r.width - 2f * m, 14f * e), textoCabecera, 12f * e,
                                  TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(colorMarco, 0.7f * k), 0.3f);

        // Icono de giro (flecha circular hacia el lado correcto).
        Vector2 centroIcono = new Vector2(r.x + m + icono * 0.5f, r.y + 33f * e);
        IconoGiro(centroIcono, icono * 0.38f, 2f * e, AimAssistVisual.ConAlfa(colorTexto, k));

        AimAssistVisual.TextoNeon(estiloTexto, new Rect(r.x + m + icono + 6f * e, r.y + 21f * e, r.width - 2f * m - icono, 26f * e), texto,
                                  19f * e, TextAnchor.MiddleLeft, AimAssistVisual.ConAlfa(colorTexto, k), 0.45f);
    }

    // Flecha circular de 3/4 de vuelta en pantalla, en el mismo sentido que el gesto (antihorario si signo > 0).
    private void IconoGiro(Vector2 centro, float radio, float grosor, Color color)
    {
        const int pasos = 14;
        float girar = Time.unscaledTime * 1.5f * signo; // gira despacio hacia el lado bueno
        Vector2 anterior = Vector2.zero;
        for (int i = 0; i <= pasos; i++)
        {
            float ang = girar + signo * (i / (float)pasos) * 1.5f * Mathf.PI;
            Vector2 p = new Vector2(centro.x + Mathf.Cos(ang) * radio, centro.y - Mathf.Sin(ang) * radio);
            if (i > 0) Segmento(anterior, p, grosor, color);
            anterior = p;
        }

        // Punta al final del arco.
        float angFin = girar + signo * 1.5f * Mathf.PI;
        Vector2 punta = new Vector2(centro.x + Mathf.Cos(angFin) * radio, centro.y - Mathf.Sin(angFin) * radio);
        Vector2 tangente = new Vector2(-Mathf.Sin(angFin), -Mathf.Cos(angFin)) * signo; // hacia donde avanza (Y abajo)
        Vector2 normal = new Vector2(Mathf.Cos(angFin), -Mathf.Sin(angFin));
        float largo = radio * 0.75f;
        Segmento(punta, punta - tangente * largo + normal * largo * 0.6f, grosor, color);
        Segmento(punta, punta - tangente * largo - normal * largo * 0.6f, grosor, color);
    }

    // =====================================================
    // PRIMITIVA: segmento de línea (rectángulo girado)
    // =====================================================

    private static void Segmento(Vector2 a, Vector2 b, float grosor, Color color)
    {
        if (color.a <= 0.002f) return;
        Vector2 d = b - a;
        float largo = d.magnitude;
        if (largo < 0.01f) return;

        float angulo = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Matrix4x4 anterior = GUI.matrix;
        GUIUtility.RotateAroundPivot(angulo, a);
        GUI.DrawTexture(new Rect(a.x, a.y - grosor * 0.5f, largo + grosor * 0.35f, grosor), Texture2D.whiteTexture,
                        ScaleMode.StretchToFill, true, 0f, color, 0f, grosor * 0.5f);
        GUI.matrix = anterior;
    }
}
