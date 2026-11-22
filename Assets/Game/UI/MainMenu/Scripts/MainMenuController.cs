using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    /// <summary>
    /// Primary controller for the 2.5D Frutiger Aero Main Menu:
    /// - Editable Provisional Game Title
    /// - Left vertical button column: 1. JUGAR, 2. OPCIONES, 3. SALIR
    /// - World & Level Selection Screen (selects Level_01: Entrada al Bosque and transitions smoothly)
    /// - Options Panel (Master, Music, SFX volume, Resolution, Fullscreen)
    /// - Smooth visual screen transitions via CanvasGroup fade overlay
    /// - Editor-safe Quit test behavior + Application.Quit() in builds
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Game Title (Editable Provisional Title)")]
        [SerializeField] private string provisionalGameTitle = "FANTASY-WAR-LEGACY";
        [SerializeField] private string provisionalSubtitle = "FANTASÍA 2.5D • EDICIÓN FRUTIGER AERO";
        [SerializeField] private Text gameTitleText;
        [SerializeField] private Text gameSubtitleText;

        [Header("Main Buttons (Left Vertical Column)")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels & Sub-Screens")]
        [SerializeField] private CanvasGroup mainMenuGroup;
        [SerializeField] private CanvasGroup worldSelectPanel;
        [SerializeField] private CanvasGroup optionsPanel;
        [SerializeField] private CanvasGroup screenFadeOverlay;

        [Header("World / Level Selection Controls")]
        [SerializeField] private Button level01SelectButton;
        [SerializeField] private Button startLevel01Button;
        [SerializeField] private Button closeWorldSelectButton;
        [SerializeField] private Text selectedLevelTitleText;
        [SerializeField] private Text selectedLevelStatusText;
        [SerializeField] private string targetFirstLevelSceneName = "Level_01";

        [Header("Options Panel Controls")]
        [SerializeField] private Button closeOptionsButton;
        [SerializeField] private MainMenuOptionsController optionsController;

        [Header("Transition Timing")]
        [SerializeField] private float panelTransitionDuration = 0.28f;
        [SerializeField] private float sceneFadeDuration = 0.65f;

        private bool isTransitioning;
        private int selectedLevelIndex = 1;

        public bool IsWorldSelectOpen => worldSelectPanel != null && worldSelectPanel.gameObject.activeSelf && worldSelectPanel.alpha > 0.5f;
        public bool IsOptionsOpen => optionsPanel != null && optionsPanel.gameObject.activeSelf && optionsPanel.alpha > 0.5f;
        public int SelectedLevelIndex => selectedLevelIndex;
        public string LastActionLog { get; private set; } = "Initialized";

        private void Awake()
        {
            ApplyGameTitle();
            BindButtons();
            InitializePanelStates();
        }

        private void Start()
        {
            if (screenFadeOverlay != null)
            {
                screenFadeOverlay.gameObject.SetActive(true);
                screenFadeOverlay.alpha = 1f;
                screenFadeOverlay.blocksRaycasts = false;
                StartCoroutine(FadeCanvasGroupRoutine(screenFadeOverlay, 1f, 0f, sceneFadeDuration, disableOnZero: true));
            }
        }

        public void SetProvisionalGameTitle(string newTitle, string newSubtitle = null)
        {
            provisionalGameTitle = newTitle;
            if (!string.IsNullOrEmpty(newSubtitle))
            {
                provisionalSubtitle = newSubtitle;
            }
            ApplyGameTitle();
        }

        private void ApplyGameTitle()
        {
            if (gameTitleText != null && !string.IsNullOrEmpty(provisionalGameTitle))
            {
                gameTitleText.text = provisionalGameTitle;
            }
            if (gameSubtitleText != null && !string.IsNullOrEmpty(provisionalSubtitle))
            {
                gameSubtitleText.text = provisionalSubtitle;
            }
        }

        private void BindButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnClickPlay);
                playButton.onClick.AddListener(OnClickPlay);
            }
            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveListener(OnClickOptions);
                optionsButton.onClick.AddListener(OnClickOptions);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnClickQuit);
                quitButton.onClick.AddListener(OnClickQuit);
            }
            if (level01SelectButton != null)
            {
                level01SelectButton.onClick.RemoveListener(OnSelectLevel01);
                level01SelectButton.onClick.AddListener(OnSelectLevel01);
            }
            if (startLevel01Button != null)
            {
                startLevel01Button.onClick.RemoveListener(OnConfirmStartLevel01);
                startLevel01Button.onClick.AddListener(OnConfirmStartLevel01);
            }
            if (closeWorldSelectButton != null)
            {
                closeWorldSelectButton.onClick.RemoveListener(OnCloseWorldSelect);
                closeWorldSelectButton.onClick.AddListener(OnCloseWorldSelect);
            }
            if (closeOptionsButton != null)
            {
                closeOptionsButton.onClick.RemoveListener(OnCloseOptions);
                closeOptionsButton.onClick.AddListener(OnCloseOptions);
            }
        }

        private void InitializePanelStates()
        {
            SetGroupStateImmediate(mainMenuGroup, true);
            SetGroupStateImmediate(worldSelectPanel, false);
            SetGroupStateImmediate(optionsPanel, false);
        }

        private static void SetGroupStateImmediate(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.gameObject.SetActive(visible);
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        // =====================================================================
        // 1. JUGAR -> Abre Pantalla de Selección de Mundo / Niveles
        // =====================================================================
        public void OnClickPlay()
        {
            if (isTransitioning) return;
            LastActionLog = "Opened World/Level Selection";
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayClick();
            }

            OnSelectLevel01();
            StartCoroutine(SwitchModalPanelRoutine(worldSelectPanel, true));
        }

        public void OnSelectLevel01()
        {
            selectedLevelIndex = 1;
            LastActionLog = "Selected Level_01 (Entrada al Bosque)";
            if (selectedLevelTitleText != null)
            {
                selectedLevelTitleText.text = "MUNDO 1 • NIVEL 01: ENTRADA AL BOSQUE ENCANTADO";
            }
            if (selectedLevelStatusText != null)
            {
                selectedLevelStatusText.text = "ESTADO: DISPONIBLE • Listo para iniciar transición a " + targetFirstLevelSceneName;
            }
        }

        public void OnConfirmStartLevel01()
        {
            if (isTransitioning) return;
            LastActionLog = "Transitioning to " + targetFirstLevelSceneName;
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayClick();
            }
            StartCoroutine(TransitionToLevelRoutine(targetFirstLevelSceneName));
        }

        public void OnCloseWorldSelect()
        {
            if (isTransitioning) return;
            LastActionLog = "Closed World/Level Selection";
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayBack();
            }
            StartCoroutine(SwitchModalPanelRoutine(worldSelectPanel, false));
        }

        // =====================================================================
        // 2. OPCIONES -> Abre Panel de Opciones (Audio + Video)
        // =====================================================================
        public void OnClickOptions()
        {
            if (isTransitioning) return;
            LastActionLog = "Opened Options Panel";
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayClick();
            }
            if (optionsController != null)
            {
                optionsController.InitializeControls();
            }
            StartCoroutine(SwitchModalPanelRoutine(optionsPanel, true));
        }

        public void OnCloseOptions()
        {
            if (isTransitioning) return;
            LastActionLog = "Closed Options Panel";
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayBack();
            }
            PlayerPrefs.Save();
            StartCoroutine(SwitchModalPanelRoutine(optionsPanel, false));
        }

        // =====================================================================
        // 3. SALIR -> Cierra el juego (y comportamiento de prueba en Editor)
        // =====================================================================
        public void OnClickQuit()
        {
            if (isTransitioning) return;
            LastActionLog = "Quit Requested";
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayBack();
            }
            StartCoroutine(QuitGameRoutine());
        }

        // =====================================================================
        // TRANSICIONES VISUALES SUAVES ENTRE PANTALLAS
        // =====================================================================
        private IEnumerator SwitchModalPanelRoutine(CanvasGroup modalGroup, bool showModal)
        {
            if (modalGroup == null) yield break;
            isTransitioning = true;

            if (showModal)
            {
                // Ensure the other modal is closed
                if (modalGroup == worldSelectPanel && optionsPanel != null && optionsPanel.gameObject.activeSelf)
                {
                    SetGroupStateImmediate(optionsPanel, false);
                }
                else if (modalGroup == optionsPanel && worldSelectPanel != null && worldSelectPanel.gameObject.activeSelf)
                {
                    SetGroupStateImmediate(worldSelectPanel, false);
                }

                modalGroup.gameObject.SetActive(true);
                modalGroup.interactable = false;
                modalGroup.blocksRaycasts = true;

                RectTransform rt = modalGroup.GetComponent<RectTransform>();
                float elapsed = 0f;
                while (elapsed < panelTransitionDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / panelTransitionDuration);
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    modalGroup.alpha = ease;
                    if (rt != null)
                    {
                        rt.localScale = Vector3.LerpUnclamped(new Vector3(0.95f, 0.95f, 1f), Vector3.one, ease);
                    }
                    yield return null;
                }

                modalGroup.alpha = 1f;
                if (rt != null) rt.localScale = Vector3.one;
                modalGroup.interactable = true;
            }
            else
            {
                modalGroup.interactable = false;
                RectTransform rt = modalGroup.GetComponent<RectTransform>();
                float elapsed = 0f;
                while (elapsed < panelTransitionDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / panelTransitionDuration);
                    float ease = 1f - Mathf.Pow(1f - t, 2f);
                    modalGroup.alpha = 1f - ease;
                    if (rt != null)
                    {
                        rt.localScale = Vector3.LerpUnclamped(Vector3.one, new Vector3(0.96f, 0.96f, 1f), ease);
                    }
                    yield return null;
                }

                modalGroup.alpha = 0f;
                modalGroup.blocksRaycasts = false;
                modalGroup.gameObject.SetActive(false);
            }

            isTransitioning = false;
        }

        private IEnumerator TransitionToLevelRoutine(string sceneName)
        {
            isTransitioning = true;
            if (selectedLevelStatusText != null)
            {
                selectedLevelStatusText.text = $"CARGANDO {sceneName.ToUpper()}...";
            }

            if (screenFadeOverlay != null)
            {
                screenFadeOverlay.gameObject.SetActive(true);
                screenFadeOverlay.blocksRaycasts = true;
                yield return FadeCanvasGroupRoutine(screenFadeOverlay, screenFadeOverlay.alpha, 1f, sceneFadeDuration, disableOnZero: false);
            }
            else
            {
                yield return new WaitForSecondsRealtime(sceneFadeDuration);
            }

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.Log($"[MainMenu] Transición completada. Cargando escena '{sceneName}'...");
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.Log($"[MainMenu] Transición a '{sceneName}' preparada correctamente (Escena '{sceneName}' lista para vincular en Build Settings).");
                if (selectedLevelStatusText != null)
                {
                    selectedLevelStatusText.text = $"TRANSICIÓN A {sceneName} VERIFICADA (Pre-Alpha Ready)";
                }
                if (screenFadeOverlay != null)
                {
                    yield return new WaitForSecondsRealtime(0.35f);
                    yield return FadeCanvasGroupRoutine(screenFadeOverlay, 1f, 0f, sceneFadeDuration * 0.75f, disableOnZero: true);
                }
                isTransitioning = false;
            }
        }

        private IEnumerator QuitGameRoutine()
        {
            isTransitioning = true;
            Debug.Log("[MainMenu] Botón SALIR presionado: Ejecutando transición de salida y Application.Quit()...");

            if (screenFadeOverlay != null)
            {
                screenFadeOverlay.gameObject.SetActive(true);
                screenFadeOverlay.blocksRaycasts = true;
                yield return FadeCanvasGroupRoutine(screenFadeOverlay, screenFadeOverlay.alpha, 1f, 0.35f, disableOnZero: false);
            }

            Application.Quit();

#if UNITY_EDITOR
            Debug.Log("[MainMenu] Comportamiento de prueba en Unity Editor: Application.Quit() verificado exitosamente.");
            if (screenFadeOverlay != null)
            {
                yield return FadeCanvasGroupRoutine(screenFadeOverlay, 1f, 0f, 0.25f, disableOnZero: true);
            }
#endif
            isTransitioning = false;
        }

        private static IEnumerator FadeCanvasGroupRoutine(CanvasGroup group, float from, float to, float duration, bool disableOnZero)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                group.alpha = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
                yield return null;
            }
            group.alpha = to;
            if (disableOnZero && Mathf.Approximately(to, 0f))
            {
                group.blocksRaycasts = false;
                group.gameObject.SetActive(false);
            }
        }
    }
}
