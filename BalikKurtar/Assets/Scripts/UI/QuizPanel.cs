using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BalikKurtar.Managers;
using TMPro;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Quiz oyun ekranı. Editörden Canvas veya World Space olarak ayarlanmalıdır.
    /// </summary>
    public class QuizPanel : MonoBehaviour
    {
        [Header("Ana Referanslar")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;

        [Header("UI Elemanları")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        
        [Tooltip("4 adet seçenek butonu (sırasıyla)")]
        [SerializeField] private Button[] optionButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];
        [SerializeField] private Image[] optionImages = new Image[4];

        private bool isVisible = false;
        private bool waitingForNext = false;
        private int lastSelectedIndex = -1;
        private Sequence currentAnimation;
        private Vector3 initialScale = Vector3.one;

        // Renkler
        private readonly Color normalColor = new Color(0.15f, 0.25f, 0.4f, 1f);
        private readonly Color correctColor = new Color(0.2f, 0.7f, 0.3f, 1f);
        private readonly Color wrongColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        private readonly Color highlightCorrectColor = new Color(0.25f, 0.8f, 0.4f, 1f);

        private void Awake()
        {
            if (panelRect != null) initialScale = panelRect.localScale;
            
            // Buton event'lerini bağla
            for (int i = 0; i < 4; i++)
            {
                if (optionButtons[i] != null)
                {
                    int capturedIndex = i;
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(capturedIndex));
                }
            }
            HideImmediate();
        }

        private void OnEnable()
        {
            if (QuizManager.Instance != null)
            {
                QuizManager.Instance.OnQuestionReady += OnQuestionReady;
                QuizManager.Instance.OnAnswerResult += OnAnswerResult;
                QuizManager.Instance.OnQuizComplete += OnQuizComplete;
            }
        }

        private void OnDisable()
        {
            if (QuizManager.Instance != null)
            {
                QuizManager.Instance.OnQuestionReady -= OnQuestionReady;
                QuizManager.Instance.OnAnswerResult -= OnAnswerResult;
                QuizManager.Instance.OnQuizComplete -= OnQuizComplete;
            }
        }

        /// <summary>Quiz panelini gösterir ve quiz'i başlatır.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            isVisible = true;

            // Event'lere tekrar abone ol
            if (QuizManager.Instance != null)
            {
                QuizManager.Instance.OnQuestionReady -= OnQuestionReady;
                QuizManager.Instance.OnAnswerResult -= OnAnswerResult;
                QuizManager.Instance.OnQuizComplete -= OnQuizComplete;
                QuizManager.Instance.OnQuestionReady += OnQuestionReady;
                QuizManager.Instance.OnAnswerResult += OnAnswerResult;
                QuizManager.Instance.OnQuizComplete += OnQuizComplete;
            }

            if (canvasGroup != null && panelRect != null)
            {
                currentAnimation?.Kill();
                currentAnimation = DOTween.Sequence();
                currentAnimation.Append(canvasGroup.DOFade(1f, 0.4f).From(0f));
                panelRect.localScale = initialScale * 0.9f;
                currentAnimation.Join(panelRect.DOScale(initialScale, 0.4f).SetEase(Ease.OutBack));
            }
        }

        /// <summary>Quiz panelini gizler.</summary>
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

        // ==================== EVENT HANDLERS ====================

        private void OnQuestionReady(QuizQuestion question, int index, int total)
        {
            waitingForNext = false;

            if (questionText != null) questionText.text = question.questionText;
            if (progressText != null) progressText.text = $"Soru {index + 1}/{total}";
            if (scoreText != null) scoreText.text = $"Skor: {QuizManager.Instance.Score}";

            for (int i = 0; i < 4; i++)
            {
                if (optionButtons[i] == null) continue;

                if (i < question.allOptions.Count)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    if (optionTexts[i] != null) optionTexts[i].text = question.allOptions[i];
                    if (optionImages[i] != null) optionImages[i].color = normalColor;
                    optionButtons[i].interactable = true;

                    // Buton animasyonu
                    int delay = i;
                    optionButtons[i].transform.localScale = Vector3.zero;
                    optionButtons[i].transform.DOScale(1f, 0.3f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(delay * 0.08f);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnAnswerResult(bool isCorrect, string correctAnswer)
        {
            waitingForNext = true;

            for (int i = 0; i < 4; i++)
            {
                if (optionButtons[i] == null || !optionButtons[i].gameObject.activeSelf) continue;
                optionButtons[i].interactable = false;

                if (optionTexts[i] != null && optionTexts[i].text == correctAnswer)
                {
                    if (optionImages[i] != null) optionImages[i].DOColor(highlightCorrectColor, 0.3f);
                    optionButtons[i].transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
                }
            }

            if (!isCorrect && lastSelectedIndex >= 0 && lastSelectedIndex < 4)
            {
                if (optionButtons[lastSelectedIndex] != null && optionButtons[lastSelectedIndex].gameObject.activeSelf)
                {
                    if (optionImages[lastSelectedIndex] != null) optionImages[lastSelectedIndex].DOColor(wrongColor, 0.3f);
                    optionButtons[lastSelectedIndex].transform.DOShakePosition(0.3f, 5f, 20);
                }
            }

            if (scoreText != null) scoreText.text = $"Skor: {QuizManager.Instance.Score}";

            StartCoroutine(WaitAndNext(isCorrect ? 1.0f : 1.8f));
        }

        private IEnumerator WaitAndNext(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (waitingForNext)
            {
                QuizManager.Instance.NextQuestion();
            }
        }

        private void OnQuizComplete(int score, int correct, int wrong)
        {
            Hide();

            var gm = Core.GameManager.Instance;
            if (gm != null && gm.QuizResultPanel != null)
            {
                gm.QuizResultPanel.Show(score, correct, wrong);
            }
        }

        private void OnOptionClicked(int index)
        {
            if (waitingForNext) return;

            lastSelectedIndex = index;
            if (optionTexts[index] == null) return;
            
            string selectedAnswer = optionTexts[index].text;

            if (optionImages[index] != null)
            {
                optionImages[index].DOColor(new Color(0.25f, 0.35f, 0.55f), 0.15f);
            }

            QuizManager.Instance.SubmitAnswer(selectedAnswer);
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
