using UnityEngine;

namespace BalikKurtar.Data
{
    /// <summary>
    /// SceneFader sırasında gösterilecek rastgele ipuçlarını tutar.
    /// </summary>
    [CreateAssetMenu(fileName = "New Loading Tips", menuName = "BalikKurtar/Loading Tips")]
    public class LoadingTipsData : ScriptableObject
    {
        [TextArea(2, 5)]
        public string[] tips;

        public string GetRandomTip()
        {
            if (tips == null || tips.Length == 0)
                return "";
            
            int index = Random.Range(0, tips.Length);
            return tips[index];
        }
    }
}
