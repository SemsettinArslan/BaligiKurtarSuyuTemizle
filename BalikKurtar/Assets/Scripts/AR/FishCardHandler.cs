using UnityEngine;
using Vuforia;
using BalikKurtar.Data;
using BalikKurtar.Managers;
using BalikKurtar.UI;

namespace BalikKurtar.AR
{
    /// <summary>
    /// Her Image Target üzerine eklenir.
    /// Kart tanındığında balık bilgisini koleksiyona ekler ve kendi altındaki World Space paneli gösterir.
    /// </summary>
    [RequireComponent(typeof(ObserverBehaviour))]
    public class FishCardHandler : MonoBehaviour
    {
        [Header("Balık Ayarları")]
        [Tooltip("Boş bırakılırsa Vuforia target adı kullanılır.")]
        [SerializeField] private string fishId;

        [Header("World Space UI")]
        [Tooltip("Bu hedefin altındaki (child) WorldSpaceFishInfo referansı")]
        [SerializeField] private WorldSpaceFishInfo localInfoPanel;

        [Header("Ses")]
        [Tooltip("Sesin çalınacağı AudioSource. Boş bırakılırsa bu objeye otomatik eklenir.")]
        [SerializeField] private AudioSource audioSource;

        private ObserverBehaviour observerBehaviour;
        private bool isCurrentlyTracked = false;

        private void Start()
        {
            observerBehaviour = GetComponent<ObserverBehaviour>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (observerBehaviour != null)
            {
                if (string.IsNullOrEmpty(fishId))
                {
                    fishId = observerBehaviour.TargetName;
                }

                observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
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
            bool tracked = status.Status == Status.TRACKED ||
                           status.Status == Status.EXTENDED_TRACKED;

            if (tracked && !isCurrentlyTracked)
            {
                isCurrentlyTracked = true;
                OnTargetFound(behaviour.TargetName);
            }
            else if (!tracked && isCurrentlyTracked)
            {
                isCurrentlyTracked = false;
                OnTargetLost();
            }
        }

        private void OnTargetFound(string detectedTargetName)
        {
            string lookupId = detectedTargetName;
            var fishData = FishDatabase.Instance?.GetFishById(lookupId);

            if (fishData == null && lookupId != fishId)
            {
                fishData = FishDatabase.Instance?.GetFishById(fishId);
            }

            if (fishData == null)
            {
                Debug.LogError($"[FishCard] BALIK VERISI BULUNAMADI! Algilanan target: {detectedTargetName}");
                return;
            }

            // Kesfi kaydet
            bool isNew = DiscoveredFishManager.Instance?.DiscoverFish(fishData.fishId) ?? false;

            // Kendi World Space panelimizi goster
            if (localInfoPanel != null)
            {
                localInfoPanel.Show(fishData, isNew);
            }
            else
            {
                Debug.LogWarning($"[FishCard] {gameObject.name} üzerinde localInfoPanel atanmamış!");
            }

            // Ses çal
            if (fishData.infoAudio != null && audioSource != null)
            {
                audioSource.clip = fishData.infoAudio;
                audioSource.Play();
            }
        }

        private void OnTargetLost()
        {
            // Hedef kaybolunca kendi panelimizi gizle
            if (localInfoPanel != null)
            {
                localInfoPanel.Hide();
            }

            // Hedef kaybolduğunda sesi durdur
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
