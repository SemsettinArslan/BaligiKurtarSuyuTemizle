using UnityEngine;
using UnityEngine.UI;
using BalikKurtar.Data;
using DG.Tweening;
using TMPro;

namespace BalikKurtar.UI
{
    /// <summary>
    /// AR objesi altında, balığın hemen yanında belirecek olan World Space bilgi paneli.
    /// Editör üzerinden tasarlanıp, FishCardHandler'a referans olarak verilmelidir.
    /// </summary>
    public class WorldSpaceFishInfo : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scientificNameText;
        [SerializeField] private TextMeshProUGUI habitatText;
        [SerializeField] private TextMeshProUGUI dietText;
        [SerializeField] private TextMeshProUGUI sizeText;
        [SerializeField] private TextMeshProUGUI funFactText;
        [SerializeField] private GameObject newBadge;
        [SerializeField] private Image accentBar;

        [Header("Animasyon Referansı")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform cardTransform;

        private Vector3 initialScale = Vector3.one;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (cardTransform == null) cardTransform = transform;
            
            if (cardTransform != null) initialScale = cardTransform.localScale;

            // Başlangıçta gizli başla
            gameObject.SetActive(false);
        }

        public void Show(FishData data, bool isNew)
        {
            if (data == null) return;

            // İçerik doldurma
            if (titleText != null)
            {
                titleText.text = data.displayName;
                titleText.color = data.themeColor;
            }
            if (scientificNameText != null) scientificNameText.text = $"<b>Bilimsel Adı:</b> {data.scientificName}";
            if (habitatText != null) habitatText.text = $"<b>Yaşam Alanı:</b> {data.habitat}";
            if (dietText != null) dietText.text = $"<b>Beslenme:</b> {data.diet}";
            if (sizeText != null) sizeText.text = $"<b>Boyutu:</b> {data.sizeInfo}";
            if (funFactText != null) funFactText.text = $"<b>Biliyor musun?:</b> {data.funFact}";
            
            if (accentBar != null) accentBar.color = data.themeColor;
            if (newBadge != null) newBadge.SetActive(isNew);

            // Görünür yap ve animasyon oynat
            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.4f);
            }

            if (cardTransform != null)
            {
                cardTransform.localScale = initialScale * 0.7f;
                cardTransform.DOScale(initialScale, 0.5f).SetEase(Ease.OutBack);
            }

            // Yeni keşif rozeti animasyonu
            if (isNew && newBadge != null)
            {
                newBadge.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 6).SetDelay(0.3f);
            }
        }

        public void Hide()
        {
            if (canvasGroup != null && cardTransform != null)
            {
                canvasGroup.DOFade(0f, 0.3f);
                cardTransform.DOScale(initialScale * 0.7f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
