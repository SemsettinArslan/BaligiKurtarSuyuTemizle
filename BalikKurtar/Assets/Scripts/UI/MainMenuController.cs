using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BalikKurtar.Core;
using BalikKurtar.Managers;
using DG.Tweening;
using TMPro;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Ana Menü kontrol scripti.
    /// Buton handler'ları, panel yönetimi ve sahne geçişlerini yönetir.
    /// Mevcut scriptlere dokunmadan, bağımsız çalışır.
    /// 
    /// Inspector Kurulumu:
    /// 1. MainMenu sahnesinde bir Canvas oluştur (Screen Space - Overlay)
    /// 2. Bu script'i Canvas veya boş bir GameObject'e ekle
    /// 3. Butonları ve panelleri Inspector'dan ata
    /// 4. Aynı objeye veya sahneye bir SceneFader ekle
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Animator Referansı")]
        [Tooltip("MainMenuAnimator bileşeni (animasyonlar için)")]
        [SerializeField] private MainMenuAnimator menuAnimator;

        // ==================== BUTONLAR ====================

        [Header("Ana Butonlar")]
        [Tooltip("AR Balık Keşfi sahnesine git")]
        [SerializeField] private Button playARButton;

        [Tooltip("Hakkında panelini aç")]
        [SerializeField] private Button aboutButton;

        [Tooltip("Ayarlar panelini aç")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Oyundan çık butonu")]
        [SerializeField] private Button quitButton;

        [SerializeField] private Button leaderboardButton;


        // ==================== PANELLERİ ====================

        [Header("Ayarlar Paneli")]
        [SerializeField] private CanvasGroup settingsPanel;
        [SerializeField] private RectTransform settingsPanelRect;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider voiceSlider;
        [SerializeField] private Slider musicSlider;

        [Header("Çıkış Onay Paneli")]
        [SerializeField] private CanvasGroup quitConfirmationPanel;
        [SerializeField] private RectTransform quitConfirmationPanelRect;
        [SerializeField] private Button quitConfirmButton;
        [SerializeField] private Button quitCancelButton;

        [Header("Hakkında Paneli")]
        [SerializeField] private CanvasGroup aboutPanel;
        [SerializeField] private RectTransform aboutPanelRect;
        [SerializeField] private Button aboutCloseButton;

        [Header("Hakkında — İçerik")]
        [SerializeField] private TextMeshProUGUI aboutTitleText;
        [SerializeField] private TextMeshProUGUI aboutContentText;

        [Header("Takım Adı Paneli")]
        [SerializeField] private CanvasGroup teamNamePanel;
        [SerializeField] private RectTransform teamNamePanelRect;
        [SerializeField] private TMP_InputField teamNameInputField;
        [SerializeField] private Button teamNameSubmitButton;
        [SerializeField] private Button teamNameCancelButton;
        [SerializeField] private TextMeshProUGUI teamNameWarningText;

        [Header("Ana Menü Liderlik Tablosu")]
        [SerializeField] private CanvasGroup leaderboardPanel;
        [SerializeField] private RectTransform leaderboardPanelRect;
        [SerializeField] private Button leaderboardCloseButton;
        [SerializeField] private List<TextMeshProUGUI> mainMenuLeaderboardRowTexts;

        // ==================== SAHNE ADLARI ====================

        [Header("Sahne Ayarları")]
        [Tooltip("AR Balık Keşfi sahnesinin adı")]
        [SerializeField] private string arSceneName = "SampleScene";

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            // Panelleri başlangıçta gizle
            HidePanelImmediate(aboutPanel);
            HidePanelImmediate(settingsPanel);
            HidePanelImmediate(quitConfirmationPanel);
            HidePanelImmediate(teamNamePanel);
            HidePanelImmediate(leaderboardPanel);

            // Sahnede halihazırda (Inspector'dan ayarlanmış logolu) bir SceneFader var mı diye kontrol et
            SceneFader existingFader = Object.FindFirstObjectByType<SceneFader>();
            
            // Eğer sahnede hiç yoksa, kodla sıfırdan oluştur
            if (existingFader == null)
            {
                GameObject faderObj = new GameObject("SceneFader");
                faderObj.AddComponent<SceneFader>();
            }
        }

        private void Start()
        {
            BindButtons();
            PopulateDefaultContent();
            LoadSettingsUI();
        }

        private void LoadSettingsUI()
        {
            if (Managers.AudioManager.Instance != null)
            {
                if (sfxSlider != null)
                {
                    sfxSlider.value = Managers.AudioManager.Instance.GetSfxVolume();
                    sfxSlider.onValueChanged.AddListener(val => Managers.AudioManager.Instance.SetSfxVolume(val));
                }
                
                if (voiceSlider != null)
                {
                    voiceSlider.value = Managers.AudioManager.Instance.GetVoiceVolume();
                    voiceSlider.onValueChanged.AddListener(val => Managers.AudioManager.Instance.SetVoiceVolume(val));
                }

                if (musicSlider != null)
                {
                    musicSlider.value = Managers.AudioManager.Instance.GetMusicVolume();
                    musicSlider.onValueChanged.AddListener(val => Managers.AudioManager.Instance.SetMusicVolume(val));
                }
            }
        }

        // ==================== BUTON BAĞLAMA ====================

        private void BindButtons()
        {
            if (playARButton != null)
            {
                playARButton.onClick.RemoveAllListeners();
                playARButton.onClick.AddListener(OnPlayARClicked);
            }

            if (aboutButton != null)
            {
                aboutButton.onClick.RemoveAllListeners();
                aboutButton.onClick.AddListener(OnAboutClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            // Panel kapat/onay butonları
            if (aboutCloseButton != null)
            {
                aboutCloseButton.onClick.RemoveAllListeners();
                aboutCloseButton.onClick.AddListener(OnAboutCloseClicked);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.RemoveAllListeners();
                settingsCloseButton.onClick.AddListener(OnSettingsCloseClicked);
            }

            if (quitConfirmButton != null)
            {
                quitConfirmButton.onClick.RemoveAllListeners();
                quitConfirmButton.onClick.AddListener(OnQuitConfirmClicked);
            }

            if (quitCancelButton != null)
            {
                quitCancelButton.onClick.RemoveAllListeners();
                quitCancelButton.onClick.AddListener(OnQuitCancelClicked);
            }

            if (teamNameSubmitButton != null)
            {
                teamNameSubmitButton.onClick.RemoveAllListeners();
                teamNameSubmitButton.onClick.AddListener(OnTeamNameSubmitClicked);
            }

            if (teamNameCancelButton != null)
            {
                teamNameCancelButton.onClick.RemoveAllListeners();
                teamNameCancelButton.onClick.AddListener(OnTeamNameCancelClicked);
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.RemoveAllListeners();
                leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            }

            if (leaderboardCloseButton != null)
            {
                leaderboardCloseButton.onClick.RemoveAllListeners();
                leaderboardCloseButton.onClick.AddListener(OnLeaderboardCloseClicked);
            }
        }

        // ==================== BUTON HANDLER'LARI ====================

        /// <summary>Takım adı panelini açar. Eğer panel atanmamışsa doğrudan sahne geçişi yapar.</summary>
        private void OnPlayARClicked()
        {
            Debug.Log("[MainMenu] Balıkları Keşfet — Takım adı isteniyor...");

            // Buton animasyonu
            if (playARButton != null)
                playARButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8);

            if (teamNameWarningText != null)
                teamNameWarningText.text = "";

            if (teamNamePanel != null)
            {
                if (teamNameInputField != null)
                {
                    // Varsa eski takım adını getir
                    teamNameInputField.text = PlayerPrefs.GetString("CurrentTeamName", "");
                }

                if (menuAnimator != null)
                    menuAnimator.OpenPanel(teamNamePanel, teamNamePanelRect);
                else
                    ShowPanelFallback(teamNamePanel);
            }
            else
            {
                StartGameSession();
            }
        }

        private void OnTeamNameSubmitClicked()
        {
            if (teamNameInputField == null) return;

            string teamName = teamNameInputField.text.Trim();
            if (string.IsNullOrEmpty(teamName))
            {
                // Input alanını hafifçe salla
                teamNameInputField.transform.DOShakePosition(0.3f, 5f, 15);
                if (teamNameWarningText != null)
                    teamNameWarningText.text = "<color=red>Takım adı boş olamaz!</color>";
                return;
            }

            // Firebase'de kontrol et
            if (FirebaseLeaderboardManager.Instance != null)
            {
                if (teamNameWarningText != null)
                    teamNameWarningText.text = "Takım adı kontrol ediliyor...";

                SetTeamPanelInteractable(false);

                FirebaseLeaderboardManager.Instance.CheckTeamNameExists(teamName, (exists) =>
                {
                    SetTeamPanelInteractable(true);

                    if (exists)
                    {
                        teamNameInputField.transform.DOShakePosition(0.3f, 5f, 15);
                        if (teamNameWarningText != null)
                            teamNameWarningText.text = "<color=red>Bu takım adı zaten kullanımda!</color>";
                    }
                    else
                    {
                        // Başarılı, kaydet ve oyunu başlat
                        PlayerPrefs.SetString("CurrentTeamName", teamName);
                        PlayerPrefs.Save();

                        if (menuAnimator != null)
                            menuAnimator.ClosePanel(teamNamePanel, teamNamePanelRect);
                        else
                            HidePanelImmediate(teamNamePanel);

                        StartGameSession();
                    }
                });
            }
            else
            {
                // FirebaseLeaderboardManager bulunamadıysa (çevrimdışı/fallback) doğrudan başlat
                PlayerPrefs.SetString("CurrentTeamName", teamName);
                PlayerPrefs.Save();
                StartGameSession();
            }
        }

        private void OnTeamNameCancelClicked()
        {
            if (menuAnimator != null)
                menuAnimator.ClosePanel(teamNamePanel, teamNamePanelRect);
            else
                HidePanelImmediate(teamNamePanel);
        }

        private void SetTeamPanelInteractable(bool interactable)
        {
            if (teamNameInputField != null) teamNameInputField.interactable = interactable;
            if (teamNameSubmitButton != null) teamNameSubmitButton.interactable = interactable;
            if (teamNameCancelButton != null) teamNameCancelButton.interactable = interactable;
        }

        private void StartGameSession()
        {
            // Sahne geçişi
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(arSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(arSceneName);
            }
        }

        /// <summary>Hakkında panelini açar.</summary>
        private void OnAboutClicked()
        {
            Debug.Log("[MainMenu] Hakkında paneli açılıyor.");

            // Buton animasyonu
            if (aboutButton != null)
                aboutButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8);

            if (menuAnimator != null)
                menuAnimator.OpenPanel(aboutPanel, aboutPanelRect);
            else
                ShowPanelFallback(aboutPanel);
        }

        /// <summary>Hakkında panelini kapatır.</summary>
        private void OnAboutCloseClicked()
        {
            if (menuAnimator != null)
                menuAnimator.ClosePanel(aboutPanel, aboutPanelRect);
            else
                HidePanelImmediate(aboutPanel);
        }

        /// <summary>Ayarlar panelini açar.</summary>
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Ayarlar paneli açılıyor.");

            if (settingsButton != null)
                settingsButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8);

            if (menuAnimator != null)
                menuAnimator.OpenPanel(settingsPanel, settingsPanelRect);
            else
                ShowPanelFallback(settingsPanel);
        }

        private void OnSettingsCloseClicked()
        {
            if (menuAnimator != null)
                menuAnimator.ClosePanel(settingsPanel, settingsPanelRect);
            else
                HidePanelImmediate(settingsPanel);
        }

        /// <summary>Uygulamadan çıkış onayı ister.</summary>
        private void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Çıkış onayı açılıyor...");

            // Buton animasyonu
            if (quitButton != null)
                quitButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8);

            if (menuAnimator != null)
                menuAnimator.OpenPanel(quitConfirmationPanel, quitConfirmationPanelRect);
            else
                ShowPanelFallback(quitConfirmationPanel);
        }

        private void OnQuitCancelClicked()
        {
            if (menuAnimator != null)
                menuAnimator.ClosePanel(quitConfirmationPanel, quitConfirmationPanelRect);
            else
                HidePanelImmediate(quitConfirmationPanel);
        }

        private void OnQuitConfirmClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Ana menü liderlik tablosunu açar ve verileri günceller.</summary>
        private void OnLeaderboardClicked()
        {
            Debug.Log("[MainMenu] Liderlik tablosu açılıyor.");

            if (leaderboardButton != null)
                leaderboardButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8);

            if (menuAnimator != null)
                menuAnimator.OpenPanel(leaderboardPanel, leaderboardPanelRect);
            else
                ShowPanelFallback(leaderboardPanel);

            UpdateMainMenuLeaderboardUI();
        }

        private void OnLeaderboardCloseClicked()
        {
            if (menuAnimator != null)
                menuAnimator.ClosePanel(leaderboardPanel, leaderboardPanelRect);
            else
                HidePanelImmediate(leaderboardPanel);
        }

        private void UpdateMainMenuLeaderboardUI()
        {
            if (mainMenuLeaderboardRowTexts == null || mainMenuLeaderboardRowTexts.Count == 0) return;

            // Başlangıçta yükleniyor yaz
            foreach (var rowText in mainMenuLeaderboardRowTexts)
            {
                if (rowText != null) rowText.text = "...";
            }

            if (FirebaseLeaderboardManager.Instance != null)
            {
                int limit = mainMenuLeaderboardRowTexts.Count;
                FirebaseLeaderboardManager.Instance.GetTopScores(limit, (entries) =>
                {
                    if (entries == null || entries.Count == 0)
                    {
                        foreach (var rowText in mainMenuLeaderboardRowTexts)
                        {
                            if (rowText != null) rowText.text = "-";
                        }
                        return;
                    }

                    int maxRows = mainMenuLeaderboardRowTexts.Count;
                    for (int i = 0; i < maxRows; i++)
                    {
                        if (mainMenuLeaderboardRowTexts[i] == null) continue;

                        if (i < entries.Count)
                        {
                            var entry = entries[i];
                            mainMenuLeaderboardRowTexts[i].text = $"{i + 1}. {entry.teamName} - {entry.score} Puan";
                        }
                        else
                        {
                            mainMenuLeaderboardRowTexts[i].text = "-";
                        }
                    }
                });
            }
            else
            {
                foreach (var rowText in mainMenuLeaderboardRowTexts)
                {
                    if (rowText != null) rowText.text = "Hata";
                }
            }
        }

        // ==================== İÇERİK ====================

        /// <summary>
        /// Panel içeriklerini varsayılan metinlerle doldurur.
        /// Inspector'dan metin atanmışsa üzerine yazmaz.
        /// </summary>
        private void PopulateDefaultContent()
        {
            // Hakkında
            if (aboutTitleText != null && string.IsNullOrEmpty(aboutTitleText.text))
                aboutTitleText.text = "Hakkında";

            if (aboutContentText != null && string.IsNullOrEmpty(aboutContentText.text))
            {
                aboutContentText.text =
                    "<b>Balığı Kurtar: Suyu Temizle</b>\n" +
                    "Karma Gerçeklik (MR) Eğitim Deneyimi\n\n" +
                    "Bu uygulama, müze ortamında öğrencilerin\n" +
                    "deniz canlıları ve ekosistem hakkında\n" +
                    "farkındalık kazanmasını amaçlar.\n\n" +
                    "<b>Geliştirici:</b> Şemsettin Arslan\n" +
                    "<b>Teknolojiler:</b> Unity 6 • Vuforia • DOTween\n" +
                    "<b>Sürüm:</b> 0.1";
            }
        }

        // ==================== YARDIMCI ====================

        private void HidePanelImmediate(CanvasGroup panel)
        {
            if (panel == null) return;
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
        }

        private void ShowPanelFallback(CanvasGroup panel)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(true);
            panel.alpha = 1f;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }
    }
}
