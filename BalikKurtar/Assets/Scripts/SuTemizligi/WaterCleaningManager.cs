using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BalikKurtar.SuTemizligi
{
    /// <summary>
    /// Su Temizligi mini oyununun ana yöneticisi.
    /// Sahnedeki tüm çöpleri takip eder, temizleme ilerlemesini yönetir
    /// ve seviye tamamlama kontrolünü yapar.
    /// </summary>
    public class WaterCleaningManager : MonoBehaviour
    {
        public static WaterCleaningManager Instance { get; private set; }

        // ==================== OYUN DURUMLARI ====================

        public enum GameState
        {
            WaitingToStart,
            Playing,
            Paused,
            Completed
        }

        [Header("Oyun Ayarları")]
        [Tooltip("Oyun başladığında otomatik başlasın mı?")]
        [SerializeField] private bool autoStart = true;

        [Tooltip("Süre limiti (saniye). 0 = sınırsız.")]
        [SerializeField] private float timeLimit = 0f;

        // ==================== DURUM ====================

        public GameState CurrentState { get; private set; } = GameState.WaitingToStart;

        private List<TrashItem> allTrash = new List<TrashItem>();
        private int totalTrashCount;
        private int cleanedCount;
        private float elapsedTime;

        // ==================== EVENTS ====================

        /// <summary>Bir çöp temizlendiğinde: (temizlenen sayı, kalan sayı)</summary>
        public event Action<int, int> OnTrashCleaned;

        /// <summary>Tüm çöpler temizlenip seviye tamamlandığında.</summary>
        public event Action OnLevelComplete;

        /// <summary>Süre güncellendiğinde: (geçen süre, kalan süre)</summary>
        public event Action<float, float> OnTimeUpdated;

        /// <summary>Süre dolduğunda (zaman limiti varsa).</summary>
        public event Action OnTimeUp;

        /// <summary>Oyun durumu değiştiğinde.</summary>
        public event Action<GameState> OnGameStateChanged;

        // ==================== PROPERTİES ====================

        public int TotalTrashCount => totalTrashCount;
        public int CleanedCount => cleanedCount;
        public int RemainingCount => totalTrashCount - cleanedCount;
        public float ElapsedTime => elapsedTime;
        public float RemainingTime => timeLimit > 0 ? Mathf.Max(0, timeLimit - elapsedTime) : -1f;
        public bool HasTimeLimit => timeLimit > 0f;

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CollectTrashItems();

            if (autoStart)
            {
                StartGame();
            }
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            // Süre takibi
            elapsedTime += Time.deltaTime;

            if (HasTimeLimit)
            {
                OnTimeUpdated?.Invoke(elapsedTime, RemainingTime);

                if (elapsedTime >= timeLimit)
                {
                    OnTimeUp?.Invoke();
                    SetState(GameState.Completed);
                }
            }
        }

        // ==================== PUBLIC API ====================

        /// <summary>Oyunu başlatır.</summary>
        public void StartGame()
        {
            cleanedCount = 0;
            elapsedTime = 0f;
            SetState(GameState.Playing);
            Debug.Log($"[WaterCleaning] Oyun başladı! {totalTrashCount} çöp temizlenecek.");
        }

        /// <summary>Oyunu duraklatır/devam ettirir.</summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
                SetState(GameState.Paused);
            else if (CurrentState == GameState.Paused)
                SetState(GameState.Playing);
        }

        /// <summary>Bir çöp temizlendiğinde TrashItem tarafından çağrılır.</summary>
        public void ReportTrashCleaned(TrashItem trash)
        {
            if (CurrentState != GameState.Playing) return;

            cleanedCount++;
            allTrash.Remove(trash);

            Debug.Log($"[WaterCleaning] Çöp temizlendi! ({cleanedCount}/{totalTrashCount})");
            OnTrashCleaned?.Invoke(cleanedCount, RemainingCount);

            if (RemainingCount <= 0)
            {
                CompleteLevelInternal();
            }
        }

        // ==================== INTERNAL ====================

        private void CollectTrashItems()
        {
            allTrash = FindObjectsByType<TrashItem>(FindObjectsSortMode.None).ToList();
            totalTrashCount = allTrash.Count;
            Debug.Log($"[WaterCleaning] {totalTrashCount} çöp bulundu.");
        }

        private void CompleteLevelInternal()
        {
            SetState(GameState.Completed);
            Debug.Log($"[WaterCleaning] Seviye tamamlandı! Süre: {elapsedTime:F1}s");
            OnLevelComplete?.Invoke();
        }

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"[WaterCleaning] Durum: {newState}");
        }
    }
}
