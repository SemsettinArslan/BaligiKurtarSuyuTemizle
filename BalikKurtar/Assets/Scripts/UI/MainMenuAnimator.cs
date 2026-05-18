using UnityEngine;
using DG.Tweening;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Ana menü UI animasyonlarını yönetir.
    /// Başlık float efekti, buton stagger girişi ve panel açma/kapama animasyonları.
    /// Inspector'dan referanslar atanmalıdır.
    /// </summary>
    public class MainMenuAnimator : MonoBehaviour
    {
        [Header("Başlık Animasyonu")]
        [Tooltip("Başlık transform'u (float efekti için)")]
        [SerializeField] private RectTransform titleTransform;

        [Tooltip("Float mesafesi (piksel)")]
        [SerializeField] private float titleFloatAmount = 12f;

        [Tooltip("Float süresi (saniye)")]
        [SerializeField] private float titleFloatDuration = 2f;

        [Header("Alt Başlık")]
        [SerializeField] private CanvasGroup subtitleCanvasGroup;

        [Header("Buton Animasyonları")]
        [Tooltip("Sıralı giriş yapacak butonlar (üstten alta sırayla)")]
        [SerializeField] private RectTransform[] menuButtons;

        [Tooltip("Buton giriş animasyon süresi")]
        [SerializeField] private float buttonEnterDuration = 0.5f;

        [Tooltip("Butonlar arası gecikme")]
        [SerializeField] private float buttonStaggerDelay = 0.1f;

        [Tooltip("Butonların başlangıç X offset'i (sola kayma)")]
        [SerializeField] private float buttonSlideOffset = -300f;

        [Header("Panel Animasyonları")]
        [Tooltip("Panel açma süresi")]
        [SerializeField] private float panelOpenDuration = 0.4f;

        [Tooltip("Panel kapama süresi")]
        [SerializeField] private float panelCloseDuration = 0.3f;

        [Header("Versiyon Metni")]
        [SerializeField] private CanvasGroup versionTextGroup;

        // ==================== DURUM ====================

        private Vector2[] buttonOriginalPositions;
        private Tween titleFloatTween;
        private Sequence entrySequence;

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            // Butonların ekrana girmeden önce tamamen gizli olması için (Eğer CanvasGroup yoksa ekliyoruz)
            if (menuButtons != null)
            {
                foreach (var btn in menuButtons)
                {
                    if (btn == null) continue;
                    var cg = btn.GetComponent<CanvasGroup>();
                    if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                }
            }
        }

        private void Start()
        {
            CacheButtonPositions();
            PlayEntryAnimation();
        }

        private void OnDestroy()
        {
            titleFloatTween?.Kill();
            entrySequence?.Kill();
        }

        // ==================== GİRİŞ ANİMASYONU ====================

        /// <summary>
        /// Menü açıldığında tüm giriş animasyonlarını başlatır.
        /// </summary>
        public void PlayEntryAnimation()
        {
            entrySequence?.Kill();
            entrySequence = DOTween.Sequence();

            // 1. Başlık — yukarıdan kayarak gelir
            if (titleTransform != null)
            {
                Vector2 titleOrigPos = titleTransform.anchoredPosition;
                titleTransform.anchoredPosition = titleOrigPos + new Vector2(0, 80f);

                entrySequence.Append(
                    titleTransform.DOAnchorPos(titleOrigPos, 0.7f)
                        .SetEase(Ease.OutBack)
                );
            }

            // 2. Alt başlık fade-in
            if (subtitleCanvasGroup != null)
            {
                subtitleCanvasGroup.alpha = 0f;
                entrySequence.Append(
                    subtitleCanvasGroup.DOFade(1f, 0.4f)
                );
            }

            // 3. Butonlar — soldan kayarak sırayla girer
            if (menuButtons != null && menuButtons.Length > 0)
            {
                for (int i = 0; i < menuButtons.Length; i++)
                {
                    if (menuButtons[i] == null) continue;

                    var btn = menuButtons[i];
                    Vector2 origPos = buttonOriginalPositions[i];

                    // Başlangıç: sola kayık ve görünmez
                    btn.anchoredPosition = origPos + new Vector2(buttonSlideOffset, 0);
                    var cg = btn.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 0f;

                    float delay = i * buttonStaggerDelay;

                    // Pozisyon animasyonu
                    entrySequence.Insert(0.6f + delay,
                        btn.DOAnchorPos(origPos, buttonEnterDuration)
                            .SetEase(Ease.OutCubic)
                    );

                    // Fade-in
                    if (cg != null)
                    {
                        entrySequence.Insert(0.6f + delay,
                            cg.DOFade(1f, buttonEnterDuration * 0.7f)
                        );
                    }
                }
            }

            // 4. Versiyon metni
            if (versionTextGroup != null)
            {
                versionTextGroup.alpha = 0f;
                entrySequence.Insert(1.2f,
                    versionTextGroup.DOFade(1f, 0.5f)
                );
            }

            // 5. Giriş tamamlandıktan sonra başlık float efektini başlat
            entrySequence.OnComplete(() => StartTitleFloat());
        }

        // ==================== BAŞLIK FLOAT EFEKTİ ====================

        /// <summary>
        /// Başlığın sürekli yukarı-aşağı hareket etmesini sağlar.
        /// </summary>
        private void StartTitleFloat()
        {
            if (titleTransform == null) return;

            titleFloatTween?.Kill();

            Vector2 basePos = titleTransform.anchoredPosition;
            titleFloatTween = titleTransform
                .DOAnchorPosY(basePos.y + titleFloatAmount, titleFloatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        // ==================== PANEL ANİMASYONLARI ====================

        /// <summary>
        /// Bir paneli animasyonlu şekilde açar.
        /// Panel'de CanvasGroup olmalıdır.
        /// </summary>
        public void OpenPanel(CanvasGroup panel, RectTransform panelRect)
        {
            if (panel == null) return;

            panel.gameObject.SetActive(true);
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;

            Sequence seq = DOTween.Sequence();

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one * 0.85f;
                seq.Append(panel.DOFade(1f, panelOpenDuration));
                seq.Join(panelRect.DOScale(Vector3.one, panelOpenDuration).SetEase(Ease.OutBack));
            }
            else
            {
                seq.Append(panel.DOFade(1f, panelOpenDuration));
            }

            seq.OnComplete(() =>
            {
                panel.interactable = true;
                panel.blocksRaycasts = true;
            });
        }

        /// <summary>
        /// Bir paneli animasyonlu şekilde kapatır.
        /// </summary>
        public void ClosePanel(CanvasGroup panel, RectTransform panelRect)
        {
            if (panel == null || !panel.gameObject.activeSelf) return;

            panel.interactable = false;
            panel.blocksRaycasts = false;

            Sequence seq = DOTween.Sequence();

            if (panelRect != null)
            {
                seq.Append(panel.DOFade(0f, panelCloseDuration));
                seq.Join(panelRect.DOScale(Vector3.one * 0.85f, panelCloseDuration).SetEase(Ease.InBack));
            }
            else
            {
                seq.Append(panel.DOFade(0f, panelCloseDuration));
            }

            seq.OnComplete(() => panel.gameObject.SetActive(false));
        }

        // ==================== BUTON HOVER EFEKTİ ====================

        /// <summary>
        /// Buton üzerine gelince scale punch efekti uygular.
        /// EventTrigger veya Button event'inden çağrılabilir.
        /// </summary>
        public void PunchButton(RectTransform button)
        {
            if (button == null) return;
            button.DOPunchScale(Vector3.one * 0.08f, 0.25f, 8, 0.5f);
        }

        // ==================== YARDIMCI ====================

        private void CacheButtonPositions()
        {
            if (menuButtons == null) return;

            buttonOriginalPositions = new Vector2[menuButtons.Length];
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] != null)
                    buttonOriginalPositions[i] = menuButtons[i].anchoredPosition;
            }
        }
    }
}
