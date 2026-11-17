using System.IO;
using PiedrasIncr.Options;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PiedrasIncr.Options.Editor
{
    public static class PiedrasOptionsInstaller
    {
        private const string RootFolder = "Assets/PiedrasIncrOptions";
        private const string GeneratedFolder = RootFolder + "/Generated";
        private const string PrefabPath = GeneratedFolder + "/PiedrasOptionsMenu.prefab";

        private static readonly Color Background = ColorOf(7, 16, 26, 242);
        private static readonly Color Frame = ColorOf(12, 27, 41);
        private static readonly Color Card = ColorOf(18, 40, 55);
        private static readonly Color Side = ColorOf(13, 28, 44);
        private static readonly Color Accent = ColorOf(71, 216, 227);
        private static readonly Color Bright = ColorOf(234, 248, 252);
        private static readonly Color Muted = ColorOf(143, 176, 196);

        [MenuItem("Tools/PiedrasIncr/Instalar o actualizar menu definitivo", false, 101)]
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("PiedrasIncr", "Sal de Play para actualizar el menu.", "Aceptar");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("PiedrasIncr", "Abre la escena primero.", "Aceptar");
                return;
            }

            PiedrasOptionsMenu[] menus = Object.FindObjectsByType<PiedrasOptionsMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int existingCount = 0;
            foreach (PiedrasOptionsMenu item in menus)
                if (item != null && item.gameObject.scene == scene)
                    existingCount++;

            if (existingCount > 0 && !EditorUtility.DisplayDialog("Actualizar menu de PiedrasIncr",
                "Hay " + existingCount + " menu(s) de opciones en esta escena. Se sustituiran por la version nueva. No se tocaran las partidas ni el gameplay. Se perderan los cambios manuales hechos en el menu anterior de la escena.",
                "Actualizar", "Cancelar"))
                return;

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.CreateFolder(RootFolder, "Generated");

            GameObject generated = null;
            try
            {
                generated = BuildVisual();
                EnsureReferences(generated);
                PrefabUtility.SaveAsPrefabAsset(generated, PrefabPath);
            }
            finally
            {
                if (generated != null)
                    Object.DestroyImmediate(generated);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("PiedrasIncr", "No se pudo guardar el prefab. Revisa la Console.", "Aceptar");
                return;
            }

            foreach (PiedrasOptionsMenu old in menus)
                if (old != null && old.gameObject.scene == scene)
                    Undo.DestroyObjectImmediate(old.gameObject);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Instalar menu definitivo PiedrasIncr");
            EnsureReferences(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<PiedrasOptionsMenu>());

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem_Options", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Crear EventSystem");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log("[Piedras Options] Menu actualizado con referencias enlazadas y prefab en " + PrefabPath, instance);
            EditorUtility.DisplayDialog("PiedrasIncr", "Menu instalado. Guarda la escena (Ctrl+S), pulsa Play y F10. No necesitas el antiguo parche.", "Aceptar");
        }

        private static void EnsureReferences(GameObject root)
        {
            PiedrasOptionsMenu menu = root.GetComponent<PiedrasOptionsMenu>();
            if (menu == null)
                throw new System.InvalidOperationException("El generador no creo PiedrasOptionsMenu.");

            menu.showFpsRow = FindRow(root.transform, 0, "Ajuste_MOSTRAR FPS");
            menu.pauseRow = FindRow(root.transform, 0, "Ajuste_PAUSAR AL ABRIR");
            menu.windowModeRow = FindRow(root.transform, 1, "Ajuste_MODO DE PANTALLA");
            menu.resolutionRow = FindRow(root.transform, 1, "Ajuste_RESOLUCION");
            menu.qualityRow = FindRow(root.transform, 2, "Ajuste_CALIDAD GRAFICA");
            menu.vsyncRow = FindRow(root.transform, 2, "Ajuste_SINCRONIZACION VERTICAL");
            menu.fpsRow = FindRow(root.transform, 2, "Ajuste_LIMITE DE FPS");
            if (menu.overlay == null || menu.pages == null || menu.pages.Length != 4 ||
                menu.categoryButtons == null || menu.categoryButtons.Length != 4 ||
                menu.showFpsRow == null || menu.pauseRow == null || menu.windowModeRow == null ||
                menu.resolutionRow == null || menu.qualityRow == null || menu.vsyncRow == null ||
                menu.fpsRow == null || menu.masterSlider == null || menu.confirmationPanel == null)
                throw new System.InvalidOperationException("El menu generado no tiene todas las referencias.");

            EditorUtility.SetDirty(menu);
        }

        private static OptionsCycleRow FindRow(Transform root, int page, string name)
        {
            string path = "Overlay/PanelPrincipal/AreaOpciones/Pagina_" + page + "/" + name;
            Transform tr = root.Find(path);
            if (tr == null)
                throw new System.InvalidOperationException("Falta la fila " + path);
            OptionsCycleRow row = tr.GetComponent<OptionsCycleRow>();
            if (row == null)
                row = tr.gameObject.AddComponent<OptionsCycleRow>();
            row.previous = tr.Find("Anterior")?.GetComponent<Button>();
            row.next = tr.Find("Siguiente")?.GetComponent<Button>();
            row.value = tr.Find("Valor")?.GetComponent<TMP_Text>();
            if (!row.IsComplete)
                throw new System.InvalidOperationException("Fila incompleta: " + path);
            EditorUtility.SetDirty(row);
            return row;
        }

        private static GameObject BuildVisual()
        {
            GameObject root = new GameObject("PiedrasOptionsMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PiedrasOptionsMenu));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            PiedrasOptionsMenu menu = root.GetComponent<PiedrasOptionsMenu>();
            Image cover = Block("Overlay", root.transform, Background);
            Stretch(cover.rectTransform, Vector2.zero, Vector2.one);
            menu.overlay = cover.gameObject;

            Image shell = Block("PanelPrincipal", cover.transform, Frame);
            Stretch(shell.rectTransform, new Vector2(0.085f, 0.08f), new Vector2(0.915f, 0.92f));

            Image lineTop = Block("LineaSuperior", shell.transform, Accent);
            Stretch(lineTop.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -5f), Vector2.zero);

            Image header = Block("Cabecera", shell.transform, ColorOf(16, 36, 52));
            Stretch(header.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -5f));
            Label("Marca", header.transform, "PIEDRASINCR   /   CONTROL DE SISTEMAS", 23, Accent, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.038f, 0.64f), new Vector2(0.72f, 0.94f));
            Label("Titulo", header.transform, "CONFIGURACIÓN", 48, Bright, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.038f, 0.11f), new Vector2(0.75f, 0.67f));
            Button closeTop = ButtonOf("CerrarCabecera", header.transform, "X", ColorOf(35, 60, 74), Bright,
                new Vector2(0.925f, 0.29f), new Vector2(0.976f, 0.75f));
            menu.closeTopButton = closeTop;
            Label("EstadoSistema", header.transform, "LOCAL / USUARIO", 17, Accent, TextAlignmentOptions.MidlineRight,
                new Vector2(0.70f, 0.65f), new Vector2(0.91f, 0.94f));

            Image sidebar = Block("Navegacion", shell.transform, Side);
            Stretch(sidebar.rectTransform, new Vector2(0.019f, 0.123f), new Vector2(0.257f, 0.816f));
            Label("SidebarLabel", sidebar.transform, "SECCIONES", 20, Muted, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.075f, 0.866f), new Vector2(0.92f, 0.961f));
            Image sideLine = Block("SidebarAccent", sidebar.transform, Accent);
            Stretch(sideLine.rectTransform, new Vector2(0.07f, 0.86f), new Vector2(0.93f, 0.865f));

            menu.categoryButtons = new Button[4];
            string[] categories = { "01   GENERAL", "02   PANTALLA", "03   GRÁFICOS", "04   AUDIO" };
            for (int i = 0; i < categories.Length; i++)
            {
                float top = 0.81f - i * 0.128f;
                menu.categoryButtons[i] = ButtonOf("Categoria_" + i, sidebar.transform, categories[i], ColorOf(19, 43, 61), Bright,
                    new Vector2(0.055f, top - 0.096f), new Vector2(0.947f, top));
            }

            Label("PieSidebar", sidebar.transform, "PERFIL LOCAL\nPREFERENCIAS v2", 18, Muted, TextAlignmentOptions.BottomLeft,
                new Vector2(0.076f, 0.045f), new Vector2(0.94f, 0.2f));

            Image main = Block("AreaOpciones", shell.transform, ColorOf(13, 32, 47));
            Stretch(main.rectTransform, new Vector2(0.272f, 0.123f), new Vector2(0.981f, 0.816f));

            Image sectionNumber = Block("MarcaSeccion", main.transform, Accent);
            Stretch(sectionNumber.rectTransform, new Vector2(0.013f, 0.883f), new Vector2(0.017f, 0.967f));
            menu.pageTitle = Label("CategoriaActiva", main.transform, "GENERAL", 38, Bright, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.043f, 0.877f), new Vector2(0.9f, 0.977f));
            menu.pageDescription = Label("SubtituloCategoria", main.transform, "PREFERENCIAS DE PARTIDA", 20, Muted, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.043f, 0.817f), new Vector2(0.94f, 0.882f));
            Image separator = Block("LineaSeccion", main.transform, Accent);
            Stretch(separator.rectTransform, new Vector2(0.044f, 0.808f), new Vector2(0.955f, 0.812f));

            menu.pages = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject page = Rect("Pagina_" + i, main.transform);
                Stretch(page.GetComponent<RectTransform>(), new Vector2(0.042f, 0.052f), new Vector2(0.96f, 0.78f));
                menu.pages[i] = page;
            }

            menu.showFpsRow = CycleRow(menu.pages[0].transform, 0.025f, "MOSTRAR FPS", "Contador de fotogramas durante el juego");
            menu.pauseRow = CycleRow(menu.pages[0].transform, 0.252f, "PAUSAR AL ABRIR", "Detiene el mundo mientras editas opciones");
            Label("AyudaGeneral", menu.pages[0].transform,
                "EL MENU SE ABRE CON F10. LOS CAMBIOS DE PREFERENCIAS SE GUARDAN AL APLICAR.",
                18, Accent, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.07f), new Vector2(0.96f, 0.40f));

            menu.windowModeRow = CycleRow(menu.pages[1].transform, 0.025f, "MODO DE PANTALLA", "Ventana o pantalla sin bordes");
            menu.resolutionRow = CycleRow(menu.pages[1].transform, 0.252f, "RESOLUCION", "Resoluciones compatibles con la pantalla");
            Label("NotaPantalla", menu.pages[1].transform,
                "EN EL EDITOR NO SE CAMBIA LA VENTANA. LA CONFIGURACION DE PANTALLA SE PRUEBA EN UNA BUILD DE WINDOWS O MACOS.",
                17, Accent, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.07f), new Vector2(0.96f, 0.40f));

            menu.qualityRow = CycleRow(menu.pages[2].transform, 0.025f, "CALIDAD GRAFICA", "Perfiles de calidad configurados en Unity");
            menu.vsyncRow = CycleRow(menu.pages[2].transform, 0.252f, "SINCRONIZACION VERTICAL", "VSync puede prevalecer sobre el limite de FPS");
            menu.fpsRow = CycleRow(menu.pages[2].transform, 0.479f, "LIMITE DE FPS", "Sin limite y frecuencias habituales de monitores");

            Image audioRow = Block("AudioMaster", menu.pages[3].transform, Card);
            Stretch(audioRow.rectTransform, new Vector2(0.03f, 0.665f), new Vector2(0.972f, 0.97f));
            Label("AudioLabel", audioRow.transform, "VOLUMEN GENERAL", 27, Bright, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.03f, 0.58f), new Vector2(0.7f, 0.91f));
            Label("AudioHint", audioRow.transform, "Afecta a todos los sonidos mediante AudioListener", 19, Muted, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.03f, 0.31f), new Vector2(0.8f, 0.57f));
            menu.masterSlider = CreateSlider(audioRow.transform);
            menu.masterValue = Label("AudioValue", audioRow.transform, "100%", 27, Accent, TextAlignmentOptions.MidlineRight,
                new Vector2(0.79f, 0.4f), new Vector2(0.96f, 0.95f));
            Label("AudioPending", menu.pages[3].transform,
                "EL MIXER DEL EQUIPO ESTA EN LA RAMA AUDIO. EL VOLUMEN GENERAL YA FUNCIONA; LOS BUSES SE INTEGRARAN SIN DUPLICAR SU SISTEMA.",
                16, Accent, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.35f), new Vector2(0.96f, 0.58f));

            Image footer = Block("Footer", shell.transform, ColorOf(11, 25, 38));
            Stretch(footer.rectTransform, new Vector2(0f, 0.012f), new Vector2(1f, 0.109f));
            menu.statusText = Label("Estado", footer.transform, "F10 O ESC PARA CERRAR", 18, Muted, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.034f, 0.16f), new Vector2(0.59f, 0.85f));
            menu.resetButton = ButtonOf("Predeterminados", footer.transform, "PREDETERMINADOS", ColorOf(30, 49, 62), Bright,
                new Vector2(0.601f, 0.21f), new Vector2(0.742f, 0.79f));
            menu.cancelButton = ButtonOf("Cancelar", footer.transform, "CANCELAR", ColorOf(29, 51, 69), Bright,
                new Vector2(0.755f, 0.21f), new Vector2(0.852f, 0.79f));
            menu.applyButton = ButtonOf("Aplicar", footer.transform, "APLICAR", ColorOf(20, 113, 122), Bright,
                new Vector2(0.863f, 0.21f), new Vector2(0.975f, 0.79f));

            Image confirmation = Block("ConfirmacionPantalla", cover.transform, ColorOf(4, 11, 20, 245));
            Stretch(confirmation.rectTransform, Vector2.zero, Vector2.one);
            menu.confirmationPanel = confirmation.gameObject;
            Image confirmationCard = Block("ConfirmacionPanel", confirmation.transform, ColorOf(19, 42, 58));
            Stretch(confirmationCard.rectTransform, new Vector2(0.24f, 0.35f), new Vector2(0.76f, 0.65f));
            Image confirmationAccent = Block("ConfirmacionLinea", confirmationCard.transform, Accent);
            Stretch(confirmationAccent.rectTransform, new Vector2(0f, 0.976f), Vector2.one);
            menu.confirmationText = Label("ConfirmacionTexto", confirmationCard.transform,
                "¿MANTENER ESTA CONFIGURACIÓN DE PANTALLA?", 25, Bright, TextAlignmentOptions.Center,
                new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.87f));
            menu.revertDisplayButton = ButtonOf("RevertirPantalla", confirmationCard.transform, "REVERTIR", ColorOf(44, 62, 76), Bright,
                new Vector2(0.10f, 0.1f), new Vector2(0.45f, 0.32f));
            menu.confirmDisplayButton = ButtonOf("ConfirmarPantalla", confirmationCard.transform, "MANTENER", ColorOf(20, 113, 122), Bright,
                new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.32f));
            confirmation.gameObject.SetActive(false);
            for (int i = 1; i < menu.pages.Length; i++)
                menu.pages[i].SetActive(false);
            cover.gameObject.SetActive(false);
            menu.fpsCounter = Label("FpsTelemetry", root.transform, "60 FPS", 24, Accent, TextAlignmentOptions.MidlineRight,
                new Vector2(0.84f, 0.931f), new Vector2(0.975f, 0.982f));
            menu.fpsCounter.gameObject.SetActive(false);
            return root;
        }

        private static OptionsCycleRow CycleRow(Transform parent, float topMargin, string label, string hint)
        {
            Image box = Block("Ajuste_" + label, parent, Card);
            float top = 1f - topMargin;
            Stretch(box.rectTransform, new Vector2(0.03f, top - 0.205f), new Vector2(0.972f, top));
            Label("Nombre", box.transform, label, 26, Bright, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.025f, 0.45f), new Vector2(0.69f, 0.92f));
            Label("Detalle", box.transform, hint, 19, Muted, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.025f, 0.11f), new Vector2(0.69f, 0.48f));
            OptionsCycleRow cycle = box.gameObject.AddComponent<OptionsCycleRow>();
            cycle.previous = ButtonOf("Anterior", box.transform, "-", ColorOf(28, 64, 78), Accent,
                new Vector2(0.68f, 0.21f), new Vector2(0.74f, 0.79f));
            cycle.value = Label("Valor", box.transform, "VALOR", 23, Accent, TextAlignmentOptions.Center,
                new Vector2(0.742f, 0.12f), new Vector2(0.925f, 0.88f));
            cycle.next = ButtonOf("Siguiente", box.transform, "+", ColorOf(28, 64, 78), Accent,
                new Vector2(0.927f, 0.21f), new Vector2(0.991f, 0.79f));
            return cycle;
        }

        private static Slider CreateSlider(Transform parent)
        {
            GameObject sliderObject = Rect("SliderVolumen", parent);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            Stretch(rect, new Vector2(0.035f, 0.09f), new Vector2(0.76f, 0.30f));
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            Image background = Block("Pista", sliderObject.transform, ColorOf(32, 62, 76));
            Stretch(background.rectTransform, new Vector2(0f, 0.37f), new Vector2(1f, 0.63f));

            GameObject fillArea = Rect("FillArea", sliderObject.transform);
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0.015f, 0.35f), new Vector2(0.985f, 0.65f));
            Image fill = Block("Fill", fillArea.transform, Accent);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one);
            slider.fillRect = fill.rectTransform;

            GameObject handleArea = Rect("HandleArea", sliderObject.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(0.025f, 0f), new Vector2(0.975f, 1f));
            Image handle = Block("Handle", handleArea.transform, Bright);
            RectTransform handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0.18f);
            handleRect.anchorMax = new Vector2(0f, 0.82f);
            handleRect.sizeDelta = new Vector2(14f, 0f);
            handleRect.anchoredPosition = Vector2.zero;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            return slider;
        }

        private static GameObject Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image Block(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static TMP_Text Label(string name, Transform parent, string content, float size, Color color,
            TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = FontStyles.Normal;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.78f;
            label.fontSizeMax = size;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            return label;
        }

        private static Button ButtonOf(string name, Transform parent, string content, Color baseColor, Color textColor,
            Vector2 min, Vector2 max)
        {
            Image image = Block(name, parent, baseColor);
            Stretch(image.rectTransform, min, max);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = ColorOf(177, 244, 252);
            colors.pressedColor = ColorOf(88, 199, 215);
            colors.selectedColor = ColorOf(190, 246, 252);
            colors.disabledColor = ColorOf(83, 89, 100, 128);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            Label("Texto", button.transform, content, 21, textColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            Stretch(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Color ColorOf(byte r, byte g, byte b, byte a = 255)
        {
            return new Color32(r, g, b, a);
        }
    }
}
