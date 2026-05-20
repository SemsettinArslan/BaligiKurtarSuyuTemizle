using UnityEngine;

namespace BalikKurtar.Data
{
    /// <summary>
    /// Level tamamlama ekranında gösterilecek rastgele tebrik/teşekkür mesajlarını barındıran veri varlığı.
    /// </summary>
    [CreateAssetMenu(fileName = "CompletionMessagesData", menuName = "BalikKurtar/Completion Messages Data")]
    public class CompletionMessagesData : ScriptableObject
    {
        [Tooltip("Su temizliği tamamlandığında gösterilebilecek tebrik mesajları listesi.")]
        [TextArea(3, 5)]
        [SerializeField] private string[] messages = new string[]
        {
            "Tüm çöpleri temizledin! Su artık tertemiz ve balıklar güvende!",
            "Harika iş çıkardın! Deniz canlıları temiz su için sana minnettar!",
            "Denizi kirleticilerden arındırdın! Geleceğimiz artık daha mavi!",
            "Harika bir çevre koruyucususun! Ekosistemi kurtardın!"
        };

        /// <summary>
        /// Tanımlı tebrik mesajlarından rastgele birini seçip döndürür.
        /// </summary>
        public string GetRandomMessage()
        {
            if (messages == null || messages.Length == 0)
            {
                return "Tüm çöpleri temizledin! Su artık tertemiz!";
            }
            int randomIndex = Random.Range(0, messages.Length);
            return messages[randomIndex];
        }
    }
}
