using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PiedrasIncr.Options
{
    public sealed class PiedrasOptionsMenu : MonoBehaviour
    {
        [Header("Vista generada")]
        public GameObject overlay;
        public GameObject[] pages;
        public Button[] categoryButtons;
        public TMP_Text pageTitle;
        public TMP_Text pageDescription;
        public TMP_Text statusText;
        public TMP_Text fpsCounter;
        public OptionsCycleRow showFpsRow;
        public OptionsCycleRow pauseRow;
        public OptionsCycleRow windowModeRow;
        public OptionsCycleRow resolutionRow;
        public OptionsCycleRow qualityRow;
        public OptionsCycleRow vsyncRow;
        public OptionsCycleRow fpsRow;
        public Slider masterSlider;
        public TMP_Text masterValue;
        public Slider sfxSlider;
        public TMP_Text sfxValue;
        public Slider musicSlider;
        public TMP_Text musicValue;
        public Slider uiSlider;
        public TMP_Text uiValue;
        public Slider ambientSlider;
        public TMP_Text ambientValue;
        public OptionsCycleRow paletteRow;
        public Slider redSlider;
        public Slider greenSlider;
        public Slider blueSlider;
        public TMP_Text redValue;
        public TMP_Text greenValue;
        public TMP_Text blueValue;
        public Image palettePreview;
        public Button applyButton;
        public Button cancelButton;
        public Button resetButton;
        public Button closeTopButton;
        public GameObject confirmationPanel;
        public TMP_Text confirmationText;
        public Button confirmDisplayButton;
        public Button revertDisplayButton;

        [Header("Tecla de apertura")]
        public KeyCode shortcut = KeyCode.F10;

        private static readonly string[] Titles = { "GENERAL", "PANTALLA", "GRÁFICOS", "AUDIO", "APARIENCIA" };
        private static readonly string[] Descriptions =
        {
            "PREFERENCIAS DE JUEGO",
            "PANTALLA Y RESOLUCIÓN",
            "CALIDAD Y RENDIMIENTO",
            "VOLUMEN Y MEZCLA",
            "PERSONALIZACIÓN DEL MENÚ"
        };
        private static readonly string[] PaletteNames =
        {
            "CIAN ELÉCTRICO", "ÁMBAR", "VIOLETA", "ESMERALDA", "CORAL", "PERSONALIZADO"
        };

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private static readonly HashSet<string> BlockedGameplayScripts = new HashSet<string>
{
    "SistemaPatada",
    "GolpeadorCentroPantalla",
    "ArmaLanzadora",
    "ThirdPersonSwordCamera",
    "CamaraPrimeraPersona"
};

        private OptionsData draft;
        private OptionsData beforeDisplay;
        private float previousTimeScale;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private GameObject previousSelected;
        private bool opened;
        private bool pausedByMenu;
        private bool waitingDisplay;
        private float confirmationTime;
        private float smoothedFps;
        private float fpsRefresh;
        private int selectedPage;

        public bool IsOpen => opened;

        private void Awake()
        {
            PiedrasOptionsService.Initialize();
            Transform estadoSistema = transform.Find("Overlay/PanelPrincipal/Cabecera/EstadoSistema");

            if (estadoSistema != null)
                estadoSistema.gameObject.SetActive(false);
            if (overlay != null)
                overlay.SetActive(false);
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);

            ResolveMissingRows();
            Bind(showFpsRow, () => ToggleShowFps(), () => ToggleShowFps());
            Bind(pauseRow, () => TogglePause(), () => TogglePause());
            Bind(windowModeRow, () => CycleWindowMode(-1), () => CycleWindowMode(1));
            Bind(resolutionRow, () => CycleResolution(-1), () => CycleResolution(1));
            Bind(qualityRow, () => CycleQuality(-1), () => CycleQuality(1));
            Bind(vsyncRow, () => CycleVSync(-1), () => CycleVSync(1));
            Bind(fpsRow, () => CycleFps(-1), () => CycleFps(1));
            Bind(paletteRow, () => CyclePalette(-1), () => CyclePalette(1));

            if (masterSlider != null)
                masterSlider.onValueChanged.AddListener(value => PreviewVolume(0, value));
            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(value => PreviewVolume(1, value));
            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(value => PreviewVolume(2, value));
            if (uiSlider != null)
                uiSlider.onValueChanged.AddListener(value => PreviewVolume(3, value));
            if (ambientSlider != null)
                ambientSlider.onValueChanged.AddListener(value => PreviewVolume(4, value));
            if (redSlider != null)
                redSlider.onValueChanged.AddListener(value => PreviewCustomColor(0, value));
            if (greenSlider != null)
                greenSlider.onValueChanged.AddListener(value => PreviewCustomColor(1, value));
            if (blueSlider != null)
                blueSlider.onValueChanged.AddListener(value => PreviewCustomColor(2, value));
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplyChanges);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelAndClose);
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetDefaults);
            if (closeTopButton != null)
                closeTopButton.onClick.AddListener(CancelAndClose);
            if (confirmDisplayButton != null)
                confirmDisplayButton.onClick.AddListener(ConfirmDisplay);
            if (revertDisplayButton != null)
                revertDisplayButton.onClick.AddListener(RevertDisplay);

            if (categoryButtons != null)
            {
                for (int i = 0; i < categoryButtons.Length; i++)
                {
                    int index = i;
                    if (categoryButtons[i] != null)
                        categoryButtons[i].onClick.AddListener(() => SetPage(index));
                }
            }
            ApplyTheme(PiedrasOptionsService.Current);
        }

        private void Start()
        {
            PiedrasOptionsService.ActivateAudio();
        }

        private void ResolveMissingRows()
        {
            if (showFpsRow == null) showFpsRow = Resolve(0, "Ajuste_MOSTRAR FPS");
            if (pauseRow == null) pauseRow = Resolve(0, "Ajuste_PAUSAR AL ABRIR");
            if (windowModeRow == null) windowModeRow = Resolve(1, "Ajuste_MODO DE PANTALLA");
            if (resolutionRow == null) resolutionRow = Resolve(1, "Ajuste_RESOLUCION");
            if (qualityRow == null) qualityRow = Resolve(2, "Ajuste_CALIDAD GRAFICA");
            if (vsyncRow == null) vsyncRow = Resolve(2, "Ajuste_SINCRONIZACION VERTICAL");
            if (fpsRow == null) fpsRow = Resolve(2, "Ajuste_LIMITE DE FPS");
            if (paletteRow == null) paletteRow = Resolve(4, "Ajuste_PALETA DE COLOR");
        }

        private OptionsCycleRow Resolve(int page, string name)
        {
            Transform rowTransform = transform.Find("Overlay/PanelPrincipal/AreaOpciones/Pagina_" + page + "/" + name);
            if (rowTransform == null) return null;
            OptionsCycleRow row = rowTransform.GetComponent<OptionsCycleRow>();
            if (row == null) return null;
            if (row.previous == null) row.previous = rowTransform.Find("Anterior")?.GetComponent<Button>();
            if (row.next == null) row.next = rowTransform.Find("Siguiente")?.GetComponent<Button>();
            if (row.value == null) row.value = rowTransform.Find("Valor")?.GetComponent<TMP_Text>();
            return row;
        }

        private static void Bind(OptionsCycleRow row, UnityEngine.Events.UnityAction previous, UnityEngine.Events.UnityAction next)
        {
            if (row == null || !row.IsComplete)
                return;
            row.previous.onClick.AddListener(previous);
            row.next.onClick.AddListener(next);
        }

        private void Update()
        {
            if (Input.GetKeyDown(shortcut))
            {
                if (opened)
                    CancelAndClose();
                else
                    Open();
            }
            else if (opened && Input.GetKeyDown(KeyCode.Escape))
            {
                if (waitingDisplay)
                    RevertDisplay();
                else
                    CancelAndClose();
            }

            if (!waitingDisplay)
                return;

            confirmationTime -= Time.unscaledDeltaTime;
            if (confirmationTime <= 0f)
            {
                RevertDisplay();
                return;
            }

            if (confirmationText != null)
                confirmationText.text = "¿MANTENER LA CONFIGURACIÓN?\nSe restaurará en " + Mathf.CeilToInt(confirmationTime) + " segundos.";
        }

        private void LateUpdate()
        {
            if (fpsCounter == null)
                return;

            bool show = !opened && PiedrasOptionsService.ShowFps;
            if (fpsCounter.gameObject.activeSelf != show)
                fpsCounter.gameObject.SetActive(show);
            if (!show || Time.unscaledDeltaTime <= 0f)
                return;

            float instantaneous = 1f / Time.unscaledDeltaTime;
            smoothedFps = smoothedFps <= 0f ? instantaneous : Mathf.Lerp(smoothedFps, instantaneous, 0.09f);
            fpsRefresh += Time.unscaledDeltaTime;
            if (fpsRefresh >= 0.25f)
            {
                fpsCounter.text = Mathf.RoundToInt(smoothedFps) + " FPS";
                fpsRefresh = 0f;
            }
        }

        public void Open()
        {
            if (opened || overlay == null || waitingDisplay)
                return;

            draft = PiedrasOptionsService.Current;
            previousTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            previousSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            resolutions.Clear();
            resolutions.AddRange(PiedrasOptionsService.GetResolutions());

            opened = true;
            pausedByMenu = draft.pauseInMenu && Time.timeScale > 0f;
            if (pausedByMenu)
                Time.timeScale = 0f;

            SuspendGameplayInput();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            overlay.SetActive(true);
            SetPage(0);
            UpdateControls();
            SetStatus("ESC · VOLVER      " + shortcut + " · CERRAR      APLICAR · GUARDAR");
            if (EventSystem.current != null && categoryButtons != null && categoryButtons.Length > 0)
                EventSystem.current.SetSelectedGameObject(categoryButtons[0].gameObject);
        }

        public void Abrir() => Open();
        public void Cerrar() => CancelAndClose();

        private void SuspendGameplayInput()
        {
            suspended.Clear();
            MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (MonoBehaviour behaviour in all)
            {
                if (behaviour == null || !behaviour.enabled || !BlockedGameplayScripts.Contains(behaviour.GetType().Name))
                    continue;
                suspended.Add(behaviour);
                behaviour.enabled = false;
            }
        }

        private void RestoreGameplayInput()
        {
            foreach (Behaviour behaviour in suspended)
                if (behaviour != null)
                    behaviour.enabled = true;
            suspended.Clear();
        }

        private void SetPage(int index)
        {
            if (pages == null || pages.Length == 0 || waitingDisplay)
                return;

            selectedPage = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null)
                    pages[i].SetActive(i == selectedPage);

            if (pageTitle != null && selectedPage < Titles.Length)
                pageTitle.text = Titles[selectedPage];
            if (pageDescription != null && selectedPage < Descriptions.Length)
                pageDescription.text = Descriptions[selectedPage];

            ApplyTheme(draft != null ? draft : PiedrasOptionsService.Current);
        }

        private void ToggleShowFps()
        {
            if (draft == null) return;
            draft.showFps = !draft.showFps;
            UpdateControls();
        }

        private void TogglePause()
        {
            if (draft == null) return;
            draft.pauseInMenu = !draft.pauseInMenu;
            UpdateControls();
        }

        private void CycleWindowMode(int direction)
        {
            if (draft == null || Application.isEditor) return;
            draft.windowMode = (draft.windowMode + direction + 2) % 2;
            UpdateControls();
        }

        private void CycleResolution(int direction)
        {
            if (draft == null || Application.isEditor || resolutions.Count == 0) return;
            int index = resolutions.FindIndex(r => r.x == draft.width && r.y == draft.height);
            if (index < 0) index = 0;
            index = (index + direction + resolutions.Count) % resolutions.Count;
            draft.width = resolutions[index].x;
            draft.height = resolutions[index].y;
            UpdateControls();
        }

        private void CycleQuality(int direction)
        {
            if (draft == null || QualitySettings.names.Length < 2) return;
            draft.quality = (draft.quality + direction + QualitySettings.names.Length) % QualitySettings.names.Length;
            UpdateControls();
        }

        private void CycleVSync(int direction)
        {
            if (draft == null) return;
            draft.vSync = (draft.vSync + direction + 3) % 3;
            UpdateControls();
        }

        private void CycleFps(int direction)
        {
            if (draft == null) return;
            IReadOnlyList<int> caps = PiedrasOptionsService.FpsOptions;
            int index = 0;
            for (int i = 0; i < caps.Count; i++)
                if (caps[i] == draft.fpsLimit)
                    index = i;
            draft.fpsLimit = caps[(index + direction + caps.Count) % caps.Count];
            UpdateControls();
        }

        private void CyclePalette(int direction)
        {
            if (draft == null) return;
            draft.palette = (draft.palette + direction + PaletteNames.Length) % PaletteNames.Length;
            UpdatePaletteControls();
            ApplyTheme(draft);
        }

        private void PreviewCustomColor(int channel, float value)
        {
            if (draft == null || !opened) return;
            Color current = PiedrasOptionsService.GetAccent(draft);
            if (draft.palette != 5)
            {
                draft.customR = Mathf.RoundToInt(current.r * 255f);
                draft.customG = Mathf.RoundToInt(current.g * 255f);
                draft.customB = Mathf.RoundToInt(current.b * 255f);
            }
            draft.palette = 5;
            int component = Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);
            if (channel == 0) draft.customR = component;
            if (channel == 1) draft.customG = component;
            if (channel == 2) draft.customB = component;
            UpdatePaletteControls();
            ApplyTheme(draft);
        }

        private void UpdatePaletteControls()
        {
            if (draft == null) return;
            int index = Mathf.Clamp(draft.palette, 0, PaletteNames.Length - 1);
            paletteRow?.SetValue(PaletteNames[index]);
            Color color = PiedrasOptionsService.GetAccent(draft);
            int red = Mathf.RoundToInt(color.r * 255f);
            int green = Mathf.RoundToInt(color.g * 255f);
            int blue = Mathf.RoundToInt(color.b * 255f);
            if (redSlider != null) redSlider.SetValueWithoutNotify(red);
            if (greenSlider != null) greenSlider.SetValueWithoutNotify(green);
            if (blueSlider != null) blueSlider.SetValueWithoutNotify(blue);
            if (redValue != null) redValue.text = red.ToString();
            if (greenValue != null) greenValue.text = green.ToString();
            if (blueValue != null) blueValue.text = blue.ToString();
            if (palettePreview != null) palettePreview.color = color;
        }

        private void ApplyTheme(OptionsData data)
        {
            if (overlay == null) return;
            Color accent = PiedrasOptionsService.GetAccent(data);
            Color card = Color.Lerp(new Color32(17, 36, 51, 255), accent, 0.055f);
            Color track = Color.Lerp(new Color32(29, 55, 70, 255), accent, 0.12f);
            Color buttonShade = Color.Lerp(new Color32(25, 54, 68, 255), accent, 0.12f);
            Color selectedShade = Color.Lerp(new Color32(13, 38, 53, 255), accent, 0.33f);
            Color panel = Color.Lerp(new Color32(11, 28, 42, 255), accent, 0.035f);
            foreach (Image image in overlay.GetComponentsInChildren<Image>(true))
            {
                string id = image.gameObject.name;
                if (id == "LineaSuperior" || id == "SidebarAccent" || id == "MarcaSeccion" || id == "LineaSeccion" || id == "ConfirmacionLinea" || id == "Fill")
                    image.color = accent;
                else if (id == "Pista")
                    image.color = track;
                else if (id == "PanelPrincipal" || id == "AreaOpciones")
                    image.color = panel;
                else if (id == "Cabecera")
                    image.color = Color.Lerp(new Color32(15, 33, 48, 255), accent, 0.04f);
                else if (id.StartsWith("Ajuste_") || id.StartsWith("Audio") || id.StartsWith("ColorCanal_") || id == "VistaPreviaColor")
                    image.color = card;
                else if (id.StartsWith("Categoria_"))
                {
                    int current;
                    image.color = int.TryParse(id.Substring(10), out current) && current == selectedPage ? selectedShade : new Color32(19, 38, 55, 255);
                }
                else if (id == "Anterior" || id == "Siguiente" || id == "Aplicar" || id == "ConfirmarPantalla")
                    image.color = buttonShade;
                else if (id == "MuestraColor")
                    image.color = accent;
            }
            foreach (TMP_Text label in overlay.GetComponentsInChildren<TMP_Text>(true))
            {
                string id = label.gameObject.name;
                if (id == "Marca" || id == "EstadoSistema" || id == "Valor" || id == "AudioValue" || id == "ValorCanal" || id == "TextoAyudaTema")
                    label.color = accent;
            }
            foreach (Button button in overlay.GetComponentsInChildren<Button>(true))
            {
                ColorBlock colors = button.colors;
                colors.highlightedColor = Color.Lerp(Color.white, accent, 0.28f);
                colors.pressedColor = Color.Lerp(accent, Color.black, 0.15f);
                colors.selectedColor = Color.Lerp(Color.white, accent, 0.4f);
                button.colors = colors;
            }
            if (fpsCounter != null) fpsCounter.color = accent;
            if (palettePreview != null) palettePreview.color = accent;
        }

        private void PreviewVolume(int channel, float value)
        {
            if (!opened || draft == null) return;
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case 0: draft.masterVolume = value; break;
                case 1: draft.sfxVolume = value; break;
                case 2: draft.musicVolume = value; break;
                case 3: draft.uiVolume = value; break;
                case 4: draft.ambientVolume = value; break;
            }
            PiedrasOptionsService.PreviewAudio(draft);
            UpdateAudioControls();
        }

        private void UpdateAudioControls()
        {
            if (draft == null) return;
            SetVolumeControl(masterSlider, masterValue, draft.masterVolume);
            SetVolumeControl(sfxSlider, sfxValue, draft.sfxVolume);
            SetVolumeControl(musicSlider, musicValue, draft.musicVolume);
            SetVolumeControl(uiSlider, uiValue, draft.uiVolume);
            SetVolumeControl(ambientSlider, ambientValue, draft.ambientVolume);
        }

        private static void SetVolumeControl(Slider slider, TMP_Text label, float volume)
        {
            if (slider != null) slider.SetValueWithoutNotify(volume);
            if (label != null) label.text = Mathf.RoundToInt(volume * 100f) + "%";
        }

        private void UpdateControls()
        {
            if (draft == null) return;
            showFpsRow?.SetValue(draft.showFps ? "ACTIVO" : "INACTIVO");
            pauseRow?.SetValue(draft.pauseInMenu ? "ACTIVADO" : "DESACTIVADO");
            windowModeRow?.SetValue(draft.windowMode == 1 ? "SIN BORDES" : "VENTANA");
            windowModeRow?.SetInteractive(!Application.isEditor);
            resolutionRow?.SetValue(draft.width + " x " + draft.height);
            resolutionRow?.SetInteractive(!Application.isEditor && resolutions.Count > 0);
            if (qualityRow != null)
            {
                string[] names = QualitySettings.names;
                qualityRow.SetValue(names.Length > 0 ? names[Mathf.Clamp(draft.quality, 0, names.Length - 1)].ToUpperInvariant() : "NO DISPONIBLE");
                qualityRow.SetInteractive(names.Length > 1);
            }
            vsyncRow?.SetValue(draft.vSync == 0 ? "NO" : draft.vSync == 1 ? "SI" : "CADA 2");
            fpsRow?.SetValue(draft.fpsLimit < 0 ? "SIN LÍMITE" : draft.fpsLimit + " FPS");
            UpdateAudioControls();
            UpdatePaletteControls();
            ApplyTheme(draft);
        }

        private void ResetDefaults()
        {
            if (draft == null || waitingDisplay) return;
            draft = PiedrasOptionsService.Defaults;
            PiedrasOptionsService.PreviewAudio(draft);
            UpdateControls();
            SetStatus("Se han restaurado los ajustes originales. Pulsa Aplicar para guardarlos.");
        }

        private void ApplyChanges()
        {
            if (draft == null || waitingDisplay) return;
            OptionsData valid = PiedrasOptionsService.Validate(draft);
            OptionsData original = PiedrasOptionsService.Current;
            bool displayChanged = valid.width != original.width || valid.height != original.height || valid.windowMode != original.windowMode;
            beforeDisplay = original;
            PiedrasOptionsService.Apply(valid, true);

#if !UNITY_EDITOR
            if (displayChanged)
            {
                waitingDisplay = true;
                confirmationTime = 15f;
                if (confirmationPanel != null)
                    confirmationPanel.SetActive(true);
                return;
            }
#endif
            SaveAndClose(valid);
        }

        private void ConfirmDisplay()
        {
            if (!waitingDisplay) return;
            waitingDisplay = false;
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            SaveAndClose(draft);
        }

        private void RevertDisplay()
        {
            if (!waitingDisplay) return;
            waitingDisplay = false;
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            if (beforeDisplay != null)
            {
                PiedrasOptionsService.Apply(beforeDisplay, true);
                draft = beforeDisplay.Copy();
            }
            UpdateControls();
            SetStatus("Se ha restaurado la configuración de pantalla anterior.");
        }

        private void SaveAndClose(OptionsData data)
        {
            if (!PiedrasOptionsService.Save(data))
            {
                PiedrasOptionsService.Apply(PiedrasOptionsService.Current, true);
                ApplyTheme(PiedrasOptionsService.Current);
                SetStatus("No se pudieron guardar los cambios. Inténtalo de nuevo.");
                return;
            }
            ApplyTheme(PiedrasOptionsService.Current);
            CloseInternal();
        }

        private void CancelAndClose()
        {
            if (!opened) return;
            if (waitingDisplay)
                RevertDisplay();
            PiedrasOptionsService.Apply(PiedrasOptionsService.Current, false);
            ApplyTheme(PiedrasOptionsService.Current);
            CloseInternal();
        }

        private void CloseInternal()
        {
            opened = false;
            waitingDisplay = false;
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            if (overlay != null)
                overlay.SetActive(false);
            if (pausedByMenu)
                Time.timeScale = previousTimeScale;
            pausedByMenu = false;
            RestoreGameplayInput();
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelected != null && previousSelected.activeInHierarchy ? previousSelected : null);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void OnDisable()
        {
            if (opened)
                CancelAndClose();
        }
    }
}
