using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Final de la demo (hoja FINAL).
/// 1. Espera a que caiga la primera piedra de nivel 2 (la luna ya está agrietada).
/// 2. Durante unos segundos intensifica la lluvia para subir la emoción.
/// 3. En el pico: bloquea el input, corte a negro y silencio.
/// 4. Pantalla final con tiempo jugado, esferas, monedas y litros del tanque, y dos botones:
///    "Añadir a la lista de deseados" (URL de Steam) y "Volver al menú".
/// Duración total por defecto ≈ 1 minuto desde el disparo.
/// La pantalla usa el mismo estilo de cartel LED que el escáner de piedras (AimAssistVisual).
/// </summary>
public class ControladorFinDemo : MonoBehaviour
{
    [Header("Tiempos")]
    [Tooltip("Segundos de lluvia de nivel 2 antes del corte a negro.")]
    [Min(1f)] public float segundosDeLluvia = 35f;
    [Tooltip("Duración del fundido a negro. 0 = corte seco.")]
    [Min(0f)] public float duracionCorte = 0f;
    [Tooltip("Segundos en negro antes de mostrar la pantalla final.")]
    [Min(0f)] public float segundosEnNegro = 2.5f;
    [Tooltip("Duración de la aparición de la pantalla final.")]
    [Min(0.01f)] public float duracionAparicion = 1.2f;

    [Header("Clímax: la lluvia va a más")]
    public bool intensificarLluvia = true;
    [Tooltip("Vacío = se busca en la escena.")]
    public LluviaMeteoritos lluvia;
    [Tooltip("Cadencia al principio de la lluvia final (segundos entre oleadas).")]
    public float cadenciaInicial = 1.5f;
    [Tooltip("Cadencia justo antes del corte.")]
    public float cadenciaFinal = 0.25f;
    [Min(1)] public int piedrasPorOleadaInicial = 1;
    [Min(1)] public int piedrasPorOleadaFinal = 4;

    [Header("Botones")]
    [Tooltip("Página de Steam del juego (botón 'Añadir a la lista de deseados').")]
    public string urlSteam = "https://store.steampowered.com/";
    [Tooltip("Escena del menú principal. Debe estar en File > Build Profiles (Scene List).")]
    public string escenaMenu = "MenuPrincipal";

    [Header("Bloqueo de input")]
    [Tooltip("Vacío = el objeto con tag Player. Se desactivan sus scripts al llegar al corte.")]
    public GameObject jugador;
    [Tooltip("Otros scripts a desactivar en el corte (menús de pausa, tiendas...).")]
    public MonoBehaviour[] scriptsADesactivar;

    [Header("Estilo (vacío = el del escáner de piedras)")]
    [Tooltip("Fuente LED para cifras y cabeceras. Vacío = DS-DIGI del escáner.")]
    public Font fuente;
    [Tooltip("Fuente para los textos con tildes y eñes (título y botones). Vacío = la fuente por defecto.")]
    public Font fuenteTexto;
    [Tooltip("Tamaño de la pantalla final (referencia 1080p).")]
    public float escala = 1f;

    public bool FinalEnCurso { get; private set; }

    private enum Estado { Esperando, Lluvia, Negro, Pantalla }
    private Estado estado = Estado.Esperando;

    private float alfaNegro;
    private float alfaPantalla;
    private int esferasPerfectas;
    private GUIStyle estilo;
    private GUIStyle estiloTexto;

    // Estadísticas congeladas en el momento del corte.
    private double segundosJugados;
    private int monedas;
    private float litros;

    private void OnEnable()
    {
        PiedraNivel2.OnPiedraNivel2Creada += AlCaerPiedraNivel2;
        DesgastePiedra.OnAlgunaEsferaPerfecta += ContarEsfera;
    }

    private void OnDisable()
    {
        PiedraNivel2.OnPiedraNivel2Creada -= AlCaerPiedraNivel2;
        DesgastePiedra.OnAlgunaEsferaPerfecta -= ContarEsfera;
    }

    private void ContarEsfera(DesgastePiedra piedra)
    {
        esferasPerfectas++;
    }

    private void AlCaerPiedraNivel2(PiedraNivel2 piedra)
    {
        if (estado != Estado.Esperando) return;
        IniciarFinal();
    }

    /// <summary>Arranca la secuencia final (también útil para probarla desde otro script).</summary>
    public void IniciarFinal()
    {
        if (FinalEnCurso) return;
        FinalEnCurso = true;
        StartCoroutine(RutinaFinal());
    }

    private IEnumerator RutinaFinal()
    {
        estado = Estado.Lluvia;
        if (lluvia == null) lluvia = FindFirstObjectByType<LluviaMeteoritos>();

        // 1. Lluvia de nivel 2, cada vez más intensa.
        float t = 0f;
        while (t < segundosDeLluvia)
        {
            t += Time.deltaTime;
            if (intensificarLluvia && lluvia != null)
            {
                float k = Mathf.Clamp01(t / segundosDeLluvia);
                k = k * k; // empieza suave y se dispara al final
                lluvia.ConfigurarLluvia(
                    Mathf.Lerp(cadenciaInicial, cadenciaFinal, k),
                    Mathf.RoundToInt(Mathf.Lerp(piedrasPorOleadaInicial, piedrasPorOleadaFinal, k)));
            }
            yield return null;
        }

        // 2. Pico de emoción: se congela todo y corte a negro.
        CapturarEstadisticas();
        BloquearInput();
        estado = Estado.Negro;

        float f = 0f;
        while (f < duracionCorte)
        {
            f += Time.unscaledDeltaTime;
            alfaNegro = Mathf.Clamp01(f / duracionCorte);
            AudioListener.volume = 1f - alfaNegro;
            yield return null;
        }
        alfaNegro = 1f;
        AudioListener.volume = 0f;
        AudioListener.pause = true;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(segundosEnNegro);

        // 3. Pantalla final.
        estado = Estado.Pantalla;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float a = 0f;
        while (a < duracionAparicion)
        {
            a += Time.unscaledDeltaTime;
            alfaPantalla = Mathf.Clamp01(a / duracionAparicion);
            yield return null;
        }
        alfaPantalla = 1f;
    }

    private void CapturarEstadisticas()
    {
        GuardadoPartidaJuego guardado = FindFirstObjectByType<GuardadoPartidaJuego>();
        segundosJugados = guardado != null ? guardado.SegundosJugados : Time.timeSinceLevelLoad;

        Cartera cartera = FindFirstObjectByType<Cartera>();
        monedas = cartera != null ? cartera.Monedas : 0;

        ContadorGasolinera tanque = FindFirstObjectByType<ContadorGasolinera>();
        litros = tanque != null ? tanque.LitrosActuales : 0f;
    }

    private void BloquearInput()
    {
        if (jugador == null) jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            foreach (MonoBehaviour script in jugador.GetComponentsInChildren<MonoBehaviour>())
                if (script != null && script != this) script.enabled = false;
        }

        if (scriptsADesactivar != null)
            foreach (MonoBehaviour script in scriptsADesactivar)
                if (script != null) script.enabled = false;
    }

    private void RestaurarEstadoGlobal()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
    }

    private void OnDestroy()
    {
        // Si se sale de la escena a mitad del final, no se queda el juego congelado ni mudo.
        if (FinalEnCurso) RestaurarEstadoGlobal();
    }

    private void VolverAlMenu()
    {
        RestaurarEstadoGlobal();
        SceneManager.LoadScene(escenaMenu);
    }

    // =====================================================
    // DIBUJO (estilo cartel LED, igual que el escáner)
    // =====================================================

    private void OnGUI()
    {
        if (estado == Estado.Esperando || estado == Estado.Lluvia) return;

        GUI.depth = -1000;
        Rect pantalla = new Rect(0f, 0f, Screen.width, Screen.height);

        if (Event.current.type == EventType.Repaint)
            AimAssistVisual.Rectangulo(pantalla, new Color(0f, 0f, 0f, alfaNegro), 0f);

        if (estado != Estado.Pantalla || alfaPantalla <= 0f) return;

        AimAssistVisual estiloEscaner = AimAssistVisual.Instancia;
        Color placa = estiloEscaner != null ? estiloEscaner.colorPlaca : new Color(0.035f, 0.025f, 0.09f, 0.88f);
        Color neon = estiloEscaner != null ? estiloEscaner.colorNeon : new Color(0.926f, 0.887f, 1f, 1f);
        Color medio = estiloEscaner != null ? estiloEscaner.colorNeonMedio : new Color(0.579f, 0.387f, 0.943f, 1f);
        Color alto = estiloEscaner != null ? estiloEscaner.colorNeonAlto : new Color(0.914f, 0.616f, 0.992f, 1f);
        Font f = fuente != null ? fuente : (estiloEscaner != null ? estiloEscaner.fuentePanel : null);
        if (estilo == null || estilo.font != f)
        {
            estilo = new GUIStyle { font = f, clipping = TextClipping.Overflow };
            estilo.normal.textColor = Color.white;
        }
        if (estiloTexto == null || estiloTexto.font != fuenteTexto)
        {
            estiloTexto = new GUIStyle { font = fuenteTexto, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
            estiloTexto.normal.textColor = Color.white;
        }

        float a = 1f - Mathf.Pow(1f - alfaPantalla, 3f);
        float e = Screen.height / 1080f * escala * Mathf.Lerp(0.9f, 1f, a);

        float ancho = 760f * e, altoPanel = 560f * e;
        Rect r = new Rect((Screen.width - ancho) * 0.5f, (Screen.height - altoPanel) * 0.5f, ancho, altoPanel);
        bool repintar = Event.current.type == EventType.Repaint;

        if (repintar)
        {
            AimAssistVisual.Rectangulo(r, AimAssistVisual.ConAlfa(placa, placa.a * a), 6f * e);
            for (float ly = r.y + 3f * e; ly < r.yMax - 2f * e; ly += 3f * e)
                AimAssistVisual.Rectangulo(new Rect(r.x + 2f * e, ly, r.width - 4f * e, Mathf.Max(1f, 0.6f * e)), AimAssistVisual.ConAlfa(neon, 0.03f * a), 0f);
            AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(neon, 0.75f * a), e, 6f * e);
            AimAssistVisual.EsquinasHUD(r, AimAssistVisual.ConAlfa(alto, a), e);
        }

        float m = 36f * e;
        float y = r.y + 26f * e;

        if (repintar)
        {
            Texto(new Rect(r.x + m, y, r.width - 2f * m, 20f * e), "ROCK IT", 18f * e, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(neon, 0.85f * a), 0.5f);
            Texto(new Rect(r.x + m, y, r.width - 2f * m, 20f * e), "END OF DEMO", 18f * e, TextAnchor.UpperRight, AimAssistVisual.ConAlfa(neon, 0.45f * a), 0f);
            y += 34f * e;
            TextoLargo(new Rect(r.x + m, y, r.width - 2f * m, 70f * e), "LA LUNA SE HA AGRIETADO", 44f * e, TextAnchor.MiddleCenter, AimAssistVisual.ConAlfa(alto, a), 1f);
            y += 74f * e;
            TextoLargo(new Rect(r.x + m, y, r.width - 2f * m, 24f * e), "Algo nuevo está cayendo del cielo...", 20f * e, TextAnchor.MiddleCenter, AimAssistVisual.ConAlfa(neon, 0.7f * a), 0.3f);
        }
        else
        {
            y += 34f * e + 74f * e;
        }

        // Estadísticas: 2 x 2 casillas con dígitos LED.
        y += 46f * e;
        float hueco = 18f * e;
        float anchoCasilla = (r.width - 2f * m - hueco) * 0.5f;
        float altoCasilla = 86f * e;
        if (repintar)
        {
            Casilla(new Rect(r.x + m, y, anchoCasilla, altoCasilla), "TIEMPO JUGADO", FormatoTiempo(segundosJugados), e, a, neon, medio, alto);
            Casilla(new Rect(r.x + m + anchoCasilla + hueco, y, anchoCasilla, altoCasilla), "ESFERAS PERFECTAS", esferasPerfectas.ToString(), e, a, neon, medio, alto);
            Casilla(new Rect(r.x + m, y + altoCasilla + hueco, anchoCasilla, altoCasilla), "MONEDAS", monedas.ToString(), e, a, neon, medio, alto);
            Casilla(new Rect(r.x + m + anchoCasilla + hueco, y + altoCasilla + hueco, anchoCasilla, altoCasilla), "LITROS EN EL TANQUE", Mathf.FloorToInt(litros).ToString("N0"), e, a, neon, medio, alto);
        }
        y += 2f * altoCasilla + hueco + 34f * e;

        // Botones.
        float altoBoton = 58f * e;
        float anchoBoton = (r.width - 2f * m - hueco) * 0.5f;
        if (Boton(new Rect(r.x + m, y, anchoBoton, altoBoton), "AÑADIR A LA LISTA DE DESEADOS", e, a, alto, true))
            Application.OpenURL(urlSteam);
        if (Boton(new Rect(r.x + m + anchoBoton + hueco, y, anchoBoton, altoBoton), "VOLVER AL MENÚ", e, a, neon, false))
            VolverAlMenu();
    }

    private void Casilla(Rect r, string titulo, string valor, float e, float a, Color neon, Color medio, Color alto)
    {
        AimAssistVisual.Rectangulo(r, new Color(0f, 0f, 0f, 0.35f * a), 4f * e);
        AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(medio, 0.6f * a), e, 4f * e);
        Texto(new Rect(r.x + 14f * e, r.y + 8f * e, r.width - 28f * e, 18f * e), titulo, 15f * e, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(neon, 0.7f * a), 0.3f);

        Rect cifras = new Rect(r.x + 14f * e, r.y + 26f * e, r.width - 28f * e, 54f * e);
        string fondo = System.Text.RegularExpressions.Regex.Replace(valor, "[0-9]", "8");
        Texto(cifras, fondo, 44f * e, TextAnchor.MiddleRight, AimAssistVisual.ConAlfa(alto, 0.08f * a), 0f);
        Texto(cifras, valor, 44f * e, TextAnchor.MiddleRight, AimAssistVisual.ConAlfa(alto, a), 1f);
    }

    private bool Boton(Rect r, string texto, float e, float a, Color color, bool principal)
    {
        bool encima = r.Contains(Event.current.mousePosition);

        if (Event.current.type == EventType.Repaint)
        {
            float pulso = principal ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f) : 1f;
            Color fondo = AimAssistVisual.ConAlfa(color, (encima ? 0.28f : (principal ? 0.14f : 0.06f)) * a);
            AimAssistVisual.Rectangulo(r, fondo, 4f * e);
            AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(color, (encima ? 1f : 0.7f * pulso) * a), e, 4f * e);
            if (encima) AimAssistVisual.EsquinasHUD(r, AimAssistVisual.ConAlfa(color, a), e);
            TextoLargo(r, texto, 18f * e, TextAnchor.MiddleCenter, AimAssistVisual.ConAlfa(color, a), encima ? 1f : 0.5f);
        }

        if (alfaPantalla < 1f) return false; // no se puede pulsar mientras aparece
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && encima)
        {
            Event.current.Use();
            return true;
        }
        return false;
    }

    // Cifras y cabeceras en fuente LED.
    private void Texto(Rect r, string texto, float tamano, TextAnchor anclaje, Color color, float halo)
    {
        AimAssistVisual.TextoNeon(estilo, r, texto, tamano, anclaje, color, halo);
    }

    // Frases en español (tildes, eñes): fuente de texto normal, mismo halo de neón.
    private void TextoLargo(Rect r, string texto, float tamano, TextAnchor anclaje, Color color, float halo)
    {
        AimAssistVisual.TextoNeon(estiloTexto, r, texto, tamano, anclaje, color, halo);
    }

    private static string FormatoTiempo(double segundos)
    {
        int total = Mathf.Max(0, (int)segundos);
        int horas = total / 3600;
        int minutos = (total / 60) % 60;
        int segs = total % 60;
        return horas > 0 ? horas + ":" + minutos.ToString("00") + ":" + segs.ToString("00")
                         : minutos.ToString("00") + ":" + segs.ToString("00");
    }
}
