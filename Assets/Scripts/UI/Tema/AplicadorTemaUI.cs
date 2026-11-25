using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aplica el TemaUINeon a una jerarquía de UI: placa + animación en la raíz, botones de neón,
// fuente DS-DIGI con halo en los textos y colores en sliders y toggles.
// Se puede poner a mano en un panel o dejar que ArranqueTemaUI lo aplique por ruta (sin tocar la escena).
[AddComponentMenu("UI/PiedrasIncr/Aplicador Tema UI")]
[DisallowMultipleComponent]
public class AplicadorTemaUI : MonoBehaviour
{
    [Tooltip("Vacío = Resources/TemaUINeon.")]
    public TemaUINeon tema;
    [Tooltip("La raíz se dibuja como placa del escáner y se anima al abrirse.")]
    public bool raizComoPanel = true;

    bool aplicado;

    void Awake()
    {
        if (tema == null) tema = TemaUINeon.Actual;
        AplicarAhora();
    }

    public void AplicarAhora()
    {
        if (aplicado) return;
        aplicado = true;
        Aplicar(transform, tema, raizComoPanel);
    }

    // Punto de entrada común. Es idempotente: si la raíz ya tiene el tema, no hace nada.
    public static void Aplicar(Transform raiz, TemaUINeon tema, bool raizComoPanel)
    {
        if (raiz == null || tema == null) return;
        if (raiz.GetComponent<TemaUIAplicado>() != null) return;
        raiz.gameObject.AddComponent<TemaUIAplicado>().hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor;

        if (raizComoPanel && raiz.GetComponent<PanelNeonUI>() == null)
            raiz.gameObject.AddComponent<PanelNeonUI>();

        foreach (Button boton in raiz.GetComponentsInChildren<Button>(true))
            if (!Ignorado(boton.transform, raiz) && boton.GetComponent<BotonNeonUI>() == null)
                boton.gameObject.AddComponent<BotonNeonUI>();

        foreach (TMP_Text texto in raiz.GetComponentsInChildren<TMP_Text>(true))
            if (!Ignorado(texto.transform, raiz))
                EstilizarTexto(texto, tema);

        foreach (Slider slider in raiz.GetComponentsInChildren<Slider>(true))
            if (!Ignorado(slider.transform, raiz))
                EstilizarSlider(slider, tema);

        foreach (TMP_Dropdown desplegable in raiz.GetComponentsInChildren<TMP_Dropdown>(true))
            if (!Ignorado(desplegable.transform, raiz))
                EstilizarDesplegable(desplegable, tema);

        foreach (Toggle toggle in raiz.GetComponentsInChildren<Toggle>(true))
            if (!Ignorado(toggle.transform, raiz) && toggle.graphic != null)
                toggle.graphic.color = tema.colorNeonAlto;
    }

    static void EstilizarTexto(TMP_Text texto, TemaUINeon tema)
    {
        if (tema.fuenteTMP != null && texto.font != tema.fuenteTMP)
            texto.font = tema.fuenteTMP;

        Material brillo = tema.MaterialTexto;
        if (brillo != null)
            texto.fontSharedMaterial = brillo;

        if (tema.forzarMayusculas)
            texto.fontStyle |= FontStyles.UpperCase;

        // Los textos de botones los colorea BotonNeonUI según el estado.
        if (texto.GetComponentInParent<Button>(true) == null)
        {
            Color c = tema.colorNeon;
            c.a = texto.color.a;
            texto.color = c;
        }
    }

    static void EstilizarSlider(Slider slider, TemaUINeon tema)
    {
        if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image relleno))
            relleno.color = tema.colorNeonMedio;
        if (slider.targetGraphic != null)
            slider.targetGraphic.color = tema.colorNeon;

        Transform fondo = slider.transform.Find("Background");
        if (fondo != null && fondo.TryGetComponent(out Image imagenFondo))
            imagenFondo.color = tema.colorPlaca;
    }

    static void EstilizarDesplegable(TMP_Dropdown desplegable, TemaUINeon tema)
    {
        if (desplegable.targetGraphic is Image fondo)
            fondo.color = tema.colorRellenoBoton;
        if (desplegable.template != null && desplegable.template.TryGetComponent(out Image lista))
            lista.color = new Color(tema.colorPlaca.r, tema.colorPlaca.g, tema.colorPlaca.b, 0.97f);
        if (desplegable.itemText != null)
            desplegable.itemText.color = tema.colorNeon;

        ColorBlock colores = desplegable.colors;
        colores.highlightedColor = tema.colorNeonAlto;
        colores.pressedColor = tema.colorNeonMedio;
        colores.disabledColor = new Color(tema.colorNeonBajo.r, tema.colorNeonBajo.g, tema.colorNeonBajo.b, 0.5f);
        desplegable.colors = colores;
    }

    // Un IgnorarTemaUI en el objeto o en un padre (por debajo de la raíz) lo deja como está.
    static bool Ignorado(Transform t, Transform raiz)
    {
        for (Transform actual = t; actual != null; actual = actual.parent)
        {
            if (actual.GetComponent<IgnorarTemaUI>() != null) return true;
            if (actual == raiz) break;
        }
        return false;
    }
}
