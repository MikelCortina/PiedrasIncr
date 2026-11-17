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

        private static readonly string[] Titles = { "GENERAL", "PANTALLA", "GRÁFICOS", "AUDIO" };
        private static readonly string[] Descriptions =
        {
            "COMPORTAMIENTO Y TELEMETRÍA",
            "MONITOR Y MODO DE VENTANA",
            "RENDIMIENTO Y RENDERIZADO",
            "VOLUMEN GENERAL DEL JUEGO"
        };

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private static readonly HashSet<string> BlockedGameplayScripts = new HashSet<string>
        {
            "SistemaPatada", "GolpeadorCentroPantalla", "ArmaLanzadora", "ThirdPersonSwordCamera"
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

            if (masterSlider != null)
                masterSlider.onValueChanged.AddListener(PreviewVolume);
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
            SetStatus("F10 / ESC: CERRAR    |    APLICAR PARA GUARDAR");
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

            if (categoryButtons == null)
                return;
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                Button button = categoryButtons[i];
                if (button == null)
                    continue;
                Image image = button.GetComponent<Image>();
                if (image != null)
                    image.color = i == selectedPage
                        ? new Color(0.07f, 0.34f, 0.41f, 1f)
                        : new Color(0.076f, 0.125f, 0.18f, 1f);
            }
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

        private void PreviewVolume(float value)
        {
            if (!opened || draft == null) return;
            draft.masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = draft.masterVolume;
            if (masterValue != null)
                masterValue.text = Mathf.RoundToInt(draft.masterVolume * 100f) + "%";
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
            if (masterSlider != null)
                masterSlider.SetValueWithoutNotify(draft.masterVolume);
            if (masterValue != null)
                masterValue.text = Mathf.RoundToInt(draft.masterVolume * 100f) + "%";
        }

        private void ResetDefaults()
        {
            if (draft == null || waitingDisplay) return;
            draft = PiedrasOptionsService.Defaults;
            AudioListener.volume = draft.masterVolume;
            UpdateControls();
            SetStatus("VALORES PREDETERMINADOS CARGADOS. PULSA APLICAR PARA GUARDARLOS.");
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
            SetStatus("CAMBIOS DE PANTALLA REVERTIDOS");
        }

        private void SaveAndClose(OptionsData data)
        {
            if (!PiedrasOptionsService.Save(data))
            {
                if (beforeDisplay != null)
                    PiedrasOptionsService.Apply(beforeDisplay, true);
                SetStatus("ERROR AL GUARDAR. NO SE HAN CONSERVADO LOS CAMBIOS.");
                return;
            }
            CloseInternal();
        }

        private void CancelAndClose()
        {
            if (!opened) return;
            if (waitingDisplay)
                RevertDisplay();
            PiedrasOptionsService.Apply(PiedrasOptionsService.Current, false);
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
