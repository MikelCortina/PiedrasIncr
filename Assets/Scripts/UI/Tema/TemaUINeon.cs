using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Tema visual común de toda la UI: el estilo de cartel LED / neón del escáner de piedras.
// Única fuente de verdad de colores, fuentes, medidas y tiempos. Se carga desde Resources/TemaUINeon.
[CreateAssetMenu(menuName = "PiedrasIncr/Tema UI Neón", fileName = "TemaUINeon")]
public class TemaUINeon : ScriptableObject
{
    public const string RutaResources = "TemaUINeon";

    [Header("Colores (los del escáner de piedras)")]
    [Tooltip("Placa de los paneles (violeta espacial muy oscuro).")]
    public Color colorPlaca = new Color(0.035f, 0.025f, 0.09f, 0.88f);
    [Tooltip("Neón de bordes y textos fijos (lavanda de los carteles).")]
    public Color colorNeon = new Color(0.926f, 0.887f, 1f, 1f);
    [Tooltip("Alerta / bloqueado / valor bajo.")]
    public Color colorNeonBajo = new Color(0.926f, 0.075f, 0.102f, 1f);
    public Color colorNeonMedio = new Color(0.579f, 0.387f, 0.943f, 1f);
    public Color colorNeonAlto = new Color(0.914f, 0.616f, 0.992f, 1f);
    [Tooltip("Color de 'listo' (parpadea).")]
    public Color colorNeonListo = new Color(0.926f, 0.887f, 1f, 1f);
    [Tooltip("Sombra que se proyecta bajo los paneles.")]
    public Color colorSombra = new Color(0f, 0f, 0f, 0.4f);

    [Header("Fuentes")]
    [Tooltip("Fuente TMP de los menús (DS-DIGI, la de los carteles LED). Vacío = se conserva la de cada texto.")]
    public TMP_FontAsset fuenteTMP;
    [Tooltip("Fuente de los HUD dibujados con OnGUI.")]
    public Font fuenteIMGUI;
    [Tooltip("Pone los textos en mayúsculas, como el panel del escáner.")]
    public bool forzarMayusculas = true;
    [Tooltip("Añade un halo de neón a los textos TMP (material compartido con Glow).")]
    public bool brilloTexto = true;
    [Range(0f, 1f)] public float intensidadBrilloTexto = 0.35f;

    [Header("Marco de los paneles (unidades de Canvas a 1080p)")]
    public float grosorBorde = 1.2f;
    [Range(0f, 1f)] public float alfaBorde = 0.75f;
    [Tooltip("Dos capas de halo por fuera del borde (alfa de la interior y de la exterior).")]
    public Vector2 alfaHalo = new Vector2(0.18f, 0.08f);
    public float separacionScanlines = 3f;
    [Range(0f, 0.2f)] public float alfaScanlines = 0.035f;
    public float largoEsquina = 12f;
    public float grosorEsquina = 2f;
    public float separacionEsquina = 5f;
    public Vector2 desplazamientoSombra = new Vector2(4f, -6f);

    [Header("Botones")]
    [Tooltip("Relleno tenue de los botones (el borde de neón lo pone el marco).")]
    public Color colorRellenoBoton = new Color(0.579f, 0.387f, 0.943f, 0.12f);
    [Range(0f, 1f)] public float alfaBordeBotonReposo = 0.6f;
    public float duracionFundidoBoton = 0.08f;

    [Header("Animaciones")]
    [Tooltip("Velocidad del fundido + pop al abrir un panel (1/segundos).")]
    public float velocidadFundido = 6f;
    [Tooltip("Escala de partida del 'pop' de aparición.")]
    [Range(0.5f, 1f)] public float escalaPopInicial = 0.85f;
    [Tooltip("Parpadeos por segundo del estado 'listo'.")]
    public float frecuenciaParpadeo = 1.6f;
    [Range(0f, 1f)] public float fraccionParpadeoEncendido = 0.65f;
    [Tooltip("Porcentaje a partir del cual un valor se considera 'listo'.")]
    public float porcentajeListo = 95f;

    [Header("Aplicación automática al cargar escena")]
    public bool aplicarAutomaticamente = true;
    [Tooltip("Rutas en la jerarquía de los paneles que se tematizan solos al cargar la escena (sin tocar la escena). " +
             "Formato: Raiz/Hijo/Panel. Ojo con los espacios en los nombres.")]
    public List<string> rutasPaneles = new List<string> { "Canvas/PanelTienda" };
    [Tooltip("Rutas cuyo contenido (textos, botones, sliders, desplegables) se tematiza sin placa ni animación. Ej: Canvas.")]
    public List<string> rutasSoloContenido = new List<string> { "Canvas" };
    [Tooltip("Nombres de componentes: todo objeto que tenga uno se tematiza sin placa (útil para prefabs, p. ej. PiedrasOptionsMenu).")]
    public List<string> componentesSoloContenido = new List<string> { "PiedrasOptionsMenu" };

    private static TemaUINeon actual;
    private Material materialTexto;

    public static TemaUINeon Actual
    {
        get
        {
            if (actual != null) return actual;

            actual = Resources.Load<TemaUINeon>(RutaResources);
            if (actual == null)
            {
                actual = CreateInstance<TemaUINeon>();
                actual.hideFlags = HideFlags.DontSave;
            }
            return actual;
        }
    }

    // Degradado rojo -> violeta -> lavanda, igual que la barra de desgaste del escáner.
    public Color ColorEstado(float porcentaje)
    {
        if (porcentaje >= porcentajeListo) return colorNeonListo;

        float t = Mathf.Clamp01(porcentaje / porcentajeListo);
        return t < 0.5f
            ? Color.Lerp(colorNeonBajo, colorNeonMedio, t * 2f)
            : Color.Lerp(colorNeonMedio, colorNeonAlto, (t - 0.5f) * 2f);
    }

    // Multiplicador de alfa del parpadeo de LED (1 si no está activo).
    public float Parpadeo(bool activo)
    {
        if (!activo) return 1f;
        return Mathf.Repeat(Time.unscaledTime * frecuenciaParpadeo, 1f) < fraccionParpadeoEncendido ? 1f : 0.35f;
    }

    public static float EaseOutCubico(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    // Material de texto con halo, compartido por todos los textos del tema (no se guarda en disco).
    public Material MaterialTexto
    {
        get
        {
            if (!brilloTexto || fuenteTMP == null || fuenteTMP.material == null) return null;
            if (materialTexto != null) return materialTexto;

            materialTexto = new Material(fuenteTMP.material)
            {
                name = fuenteTMP.material.name + " (Neon)",
                hideFlags = HideFlags.DontSave
            };
            materialTexto.EnableKeyword(ShaderUtilities.Keyword_Glow);
            ActualizarMaterialTexto();
            return materialTexto;
        }
    }

    void ActualizarMaterialTexto()
    {
        materialTexto.SetColor(ShaderUtilities.ID_GlowColor, new Color(colorNeonMedio.r, colorNeonMedio.g, colorNeonMedio.b, intensidadBrilloTexto));
        materialTexto.SetFloat(ShaderUtilities.ID_GlowOuter, 0.35f);
        materialTexto.SetFloat(ShaderUtilities.ID_GlowPower, 0.75f);
    }

    // Retocar el asset en Play actualiza el halo de los textos en vivo.
    void OnValidate()
    {
        if (materialTexto != null) ActualizarMaterialTexto();
    }
}
