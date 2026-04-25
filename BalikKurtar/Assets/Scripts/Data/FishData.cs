using UnityEngine;

namespace BalikKurtar.Data
{
    /// <summary>
    /// Her balık türü için veri tanımı.
    /// fishId alanı Vuforia Image Target adıyla birebir eşleşmelidir.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFishData", menuName = "BalikKurtar/Fish Data")]
    public class FishData : ScriptableObject
    {
        [Header("Tanımlama")]
        [Tooltip("Vuforia Image Target adıyla birebir eşleşmeli (ör: japon-baligi, kopekBaligi)")]
        public string fishId;

        [Tooltip("Ekranda gösterilecek Türkçe isim")]
        public string displayName;

        [Tooltip("Latince bilimsel ad")]
        public string scientificName;

        [Header("Bilgiler")]
        [TextArea(2, 5)]
        [Tooltip("Yaşam alanı açıklaması")]
        public string habitat;

        [TextArea(2, 5)]
        [Tooltip("Beslenme bilgisi")]
        public string diet;

        [TextArea(2, 5)]
        [Tooltip("İlginç bilgi / Biliyor musun?")]
        public string funFact;

        [Tooltip("Boy/boyut bilgisi (ör: 10-30 cm)")]
        public string sizeInfo;

        [Header("Görsel")]
        [Tooltip("Balık görseli (opsiyonel)")]
        public Sprite fishImage;

        [Tooltip("Bu balık için UI tema rengi")]
        public Color themeColor = new Color(0.1f, 0.6f, 0.9f, 1f);
    }
}
