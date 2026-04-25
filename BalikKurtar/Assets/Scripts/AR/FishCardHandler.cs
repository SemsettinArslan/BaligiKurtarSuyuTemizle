using UnityEngine;
using Vuforia;
using BalikKurtar.Data;
using BalikKurtar.Managers;

namespace BalikKurtar.AR
{
    /// <summary>
    /// Her Image Target üzerine eklenir.
    /// Kart tanındığında balık bilgisini koleksiyona ekler ve detay gösterir.
    /// 
    /// ÖNEMLİ KURULUM:
    /// - Sahnede HER balık kartı için AYRI bir ImageTarget olmalı!
    /// - Örnek: "ImageTarget_JaponBaligi" (target=japon-baligi) ve 
    ///          "ImageTarget_KopekBaligi" (target=kopekBaligi)
    /// - İki target aynı database'i kullanabilir ama farklı target name'lere sahip olmalı.
    /// - fishId alanını boş bırakırsanız Vuforia target adından otomatik alınır.
    /// </summary>
    [RequireComponent(typeof(ObserverBehaviour))]
    public class FishCardHandler : MonoBehaviour
    {
        [Header("Balık Ayarları")]
        [Tooltip("Boş bırakılırsa Vuforia target adı kullanılır. " +
                 "FishData ScriptableObject'teki fishId ile birebir eşleşmeli!")]
        [SerializeField] private string fishId;

        private ObserverBehaviour observerBehaviour;
        private bool isCurrentlyTracked = false;

        private void Start()
        {
            observerBehaviour = GetComponent<ObserverBehaviour>();

            if (observerBehaviour != null)
            {
                // fishId boşsa target adından otomatik al
                if (string.IsNullOrEmpty(fishId))
                {
                    fishId = observerBehaviour.TargetName;
                }

                observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;

                Debug.Log($"[FishCard] Handler baslatildi: GO={gameObject.name} | " +
                          $"fishId={fishId} | vuforiaTarget={observerBehaviour.TargetName}");

                // fishId ile target name uyuşuyor mu kontrol et
                if (fishId != observerBehaviour.TargetName)
                {
                    Debug.LogWarning($"[FishCard] UYARI: fishId ({fishId}) ile Vuforia " +
                                     $"target adi ({observerBehaviour.TargetName}) farkli! " +
                                     "Bu bilerek mi yapildi?");
                }
            }
            else
            {
                Debug.LogError($"[FishCard] ObserverBehaviour bulunamadi: {gameObject.name}. " +
                               "Bu script bir ImageTarget uzerine eklenmelidir.");
            }
        }

        private void OnDestroy()
        {
            if (observerBehaviour != null)
            {
                observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
            }
        }

        private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
        {
            // Algilanan target adini logla (debug icin kritik)
            string detectedName = behaviour.TargetName;
            Debug.Log($"[FishCard] Status degisti: target={detectedName} | " +
                      $"status={status.Status} | info={status.StatusInfo} | " +
                      $"GO={gameObject.name}");

            bool tracked = status.Status == Status.TRACKED ||
                           status.Status == Status.EXTENDED_TRACKED;

            if (tracked && !isCurrentlyTracked)
            {
                isCurrentlyTracked = true;
                OnTargetFound(detectedName);
            }
            else if (!tracked && isCurrentlyTracked)
            {
                isCurrentlyTracked = false;
                OnTargetLost();
            }
        }

        private void OnTargetFound(string detectedTargetName)
        {
            // Gercek algilanan target adini kullan (Vuforia'nin dondurdugu)
            string lookupId = detectedTargetName;

            Debug.Log($"[FishCard] KART TANINDI: algilanan={detectedTargetName} | " +
                      $"beklenen fishId={fishId}");

            // Once algilanan target adi ile ara
            var fishData = FishDatabase.Instance?.GetFishById(lookupId);

            // Bulunamadiysa fishId ile dene
            if (fishData == null && lookupId != fishId)
            {
                fishData = FishDatabase.Instance?.GetFishById(fishId);
                if (fishData != null)
                {
                    Debug.LogWarning($"[FishCard] Target adi '{lookupId}' ile bulunamadi, " +
                                     $"fishId '{fishId}' ile bulundu. " +
                                     "Vuforia target adi ile ScriptableObject fishId eslesmiyor olabilir.");
                }
            }

            if (fishData == null)
            {
                Debug.LogError($"[FishCard] BALIK VERISI BULUNAMADI!\n" +
                               $"  Algilanan target: {detectedTargetName}\n" +
                               $"  FishId: {fishId}\n" +
                               $"  Cozum: Resources/FishData/ klasorunde fishId alan " +
                               $"'{lookupId}' veya '{fishId}' olan bir ScriptableObject olusturun.");
                return;
            }

            // Kesfi kaydet
            bool isNew = DiscoveredFishManager.Instance?.DiscoverFish(fishData.fishId) ?? false;

            // Bilgi panelini goster
            var gm = Core.GameManager.Instance;
            if (gm != null && gm.FishInfoPanel != null)
            {
                gm.FishInfoPanel.Show(fishData, isNew);
            }
        }

        private void OnTargetLost()
        {
            Debug.Log($"[FishCard] Kart kayboldu: {fishId}");
            // Artik target kaybolunca panel kapatilmiyor.
            // Kullanici kart koleksiyonundan gezmeye devam edebilir.
        }
    }
}
