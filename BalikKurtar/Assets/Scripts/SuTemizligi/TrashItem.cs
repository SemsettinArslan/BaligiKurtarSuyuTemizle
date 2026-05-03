using System;
using UnityEngine;
using DG.Tweening;

namespace BalikKurtar.SuTemizligi
{
    /// <summary>
    /// Her cop objesine eklenen bilesen.
    /// Suda yuzme animasyonu, temizleme ilerlemesi ve
    /// cop kutusuna ucus animasyonunu yonetir.
    /// 
    /// Gereksinimler:
    /// - Obje uzerinde Collider olmali (Raycast icin)
    /// - Tag: "Trash" olarak ayarlanmali
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrashItem : MonoBehaviour
    {
        [Header("Temizleme Ayarlari")]
        [Tooltip("Basili tutma suresi (saniye)")]
        [SerializeField] private float cleanDuration = 2.0f;

        [Tooltip("Cop kutusunun Transform'u (Inspector'dan atanir)")]
        [SerializeField] private Transform trashBinTarget;

        [Header("Suda Yuzme Animasyonu")]
        [Tooltip("Dikey sallanma mesafesi")]
        [SerializeField] private float bobAmount = 0.15f;

        [Tooltip("Sallanma suresi (saniye)")]
        [SerializeField] private float bobDuration = 1.8f;

        [Tooltip("Rotasyon sallanma acisi")]
        [SerializeField] private float rotateAmount = 8f;

        [Header("Temizleme Animasyonu")]
        [Tooltip("Temizleme sirasinda titresim gucu")]
        [SerializeField] private float shakeStrength = 0.05f;

        [Tooltip("Cop kutusuna ucus suresi")]
        [SerializeField] private float flyDuration = 0.8f;

        [Tooltip("Ziplama yuksekligi (cop kutusuna giderken)")]
        [SerializeField] private float jumpHeight = 3f;

        // ==================== DURUM ====================

        private float currentProgress;
        private bool isCleaning;
        private bool isCompleted;
        private Vector3 initialPosition;
        private Vector3 initialRotation;
        private Vector3 initialScale;

        // ==================== TWEEN REFERANSLARI ====================

        private Tween bobTween;
        private Tween rotateTween;
        private Tween shakeTween;
        private Sequence flySequence;

        // ==================== EVENTS ====================

        /// <summary>Ilerleme degistiginde: (0-1 arasi ilerleme)</summary>
        public event Action<float> OnProgressChanged;

        /// <summary>Temizleme tamamlandiginda.</summary>
        public event Action<TrashItem> OnCleaningComplete;

        // ==================== PROPERTIES ====================

        public float Progress => currentProgress;
        public float CleanDuration => cleanDuration;
        public bool IsCleaning => isCleaning;
        public bool IsCompleted => isCompleted;

        // ==================== LIFECYCLE ====================

        private void Start()
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localEulerAngles;
            initialScale = transform.localScale;

            StartIdleAnimation();
        }

        private void OnDisable()
        {
            // Obje deaktif olurken tum tweenler durdurulsun
            KillAllTweens();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        // ==================== SUDA YUZME ANIMASYONU ====================

        private void StartIdleAnimation()
        {
            // Dikey sallanma (yukari-asagi)
            bobTween = transform.DOLocalMoveY(
                    initialPosition.y + bobAmount, bobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject); // GameObject yok olursa tween otomatik kill olur

            // Hafif rotasyon
            rotateTween = transform.DOLocalRotate(
                    initialRotation + new Vector3(0, 0, rotateAmount), bobDuration * 1.3f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject); // GameObject yok olursa tween otomatik kill olur
        }

        private void StopIdleAnimation()
        {
            bobTween?.Kill();
            rotateTween?.Kill();
            bobTween = null;
            rotateTween = null;
        }

        // ==================== TEMIZLEME MEKANIGI ====================

        /// <summary>Temizleme islemini baslatir (oyuncu basili tutmaya basladiginda).</summary>
        public void StartCleaning()
        {
            if (isCompleted) return;

            isCleaning = true;

            // Hafif titresim efekti
            shakeTween?.Kill();
            shakeTween = transform.DOShakePosition(
                cleanDuration, shakeStrength, 20, 90, false, true, ShakeRandomnessMode.Harmonic)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        /// <summary>Temizleme ilerlemesini gunceller. Her frame cagrilmalidir.</summary>
        public void UpdateCleaning(float deltaTime)
        {
            if (!isCleaning || isCompleted) return;

            currentProgress += deltaTime / cleanDuration;
            currentProgress = Mathf.Clamp01(currentProgress);

            OnProgressChanged?.Invoke(currentProgress);

            if (currentProgress >= 1f)
            {
                CompleteCleaning();
            }
        }

        /// <summary>Temizleme islemini iptal eder (oyuncu parmagini kaldirinca).</summary>
        public void CancelCleaning()
        {
            if (!isCleaning || isCompleted) return;

            isCleaning = false;
            currentProgress = 0f;

            // Titresimi durdur
            shakeTween?.Kill();
            shakeTween = null;

            OnProgressChanged?.Invoke(0f);
        }

        /// <summary>Temizleme tamamlandiginda cop kutusuna ucus animasyonu baslatir.</summary>
        private void CompleteCleaning()
        {
            if (isCompleted) return;

            isCompleted = true;
            isCleaning = false;

            // Tum mevcut animasyonlari durdur
            StopIdleAnimation();
            shakeTween?.Kill();
            shakeTween = null;

            // Manager'a bildir
            OnCleaningComplete?.Invoke(this);

            // Cop kutusuna ucus animasyonu
            PlayFlyToTrashBinAnimation();
        }

        private void PlayFlyToTrashBinAnimation()
        {
            Vector3 targetPos = trashBinTarget != null
                ? trashBinTarget.position
                : transform.position + Vector3.up * 5f; // Fallback: sadece yukari git

            flySequence?.Kill();
            flySequence = DOTween.Sequence();

            // SetLink ile sequence'i GameObject'e bagla
            flySequence.SetLink(gameObject);

            // 1. Hazirlik: Kucuk bir scale punch (anticipation)
            flySequence.Append(
                transform.DOScale(initialScale * 0.85f, 0.15f)
                    .SetEase(Ease.InBack));

            // 2. Yukari ziplayarak cop kutusuna git
            flySequence.Append(
                transform.DOJump(targetPos, jumpHeight, 1, flyDuration)
                    .SetEase(Ease.InOutQuad));

            // 3. Yolda donerek gitsin
            flySequence.Join(
                transform.DORotate(new Vector3(0, 0, 360f), flyDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear));

            // 4. Cop kutusuna yaklasirken kuculson
            flySequence.Join(
                transform.DOScale(Vector3.zero, flyDuration * 0.8f)
                    .SetEase(Ease.InQuad)
                    .SetDelay(flyDuration * 0.2f));

            // 5. Tamamlandiginda objeyi deaktif et
            flySequence.OnComplete(() =>
            {
                // Manager'a copun tamamen gittigini bildir
                WaterCleaningManager.Instance?.ReportTrashCleaned(this);
                gameObject.SetActive(false);
            });
        }

        // ==================== YARDIMCI ====================

        private void KillAllTweens()
        {
            bobTween?.Kill();
            rotateTween?.Kill();
            shakeTween?.Kill();
            flySequence?.Kill();
            bobTween = null;
            rotateTween = null;
            shakeTween = null;
            flySequence = null;
        }
    }
}
