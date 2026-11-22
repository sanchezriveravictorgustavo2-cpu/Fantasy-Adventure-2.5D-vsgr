using UnityEngine;

namespace Game.UI.MainMenu
{
    /// <summary>
    /// Centralizes background music (BGM) and UI sound effects (hover, click, back)
    /// for the Frutiger Aero Main Menu, with independent Master, Music, and SFX volume channels.
    /// </summary>
    public class MainMenuAudioController : MonoBehaviour
    {
        public static MainMenuAudioController Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip backClip;

        private float masterVolume = 0.85f;
        private float musicVolume = 0.75f;
        private float sfxVolume = 0.85f;

        private const string PrefMasterVol = "FA_Menu_MasterVol";
        private const string PrefMusicVol = "FA_Menu_MusicVol";
        private const string PrefSfxVol = "FA_Menu_SfxVol";

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            masterVolume = PlayerPrefs.GetFloat(PrefMasterVol, 0.85f);
            musicVolume = PlayerPrefs.GetFloat(PrefMusicVol, 0.75f);
            sfxVolume = PlayerPrefs.GetFloat(PrefSfxVol, 0.85f);

            ApplyVolumes();
        }

        private void Start()
        {
            if (musicSource != null && bgmClip != null && !musicSource.isPlaying)
            {
                musicSource.clip = bgmClip;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefMasterVol, masterVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefMusicVol, musicVolume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefSfxVol, sfxVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            AudioListener.volume = masterVolume;
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        public void PlayHover()
        {
            if (sfxSource != null && hoverClip != null)
            {
                sfxSource.PlayOneShot(hoverClip, sfxVolume * 0.75f);
            }
        }

        public void PlayClick()
        {
            if (sfxSource != null && clickClip != null)
            {
                sfxSource.PlayOneShot(clickClip, sfxVolume);
            }
        }

        public void PlayBack()
        {
            if (sfxSource != null && backClip != null)
            {
                sfxSource.PlayOneShot(backClip, sfxVolume * 0.9f);
            }
        }
    }
}
