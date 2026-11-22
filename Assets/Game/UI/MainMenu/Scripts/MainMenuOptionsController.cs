using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    /// <summary>
    /// Manages the Options Panel in the Main Menu:
    /// - Master Volume
    /// - Music Volume
    /// - SFX Volume
    /// - Screen Resolution (1920x1080 default + adaptable resolutions)
    /// - Fullscreen Toggle
    /// Persists settings via PlayerPrefs and applies them live.
    /// </summary>
    public class MainMenuOptionsController : MonoBehaviour
    {
        [Header("Volume Sliders & Value Labels")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeValueText;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Text musicVolumeValueText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text sfxVolumeValueText;

        [Header("Display Controls")]
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        private readonly List<Vector2Int> supportedResolutions = new List<Vector2Int>
        {
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(1600, 900),
            new Vector2Int(1366, 768),
            new Vector2Int(1280, 720)
        };

        private const string PrefResIndex = "FA_Menu_ResIndex";
        private const string PrefFullscreen = "FA_Menu_Fullscreen";
        private bool isInitializing;

        private void OnEnable()
        {
            InitializeControls();
        }

        public void InitializeControls()
        {
            isInitializing = true;

            float master = MainMenuAudioController.Instance != null ? MainMenuAudioController.Instance.MasterVolume : 0.85f;
            float music = MainMenuAudioController.Instance != null ? MainMenuAudioController.Instance.MusicVolume : 0.75f;
            float sfx = MainMenuAudioController.Instance != null ? MainMenuAudioController.Instance.SfxVolume : 0.85f;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(master);
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            UpdatePercentLabel(masterVolumeValueText, master);

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(music);
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            UpdatePercentLabel(musicVolumeValueText, music);

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfx);
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }
            UpdatePercentLabel(sfxVolumeValueText, sfx);

            // Populate resolutions dropdown
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < supportedResolutions.Count; i++)
                {
                    Vector2Int res = supportedResolutions[i];
                    options.Add($"{res.x} x {res.y} (16:9)");
                }
                resolutionDropdown.AddOptions(options);

                int savedIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefResIndex, 0), 0, supportedResolutions.Count - 1);
                resolutionDropdown.SetValueWithoutNotify(savedIndex);
                resolutionDropdown.RefreshShownValue();

                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            if (fullscreenToggle != null)
            {
                bool isFull = PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
                fullscreenToggle.SetIsOnWithoutNotify(isFull);
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            isInitializing = false;
        }

        public void OnMasterVolumeChanged(float value)
        {
            UpdatePercentLabel(masterVolumeValueText, value);
            if (!isInitializing && MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.SetMasterVolume(value);
            }
        }

        public void OnMusicVolumeChanged(float value)
        {
            UpdatePercentLabel(musicVolumeValueText, value);
            if (!isInitializing && MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.SetMusicVolume(value);
            }
        }

        public void OnSfxVolumeChanged(float value)
        {
            UpdatePercentLabel(sfxVolumeValueText, value);
            if (!isInitializing && MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.SetSfxVolume(value);
            }
        }

        public void OnResolutionChanged(int index)
        {
            if (index < 0 || index >= supportedResolutions.Count) return;

            PlayerPrefs.SetInt(PrefResIndex, index);
            Vector2Int res = supportedResolutions[index];
            bool isFull = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
            Screen.SetResolution(res.x, res.y, isFull);

            if (!isInitializing && MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayClick();
            }
        }

        public void OnFullscreenChanged(bool isFullscreen)
        {
            PlayerPrefs.SetInt(PrefFullscreen, isFullscreen ? 1 : 0);
            Screen.fullScreen = isFullscreen;

            if (!isInitializing && MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayClick();
            }
        }

        private static void UpdatePercentLabel(Text label, float normalizedValue)
        {
            if (label != null)
            {
                int pct = Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 100f);
                label.text = $"{pct}%";
            }
        }
    }
}
