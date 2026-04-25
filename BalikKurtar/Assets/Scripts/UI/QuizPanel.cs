using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BalikKurtar.Managers;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Quiz oyun ekranı — soru metni, 4 seçenek butonu, skor ve ilerleme gösterir.
    /// QuizManager event'lerine abone olarak otomatik güncellenir.
    /// </summary>
    public class QuizPanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform panelRect;

        // UI Elemanları
        private Text questionText;
        private Text progressText;
        private Text scoreText;
        private Button[] optionButtons = new Button[4];
        private Text[] optionTexts = new Text[4];
        private Image[] optionImages = new Image[4];
        private Image overlayBackground;

        private bool isVisible = false;
        private bool waitingForNext = false;
        private int lastSelectedIndex = -1;
        private Sequence currentAnimation;

        // Renkler
        private readonly Color normalColor = new Color(0.15f, 0.25f, 0.4f, 1f);
        private readonly Color correctColor = new Color(0.2f, 0.7f, 0.3f, 1f);
        private readonly Color wrongColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        private readonly Color highlightCorrectColor = new Color(0.25f, 0.8f, 0.4f, 1f);

        private void Awake()
        {
            BuildUI();
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

            currentAnimation?.Kill();
            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(canvasGroup.DOFade(1f, 0.4f).From(0f));
            currentAnimation.Join(panelRect.DOScale(1f, 0.4f).From(0.9f).SetEase(Ease.OutBack));
        }

        /// <summary>Quiz panelini gizler.</summary>
        public void Hide()
        {
            if (!isVisible) return;
            isVisible = false;

            currentAnimation?.Kill();
            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(canvasGroup.DOFade(0f, 0.3f));
            currentAnimation.OnComplete(() => gameObject.SetActive(false));
        }

        // ==================== EVENT HANDLERS ====================

        private void OnQuestionReady(QuizQuestion question, int index, int total)
        {
            waitingForNext = false;

            // Soru metnini güncelle
            questionText.text = question.questionText;
            progressText.text = $"Soru {index + 1}/{total}";
            scoreText.text = $"\u2b50 {QuizManager.Instance.Score}";

            // Butonları güncelle
            for (int i = 0; i < 4; i++)
            {
                if (i < question.allOptions.Count)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionTexts[i].text = question.allOptions[i];
                    optionImages[i].color = normalColor;
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

            // Tüm butonları deaktif et
            for (int i = 0; i < 4; i++)
            {
                if (!optionButtons[i].gameObject.activeSelf) continue;
                optionButtons[i].interactable = false;

                // Doğru cevabı yeşil yap
                if (optionTexts[i].text == correctAnswer)
                {
                    optionImages[i].DOColor(highlightCorrectColor, 0.3f);
                    optionButtons[i].transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
                }
            }

            // Yanlış cevap verdiyse seçilen butonu kırmızı yap
            if (!isCorrect && lastSelectedIndex >= 0 && lastSelectedIndex < 4)
            {
                if (optionButtons[lastSelectedIndex].gameObject.activeSelf)
                {
                    optionImages[lastSelectedIndex].DOColor(wrongColor, 0.3f);
                    optionButtons[lastSelectedIndex].transform.DOShakePosition(0.3f, 5f, 20);
                }
            }

            // Skor güncelle
            scoreText.text = $"\u2b50 {QuizManager.Instance.Score}";

            // Kısa beklemeden sonra sonraki soruya geç
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

            // Sonuç panelini göster
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
            string selectedAnswer = optionTexts[index].text;

            // Seçimi göster (hafif renk değişimi)
            optionImages[index].DOColor(new Color(0.25f, 0.35f, 0.55f), 0.15f);

            // Cevabı gönder — OnAnswerResult event'i renkleri halleder
            QuizManager.Instance.SubmitAnswer(selectedAnswer);
        }

        // ==================== UI OLUŞTURMA ====================

        private void BuildUI()
        {
            panelRect = gameObject.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();

            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Tam ekran karartma arka planı
            overlayBackground = gameObject.AddComponent<Image>();
            overlayBackground.color = new Color(0.03f, 0.06f, 0.12f, 0.96f);

            // Tam ekran kaplama
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Üst bar — ilerleme ve skor
            var topBar = CreateChild("TopBar", panelRect);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0, 60);
            topRect.anchoredPosition = Vector2.zero;

            var topBg = topBar.AddComponent<Image>();
            topBg.color = new Color(0.08f, 0.14f, 0.22f, 0.9f);

            progressText = CreateText(topRect, "Soru 1/10", 20, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft);
            var progRect = progressText.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0, 0);
            progRect.anchorMax = new Vector2(0.5f, 1);
            progRect.offsetMin = new Vector2(25, 0);
            progRect.offsetMax = Vector2.zero;

            scoreText = CreateText(topRect, "\u2b50 0", 22, FontStyle.Bold,
                new Color(1f, 0.84f, 0f), TextAnchor.MiddleRight);
            var scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 0);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = new Vector2(-25, 0);

            // Soru alanı
            var questionArea = CreateChild("QuestionArea", panelRect);
            var qAreaRect = questionArea.GetComponent<RectTransform>();
            qAreaRect.anchorMin = new Vector2(0.05f, 0.5f);
            qAreaRect.anchorMax = new Vector2(0.95f, 0.88f);
            qAreaRect.offsetMin = Vector2.zero;
            qAreaRect.offsetMax = Vector2.zero;

            var qBg = questionArea.AddComponent<Image>();
            qBg.color = new Color(0.1f, 0.18f, 0.28f, 0.95f);

            questionText = CreateText(qAreaRect, "Soru metni...", 22, FontStyle.Normal,
                Color.white, TextAnchor.MiddleCenter);
            var qTextRect = questionText.GetComponent<RectTransform>();
            qTextRect.anchorMin = new Vector2(0, 0);
            qTextRect.anchorMax = new Vector2(1, 1);
            qTextRect.offsetMin = new Vector2(20, 15);
            qTextRect.offsetMax = new Vector2(-20, -15);

            // 4 seçenek butonu (2x2 grid)
            float buttonAreaTop = 0.48f;
            float buttonAreaBottom = 0.06f;
            float gap = 0.02f;
            float halfW = 0.48f;
            float rowH = (buttonAreaTop - buttonAreaBottom - gap) / 2f;

            for (int i = 0; i < 4; i++)
            {
                int row = i / 2;
                int col = i % 2;

                float xMin = col == 0 ? 0.05f : 0.05f + halfW + gap;
                float xMax = xMin + halfW;
                float yMax = buttonAreaTop - row * (rowH + gap);
                float yMin = yMax - rowH;

                CreateOptionButton(i, panelRect, xMin, yMin, xMax, yMax);
            }
        }

        private void CreateOptionButton(int index, RectTransform parent,
            float anchorXMin, float anchorYMin, float anchorXMax, float anchorYMax)
        {
            var btnGO = CreateChild($"Option_{index}", parent);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(anchorXMin, anchorYMin);
            btnRect.anchorMax = new Vector2(anchorXMax, anchorYMax);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = normalColor;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            // Hover/press renk geçişi
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.95f, 1f);
            colors.pressedColor = new Color(0.7f, 0.8f, 0.9f);
            colors.disabledColor = Color.white;
            btn.colors = colors;

            var btnText = CreateText(btnRect, $"Se\u00e7enek {index + 1}", 17, FontStyle.Normal,
                Color.white, TextAnchor.MiddleCenter);
            var textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 8);
            textRect.offsetMax = new Vector2(-12, -8);

            optionButtons[index] = btn;
            optionTexts[index] = btnText;
            optionImages[index] = btnImg;

            int capturedIndex = index;
            btn.onClick.AddListener(() => OnOptionClicked(capturedIndex));
        }

        // ==================== YARDIMCI ====================

        private void HideImmediate()
        {
            isVisible = false;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private GameObject CreateChild(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Text CreateText(RectTransform parent, string content, int fontSize,
            FontStyle style, Color color, TextAnchor alignment)
        {
            var go = CreateChild("Txt", parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void OnDestroy()
        {
            currentAnimation?.Kill();
        }
    }
}
