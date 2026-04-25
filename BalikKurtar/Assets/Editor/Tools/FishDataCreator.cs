#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BalikKurtar.Data;

namespace BalikKurtar.Editor
{
    /// <summary>
    /// Örnek balık verisi ScriptableObject'lerini otomatik oluşturur.
    /// Unity menüsünden: BalikKurtar > Örnek Balık Verilerini Oluştur
    /// </summary>
    public static class FishDataCreator
    {
        private const string BASE_PATH = "Assets/Resources/FishData";

        [MenuItem("BalikKurtar/\u00d6rnek Bal\u0131k Verilerini Olu\u015ftur")]
        public static void CreateSampleFishData()
        {
            // Klasörü oluştur
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(BASE_PATH))
                AssetDatabase.CreateFolder("Assets/Resources", "FishData");

            // Japon Balığı
            CreateFishAsset(
                fileName: "JaponBaligi",
                fishId: "japon-baligi",
                displayName: "Japon Bal\u0131\u011f\u0131",
                scientificName: "Carassius auratus",
                habitat: "Tatl\u0131 su \u2014 g\u00f6letler, nehirler ve akvaryumlar. Do\u011fu Asya k\u00f6kenli olup d\u00fcnya genelinde evcil olarak yeti\u015ftirilir.",
                diet: "Omnivor \u2014 yosun, k\u00fc\u00e7\u00fck b\u00f6cekler, plankton ve pul yem ile beslenir.",
                funFact: "Japon bal\u0131klar\u0131n\u0131n haf\u0131zas\u0131n\u0131n 3 saniye oldu\u011fu efsane yanl\u0131\u015ft\u0131r! Ara\u015ft\u0131rmalar aylar boyunca hat\u0131rlayabildiklerini g\u00f6stermi\u015ftir.",
                sizeInfo: "10-30 cm",
                themeColor: new Color(1f, 0.6f, 0.1f, 1f) // Turuncu-altın
            );

            // Köpek Balığı
            CreateFishAsset(
                fileName: "KopekBaligi",
                fishId: "kopekBaligi",
                displayName: "K\u00f6pek Bal\u0131\u011f\u0131",
                scientificName: "Selachimorpha",
                habitat: "T\u00fcm okyanuslar \u2014 s\u0131\u011f k\u0131y\u0131lardan derin denizlere kadar geni\u015f bir alanda ya\u015far. Baz\u0131 t\u00fcrler tatl\u0131 suda da bulunur.",
                diet: "Karnivor \u2014 bal\u0131klar, m\u00fcrekkep bal\u0131klar\u0131, fok, kabuklular ve plankton ile beslenir (t\u00fcre g\u00f6re de\u011fi\u015fir).",
                funFact: "K\u00f6pek bal\u0131klar\u0131n\u0131n iskeleti tamamen k\u0131k\u0131rdaktan olu\u015fur, hi\u00e7 kemikleri yoktur! Ayr\u0131ca di\u015fleri \u00f6m\u00fcr boyunca yenilenir.",
                sizeInfo: "T\u00fcre g\u00f6re 20 cm \u2013 12 m",
                themeColor: new Color(0.2f, 0.4f, 0.7f, 1f) // Koyu mavi
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FishDataCreator] \u00d6rnek bal\u0131k verileri olu\u015fturuldu: " + BASE_PATH);
            EditorUtility.DisplayDialog(
                "Bal\u0131k Verileri",
                "2 \u00f6rnek bal\u0131k verisi ba\u015far\u0131yla olu\u015fturuldu!\n\n" +
                "Konum: Resources/FishData/\n\n" +
                "\u2022 Japon Bal\u0131\u011f\u0131 (japon-baligi)\n" +
                "\u2022 K\u00f6pek Bal\u0131\u011f\u0131 (kopekBaligi)\n\n" +
                "Yeni bal\u0131k eklemek i\u00e7in:\nCreate > BalikKurtar > Fish Data",
                "Tamam"
            );
        }

        private static void CreateFishAsset(
            string fileName,
            string fishId,
            string displayName,
            string scientificName,
            string habitat,
            string diet,
            string funFact,
            string sizeInfo,
            Color themeColor)
        {
            string path = $"{BASE_PATH}/{fileName}.asset";

            // Zaten varsa üzerine yaz
            var existing = AssetDatabase.LoadAssetAtPath<FishData>(path);
            if (existing != null)
            {
                existing.fishId = fishId;
                existing.displayName = displayName;
                existing.scientificName = scientificName;
                existing.habitat = habitat;
                existing.diet = diet;
                existing.funFact = funFact;
                existing.sizeInfo = sizeInfo;
                existing.themeColor = themeColor;
                EditorUtility.SetDirty(existing);
                Debug.Log($"[FishDataCreator] Güncellendi: {fileName}");
                return;
            }

            var fishData = ScriptableObject.CreateInstance<FishData>();
            fishData.fishId = fishId;
            fishData.displayName = displayName;
            fishData.scientificName = scientificName;
            fishData.habitat = habitat;
            fishData.diet = diet;
            fishData.funFact = funFact;
            fishData.sizeInfo = sizeInfo;
            fishData.themeColor = themeColor;

            AssetDatabase.CreateAsset(fishData, path);
            Debug.Log($"[FishDataCreator] Oluşturuldu: {fileName}");
        }

        [MenuItem("BalikKurtar/Ke\u015fifleri S\u0131f\u0131rla (Test)")]
        public static void ResetDiscoveries()
        {
            PlayerPrefs.DeleteKey("DiscoveredFish");
            PlayerPrefs.Save();
            Debug.Log("[FishDataCreator] Tüm keşifler sıfırlandı (PlayerPrefs).");
            EditorUtility.DisplayDialog(
                "Ke\u015fifler S\u0131f\u0131rland\u0131",
                "T\u00fcm bal\u0131k ke\u015fifleri silindi.\nBir sonraki Play'de s\u0131f\u0131rdan ba\u015flayacak.",
                "Tamam"
            );
        }
    }
}
#endif
