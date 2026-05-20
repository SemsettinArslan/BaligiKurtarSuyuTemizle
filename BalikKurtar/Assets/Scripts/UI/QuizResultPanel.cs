using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using BalikKurtar.Managers;
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
        [SerializeField] private TextMeshProUGUI playAgainButtonText;
        [SerializeField] private Button backButton;
        
        [Header("Sahne Geçişi")]
        [Tooltip("Quiz bittikten sonra geçilecek sahnenin tam adı (Örn: Level2)")]
        [SerializeField] private string nextSceneName = "NextScene";

        [Header("Firebase Liderlik Tablosu UI")]
        [SerializeField] private List<TextMeshProUGUI> leaderboardRowTexts;
        [SerializeField] private TextMeshProUGUI currentTeamRankText;

        private bool isVisible = false;
        private bool failedQuiz = false;
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

            // Puan kontrolü — 50 puanın altında başarısız
            failedQuiz = score < 50;

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
                    messageText.text = "Kartları tekrar okutup öğrenmelisin!";
                    messageText.color = new Color(1f, 0.5f, 0.3f);
                }
            }

            // Başarısız ise: sonraki oyun butonu deaktif, tekrar oyna → kartları tekrar okut
            if (failedQuiz)
            {
                if (backButton != null) backButton.interactable = false;
                if (playAgainButtonText != null) playAgainButtonText.text = "Kartları Tekrar Okut";
            }
            else
            {
                if (backButton != null) backButton.interactable = true;
                if (playAgainButtonText != null) playAgainButtonText.text = "Tekrar Oyna";
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

            // Firebase'e skor gönderimi (Takım adı girilmişse)
            string currentTeam = PlayerPrefs.GetString("CurrentTeamName", "");
            if (!string.IsNullOrEmpty(currentTeam) && FirebaseLeaderboardManager.Instance != null)
            {
                FirebaseLeaderboardManager.Instance.SubmitScore(currentTeam, score, () => 
                {
                    // Liderlik tablosunu arayüzde güncelle (Sunucuya yazıldıktan sonra)
                    UpdateLeaderboardUI();
                });
            }
            else
            {
                // Çevrimdışı/Fallback durumunda doğrudan güncelle
                UpdateLeaderboardUI();
            }
        }

        private void UpdateLeaderboardUI()
        {
            if (leaderboardRowTexts == null || leaderboardRowTexts.Count == 0) return;

            // Satırları başlangıçta yükleniyor durumuna getir
            foreach (var rowText in leaderboardRowTexts)
            {
                if (rowText != null) rowText.text = "...";
            }

            if (currentTeamRankText != null)
            {
                currentTeamRankText.text = "Sıralamanız Hesaplanıyor...";
            }

            if (FirebaseLeaderboardManager.Instance != null)
            {
                // Genel sıralamayı doğru belirlemek için en yüksek 100 skoru çekiyoruz
                FirebaseLeaderboardManager.Instance.GetTopScores(100, (entries) =>
                {
                    if (entries == null || entries.Count == 0)
                    {
                        foreach (var rowText in leaderboardRowTexts)
                        {
                            if (rowText != null) rowText.text = "-";
                        }
                        if (currentTeamRankText != null) currentTeamRankText.text = "Sıralama: -";
                        return;
                    }

                    string myTeamName = PlayerPrefs.GetString("CurrentTeamName", "");

                    // 1. Arayüzdeki satırları doldur (Top listesi kadar)
                    int maxRows = leaderboardRowTexts.Count;
                    for (int i = 0; i < maxRows; i++)
                    {
                        if (leaderboardRowTexts[i] == null) continue;

                        if (i < entries.Count)
                        {
                            var entry = entries[i];
                            string highlightStart = "";
                            string highlightEnd = "";

                            if (!string.IsNullOrEmpty(myTeamName) && entry.teamName.Equals(myTeamName, StringComparison.OrdinalIgnoreCase))
                            {
                                highlightStart = "<b><color=#FFD700>";
                                highlightEnd = "</color></b>";
                            }

                            leaderboardRowTexts[i].text = $"{i + 1}. {highlightStart}{entry.teamName} - {entry.score} Puan{highlightEnd}";
                        }
                        else
                        {
                            // Kayıtlı veri satır sayısından azsa boş bırak
                            leaderboardRowTexts[i].text = "-";
                        }
                    }

                    // 2. Kendi takımımızın sıralamasını bul
                    if (currentTeamRankText != null)
                    {
                        if (string.IsNullOrEmpty(myTeamName))
                        {
                            currentTeamRankText.text = "Sıralama: Takım adı bulunamadı";
                        }
                        else
                        {
                            // Büyük/küçük harf duyarsız aratarak sırasını bul (indeks 0'dan başladığı için +1 ekliyoruz)
                            int rank = entries.FindIndex(e => e.teamName.Equals(myTeamName, StringComparison.OrdinalIgnoreCase)) + 1;

                            if (rank > 0)
                            {
                                currentTeamRankText.text = $"Takımınız <b><color=#FFD700>{rank}.</color></b> sırada!";
                            }
                            else
                            {
                                currentTeamRankText.text = "Sıralama: Listede yer alınamadı";
                            }
                        }
                    }
                });
            }
            else
            {
                foreach (var rowText in leaderboardRowTexts)
                {
                    if (rowText != null) rowText.text = "Hata";
                }
                if (currentTeamRankText != null) currentTeamRankText.text = "Sistem Bağlantı Hatası";
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

            if (failedQuiz)
            {
                // Başarısız — keşifleri sıfırla, öğrenci kartları tekrar okutmalı
                if (DiscoveredFishManager.Instance != null)
                {
                    DiscoveredFishManager.Instance.ResetDiscoveries();
                }
                Debug.Log("[QuizResultPanel] Başarısız — keşifler sıfırlandı. Kartları tekrar okutun.");
            }
            else
            {
                // Başarılı — quiz'i yeniden başlat
                var gm = Core.GameManager.Instance;
                if (gm != null)
                {
                    gm.StartQuiz();
                }
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
