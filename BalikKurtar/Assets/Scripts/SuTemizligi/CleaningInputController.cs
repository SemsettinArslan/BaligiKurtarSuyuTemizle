using UnityEngine;
using UnityEngine.InputSystem;

namespace BalikKurtar.SuTemizligi
{
    /// <summary>
    /// Yeni Input System ile mobil dokunma ve mouse input yonetimi.
    /// Pointer.current ile hem touch hem mouse birlesik sekilde yonetilir.
    /// Kameradan Raycast ile cop tespiti yapar,
    /// basili tutma suresini TrashItem'a iletir,
    /// kamera zoom efektini tetikler.
    /// </summary>
    public class CleaningInputController : MonoBehaviour
    {
        [Header("Referanslar")]
        [Tooltip("Ana kamera. Bos birakilirsa Camera.main kullanilir.")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("Kamera kontrol bileseni.")]
        [SerializeField] private CleaningCameraController cameraController;

        [Tooltip("UI bileseni (ilerleme bari icin).")]
        [SerializeField] private CleaningUI cleaningUI;

        [Header("Raycast Ayarlari")]
        [Tooltip("Raycast mesafesi")]
        [SerializeField] private float raycastDistance = 100f;

        [Tooltip("Sadece bu Layer'daki objelere tiklanabilir.")]
        [SerializeField] private LayerMask trashLayer = -1;

        [Header("Dokunma Ayarlari")]
        [Tooltip("Parmak kayma toleransi (piksel).")]
        [SerializeField] private float touchDragThreshold = 50f;

        private TrashItem currentTarget;
        private bool isHolding;
        private Vector2 touchStartPosition;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            var mgr = WaterCleaningManager.Instance;
            if (mgr == null || mgr.CurrentState != WaterCleaningManager.GameState.Playing)
                return;

            // Yeni Input System: Pointer hem touch hem mouse'u kapsar
            var pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                OnPointerDown(pointer.position.ReadValue());
            }
            else if (pointer.press.isPressed)
            {
                OnPointerHeld(pointer.position.ReadValue());
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                OnPointerUp();
            }

            // Aktif temizleme varsa ilerlemeyi guncelle
            if (isHolding && currentTarget != null)
                currentTarget.UpdateCleaning(Time.deltaTime);
        }

        // ==================== POINTER INPUT ====================

        private void OnPointerDown(Vector2 screenPos)
        {
            if (isHolding) return;

            touchStartPosition = screenPos;
            TrashItem trash = RaycastForTrash(screenPos);
            if (trash == null || trash.IsCompleted) return;

            currentTarget = trash;
            isHolding = true;

            currentTarget.StartCleaning();
            currentTarget.OnCleaningComplete += OnTargetComplete;

            if (cameraController != null)
                cameraController.ZoomToTarget(currentTarget.transform.position);

            if (cleaningUI != null)
                cleaningUI.ShowProgressBar(currentTarget);
        }

        private void OnPointerHeld(Vector2 screenPos)
        {
            if (!isHolding || currentTarget == null) return;

            float dist = Vector2.Distance(touchStartPosition, screenPos);
            if (dist > touchDragThreshold)
                CancelCurrent();
        }

        private void OnPointerUp()
        {
            if (!isHolding || currentTarget == null) return;
            if (!currentTarget.IsCompleted)
                CancelCurrent();
        }

        // ==================== CANCEL / COMPLETE ====================

        private void CancelCurrent()
        {
            if (currentTarget != null)
            {
                currentTarget.OnCleaningComplete -= OnTargetComplete;
                currentTarget.CancelCleaning();
            }

            if (cameraController != null) cameraController.ZoomBack();
            if (cleaningUI != null) cleaningUI.HideProgressBar();

            currentTarget = null;
            isHolding = false;
        }

        private void OnTargetComplete(TrashItem trash)
        {
            if (trash != currentTarget) return;

            currentTarget.OnCleaningComplete -= OnTargetComplete;

            if (cameraController != null) cameraController.ZoomBack();
            if (cleaningUI != null) cleaningUI.HideProgressBar();

            currentTarget = null;
            isHolding = false;
        }

        // ==================== RAYCAST ====================

        private TrashItem RaycastForTrash(Vector2 screenPos)
        {
            if (mainCamera == null) return null;

            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, trashLayer))
            {
                TrashItem t = hit.collider.GetComponent<TrashItem>();
                if (t == null)
                    t = hit.collider.GetComponentInParent<TrashItem>();
                return t;
            }
            return null;
        }
    }
}
