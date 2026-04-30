using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BalikKurtar.Managers;
using TMPro;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Ekranın üst kısmında veya AR kamerasında World Space olarak görünen HUD — keşfedilen balık sayısı ve Quiz butonu.
    /// Editör üzerinden ayarlanmalıdır.
    /// </summary>
    public class MainHUD : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private TextMeshProUGUI discoveryText;
        [SerializeField] private Button quizButton;
        [SerializeField] private TextMeshProUGUI quizButtonText;
        [SerializeField] private Image quizButtonImage;

        private readonly Color activeColor = new Color(0.1f, 0.6f, 0.9f, 1f);
        private readonly Color disabledColor = new Color(0.3f, 0.35f, 0.4f, 1f);

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
            if (DiscoveredFishManager.Instance != null)
            {
                DiscoveredFishManager.Instance.OnDiscoveryCountChanged -= UpdateDiscoveryCount;
                DiscoveredFishManager.Instance.OnDiscoveryCountChanged += UpdateDiscoveryCount;
                UpdateDiscoveryCount(DiscoveredFishManager.Instance.DiscoveredCount);
            }

            if (quizButton != null)
            {
                quizButton.onClick.RemoveAllListeners();
                quizButton.onClick.AddListener(OnQuizClicked);
            }
            
            UpdateQuizButton();
        }

        private void UpdateDiscoveryCount(int count)
        {
            if (discoveryText == null) return;
            
            discoveryText.text = $"{count} Balık Keşfedildi";
            UpdateQuizButton();

            // Yeni keşif animasyonu
            discoveryText.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5);
        }

        private void UpdateQuizButton()
        {
            if (quizButton == null || quizButtonImage == null || quizButtonText == null) return;

            bool canQuiz = QuizManager.Instance != null && QuizManager.Instance.CanStartQuiz();

            quizButton.interactable = canQuiz;
            quizButtonImage.color = canQuiz ? activeColor : disabledColor;

            if (canQuiz)
            {
                quizButtonText.text = "Quiz Başlat";
            }
            else
            {
                int min = QuizManager.Instance?.GetMinRequired() ?? 2;
                int current = DiscoveredFishManager.Instance?.DiscoveredCount ?? 0;
                quizButtonText.text = $"{min - current} balık daha keşfet";
            }
        }

        private void OnQuizClicked()
        {
            if (QuizManager.Instance == null || !QuizManager.Instance.CanStartQuiz())
            {
                if (quizButton != null)
                    quizButton.transform.DOShakePosition(0.3f, 5f, 15);
                return;
            }

            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.StartQuiz();
            }
        }
    }
}
