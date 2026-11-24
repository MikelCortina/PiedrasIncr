using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Indicador del sentido de giro para las válvulas que se abren girándolas (RuletaInteractivaGasoil).
///
/// Cuando el jugador mira una válvula de cerca aparece alrededor de ella, en pantalla, un anillo de
/// luces LED (mismo estilo que el escáner de piedras):
///  - Unos "cometas" con punta de flecha recorren el anillo en el sentido en el que hay que girar.
///  - El anillo se va encendiendo con el progreso de la apertura (y se apaga si se suelta y se cierra).
///  - Si el jugador gira al revés, el anillo parpadea en rojo y avisa "AL OTRO LADO".
///  - Al abrirse del todo hace un destello y desaparece para siempre.
///
/// No hay que montar nada: se añade solo a todas las RuletaInteractivaGasoil de la escena.
/// El sentido se calcula igual que GestorRuletasFPS (sentido antihorario en pantalla = abrir si la
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
    [Min(12)] public int puntosAnillo = 44;
    [Tooltip("Tamaño del anillo respecto a la válvula.")]
    [Min(0.5f)] public float escalaAnillo = 1.2f;
    [Tooltip("Separación extra entre la válvula y el anillo (metros).")]
    public float margenAnillo = 0.05f;

    [Header("Flechas que marcan el sentido")]
    [Min(1)] public int cometas = 2;
    [Tooltip("Vueltas por segundo de las flechas en reposo (al girar bien van más rápido).")]
    public float vueltasPorSegundo = 0.3f;
    [Tooltip("Largo de la estela de cada flecha (fracción de vuelta).")]
    [Range(0.05f, 0.5f)] public float largoEstela = 0.2f;
    [Tooltip("Marca esto si en esta válvula la flecha señala el lado contrario al que abre.")]
    public bool invertirSentido = false;

    [Header("Textos")]
    public bool mostrarTexto = true;
    public string textoAyuda = "MANTÉN CLIC Y GIRA";
    public string textoAlReves = "AL OTRO LADO";

    private RuletaInteractivaGasoil ruleta;
    private Renderer render;
    private GestorRuletasFPS gestor;
    private Camera cam;
    private float radioMundo = 0.5f;
    private float signo = 1f;

    private float alfa;
    private float fase;
    private float giroReciente;
    private float tiempoVisto = -999f;
    private float avisoAlReves;
    private float destello;
    private bool estabaAbierta;
    private bool haGirado;
    private GUIStyle estiloTexto;

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
        destello = Mathf.MoveTowards(destello, 0f, dt * 1.4f);

        // Las flechas corren más cuando el jugador gira bien.
        float ritmo = girando && giroReciente > 5f ? 2.2f : 1f;
        fase = Mathf.Repeat(fase + dt * vueltasPorSegundo * ritmo, 1f);

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
    // DIBUJO (estilo cartel LED, como el escáner de piedras)
    // =====================================================

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || cam == null || ruleta == null) return;
        if (alfa <= 0.001f && destello <= 0.001f) return;

        Vector3 centroMundo = Centro();
        Vector3 c = cam.WorldToScreenPoint(centroMundo);
        if (c.z <= 0f) return;

        float e = Screen.height / 1080f;

        // El anillo va en el plano de la propia válvula (perpendicular a su eje de giro) y se ve en
        // perspectiva: de frente es un círculo y en una válvula tumbada, una elipse pegada a ella.
        if (!PrepararPlano(centroMundo)) return;
        float r = radioMundo * escalaAnillo + margenAnillo; // metros
        Vector2 centro = new Vector2(c.x, c.y);

        AimAssistVisual estilo = AimAssistVisual.Instancia;
        Color neon = estilo != null ? estilo.colorNeon : new Color(0.926f, 0.887f, 1f, 1f);
        Color medio = estilo != null ? estilo.colorNeonMedio : new Color(0.579f, 0.387f, 0.943f, 1f);
        Color alto = estilo != null ? estilo.colorNeonAlto : new Color(0.914f, 0.616f, 0.992f, 1f);
        Color listo = estilo != null ? estilo.colorNeonListo : neon;
        Color rojo = estilo != null ? estilo.colorNeonBajo : new Color(0.926f, 0.075f, 0.102f, 1f);

        float a = alfa * alfa * (3f - 2f * alfa); // suavizado
        float progreso = ruleta.Progreso;
        bool parpadeoRojo = avisoAlReves > 0f && Mathf.Repeat(Time.unscaledTime * 6f, 1f) < 0.6f;

        // 1. Anillo de progreso: empieza arriba y se enciende en el sentido de apertura.
        if (a > 0.001f)
        {
            for (int i = 0; i < puntosAnillo; i++)
            {
                float t = (float)i / puntosAnillo;
                Vector2 p = PuntoAnillo(centro, r, t);
                bool encendido = t < progreso;
                Color color = encendido ? Color.Lerp(medio, listo, progreso) : medio;
                if (parpadeoRojo) color = rojo;
                float brillo = encendido ? 1f : 0.35f;
                Punto(p, (encendido ? 8f : 5.5f) * e, AimAssistVisual.ConAlfa(color, brillo * a), encendido);
            }

            // 2. Flechas (cometas con estela) que recorren el anillo en el sentido correcto.
            Color colorFlecha = avisoAlReves > 0f ? rojo : alto;
            const int pasosEstela = 12;
            for (int k = 0; k < cometas; k++)
            {
                float cabeza = Mathf.Repeat(fase + (float)k / cometas, 1f);
                for (int j = pasosEstela; j >= 0; j--)
                {
                    float t = cabeza - largoEstela * j / pasosEstela;
                    float caida = 1f - (float)j / pasosEstela;
                    Punto(PuntoAnillo(centro, r, t), Mathf.Lerp(4f, 11f, caida) * e,
                          AimAssistVisual.ConAlfa(colorFlecha, caida * caida * a), j == 0);
                }
                PuntaFlecha(centro, r, cabeza, 24f * e, AimAssistVisual.ConAlfa(colorFlecha, a));
            }

            // 3. Texto de ayuda / progreso / aviso.
            if (mostrarTexto)
            {
                if (estiloTexto == null)
                {
                    estiloTexto = new GUIStyle { fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
                    estiloTexto.normal.textColor = Color.white;
                }

                string texto;
                Color colorTexto;
                if (avisoAlReves > 0f) { texto = textoAlReves; colorTexto = rojo; }
                else if (!haGirado || progreso <= 0.001f) { texto = textoAyuda; colorTexto = neon; }
                else { texto = Mathf.FloorToInt(progreso * 100f) + " %"; colorTexto = Color.Lerp(alto, listo, progreso); }

                float yTexto = BordeInferiorGui(r) + 12f * e;
                Rect rt = new Rect(centro.x - 250f * e, yTexto, 500f * e, 30f * e);
                AimAssistVisual.TextoNeon(estiloTexto, rt, texto, 22f * e, TextAnchor.UpperCenter, AimAssistVisual.ConAlfa(colorTexto, a), 0.5f);
            }
        }

        // 4. Destello al abrirse: un aro que se expande y se desvanece.
        if (destello > 0.001f)
        {
            float d = destello * destello;
            float rd = r * (1f + 0.6f * (1f - destello));
            for (int i = 0; i < puntosAnillo; i++)
                Punto(PuntoAnillo(centro, rd, (float)i / puntosAnillo), 9f * e, AimAssistVisual.ConAlfa(listo, d), true);
        }
    }

    // Plano del anillo: centro de la válvula y dos ejes perpendiculares a su eje de giro.
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
    private Vector2 PuntoAnillo(Vector2 centro, float r, float t)
    {
        float angulo = signoPlano * t * 2f * Mathf.PI;
        Vector3 mundo = planoCentro + (planoU * Mathf.Cos(angulo) + planoV * Mathf.Sin(angulo)) * r;
        Vector3 p = cam.WorldToScreenPoint(mundo);
        return new Vector2(p.x, Screen.height - p.y);
    }

    // Parte más baja del anillo en pantalla, para colocar el texto debajo.
    private float BordeInferiorGui(float r)
    {
        float maximo = float.MinValue;
        for (int i = 0; i < 24; i++)
            maximo = Mathf.Max(maximo, PuntoAnillo(Vector2.zero, r, i / 24f).y);
        return maximo;
    }

    // Punta de flecha en forma de "V" hecha de luces, apuntando hacia donde hay que girar.
    private void PuntaFlecha(Vector2 centro, float r, float t, float largo, Color color)
    {
        Vector2 punta = PuntoAnillo(centro, r, t);
        Vector2 atras = PuntoAnillo(centro, r, t - 0.01f) - punta;
        if (atras.sqrMagnitude < 0.0001f) return;
        atras.Normalize();

        Vector2 brazo1 = Rotar(atras, 38f);
        Vector2 brazo2 = Rotar(atras, -38f);
        float e = Screen.height / 1080f;
        for (int i = 0; i <= 4; i++)
        {
            float s = largo * i / 4f;
            float tamano = Mathf.Lerp(10f, 7f, i / 4f) * e;
            Punto(punta + brazo1 * s, tamano, color, true);
            Punto(punta + brazo2 * s, tamano, color, true);
        }
    }

    private static Vector2 Rotar(Vector2 v, float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private static readonly Color Contorno = new Color(0.035f, 0.025f, 0.09f, 1f);

    // Luz redonda: contorno oscuro (para que se lea sobre fondos claros), halo y núcleo.
    private static void Punto(Vector2 p, float tamano, Color color, bool halo)
    {
        if (color.a <= 0.002f) return;

        float b = tamano * 1.45f;
        AimAssistVisual.Rectangulo(new Rect(p.x - b * 0.5f, p.y - b * 0.5f, b, b), AimAssistVisual.ConAlfa(Contorno, color.a * 0.55f), b * 0.5f);

        if (halo)
        {
            float h = tamano * 2.6f;
            AimAssistVisual.Rectangulo(new Rect(p.x - h * 0.5f, p.y - h * 0.5f, h, h), AimAssistVisual.ConAlfa(color, color.a * 0.25f), h * 0.5f);
        }
        AimAssistVisual.Rectangulo(new Rect(p.x - tamano * 0.5f, p.y - tamano * 0.5f, tamano, tamano), color, tamano * 0.5f);
    }
}
