using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BalikKurtar.Managers;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Ekranın üst kısmında sürekli görünen HUD — keşfedilen balık sayısı ve Quiz butonu.
    /// </summary>
    public class MainHUD : MonoBehaviour
    {
        private RectTransform panelRect;
        private Text discoveryText;
        private Button quizButton;
        private Text quizButtonText;
        private Image quizButtonImage;

        private readonly Color activeColor = new Color(0.1f, 0.6f, 0.9f, 1f);
        private readonly Color disabledColor = new Color(0.3f, 0.35f, 0.4f, 1f);

        private void Awake()
        {
            BuildUI();
        }

        private void OnEnable()
        {
            if (DiscoveredFishManager.Instance != null)
            {
                DiscoveredFishManager.Instance.OnDiscoveryCountChanged += UpdateDiscoveryCount;
                UpdateDiscoveryCount(DiscoveredFishManager.Instance.DiscoveredCount);
            }
        }

        private void OnDisable()
        {
            if (DiscoveredFishManager.Instance != null)
            {
                DiscoveredFishManager.Instance.OnDiscoveryCountChanged -= UpdateDiscoveryCount;
            }
        }

        private void Start()
        {
            // Manager'lar Awake'de init olduğu için Start'ta tekrar bağlan
            if (DiscoveredFishManager.Instance != null)
            {
                DiscoveredFishManager.Instance.OnDiscoveryCountChanged -= UpdateDiscoveryCount;
                DiscoveredFishManager.Instance.OnDiscoveryCountChanged += UpdateDiscoveryCount;
                UpdateDiscoveryCount(DiscoveredFishManager.Instance.DiscoveredCount);
            }

            UpdateQuizButton();
        }

        private void UpdateDiscoveryCount(int count)
        {
            discoveryText.text = $"\ud83d\udc1f {count} Bal\u0131k Ke\u015ffedildi";
            UpdateQuizButton();

            // Yeni keşif animasyonu
            discoveryText.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5);
        }

        private void UpdateQuizButton()
        {
            bool canQuiz = QuizManager.Instance != null && QuizManager.Instance.CanStartQuiz();

            quizButton.interactable = canQuiz;
            quizButtonImage.color = canQuiz ? activeColor : disabledColor;

            if (canQuiz)
            {
                quizButtonText.text = "\ud83c\udfae Quiz Ba\u015flat";
            }
            else
            {
                int min = QuizManager.Instance?.GetMinRequired() ?? 2;
                int current = DiscoveredFishManager.Instance?.DiscoveredCount ?? 0;
                quizButtonText.text = $"\ud83d\udd12 {min - current} bal\u0131k daha ke\u015ffet";
            }
        }

        private void OnQuizClicked()
        {
            if (QuizManager.Instance == null || !QuizManager.Instance.CanStartQuiz())
            {
                // Buton zaten disabled olmalı ama güvenlik kontrolü
                quizButton.transform.DOShakePosition(0.3f, 5f, 15);
                return;
            }

            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.StartQuiz();
            }
        }

        // ==================== UI OLUŞTURMA ====================

        private void BuildUI()
        {
            panelRect = gameObject.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();

            // Üst bar arka planı
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.1f, 0.18f, 0.88f);

            // Konum (üst kısım)
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(0, 55);
            panelRect.anchoredPosition = Vector2.zero;

            // Keşif sayısı (sol taraf)
            discoveryText = CreateText(panelRect, "\ud83d\udc1f 0 Bal\u0131k Ke\u015ffedildi",
                17, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            var discRect = discoveryText.GetComponent<RectTransform>();
            discRect.anchorMin = new Vector2(0, 0);
            discRect.anchorMax = new Vector2(0.55f, 1);
            discRect.offsetMin = new Vector2(18, 0);
            discRect.offsetMax = Vector2.zero;

            // Quiz butonu (sağ taraf)
            var btnGO = CreateChild("QuizBtn", panelRect);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.58f, 0.12f);
            btnRect.anchorMax = new Vector2(0.97f, 0.88f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            quizButtonImage = btnGO.AddComponent<Image>();
            quizButtonImage.color = disabledColor;

            quizButton = btnGO.AddComponent<Button>();
            quizButton.targetGraphic = quizButtonImage;
            quizButton.onClick.AddListener(OnQuizClicked);

            quizButtonText = CreateText(btnRect, "\ud83d\udd12 2 bal\u0131k daha ke\u015ffet",
                15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            var qbtRect = quizButtonText.GetComponent<RectTransform>();
            qbtRect.anchorMin = Vector2.zero;
            qbtRect.anchorMax = Vector2.one;
            qbtRect.offsetMin = new Vector2(8, 0);
            qbtRect.offsetMax = new Vector2(-8, 0);
        }

        // ==================== YARDIMCI ====================

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
    }
}
