using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// TEMPORAL: se borra al terminar.
public static class TmpMenuBuilder
{
    const string Out = @"C:\Users\egoit\AppData\Local\Temp\claude\b6df86ffc997a27b\217b105a-07b3-4a2d-b00b-9902a72abb08\scratchpad\";
    static readonly Color Placa = new Color(0.035f, 0.025f, 0.09f, 1f);
    static readonly Color Neon = new Color(0.926f, 0.887f, 1f, 1f);
    static readonly Color Violeta = new Color(0.579f, 0.387f, 0.943f, 1f);

    public static void Probe()
    {
        var sb = new System.Text.StringBuilder();
        EditorSceneManager.OpenScene("Assets/Piedra.unity");
        foreach (var c in new Component[] { Object.FindFirstObjectByType<Fisionador>(FindObjectsInactive.Include), Object.FindFirstObjectByType<GeneradorAranas>(FindObjectsInactive.Include) })
        {
            sb.AppendLine("## " + c.name + " pos=" + c.transform.position.ToString("F3") + " rotY=" + c.transform.eulerAngles.y + " scale=" + c.transform.lossyScale);
            Bounds b = default; bool has = false;
            foreach (var r in c.GetComponentsInChildren<Renderer>(true))
            {
                sb.AppendLine("  R " + Ruta(r.transform, c.transform) + " " + r.GetType().Name + " act=" + r.gameObject.activeInHierarchy + " en=" + r.enabled + " c=" + r.bounds.center.ToString("F2") + " s=" + r.bounds.size.ToString("F2"));
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || r.GetComponent<TMP_Text>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            foreach (var k in c.GetComponentsInChildren<Collider>(true))
                sb.AppendLine("  C " + Ruta(k.transform, c.transform) + " " + k.GetType().Name + " trig=" + k.isTrigger + " layer=" + k.gameObject.layer + " c=" + k.bounds.center.ToString("F2") + " s=" + k.bounds.size.ToString("F2"));
            foreach (Transform t in c.transform) sb.AppendLine("  child " + t.name + " act=" + t.gameObject.activeSelf);
            sb.AppendLine("  TOTAL c=" + b.center.ToString("F3") + " s=" + b.size.ToString("F3") + " min=" + b.min.ToString("F3") + " max=" + b.max.ToString("F3"));
            foreach (var off in new[] { Vector3.zero, new Vector3(2, 0, 2), new Vector3(-2, 0, -2), new Vector3(2, 0, -2), new Vector3(-2, 0, 2) })
                foreach (var h in Physics.RaycastAll(b.center + off + Vector3.up * 20, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
                    sb.AppendLine("  ray" + off + " -> " + h.collider.name + " y=" + h.point.y.ToString("F3") + " layer=" + LayerMask.LayerToName(h.collider.gameObject.layer));
        }
        foreach (var bot in Object.FindObjectsByType<BotRecolector>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.AppendLine("BOT " + bot.name + " " + bot.transform.position);
        File.WriteAllText(Out + "probe.txt", sb.ToString());
    }

    static string Ruta(Transform t, Transform raiz) { string s = t.name; while (t != raiz && t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }

    static GameObject Buscar(string ruta)
    {
        string[] p = ruta.Split('/');
        GameObject g = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().First(x => x.name == p[0]);
        for (int i = 1; i < p.Length; i++) g = g.transform.Find(p[i]).gameObject;
        return g;
    }

    static RectTransform Colocar(GameObject g, Transform padre, float x, float y, float w, float h)
    {
        var r = g.GetComponent<RectTransform>();
        if (r == null) r = g.AddComponent<RectTransform>();
        r.SetParent(padre, false);
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition3D = new Vector3(x, y, 0);
        r.sizeDelta = new Vector2(w, h);
        return r;
    }

    static void Estirar(GameObject g, Transform padre)
    {
        var r = g.GetComponent<RectTransform>();
        if (r == null) r = g.AddComponent<RectTransform>();
        r.SetParent(padre, false);
        r.localScale = Vector3.one;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static TMP_Text Texto(TMP_Text t, string txt, float size, TextAlignmentOptions al, bool bold)
    {
        t.text = txt; t.fontSize = size; t.alignment = al; t.color = Neon;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.enableAutoSizing = false; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static void Boton(GameObject g, string nombre, string txt, float size, TextAlignmentOptions al)
    {
        g.name = nombre;
        var img = g.GetComponent<Image>();
        img.color = Color.white;
        var b = g.GetComponent<Button>();
        var cb = b.colors;
        cb.normalColor = new Color(0.16f, 0.12f, 0.30f, 1f);
        cb.highlightedColor = Violeta;
        cb.selectedColor = new Color(0.30f, 0.21f, 0.52f, 1f);
        cb.pressedColor = new Color(0.914f, 0.616f, 0.992f, 1f);
        cb.disabledColor = new Color(0.16f, 0.12f, 0.30f, 0.35f);
        cb.colorMultiplier = 1f; cb.fadeDuration = 0.08f;
        b.colors = cb;
        var t = g.GetComponentInChildren<TMP_Text>(true);
        Texto(t, txt, size, al, true);
        var r = t.rectTransform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(28, 4); r.offsetMax = new Vector2(-28, -4);
    }

    static void Enlazar(GameObject g, UnityEngine.Events.UnityAction a)
    {
        var b = g.GetComponent<Button>();
        while (b.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(b.onClick, 0);
        UnityEventTools.AddPersistentListener(b.onClick, a);
    }

    public static void Build()
    {
        var escena = EditorSceneManager.OpenScene("Assets/MenuPrincipal.unity");
        GameObject canvas = Buscar("Canvas");
        var gestor = Buscar("GestorMenuPrincipal").GetComponent<MenuPrincipalGuardado>();

        var sc = canvas.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        var cam = Buscar("Main Camera").GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Placa;

        GameObject nueva = Buscar("Canvas/Button"), continuar = Buscar("Canvas/Button (1)"), cargar = Buscar("Canvas/Button (2)");
        GameObject panelR = Buscar("Canvas/PanelSeleccionRanura");

        // Logo (tapado solo por la confirmación).
        var logo = new GameObject("Logo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        logo.layer = canvas.layer;
        Colocar(logo, canvas.transform, 0, 305, 760, 335);
        var li = logo.GetComponent<Image>();
        li.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/rock it logo title 2.png").OfType<Sprite>().OrderByDescending(s => s.rect.width * s.rect.height).First();
        li.preserveAspect = true; li.raycastTarget = false;
        Debug.Log("[TMP] sprite " + li.sprite.name + " " + li.sprite.rect);

        // Botones principales.
        var principal = new GameObject("PanelPrincipal", typeof(RectTransform));
        principal.layer = canvas.layer;
        Estirar(principal, canvas.transform);
        Colocar(continuar, principal.transform, 0, 40, 560, 84);
        Colocar(nueva, principal.transform, 0, -64, 560, 84);
        Colocar(cargar, principal.transform, 0, -168, 560, 84);
        Boton(continuar, "BotonContinuar", "CONTINUAR", 36, TextAlignmentOptions.Center);
        Boton(nueva, "BotonNuevaPartida", "NUEVA PARTIDA", 36, TextAlignmentOptions.Center);
        Boton(cargar, "BotonCargarPartida", "CARGAR PARTIDA", 36, TextAlignmentOptions.Center);
        GameObject salir = Object.Instantiate(cargar);
        Colocar(salir, principal.transform, 0, -272, 560, 84);
        Boton(salir, "BotonSalir", "SALIR", 36, TextAlignmentOptions.Center);
        continuar.transform.SetAsFirstSibling();
        Enlazar(continuar, gestor.ContinuarPartida);
        Enlazar(nueva, gestor.NuevaPartida);
        Enlazar(cargar, gestor.CargarPartida);
        Enlazar(salir, gestor.Salir);

        // Selección de ranura.
        Estirar(panelR, canvas.transform);
        panelR.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        panelR.GetComponent<Image>().raycastTarget = false;
        TMP_Text titulo = panelR.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
        titulo.gameObject.name = "TituloSeleccion";
        Colocar(titulo.gameObject, panelR.transform, 0, 120, 900, 60);
        Texto(titulo, "NUEVA PARTIDA", 44, TextAlignmentOptions.Center, true);
        for (int i = 1; i <= 3; i++)
        {
            GameObject g = panelR.transform.Find("Ranura" + i).gameObject;
            Colocar(g, panelR.transform, 0, 20 - (i - 1) * 124, 900, 108);
            Boton(g, "Ranura" + i, "RANURA " + i, 36, TextAlignmentOptions.Left);
        }
        GameObject volver = panelR.transform.Find("Volver").gameObject;
        Colocar(volver, panelR.transform, 0, -370, 360, 72);
        Boton(volver, "Volver", "VOLVER", 32, TextAlignmentOptions.Center);
        Enlazar(panelR.transform.Find("Ranura1").gameObject, gestor.ElegirRanura1);
        Enlazar(panelR.transform.Find("Ranura2").gameObject, gestor.ElegirRanura2);
        Enlazar(panelR.transform.Find("Ranura3").gameObject, gestor.ElegirRanura3);
        Enlazar(volver, gestor.Volver);

        // Mensajes.
        var msg = new GameObject("TextoMensaje", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        msg.layer = canvas.layer;
        Colocar(msg, canvas.transform, 0, -465, 1400, 50);
        TMP_Text tm = Texto(msg.GetComponent<TMP_Text>(), "", 30, TextAlignmentOptions.Center, false);

        // Confirmación.
        var conf = new GameObject("PanelConfirmacion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        conf.layer = canvas.layer;
        Estirar(conf, canvas.transform);
        conf.GetComponent<Image>().color = new Color(0.01f, 0.005f, 0.03f, 0.92f);
        var caja = new GameObject("Caja", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        caja.layer = canvas.layer;
        Colocar(caja, conf.transform, 0, 0, 1040, 380);
        var ci = caja.GetComponent<Image>();
        ci.sprite = nueva.GetComponent<Image>().sprite; ci.type = Image.Type.Sliced;
        ci.color = new Color(0.09f, 0.065f, 0.19f, 1f);
        var tc = new GameObject("TextoConfirmacion", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        tc.layer = canvas.layer;
        Colocar(tc, caja.transform, 0, 70, 940, 180);
        TMP_Text tct = Texto(tc.GetComponent<TMP_Text>(), "La ranura 1 ya tiene una partida.\nSi empiezas de cero, se borrará y no se podrá recuperar.", 34, TextAlignmentOptions.Center, false);
        GameObject si = Object.Instantiate(volver), no = Object.Instantiate(volver);
        Colocar(si, caja.transform, -245, -115, 450, 78);
        Colocar(no, caja.transform, 245, -115, 450, 78);
        Boton(si, "BotonBorrarYEmpezar", "BORRAR Y EMPEZAR", 30, TextAlignmentOptions.Center);
        Boton(no, "BotonCancelar", "CANCELAR", 30, TextAlignmentOptions.Center);
        Enlazar(si, gestor.ConfirmarSobrescritura);
        Enlazar(no, gestor.CancelarSobrescritura);

        // Referencias.
        var so = new SerializedObject(gestor);
        void Ref(string p, Object o) { var sp = so.FindProperty(p); if (sp == null) throw new System.Exception("Falta " + p); sp.objectReferenceValue = o; }
        Ref("panelPrincipal", principal);
        Ref("botonContinuar", continuar.GetComponent<Button>());
        Ref("botonCargar", cargar.GetComponent<Button>());
        Ref("textoContinuar", continuar.GetComponentInChildren<TMP_Text>(true));
        Ref("textoMensaje", tm);
        Ref("panelSeleccionRanura", panelR);
        Ref("tituloSeleccion", titulo);
        for (int i = 1; i <= 3; i++)
        {
            GameObject g = panelR.transform.Find("Ranura" + i).gameObject;
            Ref("botonRanura" + i, g.GetComponent<Button>());
            Ref("textoRanura" + i, g.GetComponentInChildren<TMP_Text>(true));
        }
        Ref("panelConfirmacion", conf);
        Ref("textoConfirmacion", tct);
        so.ApplyModifiedPropertiesWithoutUndo();

        // Guardado tal y como se ve al arrancar.
        panelR.SetActive(false);
        conf.SetActive(false);
        EditorSceneManager.MarkSceneDirty(escena);
        bool ok = EditorSceneManager.SaveScene(escena);
        Debug.Log("[TMP] MENU GUARDADO " + ok);

        // Capturas (sin guardar).
        Captura(canvas, cam, "menu1.png");
        principal.SetActive(false); panelR.SetActive(true);
        for (int i = 1; i <= 3; i++) panelR.transform.Find("Ranura" + i).GetComponentInChildren<TMP_Text>(true).text = "RANURA " + i + "\n<size=60%>" + (i < 3 ? "997 monedas · 1 min jugados · 02/11/2026 22:43" : "Vacía") + "</size>";
        Captura(canvas, cam, "menu2.png");
        conf.SetActive(true);
        Captura(canvas, cam, "menu3.png");
    }

    static void Captura(GameObject canvas, Camera cam, string nombre)
    {
        var c = canvas.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 5;
        var rt = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        foreach (var t in canvas.GetComponentsInChildren<TMP_Text>()) t.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
        File.WriteAllBytes(Out + nombre, tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null;
    }

    public static void ProbeYMenu()
    {
        try { Probe(); Build(); EditorApplication.Exit(0); }
        catch (System.Exception e) { Debug.LogError("[TMP] FALLO " + e); EditorApplication.Exit(3); }
    }
}
