using UnityEngine;
using UnityEngine.UI;

// Gráfico uGUI que dibuja el marco del escáner sobre su RectTransform:
// sombra, placa oscura, líneas de barrido, borde de neón con halo y esquinas de visor.
// No usa sprites ni texturas: todo son quads generados, así que no hay assets que perder.
[AddComponentMenu("UI/PiedrasIncr/Marco Neón")]
[RequireComponent(typeof(CanvasRenderer))]
public class MarcoNeonUI : MaskableGraphic
{
    [Header("Capas")]
    public bool sombra = true;
    public bool placa = true;
    public bool scanlines = true;
    public bool borde = true;
    public bool halo = true;
    public bool esquinas = true;

    [Header("Ajustes")]
    [Tooltip("Alfa del borde. Negativo = el del tema.")]
    public float alfaBorde = -1f;
    [Tooltip("Color de las esquinas de visor (por defecto, el violeta medio del tema).")]
    public bool usarColorEsquinasPropio = false;
    public Color colorEsquinas = Color.white;

    TemaUINeon Tema => TemaUINeon.Actual;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void Refrescar() => SetVerticesDirty();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        TemaUINeon t = Tema;
        Rect r = GetPixelAdjustedRect();

        if (sombra)
            Quad(vh, new Rect(r.x + t.desplazamientoSombra.x, r.y + t.desplazamientoSombra.y, r.width, r.height), t.colorSombra);

        if (placa)
            Quad(vh, r, t.colorPlaca);

        if (scanlines && t.separacionScanlines >= 1f && t.alfaScanlines > 0f)
        {
            Color linea = ConAlfa(t.colorNeon, t.alfaScanlines);
            for (float y = r.y + t.separacionScanlines; y < r.yMax - 2f; y += t.separacionScanlines)
                Quad(vh, new Rect(r.x + 2f, y, r.width - 4f, 0.6f), linea);
        }

        float alfa = alfaBorde >= 0f ? alfaBorde : t.alfaBorde;
        if (halo)
        {
            Contorno(vh, Expandir(r, 4f), 2f, ConAlfa(t.colorNeon, alfa * t.alfaHalo.y));
            Contorno(vh, Expandir(r, 2f), 2f, ConAlfa(t.colorNeon, alfa * t.alfaHalo.x));
        }
        if (borde)
            Contorno(vh, r, t.grosorBorde, ConAlfa(t.colorNeon, alfa));

        if (esquinas)
            Esquinas(vh, r, usarColorEsquinasPropio ? colorEsquinas : t.colorNeonMedio, t);
    }

    void Esquinas(VertexHelper vh, Rect r, Color c, TemaUINeon t)
    {
        float l = t.largoEsquina, g = t.grosorEsquina, s = t.separacionEsquina;
        float izq = r.xMin - s, der = r.xMax + s, abajo = r.yMin - s, arriba = r.yMax + s;

        Quad(vh, new Rect(izq, arriba - g, l, g), c);
        Quad(vh, new Rect(izq, arriba - l, g, l), c);
        Quad(vh, new Rect(der - l, arriba - g, l, g), c);
        Quad(vh, new Rect(der - g, arriba - l, g, l), c);
        Quad(vh, new Rect(izq, abajo, l, g), c);
        Quad(vh, new Rect(izq, abajo, g, l), c);
        Quad(vh, new Rect(der - l, abajo, l, g), c);
        Quad(vh, new Rect(der - g, abajo, g, l), c);
    }

    void Contorno(VertexHelper vh, Rect r, float grosor, Color c)
    {
        Quad(vh, new Rect(r.xMin, r.yMax - grosor, r.width, grosor), c);
        Quad(vh, new Rect(r.xMin, r.yMin, r.width, grosor), c);
        Quad(vh, new Rect(r.xMin, r.yMin + grosor, grosor, r.height - 2f * grosor), c);
        Quad(vh, new Rect(r.xMax - grosor, r.yMin + grosor, grosor, r.height - 2f * grosor), c);
    }

    void Quad(VertexHelper vh, Rect q, Color c)
    {
        if (q.width <= 0f || q.height <= 0f || c.a <= 0f) return;

        Color32 c32 = c * color;
        int i = vh.currentVertCount;
        vh.AddVert(new Vector3(q.xMin, q.yMin), c32, Vector2.zero);
        vh.AddVert(new Vector3(q.xMin, q.yMax), c32, Vector2.zero);
        vh.AddVert(new Vector3(q.xMax, q.yMax), c32, Vector2.zero);
        vh.AddVert(new Vector3(q.xMax, q.yMin), c32, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }

    static Rect Expandir(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + 2f * d, r.height + 2f * d);

    static Color ConAlfa(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
