using UnityEngine;
using UnityEngine.InputSystem;

namespace BalikKurtar.AR
{
    /// <summary>
    /// Mobil dokunmatik ekranlar (veya editörde fare) için AR balık modelini döndürme ve büyütme/küçültme (Pinch Zoom) etkileşimini sağlar.
    /// Balık modeline veya Image Target altındaki etkileşim kurulacak container nesnesine eklenmelidir.
    /// </summary>
    public class FishInteractionHandler : MonoBehaviour
    {
        [Header("Rotasyon Ayarları")]
        [Tooltip("Döndürme hassasiyeti.")]
        [SerializeField] private float rotationSpeed = 0.3f;

        [Tooltip("Sadece sağa/sola döndürmeye izin ver (Tavsiye edilen: true. Balığın ters dönmesini önler).")]
        [SerializeField] private bool rotateYOnly = true;

        [Tooltip("Döndürme yumuşatılsın mı?")]
        [SerializeField] private bool smoothRotation = true;

        [Tooltip("Döndürme yumuşatma hızı.")]
        [SerializeField] private float rotationSmoothing = 8f;

        [Header("Ölçeklendirme (Büyütme/Küçültme) Ayarları")]
        [Tooltip("Orijinal boyuta göre minimum ölçek çarpanı.")]
        [SerializeField] private float minScaleMultiplier = 0.5f;

        [Tooltip("Orijinal boyuta göre maksimum ölçek çarpanı.")]
        [SerializeField] private float maxScaleMultiplier = 2.5f;

        [Tooltip("Pinch zoom (dokunmatik ekran) hassasiyeti.")]
        [SerializeField] private float pinchScaleSpeed = 0.005f;

        [Tooltip("Fare tekerleği (editör testi) hassasiyeti.")]
        [SerializeField] private float mouseScrollSpeed = 0.05f;

        [Tooltip("Ölçeklendirme yumuşatılsın mı?")]
        [SerializeField] private bool smoothScaling = true;

        [Tooltip("Ölçeklendirme yumuşatma hızı.")]
        [SerializeField] private float scaleSmoothing = 8f;

        private Vector3 initialScale;
        private Quaternion initialRotation;

        private Vector3 targetScale;
        private Quaternion targetRotation;

        // Sürükleme (Döndürme) Durumu
        private bool isDragging = false;
        private Vector2 previousPointerPosition;

        // Pinch Zoom Durumu
        private float previousPinchDistance = 0f;

        private Camera mainCamera;
        private Collider objectCollider;

        private void Start()
        {
            initialScale = transform.localScale;
            initialRotation = transform.localRotation;

            targetScale = initialScale;
            targetRotation = initialRotation;

            mainCamera = Camera.main;
            
            // Tıklamayı algılamak için collider'ı al
            objectCollider = GetComponent<Collider>();
            if (objectCollider == null)
            {
                objectCollider = GetComponentInChildren<Collider>();
            }
        }

        /// <summary>
        /// Balık kartı kameradan kaybolduğunda durumu orijinal haline sıfırlar.
        /// </summary>
        public void ResetInteraction()
        {
            targetScale = initialScale;
            targetRotation = initialRotation;

            if (!smoothRotation && !smoothScaling)
            {
                transform.localScale = initialScale;
                transform.localRotation = initialRotation;
            }

            isDragging = false;
            previousPinchDistance = 0f;
        }

        private void Update()
        {
            HandleInput();
            ApplyTransformations();
        }

        private void HandleInput()
        {
            var touchscreen = Touchscreen.current;
            var pointer = Pointer.current;

            // 1. PINCH ZOOM TESPİTİ (Dokunmatik Ekran - Çoklu Dokunma)
            if (touchscreen != null && touchscreen.touches.Count >= 2)
            {
                var t0 = touchscreen.touches[0];
                var t1 = touchscreen.touches[1];

                if (t0.isInProgress && t1.isInProgress)
                {
                    isDragging = false; // Zoom esnasında döndürmeyi durdur
                    HandlePinchZoom(t0.position.ReadValue(), t1.position.ReadValue());
                    return;
                }
            }
            
            // İki parmak bırakıldığında pinch mesafesini sıfırla
            previousPinchDistance = 0f;

            // 2. DÖNDÜRME TESPİTİ (Tek Dokunuş / Fare Sürükleme)
            if (pointer != null)
            {
                // Fare tekerleği ile ölçeklendirme (Editör kolaylığı için)
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    float scrollValue = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scrollValue) > 0.01f)
                    {
                        HandleScrollZoom(scrollValue);
                    }
                }

                // Ekrana basıldığında (Tıklama / Dokunma başlangıcı)
                if (pointer.press.wasPressedThisFrame)
                {
                    // Tıklanan konum balık modelinin üzerindeyse sürüklemeyi başlat
                    if (IsPointerOverObject(pointer.position.ReadValue()))
                    {
                        isDragging = true;
                        previousPointerPosition = pointer.position.ReadValue();
                    }
                }
                // Basılı tutulup sürüklendiğinde
                else if (pointer.press.isPressed && isDragging)
                {
                    Vector2 currentPos = pointer.position.ReadValue();
                    Vector2 delta = currentPos - previousPointerPosition;

                    if (mainCamera != null)
                    {
                        // Kameranın yukarı ve sağ yönlerini al
                        Vector3 cameraUp = mainCamera.transform.up;
                        Vector3 cameraRight = mainCamera.transform.right;

                        // Bu yönleri nesnenin parent yerel alanına dönüştür (Vuforia takibi bozulmasın diye)
                        Vector3 localUp = transform.parent != null ? transform.parent.InverseTransformDirection(cameraUp) : cameraUp;
                        Vector3 localRight = transform.parent != null ? transform.parent.InverseTransformDirection(cameraRight) : cameraRight;

                        float rotY = -delta.x * rotationSpeed;
                        Quaternion yawRotation = Quaternion.AngleAxis(rotY, localUp);

                        if (rotateYOnly)
                        {
                            targetRotation = yawRotation * targetRotation;
                        }
                        else
                        {
                            float rotX = delta.y * rotationSpeed;
                            Quaternion pitchRotation = Quaternion.AngleAxis(rotX, localRight);
                            targetRotation = pitchRotation * yawRotation * targetRotation;
                        }
                    }
                    else
                    {
                        // Kamera yoksa standart eksen rotasyonu yap
                        float rotY = -delta.x * rotationSpeed;
                        if (rotateYOnly)
                        {
                            targetRotation = Quaternion.Euler(0f, rotY, 0f) * targetRotation;
                        }
                        else
                        {
                            float rotX = delta.y * rotationSpeed;
                            targetRotation = Quaternion.Euler(rotX, rotY, 0f) * targetRotation;
                        }
                    }

                    previousPointerPosition = currentPos;
                }

                // Ekrandan parmak/tık kaldırıldığında
                if (pointer.press.wasReleasedThisFrame)
                {
                    isDragging = false;
                }
            }
        }

        private void HandlePinchZoom(Vector2 pos0, Vector2 pos1)
        {
            float currentDistance = Vector2.Distance(pos0, pos1);

            if (previousPinchDistance > 0f)
            {
                float delta = currentDistance - previousPinchDistance;
                float scaleAmount = delta * pinchScaleSpeed;

                Vector3 newScale = targetScale + Vector3.one * scaleAmount;

                // Ölçek sınırlarını koru
                float minS = initialScale.x * minScaleMultiplier;
                float maxS = initialScale.x * maxScaleMultiplier;
                newScale.x = Mathf.Clamp(newScale.x, minS, maxS);
                newScale.y = Mathf.Clamp(newScale.y, minS, maxS);
                newScale.z = Mathf.Clamp(newScale.z, minS, maxS);

                targetScale = newScale;
            }

            previousPinchDistance = currentDistance;
        }

        private void HandleScrollZoom(float scrollValue)
        {
            float scaleAmount = scrollValue * mouseScrollSpeed * 0.01f;
            Vector3 newScale = targetScale + Vector3.one * scaleAmount;

            // Ölçek sınırlarını koru
            float minS = initialScale.x * minScaleMultiplier;
            float maxS = initialScale.x * maxScaleMultiplier;
            newScale.x = Mathf.Clamp(newScale.x, minS, maxS);
            newScale.y = Mathf.Clamp(newScale.y, minS, maxS);
            newScale.z = Mathf.Clamp(newScale.z, minS, maxS);

            targetScale = newScale;
        }

        private bool IsPointerOverObject(Vector2 screenPosition)
        {
            if (mainCamera == null) return false;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Tıklanan nesne bu scriptin bağlı olduğu nesne mi veya alt nesnelerinden biri mi?
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyTransformations()
        {
            // Yumuşak döndürme
            if (smoothRotation)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSmoothing);
            }
            else
            {
                transform.localRotation = targetRotation;
            }

            // Yumuşak ölçekleme
            if (smoothScaling)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSmoothing);
            }
            else
            {
                transform.localScale = targetScale;
            }
        }
    }
}
