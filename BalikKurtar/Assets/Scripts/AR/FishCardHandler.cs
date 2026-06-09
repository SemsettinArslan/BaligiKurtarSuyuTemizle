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

        [Header("Gelişmiş Takip Ayarları")]
        [Tooltip("Genişletilmiş Takip (Extended Tracking) aktif olsun mu? " +
                 "Eğer kapatılırsa (tavsiye edilen), kamerayı karttan çektiğiniz anda balık, UI ve ses anında kaybolur.")]
        [SerializeField] private bool allowExtendedTracking = false;

        [Header("Balık Modeli")]
        [Tooltip("Döndürülebilen balık modeli veya wrapper konteyneri (Örn: Balik_Etkilesim). Kamerayı çekince anında kaybolması için buraya atanmalıdır.")]
        [SerializeField] private GameObject fishModelContainer;

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
            bool tracked = status.Status == Status.TRACKED;

            if (allowExtendedTracking)
            {
                tracked = tracked || status.Status == Status.EXTENDED_TRACKED;
            }

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

            // Balık modelini aktifleştir
            if (fishModelContainer != null)
            {
                fishModelContainer.SetActive(true);
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

            // Balık modelini gizle (Böylece Extended Tracking açık olsa bile anında kapanır)
            if (fishModelContainer != null)
            {
                fishModelContainer.SetActive(false);
            }

            // Hedef kaybolduğunda sesi durdur
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Etkileşim durumunu sıfırla (eğer varsa)
            var interactionHandlers = GetComponentsInChildren<FishInteractionHandler>(true);
            foreach (var handler in interactionHandlers)
            {
                handler.ResetInteraction();
            }
        }

        private void Update()
        {
            // Balık şu an ekranda tespit edilmiş durumdaysa tıklama kontrolü yap
            if (!isCurrentlyTracked) return;

            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                DetectClick(pointer.position.ReadValue());
            }
        }

        private void DetectClick(Vector2 screenPosition)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Tıklanan nesne bu Image Target'ın kendisi veya altındaki (child) çocuk nesnelerden biri mi?
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    Debug.Log($"<color=cyan>[FishCardHandler]</color> Okutulan balığa tıklandı! " +
                              $"<b>Balık ID:</b> {fishId}, <b>Tıklanan Obje:</b> {hit.transform.name}");
                }
            }
        }
    }
}
