using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Quiz sonuç ekranı — skor, doğru/yanlış sayısı ve aksiyon butonları gösterir.
    /// </summary>
    public class QuizResultPanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform panelRect;

        private Text titleText;
        private Text scoreValueText;
        private Text correctText;
        private Text wrongText;
        private Text messageText;
        private Button playAgainButton;
        private Button backButton;

        private bool isVisible = false;
        private Sequence currentAnimation;

        private void Awake()
        {
            BuildUI();
            HideImmediate();
        }

        /// <summary>Sonuç panelini gösterir.</summary>
        public void Show(int score, int correct, int wrong)
        {
            gameObject.SetActive(true);
            isVisible = true;

            int total = correct + wrong;
            float percentage = total > 0 ? (float)correct / total * 100f : 0f;

            scoreValueText.text = score.ToString();
            correctText.text = $"\u2705 Do\u011fru: {correct}";
            wrongText.text = $"\u274c Yanl\u0131\u015f: {wrong}";

            // Performansa göre mesaj
            if (percentage >= 90)
            {
                messageText.text = "\ud83c\udf1f M\u00fckemmel! Deniz uzman\u0131s\u0131n!";
                messageText.color = new Color(1f, 0.84f, 0f);
            }
            else if (percentage >= 70)
            {
                messageText.text = "\ud83d\udc4f \u00c7ok iyi! Balıkları iyi tanıyorsun!";
                messageText.color = new Color(0.3f, 0.85f, 0.4f);
            }
            else if (percentage >= 50)
            {
                messageText.text = "\ud83d\ude0a Fena de\u011fil! Biraz daha \u00e7alı\u015f!";
                messageText.color = new Color(0.2f, 0.7f, 0.9f);
            }
            else
            {
                messageText.text = "\ud83d\udcda Daha fazla kart okut ve \u00f6\u011fren!";
                messageText.color = new Color(1f, 0.5f, 0.3f);
            }

            // Animasyon
            currentAnimation?.Kill();
            canvasGroup.alpha = 0f;
            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(canvasGroup.DOFade(1f, 0.5f));
            currentAnimation.Join(panelRect.DOScale(1f, 0.5f).From(0.85f).SetEase(Ease.OutBack));

            // Skor animasyonu (0'dan hedefe)
            int targetScore = score;
            scoreValueText.text = "0";
            DOTween.To(() => 0, x => scoreValueText.text = x.ToString(), targetScore, 1f)
                .SetDelay(0.5f)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>Sonuç panelini gizler.</summary>
        public void Hide()
        {
            if (!isVisible) return;
            isVisible = false;

            currentAnimation?.Kill();
            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(canvasGroup.DOFade(0f, 0.3f));
            currentAnimation.OnComplete(() => gameObject.SetActive(false));
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
            // AR moduna geri dön (quiz panelini kapat, HUD'ı göster)
        }

        // ==================== UI OLUŞTURMA ====================

        private void BuildUI()
        {
            panelRect = gameObject.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();

            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Tam ekran arka plan
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.06f, 0.12f, 0.97f);

            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // İç panel (kartlı görünüm)
            var cardGO = CreateChild("Card", panelRect);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.1f, 0.15f);
            cardRect.anchorMax = new Vector2(0.9f, 0.85f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            var cardBg = cardGO.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.14f, 0.24f, 1f);

            float y = 0;

            // Başlık
            titleText = CreateText(cardRect, "\ud83c\udfc6 Quiz Sonucu", 30, FontStyle.Bold,
                new Color(1f, 0.84f, 0f), TextAnchor.MiddleCenter);
            SetTextRect(titleText, cardRect, ref y, 45);
            y += 10;

            // Skor değeri (büyük)
            scoreValueText = CreateText(cardRect, "0", 64, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            SetTextRect(scoreValueText, cardRect, ref y, 75);

            // "Puan" yazısı
            var puanText = CreateText(cardRect, "Puan", 18, FontStyle.Normal,
                new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleCenter);
            SetTextRect(puanText, cardRect, ref y, 25);
            y += 15;

            // Doğru sayısı
            correctText = CreateText(cardRect, "\u2705 Do\u011fru: 0", 22, FontStyle.Normal,
                new Color(0.3f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            SetTextRect(correctText, cardRect, ref y, 32);

            // Yanlış sayısı
            wrongText = CreateText(cardRect, "\u274c Yanl\u0131\u015f: 0", 22, FontStyle.Normal,
                new Color(0.9f, 0.35f, 0.3f), TextAnchor.MiddleCenter);
            SetTextRect(wrongText, cardRect, ref y, 32);
            y += 10;

            // Mesaj
            messageText = CreateText(cardRect, "...", 20, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            SetTextRect(messageText, cardRect, ref y, 35);
            y += 20;

            // Tekrar Oyna butonu
            playAgainButton = CreateButton(cardRect, "\ud83d\udd01 Tekrar Oyna",
                new Color(0.1f, 0.6f, 0.9f), ref y);
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            y += 10;

            // Geri Dön butonu
            backButton = CreateButton(cardRect, "\u2b05 Kart Okutmaya D\u00f6n",
                new Color(0.3f, 0.4f, 0.5f), ref y);
            backButton.onClick.AddListener(OnBackClicked);
        }

        private void SetTextRect(Text text, RectTransform parent, ref float yPos, float height)
        {
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -yPos);
            rect.sizeDelta = new Vector2(-40, height);
            yPos += height;
        }

        private Button CreateButton(RectTransform parent, string label, Color color, ref float yPos)
        {
            var btnGO = CreateChild("Btn_" + label, parent);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.15f, 1);
            btnRect.anchorMax = new Vector2(0.85f, 1);
            btnRect.pivot = new Vector2(0.5f, 1f);
            btnRect.anchoredPosition = new Vector2(0, -yPos);
            btnRect.sizeDelta = new Vector2(0, 50);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = color;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            var btnText = CreateText(btnRect, label, 19, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            var textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            yPos += 55;
            return btn;
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
