using UnityEngine;
using DG.Tweening;

namespace BalikKurtar.SuTemizligi
{
    /// <summary>
    /// Kamera zoom efektini yonetir.
    /// Oyuncu bir cope basili tuttugunda kamera hafifce
    /// cope dogru yaklasir, islem bitince geri doner.
    /// </summary>
    public class CleaningCameraController : MonoBehaviour
    {
        [Header("Kamera Referansi")]
        [Tooltip("Kontrol edilecek kamera. Bos birakilirsa Camera.main kullanilir.")]
        [SerializeField] private Camera targetCamera;

        [Header("Zoom Ayarlari")]
        [Tooltip("Hedefe dogru yaklasma miktari (0-1 arasi, 0.2 = mesafenin %20'si)")]
        [SerializeField, Range(0.05f, 0.5f)] private float zoomAmount = 0.15f;

        [Tooltip("Zoom animasyon suresi (saniye)")]
        [SerializeField] private float zoomInDuration = 0.6f;

        [Tooltip("Geri donus animasyon suresi (saniye)")]
        [SerializeField] private float zoomOutDuration = 0.4f;

        [Header("Ortografik Kamera (Opsiyonel)")]
        [Tooltip("Ortografik kamerada zoom miktari (size azaltma)")]
        [SerializeField] private float orthoZoomReduction = 1.5f;

        // ==================== DURUM ====================

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float originalFOV;
        private float originalOrthoSize;
        private bool isZoomed;
        private Tween currentZoomTween;
        private Tween currentMoveTween;

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Start()
        {
            if (targetCamera != null)
            {
                SaveOriginalState();
            }
        }

        private void OnDestroy()
        {
            currentZoomTween?.Kill();
            currentMoveTween?.Kill();
        }

        // ==================== PUBLIC API ====================

        /// <summary>Kamerayi belirtilen hedefe dogru yaklastirir.</summary>
        public void ZoomToTarget(Vector3 targetPosition)
        {
            if (targetCamera == null || isZoomed) return;
            isZoomed = true;

            currentZoomTween?.Kill();
            currentMoveTween?.Kill();

            // Hedefe dogru pozisyon hesapla
            Vector3 direction = (targetPosition - originalPosition).normalized;
            float distance = Vector3.Distance(originalPosition, targetPosition);
            Vector3 zoomPosition = originalPosition + direction * (distance * zoomAmount);

            // Pozisyon animasyonu
            currentMoveTween = targetCamera.transform
                .DOMove(zoomPosition, zoomInDuration)
                .SetEase(Ease.OutSine);

            // FOV veya ortho size animasyonu
            if (targetCamera.orthographic)
            {
                currentZoomTween = DOTween.To(
                    () => targetCamera.orthographicSize,
                    x => targetCamera.orthographicSize = x,
                    originalOrthoSize - orthoZoomReduction,
                    zoomInDuration).SetEase(Ease.OutSine);
            }
        }

        /// <summary>Kamerayi orijinal konumuna geri dondurur.</summary>
        public void ZoomBack()
        {
            if (targetCamera == null || !isZoomed) return;
            isZoomed = false;

            currentZoomTween?.Kill();
            currentMoveTween?.Kill();

            // Pozisyon geri donus
            currentMoveTween = targetCamera.transform
                .DOMove(originalPosition, zoomOutDuration)
                .SetEase(Ease.InOutSine);

            // FOV veya ortho size geri donus
            if (targetCamera.orthographic)
            {
                currentZoomTween = DOTween.To(
                    () => targetCamera.orthographicSize,
                    x => targetCamera.orthographicSize = x,
                    originalOrthoSize,
                    zoomOutDuration).SetEase(Ease.InOutSine);
            }
        }

        // ==================== YARDIMCI ====================

        private void SaveOriginalState()
        {
            originalPosition = targetCamera.transform.position;
            originalRotation = targetCamera.transform.rotation;
            originalFOV = targetCamera.fieldOfView;
            originalOrthoSize = targetCamera.orthographicSize;
        }
    }
}
