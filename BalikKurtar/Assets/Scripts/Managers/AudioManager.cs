using UnityEngine;
using UnityEngine.Audio;

namespace BalikKurtar.Managers
{
    /// <summary>
    /// Proje genelindeki sesleri (SFX ve Voice) yönetir.
    /// DontDestroyOnLoad özelliğiyle tüm sahnelerde yaşar.
    /// Ses ayarlarını PlayerPrefs üzerinden kaydeder ve yükler.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("Buton tıklamaları, UI efektleri vb. kısa sesler için.")]
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("Balık bilgi seslendirmeleri gibi uzun sesler için.")]
        [SerializeField] private AudioSource voiceSource;

        [Header("Default UI Sounds")]
        public AudioClip defaultHoverSound;
        public AudioClip defaultClickSound;

        // PlayerPrefs Keys
        private const string SFX_VOL_KEY = "SFXVolume";
        private const string VOICE_VOL_KEY = "VoiceVolume";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Gerekli AudioSource'lar yoksa otomatik oluştur
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();
            if (voiceSource == null)
                voiceSource = gameObject.AddComponent<AudioSource>();

            LoadAudioSettings();
        }

        // ==================== SES ÇALMA METOTLARI ====================

        public void PlayHover()
        {
            if (defaultHoverSound != null)
                sfxSource.PlayOneShot(defaultHoverSound);
        }

        public void PlayClick()
        {
            if (defaultClickSound != null)
                sfxSource.PlayOneShot(defaultClickSound);
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null)
                sfxSource.PlayOneShot(clip);
        }

        public void PlayVoice(AudioClip clip)
        {
            if (clip != null)
            {
                voiceSource.Stop();
                voiceSource.clip = clip;
                voiceSource.Play();
            }
        }

        public void StopVoice()
        {
            voiceSource.Stop();
        }

        // ==================== SES AYARLARI ====================

        public void SetSfxVolume(float volume)
        {
            sfxSource.volume = volume;
            PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
            PlayerPrefs.Save();
        }

        public void SetVoiceVolume(float volume)
        {
            voiceSource.volume = volume;
            PlayerPrefs.SetFloat(VOICE_VOL_KEY, volume);
            PlayerPrefs.Save();
        }

        public float GetSfxVolume() => PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
        public float GetVoiceVolume() => PlayerPrefs.GetFloat(VOICE_VOL_KEY, 1f);

        private void LoadAudioSettings()
        {
            sfxSource.volume = GetSfxVolume();
            voiceSource.volume = GetVoiceVolume();
        }
    }
}
