using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Botón con estilo del escáner: relleno tenue, borde de neón que se ilumina al pasar por encima
// y textos en lavanda (rojo apagado si el botón no se puede usar, como "LOCKED").
[AddComponentMenu("UI/PiedrasIncr/Botón Neón")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class BotonNeonUI : MonoBehaviour
{
    Button boton;
    TMP_Text[] textos;
    MarcoNeonUI marco;
    int ultimoEstado = -1;

    void Awake()
    {
        boton = GetComponent<Button>();
        textos = GetComponentsInChildren<TMP_Text>(true);
        Configurar();
    }

    void Configurar()
    {
        TemaUINeon tema = TemaUINeon.Actual;

        Image fondo = boton.targetGraphic as Image;
        if (fondo == null) fondo = GetComponent<Image>();
        if (fondo != null) fondo.color = tema.colorRellenoBoton;

        Transform existente = transform.Find(PanelNeonUI.NombreMarco);
        if (existente == null || !existente.TryGetComponent(out marco))
        {
            GameObject go = new GameObject(PanelNeonUI.NombreMarco, typeof(RectTransform), typeof(CanvasRenderer), typeof(MarcoNeonUI), typeof(LayoutElement));
            go.hideFlags = HideFlags.DontSaveInEditor;
            go.layer = gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            marco = go.GetComponent<MarcoNeonUI>();
        }
        marco.sombra = false;
        marco.placa = false;
        marco.scanlines = false;
        marco.esquinas = false;
        marco.alfaBorde = 1f;

        // El tinte de estados (hover, pulsado, bloqueado) lo recibe el borde de neón.
        boton.transition = Selectable.Transition.ColorTint;
        boton.targetGraphic = marco;
        ColorBlock colores = boton.colors;
        colores.normalColor = new Color(1f, 1f, 1f, tema.alfaBordeBotonReposo);
        colores.highlightedColor = Color.white;
        colores.selectedColor = Color.white;
        colores.pressedColor = tema.colorNeonAlto;
        colores.disabledColor = new Color(tema.colorNeonBajo.r, tema.colorNeonBajo.g, tema.colorNeonBajo.b, 0.6f);
        colores.colorMultiplier = 1f;
        colores.fadeDuration = tema.duracionFundidoBoton;
        boton.colors = colores;
    }

    // Solo actúa cuando cambia "interactable" (lo cambian las tiendas al comprar o al llegar al máximo).
    void LateUpdate()
    {
        int estado = boton.IsInteractable() ? 1 : 0;
        if (estado == ultimoEstado) return;
        ultimoEstado = estado;

        TemaUINeon tema = TemaUINeon.Actual;
        Color color = estado == 1 ? tema.colorNeon : new Color(tema.colorNeonBajo.r, tema.colorNeonBajo.g, tema.colorNeonBajo.b, 0.8f);
        foreach (TMP_Text texto in textos)
            if (texto != null) texto.color = color;
    }
}
