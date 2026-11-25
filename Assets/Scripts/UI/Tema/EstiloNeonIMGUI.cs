using UnityEngine;

// Primitivas de dibujo OnGUI del estilo cartel LED (extraídas del panel del escáner de piedras).
// Las usan AimAssistVisual, Fisionador, BotroDice, ControladorFinDemo, IndicadorGiroRuleta, SecuenciaIntro...
public static class EstiloNeonIMGUI
{
    public static void Rectangulo(Rect r, Color color, float radio)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radio);
    }

    public static void Borde(Rect r, Color color, float grosor, float radio)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, grosor, radio);
    }

    public static Color ConAlfa(Color color, float alfa)
    {
        color.a = alfa;
        return color;
    }

    // Borde de neón: línea nítida y dos capas de halo por fuera.
    public static void BordeNeon(Rect r, Color color, float e, float radio)
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
    public static void EsquinasHUD(Rect r, Color color, float e)
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

    // Texto luminoso: copias desplazadas y tenues alrededor (halo) y el texto nítido encima.
    public static void TextoNeon(GUIStyle estilo, Rect r, string texto, float tamano, TextAnchor anclaje, Color color, float halo)
    {
        estilo.fontSize = Mathf.Max(8, Mathf.RoundToInt(tamano));
        estilo.alignment = anclaje;

        Color guardado = GUI.color;
        if (halo > 0f)
        {
            float d = Mathf.Max(1f, tamano * 0.05f);
            GUI.color = ConAlfa(color, color.a * 0.22f * halo);
            GUI.Label(new Rect(r.x - d, r.y, r.width, r.height), texto, estilo);
            GUI.Label(new Rect(r.x + d, r.y, r.width, r.height), texto, estilo);
            GUI.Label(new Rect(r.x, r.y - d, r.width, r.height), texto, estilo);
            GUI.Label(new Rect(r.x, r.y + d, r.width, r.height), texto, estilo);
        }
        GUI.color = color;
        GUI.Label(r, texto, estilo);
        GUI.color = guardado;
    }

    // Placa completa del escáner: sombra, placa, líneas de barrido, borde de neón y esquinas de visor.
    // a = alfa global (0..1), e = escala (Screen.height / 1080).
    public static void PlacaNeon(Rect r, TemaUINeon tema, Color colorEsquinas, float a, float e)
    {
        float radio = 4f * e;
        Rectangulo(new Rect(r.x + tema.desplazamientoSombra.x * e, r.y - tema.desplazamientoSombra.y * e, r.width, r.height),
            ConAlfa(tema.colorSombra, tema.colorSombra.a * a), radio);
        Rectangulo(r, ConAlfa(tema.colorPlaca, tema.colorPlaca.a * a), radio);

        float paso = Mathf.Max(1f, tema.separacionScanlines * e);
        for (float y = r.y + paso; y < r.yMax - 2f * e; y += paso)
            Rectangulo(new Rect(r.x + 2f * e, y, r.width - 4f * e, Mathf.Max(1f, 0.6f * e)), ConAlfa(tema.colorNeon, tema.alfaScanlines * a), 0f);

        BordeNeon(r, ConAlfa(tema.colorNeon, tema.alfaBorde * a), e, radio);
        EsquinasHUD(r, ConAlfa(colorEsquinas, colorEsquinas.a * a), e);
    }

    // GUISkin con el estilo del tema para HUD hechos con GUILayout (diálogos, menús de depuración...).
    // Llamar solo dentro de OnGUI. Se crea una vez y se reutiliza.
    static GUISkin skin;
    static TemaUINeon temaSkin;

    public static GUISkin Skin(TemaUINeon tema)
    {
        if (skin != null && temaSkin == tema) return skin;

        skin = Object.Instantiate(GUI.skin);
        skin.hideFlags = HideFlags.DontSave;
        temaSkin = tema;

        Font fuente = tema.fuenteIMGUI;
        Estilo(skin.label, fuente, tema.colorNeon, null, 18);
        skin.label.wordWrap = true;

        Estilo(skin.box, fuente, tema.colorNeonAlto, null, 20);
        skin.box.alignment = TextAnchor.MiddleCenter;

        Estilo(skin.window, fuente, tema.colorNeon, null, 18);
        skin.window.padding = new RectOffset(18, 18, 14, 14);

        Estilo(skin.button, fuente, tema.colorNeon, Textura(tema.colorRellenoBoton), 18);
        skin.button.hover.background = Textura(ConAlfa(tema.colorNeonMedio, 0.35f));
        skin.button.hover.textColor = Color.white;
        skin.button.focused.background = skin.button.hover.background;
        skin.button.focused.textColor = Color.white;
        skin.button.active.background = Textura(ConAlfa(tema.colorNeonAlto, 0.5f));
        skin.button.active.textColor = Color.white;
        return skin;
    }

    static void Estilo(GUIStyle estilo, Font fuente, Color texto, Texture2D fondo, int tamano)
    {
        if (fuente != null) estilo.font = fuente;
        estilo.fontSize = tamano;
        estilo.border = new RectOffset(0, 0, 0, 0);
        foreach (GUIStyleState estado in new[] { estilo.normal, estilo.hover, estilo.active, estilo.focused,
                                                 estilo.onNormal, estilo.onHover, estilo.onActive, estilo.onFocused })
        {
            estado.background = fondo;
            estado.scaledBackgrounds = new Texture2D[0];
            estado.textColor = texto;
        }
    }

    static Texture2D Textura(Color color)
    {
        Texture2D t = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }

    // Barra de segmentos LED; cada segmento lleva el color de su tramo y el último se enciende a medias.
    public static void BarraSegmentos(Rect barra, float porcentaje, int segmentos, float hueco, float alfa, TemaUINeon tema)
    {
        float anchoSegmento = (barra.width - hueco * (segmentos - 1)) / segmentos;
        float encendidos = porcentaje / 100f * segmentos;

        for (int i = 0; i < segmentos; i++)
        {
            Rect segmento = new Rect(barra.x + i * (anchoSegmento + hueco), barra.y, anchoSegmento, barra.height);
            Color color = tema.ColorEstado((i + 1f) / segmentos * 100f);

            float encendido = Mathf.Clamp01(encendidos - i);
            Rectangulo(segmento, ConAlfa(color, Mathf.Lerp(0.1f, 1f, encendido) * alfa), 0.5f * barra.height * 0.3f);
            if (encendido > 0.5f)
                Rectangulo(new Rect(segmento.x - 1f, segmento.y - 1f, segmento.width + 2f, segmento.height + 2f), ConAlfa(color, 0.18f * encendido * alfa), 1f);
        }
    }
}
