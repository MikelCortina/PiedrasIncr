using System;
using System.Collections.Generic;
using UnityEngine;
using PiedrasIncr.Audio;

/// <summary>
/// Líneas cortas de Botro por "radio" (subtítulo inferior), sin bloquear al jugador.
/// Cola de mensajes: si llegan varias a la vez, se dicen en orden.
/// Estilo cartel LED, el mismo que el escáner de piedras y el Fisionador.
///
/// Uso: BotroDice.Decir("id", "texto");  y escuchar BotroDice.OnLineaTerminada("id").
/// </summary>
public class BotroDice : MonoBehaviour
{
    public static BotroDice Instancia { get; private set; }

    /// <summary>Se lanza al terminar de mostrarse una línea (con su id).</summary>
    public static event Action<string> OnLineaTerminada;

    [Header("Personaje")]
    public string nombre = "BOTRO";

    [Header("Tiempo en pantalla")]
    [Tooltip("Segundos mínimos que se queda una línea.")]
    public float segundosBase = 1.8f;
    [Tooltip("Segundos extra por cada letra (tiempo de lectura).")]
    public float segundosPorLetra = 0.055f;
    [Tooltip("Letras por segundo del efecto máquina de escribir.")]
    public float letrasPorSegundo = 45f;
    [Tooltip("Tecla para omitir la frase y pasar a la siguiente.")]
    public KeyCode teclaOmitir = KeyCode.G;

    [Header("Sonido")]
    [SoundName] public string sonidoLinea = "";

    [Header("Estilo (vacío = el del escáner de piedras)")]
    [Tooltip("Fuente con tildes y eñes para el texto. Vacío = la fuente por defecto.")]
    public Font fuenteTexto;
    [Tooltip("Tamaño del panel (referencia 1080p).")]
    public float escala = 1f;
    [Tooltip("Altura del panel desde el borde inferior (referencia 1080p).")]
    public float margenInferior = 150f;

    private struct Linea
    {
        public string id;
        public string texto;
        public float duracion; // <= 0: automática según la longitud
    }

    private readonly Queue<Linea> cola = new Queue<Linea>();
    private bool hablando;
    private Linea actual;
    private float inicio;
    private float duracion;
    private GUIStyle estiloLED;
    private GUIStyle estiloTexto;

    public bool Hablando => hablando || cola.Count > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEventos()
    {
        OnLineaTerminada = null;
        Instancia = null;
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Debug.LogWarning("Hay más de un BotroDice en la escena; se usa el primero.", this);
            return;
        }
        Instancia = this;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    /// <summary>Pone una línea de Botro en la cola.</summary>
    /// <param name="duracion">Segundos en pantalla si no se omite con la tecla. 0 = automático según la longitud.</param>
    public static void Decir(string id, string texto, float duracion = 0f)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            OnLineaTerminada?.Invoke(id);
            return;
        }

        if (Instancia == null)
        {
            Debug.Log("BOTRO: " + texto);
            OnLineaTerminada?.Invoke(id);
            return;
        }

        Instancia.cola.Enqueue(new Linea { id = id, texto = texto, duracion = duracion });
    }

    /// <summary>Quita de la cola las líneas que aún no han empezado y cuyo id empieza por 'prefijo'.</summary>
    public static void CancelarPendientes(string prefijo)
    {
        if (Instancia == null || Instancia.cola.Count == 0) return;
        Linea[] pendientes = Instancia.cola.ToArray();
        Instancia.cola.Clear();
        foreach (Linea linea in pendientes)
            if (linea.id == null || !linea.id.StartsWith(prefijo, StringComparison.Ordinal))
                Instancia.cola.Enqueue(linea);
    }

    private void Update()
    {
        if (!hablando)
        {
            if (cola.Count == 0) return;

            actual = cola.Dequeue();
            hablando = true;
            inicio = Time.time;
            duracion = actual.duracion > 0f ? Mathf.Max(0.3f, actual.duracion) : DuracionAutomatica(actual.texto);
            if (!string.IsNullOrEmpty(sonidoLinea)) AudioManager.Play(sonidoLinea);
            return;
        }

        bool saltar = Input.GetKeyDown(teclaOmitir) && Time.time - inicio > 0.3f;
        if (Time.time - inicio >= duracion || saltar)
        {
            hablando = false;
            OnLineaTerminada?.Invoke(actual.id);
        }
    }

    /// <summary>Segundos que se queda una frase si no tiene duración propia.</summary>
    public float DuracionAutomatica(string texto)
    {
        return segundosBase + (texto != null ? texto.Length : 0) * segundosPorLetra;
    }

    // Letras por segundo: si la duración es corta, escribe más rápido para que se lea entera.
    private float VelocidadEscritura()
    {
        return Mathf.Max(letrasPorSegundo, actual.texto.Length / Mathf.Max(0.1f, duracion * 0.7f));
    }

    private void OnGUI()
    {
        if (!hablando || Event.current.type != EventType.Repaint) return;

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
            estiloTexto = new GUIStyle { font = fuenteTexto, fontStyle = FontStyle.Bold, wordWrap = true, clipping = TextClipping.Overflow };
            estiloTexto.normal.textColor = Color.white;
        }

        float t = Time.time - inicio;
        float aparicion = Mathf.Clamp01(t / 0.18f);
        float salida = Mathf.Clamp01((duracion - t) / 0.25f);
        float a = Mathf.Min(aparicion, salida);
        float e = Screen.height / 1080f * escala;

        float ancho = Mathf.Min(Screen.width - 40f, 820f * e);
        // El panel crece hacia arriba con el texto (se mide la frase entera para que no salte mientras se escribe).
        estiloTexto.fontSize = Mathf.Max(8, Mathf.RoundToInt(22f * e));
        float altoTexto = estiloTexto.CalcHeight(new GUIContent(actual.texto), ancho - 36f * e);
        float alto_ = Mathf.Max(112f * e, 34f * e + altoTexto + 26f * e);
        Rect r = new Rect((Screen.width - ancho) * 0.5f, Screen.height - margenInferior * e - alto_ + (1f - aparicion) * 12f * e, ancho, alto_);

        AimAssistVisual.Rectangulo(new Rect(r.x + 4f * e, r.y + 6f * e, r.width, r.height), new Color(0f, 0f, 0f, 0.4f * a), 4f * e);
        AimAssistVisual.Rectangulo(r, AimAssistVisual.ConAlfa(placa, placa.a * a), 4f * e);
        for (float ly = r.y + 3f * e; ly < r.yMax - 2f * e; ly += 3f * e)
            AimAssistVisual.Rectangulo(new Rect(r.x + 2f * e, ly, r.width - 4f * e, Mathf.Max(1f, 0.6f * e)), AimAssistVisual.ConAlfa(neon, 0.035f * a), 0f);
        AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(neon, 0.75f * a), e, 4f * e);
        AimAssistVisual.EsquinasHUD(r, AimAssistVisual.ConAlfa(alto, a), e);

        float m = 18f * e;

        // Cabecera: nombre y un "LED" de radio que parpadea mientras escribe.
        float velocidad = VelocidadEscritura();
        bool escribiendo = t * velocidad < actual.texto.Length;
        bool led = !escribiendo || Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
        AimAssistVisual.Rectangulo(new Rect(r.x + m, r.y + 13f * e, 8f * e, 8f * e), AimAssistVisual.ConAlfa(alto, (led ? 1f : 0.25f) * a), 4f * e);
        AimAssistVisual.TextoNeon(estiloLED, new Rect(r.x + m + 16f * e, r.y + 8f * e, 300f * e, 18f * e), nombre, 17f * e, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(alto, a), 0.6f);
        AimAssistVisual.TextoNeon(estiloLED, new Rect(r.x + m, r.y + 8f * e, r.width - 2f * m, 18f * e), "RADIO", 15f * e, TextAnchor.UpperRight, AimAssistVisual.ConAlfa(neon, 0.4f * a), 0f);

        // Texto con efecto máquina de escribir.
        int letras = Mathf.Clamp(Mathf.FloorToInt(t * velocidad), 0, actual.texto.Length);
        string visible = actual.texto.Substring(0, letras);
        AimAssistVisual.TextoNeon(estiloTexto, new Rect(r.x + m, r.y + 34f * e, r.width - 2f * m, altoTexto + 4f * e), visible, 22f * e, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(neon, a), 0.25f);

        // Recordatorio de la tecla para omitir.
        AimAssistVisual.TextoNeon(estiloLED, new Rect(r.x + m, r.yMax - 22f * e, r.width - 2f * m, 16f * e), "[" + teclaOmitir + "] OMITIR",
            13f * e, TextAnchor.LowerRight, AimAssistVisual.ConAlfa(neon, 0.45f * a), 0f);
    }
}
