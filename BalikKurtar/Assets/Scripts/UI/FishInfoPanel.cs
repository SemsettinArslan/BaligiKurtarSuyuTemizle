using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BalikKurtar.Data;
using BalikKurtar.Managers;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Balik kart koleksiyonu + detay gorunumu.
    /// 
    /// Alt kisimda kesfedilen baliklarin kartlari FAN (yelpaze) seklinde dizilir.
    /// - Ortadaki kart duz durur.
    /// - Sol/saga gidildikce kartlar belirli aciyla doner.
    /// - Kart sayisina gore otomatik arc hesaplanir.
    /// 
    /// Karta tiklaninca detay ekrani animasyonlu acilir.
    /// </summary>
    public class FishInfoPanel : MonoBehaviour
    {
        // ===== FAN COLLECTION (alt kisim) =====
        private RectTransform fanContainer;
        private List<RectTransform> fanCards = new List<RectTransform>();
        private List<FishData> fanCardData = new List<FishData>();
        private Dictionary<string, int> cardIndexMap = new Dictionary<string, int>();
        private Text placeholderText;

        // ===== FAN AYARLARI =====
        private const float MAX_TOTAL_ANGLE = 50f;   // Maksimum yelpaze acisi (derece)
        private const float MAX_ANGLE_PER_CARD = 14f; // Kart basi maksimum aci
        private const float ARC_RADIUS = 1100f;       // Arc yaricapi (buyuk = daha subtle egri)
        private const float CARD_WIDTH = 125f;
        private const float CARD_HEIGHT = 160f;
        private const float FAN_BASE_Y = 20f;         // Kartlarin alt kenardan mesafesi

        // ===== DETAIL VIEW (overlay) =====
        private GameObject detailOverlay;
        private CanvasGroup detailCanvasGroup;
        private RectTransform detailCardRect;
        private Image detailAccentBar;
        private Text detailTitle;
        private Text detailScientific;
        private Text detailHabitat;
        private Text detailDiet;
        private Text detailSize;
        private Text detailFunFact;
        private GameObject detailNewBadge;

        private bool isDetailVisible = false;
        private Sequence currentAnimation;
        private Font cachedFont;

        // ===== RENKLER =====
        private static readonly Color DETAIL_OVERLAY_BG = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color DETAIL_CARD_BG = new Color(0.06f, 0.11f, 0.2f, 0.98f);
        private static readonly Color MINI_CARD_BG = new Color(0.1f, 0.16f, 0.26f, 0.97f);
        private static readonly Color LABEL_COLOR = new Color(0.5f, 0.75f, 1f);
        private static readonly Color VALUE_COLOR = new Color(0.88f, 0.9f, 0.93f);
        private static readonly Color BADGE_COLOR = new Color(1f, 0.42f, 0f);

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            // Root rect tam ekran kaplar
            var rootRect = GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            BuildFanContainer();
            BuildDetailOverlay();
            detailOverlay.SetActive(false);
        }

        // ==================== PUBLIC API ====================

        /// <summary>
        /// AR kart algilandiginda cagirilir.
        /// Koleksiyona ekler + detay acar.
        /// </summary>
        public void Show(FishData data, bool isNew)
        {
            if (data == null) return;
            AddToFan(data, animate: isNew);
            ShowDetail(data, isNew);
        }

        /// <summary>
        /// Detay gorunumunu acar.
        /// </summary>
        public void ShowDetail(FishData data, bool isNew = false)
        {
            if (data == null) return;

            // Icerik doldur
            detailTitle.text = data.displayName;
            detailScientific.text = data.scientificName;
            detailHabitat.text = data.habitat;
            detailDiet.text = data.diet;
            detailSize.text = data.sizeInfo;
            detailFunFact.text = data.funFact;

            // Tema rengi
            detailAccentBar.color = data.themeColor;
            detailTitle.color = data.themeColor;

            // Yeni kesif badge
            detailNewBadge.SetActive(isNew);

            if (!isDetailVisible)
            {
                detailOverlay.SetActive(true);
                isDetailVisible = true;

                currentAnimation?.Kill();
                detailCanvasGroup.alpha = 0;
                detailCardRect.localScale = Vector3.one * 0.6f;

                currentAnimation = DOTween.Sequence();
                currentAnimation.Append(detailCanvasGroup.DOFade(1f, 0.3f));
                currentAnimation.Join(detailCardRect.DOScale(1f, 0.45f).SetEase(Ease.OutBack));

                if (isNew && detailNewBadge.activeSelf)
                {
                    currentAnimation.Append(
                        detailNewBadge.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 6));
                }
            }
            else
            {
                detailCardRect.DOPunchScale(Vector3.one * 0.04f, 0.25f, 4);
            }
        }

        /// <summary>Detay gorunumunu kapatir.</summary>
        public void HideDetail()
        {
            if (!isDetailVisible) return;
            isDetailVisible = false;

            currentAnimation?.Kill();
            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(detailCardRect.DOScale(0.6f, 0.3f).SetEase(Ease.InBack));
            currentAnimation.Join(detailCanvasGroup.DOFade(0f, 0.28f));
            currentAnimation.OnComplete(() => detailOverlay.SetActive(false));
        }

        /// <summary>AR target kaybolunca — artik kapatmiyor.</summary>
        public void Hide(bool immediate = false)
        {
            // Bos — kullanici X ile kapatir
        }

        // ==================== FAN LAYOUT ====================

        private void AddToFan(FishData data, bool animate)
        {
            if (data == null || cardIndexMap.ContainsKey(data.fishId)) return;

            // Placeholder'i gizle
            if (placeholderText != null)
                placeholderText.gameObject.SetActive(false);

            CreateFanCard(data, animate);
            UpdateFanLayout(animate);
        }

        private void CreateFanCard(FishData data, bool animate)
        {
            var cardGO = new GameObject($"FanCard_{data.fishId}", typeof(RectTransform));
            cardGO.transform.SetParent(fanContainer, false);

            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

            // Pivot: alt-orta (fan rotasyonu icin kritik)
            cardRect.pivot = new Vector2(0.5f, 0f);

            // Anchor: container'in alt-ortasi
            cardRect.anchorMin = new Vector2(0.5f, 0f);
            cardRect.anchorMax = new Vector2(0.5f, 0f);

            // Kart arka plan
            var cardBg = cardGO.AddComponent<Image>();
            cardBg.color = MINI_CARD_BG;

            // === Tema rengi seridi (ust) ===
            var stripGO = CreateChild("Strip", cardRect);
            var stripRect = stripGO.GetComponent<RectTransform>();
            stripRect.anchorMin = new Vector2(0, 1);
            stripRect.anchorMax = new Vector2(1, 1);
            stripRect.pivot = new Vector2(0.5f, 1f);
            stripRect.sizeDelta = new Vector2(0, 8);
            var stripImg = stripGO.AddComponent<Image>();
            stripImg.color = data.themeColor;

            // === Alt ayirici cizgi ===
            var bottomLine = CreateChild("BottomLine", cardRect);
            var blRect = bottomLine.GetComponent<RectTransform>();
            blRect.anchorMin = new Vector2(0.1f, 0f);
            blRect.anchorMax = new Vector2(0.9f, 0f);
            blRect.pivot = new Vector2(0.5f, 0f);
            blRect.sizeDelta = new Vector2(0, 3);
            blRect.anchoredPosition = new Vector2(0, 3);
            var blImg = bottomLine.AddComponent<Image>();
            Color dimTheme = data.themeColor;
            dimTheme.a = 0.5f;
            blImg.color = dimTheme;

            // === Renkli daire (ust orta icinde) ===
            var circleGO = CreateChild("Circle", cardRect);
            var circleRect = circleGO.GetComponent<RectTransform>();
            circleRect.anchorMin = new Vector2(0.5f, 0.52f);
            circleRect.anchorMax = new Vector2(0.5f, 0.52f);
            circleRect.sizeDelta = new Vector2(52, 52);
            var circleImg = circleGO.AddComponent<Image>();
            Color lightTheme = data.themeColor;
            lightTheme.a = 0.28f;
            circleImg.color = lightTheme;

            // Daire ici bas harf
            string initials = GetInitials(data.displayName);
            var initialText = MakeText(circleRect, initials, 22, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            var initRect = initialText.GetComponent<RectTransform>();
            initRect.anchorMin = Vector2.zero;
            initRect.anchorMax = Vector2.one;
            initRect.offsetMin = Vector2.zero;
            initRect.offsetMax = Vector2.zero;

            // === Balik adi (alt) ===
            var nameText = MakeText(cardRect, data.displayName, 13, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.06f);
            nameRect.anchorMax = new Vector2(1, 0.36f);
            nameRect.offsetMin = new Vector2(4, 0);
            nameRect.offsetMax = new Vector2(-4, 0);

            // === Buton ===
            var button = cardGO.AddComponent<Button>();
            button.targetGraphic = cardBg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.2f);
            colors.pressedColor = new Color(0.85f, 0.88f, 0.95f);
            button.colors = colors;

            FishData capturedData = data;
            RectTransform capturedRect = cardRect;
            button.onClick.AddListener(() =>
            {
                // Tiklanan kart kucuk bir ziplama yapar
                capturedRect.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6);
                // Kucuk gecikme ile detay ac
                DOVirtual.DelayedCall(0.12f, () => ShowDetail(capturedData));
            });

            // Kaydet
            int idx = fanCards.Count;
            cardIndexMap[data.fishId] = idx;
            fanCards.Add(cardRect);
            fanCardData.Add(data);

            // Baslangic — gorunmez (animate edilecek)
            if (animate)
            {
                cardRect.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// Tum kartlari fan (yelpaze) seklinde yeniden konumlandirir.
        /// Ortadaki kart duz, sol/sag kartlar aciyla doner.
        /// </summary>
        private void UpdateFanLayout(bool animate)
        {
            int count = fanCards.Count;
            if (count == 0) return;

            // Aci hesapla
            float angleStep;
            if (count <= 1)
            {
                angleStep = 0f;
            }
            else
            {
                float totalAngle = Mathf.Min(MAX_TOTAL_ANGLE, MAX_ANGLE_PER_CARD * (count - 1));
                angleStep = totalAngle / (count - 1);
            }

            for (int i = 0; i < count; i++)
            {
                // normalizedIndex: ortadaki kart = 0, sol = negatif, sag = pozitif
                float normalizedIndex = i - (count - 1) / 2f;
                float cardAngle = normalizedIndex * angleStep; // derece

                // Arc pozisyon hesapla
                float rad = cardAngle * Mathf.Deg2Rad;
                float x = ARC_RADIUS * Mathf.Sin(rad);
                float y = ARC_RADIUS * (1f - Mathf.Cos(rad)) + FAN_BASE_Y;

                // Rotasyon: sol kartlar sola yatik (+Z), sag kartlar saga yatik (-Z)
                float rotation = -cardAngle;

                var card = fanCards[i];

                if (animate)
                {
                    card.DOAnchorPos(new Vector2(x, y), 0.5f).SetEase(Ease.OutBack);
                    card.DOLocalRotate(new Vector3(0, 0, rotation), 0.5f).SetEase(Ease.OutBack);
                    card.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(0.05f);
                }
                else
                {
                    card.anchoredPosition = new Vector2(x, y);
                    card.localEulerAngles = new Vector3(0, 0, rotation);
                    card.localScale = Vector3.one;
                }
            }

            // Z-order: kenar kartlar altta, merkez kartlar ustte
            ReorderCardSiblings();
        }

        /// <summary>
        /// Kartlarin sibling index'ini merkeze yakinliga gore ayarlar.
        /// Merkeze en yakin kart en ustte (en son renderlanir).
        /// </summary>
        private void ReorderCardSiblings()
        {
            int count = fanCards.Count;
            if (count <= 1) return;

            // Mesafe buyukten kucuge sirala (en uzak = en dusuk sibling = altta)
            var ordered = Enumerable.Range(0, count)
                .OrderByDescending(i => Mathf.Abs(i - (count - 1) / 2f))
                .ToList();

            for (int s = 0; s < ordered.Count; s++)
            {
                fanCards[ordered[s]].SetSiblingIndex(s);
            }
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Substring(0, Mathf.Min(2, name.Length)).ToUpper();
        }

        // ==================== UI BUILDING: FAN CONTAINER ====================

        private void BuildFanContainer()
        {
            var rootRect = GetComponent<RectTransform>();

            // Fan alani — alt kisim, arka plansiz (seffaf)
            var containerGO = CreateChild("FanContainer", rootRect);
            fanContainer = containerGO.GetComponent<RectTransform>();
            fanContainer.anchorMin = new Vector2(0, 0);
            fanContainer.anchorMax = new Vector2(1, 0);
            fanContainer.pivot = new Vector2(0.5f, 0f);
            fanContainer.sizeDelta = new Vector2(0, 220);

            // Cok hafif gradient benzeri arka plan (alt kisim)
            var bgGO = CreateChild("FanBg", fanContainer);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.55f);
            bgImg.raycastTarget = false; // Arkaplana tiklanmasin

            // Placeholder text
            placeholderText = MakeText(fanContainer,
                "Balik kartlarini kameraya okutarak kesfet!",
                16, FontStyle.Italic, new Color(0.35f, 0.45f, 0.55f), TextAnchor.MiddleCenter);
            var phRect = placeholderText.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
        }

        // ==================== UI BUILDING: DETAIL OVERLAY ====================

        private void BuildDetailOverlay()
        {
            var rootRect = GetComponent<RectTransform>();

            // Overlay arka plan
            detailOverlay = CreateChild("DetailOverlay", rootRect);
            var overlayRect = detailOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            detailCanvasGroup = detailOverlay.AddComponent<CanvasGroup>();

            var overlayBg = detailOverlay.AddComponent<Image>();
            overlayBg.color = DETAIL_OVERLAY_BG;

            // Overlay'e tikla → kapat
            var overlayBtn = detailOverlay.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayBg;
            var oColors = overlayBtn.colors;
            oColors.normalColor = Color.white;
            oColors.highlightedColor = Color.white;
            oColors.pressedColor = Color.white;
            overlayBtn.colors = oColors;
            overlayBtn.onClick.AddListener(HideDetail);

            // Detail card
            var cardGO = CreateChild("DetailCard", overlayRect);
            detailCardRect = cardGO.GetComponent<RectTransform>();
            detailCardRect.anchorMin = new Vector2(0.06f, 0.14f);
            detailCardRect.anchorMax = new Vector2(0.94f, 0.84f);
            detailCardRect.offsetMin = Vector2.zero;
            detailCardRect.offsetMax = Vector2.zero;

            var cardBg = cardGO.AddComponent<Image>();
            cardBg.color = DETAIL_CARD_BG;
            cardBg.raycastTarget = true;

            // Kart tiklamasini engelle (overlay'e gecmesin)
            var cardBlocker = cardGO.AddComponent<Button>();
            cardBlocker.targetGraphic = cardBg;
            var cbColors = cardBlocker.colors;
            cbColors.normalColor = Color.white;
            cbColors.highlightedColor = Color.white;
            cbColors.pressedColor = Color.white;
            cardBlocker.colors = cbColors;

            // Accent bar
            var accentGO = CreateChild("AccentBar", detailCardRect);
            var accentRect = accentGO.GetComponent<RectTransform>();
            detailAccentBar = accentGO.AddComponent<Image>();
            detailAccentBar.color = new Color(0.1f, 0.6f, 0.9f);
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(0, 7);

            // Close butonu
            var closeBtnGO = CreateChild("CloseBtn", detailCardRect);
            var closeBtnRect = closeBtnGO.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 1);
            closeBtnRect.anchorMax = new Vector2(1, 1);
            closeBtnRect.pivot = new Vector2(1, 1);
            closeBtnRect.anchoredPosition = new Vector2(-10, -14);
            closeBtnRect.sizeDelta = new Vector2(38, 38);

            var closeBtnImg = closeBtnGO.AddComponent<Image>();
            closeBtnImg.color = new Color(0.3f, 0.35f, 0.42f, 0.85f);

            var closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            closeBtn.onClick.AddListener(HideDetail);

            var closeTxt = MakeText(closeBtnRect, "X", 20, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            var closeTxtRect = closeTxt.GetComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.offsetMin = Vector2.zero;
            closeTxtRect.offsetMax = Vector2.zero;

            // Icerik alani
            var contentGO = CreateChild("Content", detailCardRect);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(24, 20);
            contentRect.offsetMax = new Vector2(-24, -18);

            float y = 0;

            // YENI KESIF badge
            detailNewBadge = CreateChild("NewBadge", contentRect);
            var badgeRect = detailNewBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0, 1);
            badgeRect.anchorMax = new Vector2(0, 1);
            badgeRect.pivot = new Vector2(0, 1);
            badgeRect.anchoredPosition = new Vector2(0, -y);
            badgeRect.sizeDelta = new Vector2(170, 30);
            var badgeBg = detailNewBadge.AddComponent<Image>();
            badgeBg.color = BADGE_COLOR;
            var badgeTxt = MakeText(badgeRect, "* YENI KESIF!", 13, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            var badgeTxtRect = badgeTxt.GetComponent<RectTransform>();
            badgeTxtRect.anchorMin = Vector2.zero;
            badgeTxtRect.anchorMax = Vector2.one;
            badgeTxtRect.offsetMin = Vector2.zero;
            badgeTxtRect.offsetMax = Vector2.zero;
            detailNewBadge.SetActive(false);
            y += 38;

            // Baslik
            detailTitle = MakeText(contentRect, "Balik Adi", 28, FontStyle.Bold,
                new Color(0.1f, 0.6f, 0.9f), TextAnchor.UpperLeft);
            SetAnchoredText(detailTitle, y, 36); y += 38;

            // Bilimsel ad
            detailScientific = MakeText(contentRect, "Bilimsel ad", 16, FontStyle.Italic,
                new Color(0.6f, 0.65f, 0.72f), TextAnchor.UpperLeft);
            SetAnchoredText(detailScientific, y, 22); y += 32;

            // Ayirici cizgi
            var divGO = CreateChild("Divider", contentRect);
            var divRect = divGO.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0, 1);
            divRect.anchorMax = new Vector2(1, 1);
            divRect.pivot = new Vector2(0, 1);
            divRect.anchoredPosition = new Vector2(0, -y);
            divRect.sizeDelta = new Vector2(0, 1);
            var divImg = divGO.AddComponent<Image>();
            divImg.color = new Color(0.25f, 0.32f, 0.42f, 0.5f);
            y += 14;

            // Bilgi bolumleri
            y = CreateInfoSection(contentRect, "YASAM ALANI", out detailHabitat, y);
            y = CreateInfoSection(contentRect, "BESLENME", out detailDiet, y);
            y = CreateInfoSection(contentRect, "BOYUT", out detailSize, y);
            y = CreateInfoSection(contentRect, "BILIYOR MUSUN?", out detailFunFact, y);
        }

        private float CreateInfoSection(RectTransform parent, string label,
            out Text valueOut, float yPos)
        {
            var labelText = MakeText(parent, label, 13, FontStyle.Bold,
                LABEL_COLOR, TextAnchor.UpperLeft);
            SetAnchoredText(labelText, yPos, 18);
            yPos += 20;

            valueOut = MakeText(parent, "-", 15, FontStyle.Normal,
                VALUE_COLOR, TextAnchor.UpperLeft);
            SetAnchoredText(valueOut, yPos, 45);
            var valRect = valueOut.GetComponent<RectTransform>();
            valRect.offsetMin = new Vector2(10, valRect.offsetMin.y);
            yPos += 48;

            return yPos;
        }

        // ==================== HELPERS ====================

        private void SetAnchoredText(Text text, float yPos, float height)
        {
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0, -yPos);
            rect.sizeDelta = new Vector2(0, height);
        }

        private GameObject CreateChild(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Font GetFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }

        private Text MakeText(RectTransform parent, string content, int fontSize,
            FontStyle style, Color color, TextAnchor alignment)
        {
            var go = CreateChild("Txt", parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.font = GetFont();
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void OnDestroy()
        {
            currentAnimation?.Kill();
        }
    }
}
