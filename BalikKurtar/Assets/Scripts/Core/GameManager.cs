using UnityEngine;
using BalikKurtar.Managers;
using BalikKurtar.UI;

namespace BalikKurtar.Core
{
    /// <summary>
    /// Ana oyun yöneticisi — Manager'ları başlatır.
    /// UI artık Editör üzerinden World Space Canvas'larla veya direkt sahneye eklenerek yönetiliyor.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Global UI Referansları")]
        [Tooltip("Sahnedeki QuizPanel'i buraya sürükleyin")]
        public QuizPanel QuizPanel;
        
        [Tooltip("Sahnedeki QuizResultPanel'i buraya sürükleyin")]
        public QuizResultPanel QuizResultPanel;

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

        /// <summary>Quiz'i başlatır (HUD butonundan çağrılır).</summary>
        public void StartQuiz()
        {
            if (QuizManager.Instance == null || !QuizManager.Instance.CanStartQuiz())
            {
                Debug.LogWarning("[GameManager] Quiz başlatılamıyor — yeterli balık keşfedilmedi.");
                return;
            }

            // Quiz panelini aç ve başlat
            if (QuizPanel != null)
            {
                QuizPanel.Show();
                QuizManager.Instance.StartQuiz();
            }
            else
            {
                Debug.LogError("[GameManager] QuizPanel referansı atanmamış!");
            }
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
