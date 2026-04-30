using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using TMPro;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Quiz sonuç ekranı. Editörden ayarlanmalıdır.
    /// </summary>
    public class QuizResultPanel : MonoBehaviour
    {
        [Header("Ana Referanslar")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;

        [Header("UI Elemanları")]
        [SerializeField] private TextMeshProUGUI scoreValueText;
        [SerializeField] private TextMeshProUGUI correctText;
        [SerializeField] private TextMeshProUGUI wrongText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button backButton;
        
        [Header("Sahne Geçişi")]
        [Tooltip("Quiz bittikten sonra geçilecek sahnenin tam adı (Örn: Level2)")]
        [SerializeField] private string nextSceneName = "NextScene";

        private bool isVisible = false;
        private Sequence currentAnimation;
        private Vector3 initialScale = Vector3.one;

        private void Awake()
        {
            if (panelRect != null) initialScale = panelRect.localScale;
            
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            HideImmediate();
        }

        /// <summary>Sonuç panelini gösterir.</summary>
        public void Show(int score, int correct, int wrong)
        {
            gameObject.SetActive(true);
            isVisible = true;

            int total = correct + wrong;
            float percentage = total > 0 ? (float)correct / total * 100f : 0f;

            if (scoreValueText != null) scoreValueText.text = score.ToString();
            if (correctText != null) correctText.text = $"Doğru: {correct}";
            if (wrongText != null) wrongText.text = $"Yanlış: {wrong}";

            if (messageText != null)
            {
                // Performansa göre mesaj
                if (percentage >= 90)
                {
                    messageText.text = "Mükemmel! Deniz uzmansın!";
                    messageText.color = new Color(1f, 0.84f, 0f);
                }
                else if (percentage >= 70)
                {
                    messageText.text = "Çok iyi! Balıkları iyi tanıyorsun!";
                    messageText.color = new Color(0.3f, 0.85f, 0.4f);
                }
                else if (percentage >= 50)
                {
                    messageText.text = "Fena değil! Biraz daha çalış!";
                    messageText.color = new Color(0.2f, 0.7f, 0.9f);
                }
                else
                {
                    messageText.text = "Daha fazla kart okut ve öğren!";
                    messageText.color = new Color(1f, 0.5f, 0.3f);
                }
            }

            // Animasyon
            if (canvasGroup != null && panelRect != null)
            {
                currentAnimation?.Kill();
                canvasGroup.alpha = 0f;
                currentAnimation = DOTween.Sequence();
                currentAnimation.Append(canvasGroup.DOFade(1f, 0.5f));
                panelRect.localScale = initialScale * 0.85f;
                currentAnimation.Join(panelRect.DOScale(initialScale, 0.5f).SetEase(Ease.OutBack));

                if (scoreValueText != null)
                {
                    int targetScore = score;
                    scoreValueText.text = "0";
                    DOTween.To(() => 0, x => scoreValueText.text = x.ToString(), targetScore, 1f)
                        .SetDelay(0.5f)
                        .SetEase(Ease.OutCubic);
                }
            }
        }

        /// <summary>Sonuç panelini gizler.</summary>
        public void Hide()
        {
            if (!isVisible) return;
            isVisible = false;

            if (canvasGroup != null)
            {
                currentAnimation?.Kill();
                currentAnimation = DOTween.Sequence();
                currentAnimation.Append(canvasGroup.DOFade(0f, 0.3f));
                currentAnimation.OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnPlayAgainClicked()
        {
            Hide();

            // Quiz'i yeniden başlat
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.StartQuiz();
            }
        }

        private void OnBackClicked()
        {
            Hide();
            // Yeni sahneye geçiş yap
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"[QuizResultPanel] Sahne yükleniyor: {nextSceneName}");
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("[QuizResultPanel] Geçiş yapılacak sahne adı (nextSceneName) atanmamış!");
            }
        }

        private void HideImmediate()
        {
            isVisible = false;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            currentAnimation?.Kill();
        }
    }
}
