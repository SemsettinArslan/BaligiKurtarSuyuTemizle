using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BalikKurtar.Data;

namespace BalikKurtar.Managers
{
    /// <summary>
    /// Keşfedilen balıkları takip eder ve PlayerPrefs ile kalıcı saklar.
    /// Quiz sistemi bu listeyi kullanarak sadece öğrenilmiş balıklardan soru üretir.
    /// </summary>
    public class DiscoveredFishManager : MonoBehaviour
    {
        public static DiscoveredFishManager Instance { get; private set; }

        private const string PREFS_KEY = "DiscoveredFish";
        private HashSet<string> discoveredIds = new HashSet<string>();

        /// <summary>Yeni bir balık keşfedildiğinde tetiklenir.</summary>
        public event Action<FishData> OnFishDiscovered;

        /// <summary>Keşif sayısı değiştiğinde tetiklenir.</summary>
        public event Action<int> OnDiscoveryCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Oyun her basladiginda kesifleri sifirla
            // Ogrenci kartlari tekrar okutarak kesfetmeli
            discoveredIds.Clear();
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();
            Debug.Log("[Discovery] Oyun basladi — kesifler sifirlandi.");
        }

        /// <summary>
        /// Balığı keşfedilmiş olarak işaretler.
        /// Zaten keşfedilmişse false döner.
        /// </summary>
        public bool DiscoverFish(string fishId)
        {
            if (string.IsNullOrEmpty(fishId)) return false;
            if (discoveredIds.Contains(fishId)) return false;

            discoveredIds.Add(fishId);
            SaveDiscoveries();

            var fishData = FishDatabase.Instance?.GetFishById(fishId);
            if (fishData != null)
            {
                OnFishDiscovered?.Invoke(fishData);
            }
            OnDiscoveryCountChanged?.Invoke(discoveredIds.Count);

            Debug.Log($"[Discovery] Yeni balık keşfedildi: {fishId} (Toplam: {discoveredIds.Count})");
            return true;
        }

        /// <summary>Belirtilen balık daha önce keşfedilmiş mi?</summary>
        public bool IsFishDiscovered(string fishId)
        {
            return discoveredIds.Contains(fishId);
        }

        /// <summary>Keşfedilmiş balıkların FishData listesini döndürür.</summary>
        public List<FishData> GetDiscoveredFish()
        {
            var db = FishDatabase.Instance;
            if (db == null) return new List<FishData>();

            return discoveredIds
                .Select(id => db.GetFishById(id))
                .Where(f => f != null)
                .ToList();
        }

        /// <summary>Keşfedilmiş balık sayısı.</summary>
        public int DiscoveredCount => discoveredIds.Count;

        /// <summary>Tüm keşifleri sıfırlar.</summary>
        public void ResetDiscoveries()
        {
            discoveredIds.Clear();
            SaveDiscoveries();
            OnDiscoveryCountChanged?.Invoke(0);
            Debug.Log("[Discovery] Tüm keşifler sıfırlandı.");
        }

        private void SaveDiscoveries()
        {
            string data = string.Join(",", discoveredIds);
            PlayerPrefs.SetString(PREFS_KEY, data);
            PlayerPrefs.Save();
        }

        private void LoadDiscoveries()
        {
            string saved = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (var id in saved.Split(','))
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        discoveredIds.Add(id);
                    }
                }
            }
            Debug.Log($"[Discovery] {discoveredIds.Count} önceki keşif yüklendi.");
        }
    }
}
