using UnityEngine;
using UnityEngine.UI;

// Convierte un panel uGUI en una placa del escáner: le pone un MarcoNeonUI detrás del contenido
// y lo anima al abrirse (fundido + "pop" con ease-out cúbico, como el panel de desgaste).
// Si el panel está estirado a toda la pantalla, el marco se ajusta al contenido visible (más un margen).
[AddComponentMenu("UI/PiedrasIncr/Panel Neón")]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PanelNeonUI : MonoBehaviour
{
    public const string NombreMarco = "~MarcoNeon";

    [Tooltip("Vuelve transparente la Image propia del panel para que solo se vea la placa del tema.")]
    public bool ocultarFondoOriginal = true;
    [Tooltip("Fundido + pop al activarse el panel.")]
    public bool animarAlAbrir = true;
    [Tooltip("Ajustar el marco al contenido: Auto = solo si el panel está estirado (anclas distintas).")]
    public ModoAjuste ajusteAlContenido = ModoAjuste.Auto;
    [Tooltip("Margen alrededor del contenido cuando el marco se ajusta a él.")]
    public float margenContenido = 28f;
    [Tooltip("Cada cuántos segundos se recalcula el marco ajustado (por si se muestran u ocultan partes).")]
    public float intervaloAjuste = 0.25f;

    public enum ModoAjuste { Auto, Siempre, Nunca }

    MarcoNeonUI marco;
    RectTransform rectMarco;
    CanvasGroup grupo;
    bool grupoPropio;
    Vector3 escalaBase;
    float progreso = 1f;
    float siguienteAjuste;

    public MarcoNeonUI Marco => marco;

    bool AjustaAlContenido
    {
        get
        {
            if (ajusteAlContenido == ModoAjuste.Nunca) return false;
            if (ajusteAlContenido == ModoAjuste.Siempre) return true;
            RectTransform rt = (RectTransform)transform;
            return rt.anchorMin != rt.anchorMax;
        }
    }

    void Awake()
    {
        escalaBase = transform.localScale;
        CrearMarco();

        if (ocultarFondoOriginal && TryGetComponent(out Image fondo))
        {
            // Alfa 0 en vez de desactivar: si el panel tiene una Mask, sigue funcionando.
            Color c = fondo.color;
            c.a = 0f;
            fondo.color = c;
        }

        grupo = GetComponent<CanvasGroup>();
        if (grupo == null)
        {
            grupo = gameObject.AddComponent<CanvasGroup>();
            grupoPropio = true;
        }
    }

    void CrearMarco()
    {
        Transform existente = transform.Find(NombreMarco);
        if (existente == null || !existente.TryGetComponent(out marco))
        {
            GameObject go = new GameObject(NombreMarco, typeof(RectTransform), typeof(CanvasRenderer), typeof(MarcoNeonUI), typeof(LayoutElement));
            go.hideFlags = HideFlags.DontSaveInEditor;
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            marco = go.GetComponent<MarcoNeonUI>();
        }

        rectMarco = (RectTransform)marco.transform;
        rectMarco.SetAsFirstSibling();
        rectMarco.anchorMin = Vector2.zero;
        rectMarco.anchorMax = Vector2.one;
        rectMarco.offsetMin = Vector2.zero;
        rectMarco.offsetMax = Vector2.zero;
    }

    void OnEnable()
    {
        siguienteAjuste = 0f;
        if (!animarAlAbrir) return;
        progreso = 0f;
        Aplicar(0f);
    }

    void OnDisable()
    {
        progreso = 1f;
        transform.localScale = escalaBase;
        if (grupoPropio && grupo != null) grupo.alpha = 1f;
    }

    void Update()
    {
        if (AjustaAlContenido && Time.unscaledTime >= siguienteAjuste)
        {
            AjustarMarcoAlContenido();
            siguienteAjuste = Time.unscaledTime + intervaloAjuste;
        }

        if (progreso >= 1f) return;

        progreso = Mathf.MoveTowards(progreso, 1f, Time.unscaledDeltaTime * TemaUINeon.Actual.velocidadFundido);
        Aplicar(TemaUINeon.EaseOutCubico(progreso));
    }

    void Aplicar(float a)
    {
        transform.localScale = escalaBase * Mathf.Lerp(TemaUINeon.Actual.escalaPopInicial, 1f, a);
        // Si el CanvasGroup ya existía, otro script puede estar usando su alfa: solo animamos la escala.
        if (grupoPropio && grupo != null) grupo.alpha = a;
    }

    static readonly Vector3[] esquinas = new Vector3[4];

    // Caja que envuelve a los hijos activos (en el espacio local del panel) + margen.
    void AjustarMarcoAlContenido()
    {
        RectTransform panel = (RectTransform)transform;
        bool hay = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;

        for (int i = 0; i < panel.childCount; i++)
        {
            if (!(panel.GetChild(i) is RectTransform hijo) || hijo == rectMarco || !hijo.gameObject.activeSelf) continue;

            hijo.GetWorldCorners(esquinas);
            for (int k = 0; k < 4; k++)
            {
                Vector2 p = panel.InverseTransformPoint(esquinas[k]);
                if (!hay) { min = max = p; hay = true; }
                else { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            }
        }

        if (!hay)
        {
            marco.enabled = false;
            return;
        }

        marco.enabled = true;
        min -= Vector2.one * margenContenido;
        max += Vector2.one * margenContenido;

        // Anclas en el pivote del panel: así las posiciones locales se usan tal cual.
        Vector2 ancla = panel.pivot;
        rectMarco.anchorMin = ancla;
        rectMarco.anchorMax = ancla;
        rectMarco.pivot = new Vector2(0.5f, 0.5f);
        rectMarco.sizeDelta = max - min;
        rectMarco.anchoredPosition = (min + max) * 0.5f;
    }
}
