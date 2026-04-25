using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BalikKurtar.Managers;
using BalikKurtar.UI;

namespace BalikKurtar.Core
{
    /// <summary>
    /// Ana oyun yöneticisi — Manager'ları ve UI'ı başlatır.
    /// Sahneye boş bir GameObject ekleyip bu script'i bağlamanız yeterlidir.
    /// Her şey otomatik oluşturulur.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // UI Panel referansları (otomatik oluşturulur)
        public FishInfoPanel FishInfoPanel { get; private set; }
        public QuizPanel QuizPanel { get; private set; }
        public QuizResultPanel QuizResultPanel { get; private set; }
        public MainHUD MainHUD { get; private set; }

        private Canvas mainCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Manager'ları oluştur (aynı GameObject üzerinde)
            EnsureComponent<FishDatabase>();
            EnsureComponent<DiscoveredFishManager>();
            EnsureComponent<QuizManager>();

            Debug.Log("[GameManager] Sistem başlatıldı.");
        }

        private void Start()
        {
            // UI'ı oluştur (Manager'lar Awake'de hazır olduktan sonra)
            CreateUI();
            Debug.Log("[GameManager] UI oluşturuldu.");
        }

        /// <summary>Quiz'i başlatır (HUD butonundan çağrılır).</summary>
        public void StartQuiz()
        {
            if (QuizManager.Instance == null || !QuizManager.Instance.CanStartQuiz())
            {
                Debug.LogWarning("[GameManager] Quiz başlatılamıyor — yeterli balık keşfedilmedi.");
                return;
            }

            // Bilgi panelini kapat
            if (FishInfoPanel != null)
            {
                FishInfoPanel.Hide(true);
            }

            // Quiz panelini aç ve başlat
            if (QuizPanel != null)
            {
                QuizPanel.Show();
                QuizManager.Instance.StartQuiz();
            }
        }

        // ==================== UI OLUŞTURMA ====================

        private void CreateUI()
        {
            EnsureEventSystem();
            CreateCanvas();
            CreatePanels();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
                Debug.Log("[GameManager] EventSystem oluşturuldu.");
            }
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("GameUI_Canvas");
            canvasGO.transform.SetParent(transform);

            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100; // AR kameranın üstünde

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanels()
        {
            var canvasRect = mainCanvas.GetComponent<RectTransform>();

            // MainHUD (üst bar — her zaman görünür)
            var hudGO = new GameObject("MainHUD", typeof(RectTransform));
            hudGO.transform.SetParent(canvasRect, false);
            MainHUD = hudGO.AddComponent<MainHUD>();

            // FishInfoPanel (alt bilgi paneli)
            var infoGO = new GameObject("FishInfoPanel", typeof(RectTransform));
            infoGO.transform.SetParent(canvasRect, false);
            FishInfoPanel = infoGO.AddComponent<FishInfoPanel>();

            // QuizPanel (tam ekran quiz overlay)
            var quizGO = new GameObject("QuizPanel", typeof(RectTransform));
            quizGO.transform.SetParent(canvasRect, false);
            QuizPanel = quizGO.AddComponent<QuizPanel>();

            // QuizResultPanel (sonuç ekranı)
            var resultGO = new GameObject("QuizResultPanel", typeof(RectTransform));
            resultGO.transform.SetParent(canvasRect, false);
            QuizResultPanel = resultGO.AddComponent<QuizResultPanel>();
        }

        // ==================== YARDIMCI ====================

        private T EnsureComponent<T>() where T : Component
        {
            var comp = GetComponent<T>();
            if (comp == null)
                comp = gameObject.AddComponent<T>();
            return comp;
        }
    }
}
