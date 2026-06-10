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
        [Tooltip("Arka plan müzikleri için (Loop olarak çalar).")]
        [SerializeField] private AudioSource bgmSource;

        [Header("Default UI Sounds")]
        public AudioClip defaultHoverSound;
        public AudioClip defaultClickSound;

        [Header("Default Music")]
        [Tooltip("Oyun açıldığında otomatik çalacak varsayılan arka plan müziği (Yedek).")]
        [SerializeField] private AudioClip defaultBackgroundMusic;

        [Header("Sahne Bazlı Müzik Ayarları")]
        [Tooltip("Ana Menü sahnesinin adı.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Tooltip("AR (2. Oyun) sahnesinin adı.")]
        [SerializeField] private string arSceneName = "SampleScene";

        [Tooltip("Su Temizliği (3. Oyun) sahnesinin adı.")]
        [SerializeField] private string waterCleaningSceneName = "WaterCleaningScene";

        [Tooltip("Ana Menüde çalacak müzik.")]
        [SerializeField] private AudioClip mainMenuMusic;

        [Tooltip("Su Temizliği sahnesinde çalacak müzik.")]
        [SerializeField] private AudioClip waterCleaningMusic;

        // PlayerPrefs Keys
        private const string SFX_VOL_KEY = "SFXVolume";
        private const string VOICE_VOL_KEY = "VoiceVolume";
        private const string MUSIC_VOL_KEY = "MusicVolume";

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
            
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            LoadAudioSettings();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            // İlk açılışta sahne yükleme eventi tetiklenmez, bu yüzden başlangıç sahnesini manuel kontrol ediyoruz.
            HandleSceneBGM(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            HandleSceneBGM(scene.name);
        }

        /// <summary>
        /// Sahnelerin isimlerine göre müzik durumunu yönetir.
        /// </summary>
        private void HandleSceneBGM(string sceneName)
        {
            if (sceneName == mainMenuSceneName)
            {
                if (mainMenuMusic != null)
                {
                    PlayMusic(mainMenuMusic);
                }
                else if (defaultBackgroundMusic != null)
                {
                    PlayMusic(defaultBackgroundMusic);
                }
            }
            else if (sceneName == arSceneName)
            {
                // AR (2. Sahne) sahnesinde müzik çalmasın
                StopMusic();
            }
            else if (sceneName == waterCleaningSceneName)
            {
                if (waterCleaningMusic != null)
                {
                    PlayMusic(waterCleaningMusic);
                }
            }
        }

        // ==================== SES ÇALMA METOTLARI ====================

        /// <summary>
        /// Arka plan müziği çalar.
        /// </summary>
        public void PlayMusic(AudioClip musicClip, bool loop = true)
        {
            if (musicClip == null || bgmSource == null) return;

            // Zaten aynı müzik çalıyorsa tekrar başlatma
            if (bgmSource.isPlaying && bgmSource.clip == musicClip) return;

            bgmSource.Stop();
            bgmSource.clip = musicClip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        /// <summary>
        /// Çalan arka plan müziğini durdurur.
        /// </summary>
        public void StopMusic()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        /// <summary>
        /// Çalan müziği duraklatır.
        /// </summary>
        public void PauseMusic()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
        }

        /// <summary>
        /// Duraklatılan müziği devam ettirir.
        /// </summary>
        public void UnpauseMusic()
        {
            if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
            {
                bgmSource.UnPause();
            }
        }

        public void PlayHover()
        {
            if (defaultHoverSound != null && sfxSource != null)
                sfxSource.PlayOneShot(defaultHoverSound);
        }

        public void PlayClick()
        {
            if (defaultClickSound != null && sfxSource != null)
                sfxSource.PlayOneShot(defaultClickSound);
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }

        public void PlayVoice(AudioClip clip)
        {
            if (clip != null && voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = clip;
                voiceSource.Play();
            }
        }

        public void StopVoice()
        {
            if (voiceSource != null)
            {
                voiceSource.Stop();
            }
        }

        // ==================== SES AYARLARI ====================

        public void SetMusicVolume(float volume)
        {
            if (bgmSource != null) bgmSource.volume = volume;
            PlayerPrefs.SetFloat(MUSIC_VOL_KEY, volume);
            PlayerPrefs.Save();
        }

        public void SetSfxVolume(float volume)
        {
            if (sfxSource != null) sfxSource.volume = volume;
            PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
            PlayerPrefs.Save();
        }

        public void SetVoiceVolume(float volume)
        {
            if (voiceSource != null) voiceSource.volume = volume;
            PlayerPrefs.SetFloat(VOICE_VOL_KEY, volume);
            PlayerPrefs.Save();
        }

        public float GetMusicVolume() => PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.7f);
        public float GetSfxVolume() => PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
        public float GetVoiceVolume() => PlayerPrefs.GetFloat(VOICE_VOL_KEY, 1f);

        private void LoadAudioSettings()
        {
            if (bgmSource != null) bgmSource.volume = GetMusicVolume();
            if (sfxSource != null) sfxSource.volume = GetSfxVolume();
            if (voiceSource != null) voiceSource.volume = GetVoiceVolume();
        }
    }
}
