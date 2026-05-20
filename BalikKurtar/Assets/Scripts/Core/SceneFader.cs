using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;
using BalikKurtar.Data;

namespace BalikKurtar.Core
{
    /// <summary>
    /// Sahneler arası geçişlerde profesyonel fade efekti sağlar.
    /// DontDestroyOnLoad singleton — tüm sahnelerde çalışır.
    /// Kendi Canvas ve Image'ını runtime'da oluşturur, prefab gerekmez.
    /// 
    /// Kullanım:
    ///   SceneFader.Instance.FadeToScene("SahneName");
    ///   SceneFader.Instance.FadeIn();
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [Header("Fade Ayarları")]
        [Tooltip("Fade süresi (saniye)")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("Fade rengi")]
        [SerializeField] private Color fadeColor = Color.black;

        [Header("Gelişmiş")]
        [Tooltip("Sahne yükleme sırasında loading göstergesi gösterilsin mi?")]
        [SerializeField] private bool showLoadingIndicator = false;

        [Header("Logo Ayarları")]
        [Tooltip("Ekranın ortasında belirecek geçiş logosu (örn: Keşfet Öğren Temizle yazısı)")]
        [SerializeField] private Sprite loadingLogo;

        [Tooltip("Logonun ekrandaki boyutu")]
        [SerializeField] private Vector2 logoSize = new Vector2(800, 400);

        [Tooltip("Logo hafifçe büyüyüp küçülsün mü (Nefes Alma efekti)?")]
        [SerializeField] private bool pulseLogo = true;

        [Header("İpuçları Ayarları")]
        [Tooltip("Sahne geçişlerinde gösterilecek rastgele ipuçları verisi")]
        [SerializeField] private LoadingTipsData loadingTips;

        [Header("Metin Ayarları")]
        [Tooltip("İpucu metninin yazı tipi (Boş bırakılırsa varsayılan TMP yazı tipi kullanılır)")]
        [SerializeField] private TMP_FontAsset tipTextFont;

        [Tooltip("İpucu metninin yazı boyutu")]
        [SerializeField] private float tipTextSize = 36f;

        [Tooltip("İpucu metninin rengi")]
        [SerializeField] private Color tipTextColor = Color.white;

        [Tooltip("Metnin hizalama seçeneği")]
        [SerializeField] private TextAlignmentOptions tipTextAlignment = TextAlignmentOptions.Center;

        [Tooltip("Yazının ekrandaki dikey konumu (Y min - max aralığı)")]
        [SerializeField] private Vector2 tipTextVerticalPosition = new Vector2(0.2f, 0.4f);

        // ==================== DURUM ====================

        private Canvas fadeCanvas;
        private Image fadeImage;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI tipText;
        private bool isFading;

        /// <summary>Fade işlemi devam ediyor mu?</summary>
        public bool IsFading => isFading;

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            // Singleton — sahneler arası korunur
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateFadeUI();

            // Sahne yüklendiğinde otomatik fade-in
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (Instance == this)
                Instance = null;
        }

        // ==================== UI OLUŞTURMA ====================

        /// <summary>
        /// Runtime'da fade Canvas ve Image'ı oluşturur.
        /// En üst sorting order ile her şeyin üstünde görünür.
        /// </summary>
        private void CreateFadeUI()
        {
            // Canvas
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);
            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // Her şeyin üstünde

            // Canvas Scaler
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Graphic Raycaster (input bloklama için)
            canvasObj.AddComponent<GraphicRaycaster>();

            // CanvasGroup (alpha kontrolü)
            canvasGroup = canvasObj.AddComponent<CanvasGroup>();

            // Fade Image (tam ekran)
            GameObject imgObj = new GameObject("FadeOverlay");
            imgObj.transform.SetParent(canvasObj.transform, false);
            fadeImage = imgObj.AddComponent<Image>();
            fadeImage.color = fadeColor;
            fadeImage.raycastTarget = true;

            // Tam ekranı kaplasın
            RectTransform rt = fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // --- LOGO (Eğer Atandıysa) ---
            if (loadingLogo != null)
            {
                GameObject logoObj = new GameObject("FadeLogo");
                logoObj.transform.SetParent(canvasObj.transform, false);
                
                Image logoImage = logoObj.AddComponent<Image>();
                logoImage.sprite = loadingLogo;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false; 

                RectTransform logoRt = logoImage.rectTransform;
                logoRt.anchorMin = new Vector2(0.5f, 0.5f);
                logoRt.anchorMax = new Vector2(0.5f, 0.5f);
                logoRt.pivot = new Vector2(0.5f, 0.5f);
                logoRt.anchoredPosition = Vector2.zero;
                logoRt.sizeDelta = logoSize;

                if (pulseLogo)
                {
                    logoRt.DOScale(1.05f, 1f)
                          .SetEase(Ease.InOutSine)
                          .SetLoops(-1, LoopType.Yoyo)
                          .SetUpdate(true); // Yükleme sırasında animasyonun donmaması için
                }
            }

            // --- İPUCU METNİ (Eğer Atandıysa) ---
            if (loadingTips != null)
            {
                GameObject txtObj = new GameObject("FadeTipText");
                txtObj.transform.SetParent(canvasObj.transform, false);
                
                tipText = txtObj.AddComponent<TextMeshProUGUI>();
                
                if (tipTextFont != null)
                {
                    tipText.font = tipTextFont;
                }
                
                tipText.alignment = tipTextAlignment;
                tipText.color = tipTextColor;
                tipText.fontSize = tipTextSize;
                tipText.enableWordWrapping = true;
                tipText.raycastTarget = false;
                
                RectTransform txtRt = tipText.rectTransform;
                txtRt.anchorMin = new Vector2(0.1f, tipTextVerticalPosition.x); // Ekranın alt kısımlarına hizala
                txtRt.anchorMax = new Vector2(0.9f, tipTextVerticalPosition.y);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            // Başlangıçta tamamen opak (ilk sahne açılışında fade-in yapılacak)
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // ==================== PUBLIC API ====================

        /// <summary>
        /// Fade out yaparak belirtilen sahneye geçiş yapar.
        /// Yeni sahne yüklendikten sonra otomatik fade in yapılır.
        /// </summary>
        /// <param name="sceneName">Yüklenecek sahnenin adı</param>
        /// <param name="customDuration">Özel fade süresi (-1 ise default kullanılır)</param>
        public void FadeToScene(string sceneName, float customDuration = -1f)
        {
            if (isFading)
            {
                Debug.LogWarning("[SceneFader] Zaten bir fade işlemi devam ediyor!");
                return;
            }

            float duration = customDuration > 0 ? customDuration : fadeDuration;
            StartCoroutine(FadeToSceneCoroutine(sceneName, duration));
        }

        /// <summary>
        /// Karanlıktan aydınlığa geçiş (sahne açıldıktan sonra).
        /// </summary>
        public void FadeIn(float customDuration = -1f, Action onComplete = null)
        {
            if (isFading) return;

            float duration = customDuration > 0 ? customDuration : fadeDuration;
            StartCoroutine(FadeCoroutine(1f, 0f, duration, onComplete));
        }

        /// <summary>
        /// Aydınlıktan karanlığa geçiş.
        /// </summary>
        public void FadeOut(float customDuration = -1f, Action onComplete = null)
        {
            if (isFading) return;

            float duration = customDuration > 0 ? customDuration : fadeDuration;
            StartCoroutine(FadeCoroutine(0f, 1f, duration, onComplete));
        }

        /// <summary>
        /// Anlık olarak fade'i sıfırlar (görünmez yapar).
        /// </summary>
        public void ClearFade()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            isFading = false;
        }

        /// <summary>
        /// Anlık olarak ekranı karartır.
        /// </summary>
        public void SetBlack()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            isFading = false;
        }

        // ==================== COROUTINE'LER ====================

        private IEnumerator FadeToSceneCoroutine(string sceneName, float duration)
        {
            if (tipText != null && loadingTips != null)
            {
                tipText.text = loadingTips.GetRandomTip();
            }

            isFading = true;

            // 1. Fade out (ekranı karart)
            yield return StartCoroutine(FadeAlpha(0f, 1f, duration));

            // 2. Sahneyi yükle
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            if (asyncLoad == null)
            {
                Debug.LogError($"[SceneFader] Sahne yüklenemedi: {sceneName}");
                isFading = false;
                yield break;
            }

            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Not: Fade-in OnSceneLoaded event'inde tetiklenir
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration, Action onComplete)
        {
            isFading = true;
            yield return StartCoroutine(FadeAlpha(from, to, duration));
            isFading = false;
            onComplete?.Invoke();
        }

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            canvasGroup.alpha = from;
            canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // TimeScale'den bağımsız
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;

            // Fade tamamlandıysa ve tamamen şeffafsa raycasting'i kapat
            if (Mathf.Approximately(to, 0f))
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        // ==================== EVENT HANDLER ====================

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Her sahne yüklendiğinde otomatik fade-in yap
            if (canvasGroup.alpha > 0.01f)
            {
                StartCoroutine(FadeInAfterSceneLoad());
            }
        }

        private IEnumerator FadeInAfterSceneLoad()
        {
            // Bir frame bekle — sahnenin Awake/Start'ları tamamlansın
            yield return null;

            isFading = true;
            yield return StartCoroutine(FadeAlpha(1f, 0f, fadeDuration));
            isFading = false;
        }
    }
}
