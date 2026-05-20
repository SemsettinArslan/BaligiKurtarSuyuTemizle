using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;
using BalikKurtar.Core;
using BalikKurtar.Data;

namespace BalikKurtar.SuTemizligi
{
    /// <summary>
    /// Su Temizligi mini oyununun tum UI bilesenlerini yonetir.
    /// Ilerleme bari, skor, durum metni ve seviye tamamlama paneli.
    /// Screen Space - Overlay Canvas uzerinde calisir.
    /// </summary>
    public class CleaningUI : MonoBehaviour
    {
        [Header("Ilerleme Bari")]
        [Tooltip("Dairesel veya lineer dolum gorseli")]
        [SerializeField] private Image progressBarFill;

        [Tooltip("Ilerleme barinin parent konteyneri")]
        [SerializeField] private RectTransform progressBarContainer;

        [Tooltip("Ilerleme bari offset (cop pozisyonunun ustunde)")]
        [SerializeField] private Vector2 progressBarOffset = new Vector2(0, 80f);



        [Header("Kirlilik Çubuğu")]
        [Tooltip("Kirlilik yüzdesini gösteren dolum görseli (Image.fillAmount)")]
        [SerializeField] private Image pollutionBarFill;

        [Tooltip("Kirlilik yüzde metni (ör: %75)")]
        [SerializeField] private TextMeshProUGUI pollutionPercentText;

        [Tooltip("Kirlilik çubuğu animasyon süresi (saniye)")]
        [SerializeField] private float pollutionAnimDuration = 0.5f;

        [Header("Tamamlama Paneli")]
        [SerializeField] private CanvasGroup completionPanel;
        [SerializeField] private RectTransform completionPanelRect;
        [SerializeField] private TextMeshProUGUI completionTitle;
        [SerializeField] private TextMeshProUGUI completionMessage;
        [SerializeField] private TextMeshProUGUI completionTimeText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button nextButton;

        [Tooltip("Rastgele tebrik mesajlarını barındıran veri kaynağı.")]
        [SerializeField] private CompletionMessagesData completionMessages;

        [Header("Sahne Gecisi")]
        [Tooltip("Sonraki sahnenin adi")]
        [SerializeField] private string nextSceneName = "";

        // ==================== DURUM ====================

        private Camera mainCamera;
        private TrashItem currentTrashTarget;
        private bool progressBarVisible;
        private Sequence completionSequence;
        private Tween pollutionTween;

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            mainCamera = Camera.main;

            // Ilerleme barini gizle
            if (progressBarContainer != null)
                progressBarContainer.gameObject.SetActive(false);

            // Tamamlama panelini gizle
            if (completionPanel != null)
            {
                completionPanel.alpha = 0f;
                completionPanel.gameObject.SetActive(false);
            }

            // Buton event'leri
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);
        }

        private void OnEnable()
        {
            if (WaterCleaningManager.Instance != null)
            {
                WaterCleaningManager.Instance.OnTrashCleaned += OnTrashCleaned;
                WaterCleaningManager.Instance.OnLevelComplete += OnLevelComplete;
            }
        }

        private void OnDisable()
        {
            if (WaterCleaningManager.Instance != null)
            {
                WaterCleaningManager.Instance.OnTrashCleaned -= OnTrashCleaned;
                WaterCleaningManager.Instance.OnLevelComplete -= OnLevelComplete;
            }
        }

        private void Start()
        {
            // Event'lere tekrar abone ol (Start, OnEnable'dan sonra gelir)
            if (WaterCleaningManager.Instance != null)
            {
                WaterCleaningManager.Instance.OnTrashCleaned -= OnTrashCleaned;
                WaterCleaningManager.Instance.OnLevelComplete -= OnLevelComplete;
                WaterCleaningManager.Instance.OnTrashCleaned += OnTrashCleaned;
                WaterCleaningManager.Instance.OnLevelComplete += OnLevelComplete;

                UpdatePollutionBar(0, WaterCleaningManager.Instance.TotalTrashCount, false);
            }
        }

        private void LateUpdate()
        {
            // Ilerleme barini copu takip ettir
            if (progressBarVisible && currentTrashTarget != null && progressBarContainer != null)
            {
                UpdateProgressBarPosition();
            }
        }

        private void OnDestroy()
        {
            completionSequence?.Kill();
            pollutionTween?.Kill();
        }

        // ==================== ILERLEME BARI ====================

        /// <summary>Ilerleme barini gosterir ve belirtilen copu takip eder.</summary>
        public void ShowProgressBar(TrashItem trash)
        {
            if (progressBarContainer == null || progressBarFill == null) return;

            currentTrashTarget = trash;
            progressBarVisible = true;

            progressBarFill.fillAmount = 0f;
            progressBarContainer.gameObject.SetActive(true);

            // Animasyonla gorun
            progressBarContainer.localScale = Vector3.zero;
            progressBarContainer.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);

            // Progress event'ine abone ol
            trash.OnProgressChanged += UpdateProgressBarFill;
        }

        /// <summary>Ilerleme barini gizler.</summary>
        public void HideProgressBar()
        {
            if (progressBarContainer == null) return;

            if (currentTrashTarget != null)
            {
                currentTrashTarget.OnProgressChanged -= UpdateProgressBarFill;
            }

            progressBarVisible = false;
            currentTrashTarget = null;

            progressBarContainer.DOScale(Vector3.zero, 0.15f)
                .SetEase(Ease.InBack)
                .OnComplete(() => progressBarContainer.gameObject.SetActive(false));
        }

        private void UpdateProgressBarFill(float progress)
        {
            if (progressBarFill == null) return;
            progressBarFill.fillAmount = progress;

            // Renk gecisi: mavi -> yesil
            progressBarFill.color = Color.Lerp(
                new Color(0.2f, 0.6f, 1f, 1f),  // Mavi
                new Color(0.2f, 0.9f, 0.3f, 1f), // Yesil
                progress);
        }

        private void UpdateProgressBarPosition()
        {
            if (mainCamera == null || currentTrashTarget == null) return;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(currentTrashTarget.transform.position);

            // Kameranin arkasindaysa gizle
            if (screenPos.z < 0)
            {
                progressBarContainer.gameObject.SetActive(false);
                return;
            }

            progressBarContainer.position = (Vector2)screenPos + progressBarOffset;
        }

        // ==================== EVENT HANDLERS ====================

        private void OnTrashCleaned(int cleaned, int remaining)
        {
            UpdatePollutionBar(cleaned, remaining, true);
        }

        private void OnLevelComplete()
        {
            ShowCompletionPanel();
        }

        // ==================== KİRLİLİK ÇUBUĞU ====================

        /// <summary>
        /// Kirlilik yüzdesini günceller.
        /// Başlangıçta %100 (tüm çöpler var), temizledikçe azalır.
        /// </summary>
        private void UpdatePollutionBar(int cleaned, int remaining, bool animate)
        {
            int total = cleaned + remaining;
            if (total <= 0) return;

            float pollutionRatio = (float)remaining / total;
            int percent = Mathf.RoundToInt(pollutionRatio * 100f);

            // Yüzde metnini güncelle
            if (pollutionPercentText != null)
            {
                pollutionPercentText.text = $"%{percent}";

                if (animate && cleaned > 0)
                {
                    pollutionPercentText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
                }
            }

            // Fill amount güncelle
            if (pollutionBarFill != null)
            {
                pollutionTween?.Kill();

                if (animate)
                {
                    pollutionTween = pollutionBarFill.DOFillAmount(pollutionRatio, pollutionAnimDuration)
                        .SetEase(Ease.OutQuad);
                }
                else
                {
                    pollutionBarFill.fillAmount = pollutionRatio;
                }

                // Renk geçişi: kırmızı (kirli) -> turuncu -> yeşil (temiz)
                Color pollutionColor = Color.Lerp(
                    new Color(0.2f, 0.85f, 0.3f, 1f),  // Yeşil (temiz)
                    new Color(0.9f, 0.2f, 0.2f, 1f),    // Kırmızı (kirli)
                    pollutionRatio);
                pollutionBarFill.color = pollutionColor;
            }
        }

        // ==================== TAMAMLAMA PANELİ ====================

        private void ShowCompletionPanel()
        {
            if (completionPanel == null) return;

            completionPanel.gameObject.SetActive(true);

            var mgr = WaterCleaningManager.Instance;
            float time = mgr != null ? mgr.ElapsedTime : 0f;

            // Title'ı değiştirmeyip kullanıcının belirlediği haliyle bırakıyoruz.

            if (completionMessage != null)
            {
                if (completionMessages != null)
                {
                    completionMessage.text = completionMessages.GetRandomMessage();
                }
                else
                {
                    completionMessage.text = "Tüm çöpleri temizledin!\nSu artık tertemiz!";
                }
            }

            if (completionTimeText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                completionTimeText.text = $"Toplam Süre {minutes:00}:{seconds:00}";
            }

            // Animasyon
            completionSequence?.Kill();
            completionSequence = DOTween.Sequence();
            completionPanel.alpha = 0f;

            if (completionPanelRect != null)
            {
                completionPanelRect.localScale = Vector3.one * 0.8f;
                completionSequence.Append(completionPanel.DOFade(1f, 0.5f));
                completionSequence.Join(
                    completionPanelRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));
            }
            else
            {
                completionSequence.Append(completionPanel.DOFade(1f, 0.5f));
            }
        }

        // ==================== BUTON HANDLER'LARI ====================

        private void OnRestartClicked()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (SceneFader.Instance != null)
                SceneFader.Instance.FadeToScene(currentScene);
            else
                SceneManager.LoadScene(currentScene);
        }

        private void OnNextClicked()
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                if (SceneFader.Instance != null)
                    SceneFader.Instance.FadeToScene(nextSceneName);
                else
                    SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("[CleaningUI] Sonraki sahne adi atanmamis!");
            }
        }
    }
}
