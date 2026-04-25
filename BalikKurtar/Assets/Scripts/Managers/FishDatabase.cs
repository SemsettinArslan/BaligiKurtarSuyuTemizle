using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BalikKurtar.Data;

namespace BalikKurtar.Managers
{
    /// <summary>
    /// Tüm balık verilerini Resources/FishData klasöründen yükler
    /// ve fishId ile erişim sağlar.
    /// </summary>
    public class FishDatabase : MonoBehaviour
    {
        public static FishDatabase Instance { get; private set; }

        private Dictionary<string, FishData> fishLookup = new Dictionary<string, FishData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadAllFishData();
        }

        private void LoadAllFishData()
        {
            var allFish = Resources.LoadAll<FishData>("FishData");

            foreach (var fish in allFish)
            {
                if (!string.IsNullOrEmpty(fish.fishId))
                {
                    fishLookup[fish.fishId] = fish;
                }
                else
                {
                    Debug.LogWarning($"[FishDatabase] Boş fishId olan veri atlandı: {fish.name}");
                }
            }

            Debug.Log($"[FishDatabase] {fishLookup.Count} balık verisi yüklendi.");
        }

        /// <summary>Vuforia target adına göre balık verisini döndürür.</summary>
        public FishData GetFishById(string id)
        {
            if (fishLookup.TryGetValue(id, out var data))
                return data;

            Debug.LogWarning($"[FishDatabase] '{id}' için balık verisi bulunamadı.");
            return null;
        }

        /// <summary>Tüm kayıtlı balık verilerini döndürür.</summary>
        public List<FishData> GetAllFish()
        {
            return fishLookup.Values.ToList();
        }

        /// <summary>Kayıtlı balık sayısı.</summary>
        public int Count => fishLookup.Count;
    }
}
