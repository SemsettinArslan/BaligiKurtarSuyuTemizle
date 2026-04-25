using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BalikKurtar.Data;

namespace BalikKurtar.Managers
{
    /// <summary>Soru kategorileri</summary>
    public enum QuestionCategory
    {
        Habitat,
        Diet,
        FunFact,
        ScientificName,
        Size,
        Identification
    }

    /// <summary>Bir quiz sorusunun tüm bilgilerini tutar.</summary>
    [Serializable]
    public class QuizQuestion
    {
        public string questionText;
        public string correctAnswer;
        public List<string> allOptions; // doğru cevap dahil, karışık
        public FishData relatedFish;
        public QuestionCategory category;
    }

    /// <summary>
    /// Quiz oyun mantığı — sadece keşfedilmiş balıklardan otomatik soru üretir.
    /// Keşfedilmemiş balıklardan asla soru gelmez.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        public static QuizManager Instance { get; private set; }

        [Header("Quiz Ayarları")]
        [SerializeField] private int questionsPerQuiz = 10;
        [SerializeField] private int minDiscoveredFish = 2;

        private List<QuizQuestion> currentQuestions;
        private int currentQuestionIndex;
        private int score;
        private int correctCount;
        private int wrongCount;

        // Events — UI bu event'lere abone olur
        /// <summary>Yeni soru hazır olduğunda: (soru, index, toplam)</summary>
        public event Action<QuizQuestion, int, int> OnQuestionReady;

        /// <summary>Cevap sonucu: (doğru mu, doğru cevap)</summary>
        public event Action<bool, string> OnAnswerResult;

        /// <summary>Quiz tamamlandığında: (skor, doğru sayı, yanlış sayı)</summary>
        public event Action<int, int, int> OnQuizComplete;

        public int Score => score;
        public int CorrectCount => correctCount;
        public int WrongCount => wrongCount;
        public int TotalQuestions => currentQuestions?.Count ?? 0;
        public int CurrentIndex => currentQuestionIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Quiz başlatılabilir mi? (yeterli keşif var mı)</summary>
        public bool CanStartQuiz()
        {
            return DiscoveredFishManager.Instance != null &&
                   DiscoveredFishManager.Instance.DiscoveredCount >= minDiscoveredFish;
        }

        /// <summary>Quiz başlatmak için gereken minimum balık sayısı.</summary>
        public int GetMinRequired() => minDiscoveredFish;

        /// <summary>Yeni bir quiz başlatır.</summary>
        public void StartQuiz()
        {
            if (!CanStartQuiz())
            {
                Debug.LogWarning($"[Quiz] En az {minDiscoveredFish} balık keşfedilmeli! " +
                                 $"Şu an: {DiscoveredFishManager.Instance?.DiscoveredCount ?? 0}");
                return;
            }

            score = 0;
            correctCount = 0;
            wrongCount = 0;
            currentQuestionIndex = 0;
            currentQuestions = GenerateQuestions();

            Debug.Log($"[Quiz] {currentQuestions.Count} soru ile quiz başlatıldı.");

            if (currentQuestions.Count > 0)
            {
                OnQuestionReady?.Invoke(currentQuestions[0], 0, currentQuestions.Count);
            }
        }

        /// <summary>Kullanıcının seçtiği cevabı değerlendirir.</summary>
        public void SubmitAnswer(string answer)
        {
            if (currentQuestions == null || currentQuestionIndex >= currentQuestions.Count)
                return;

            var question = currentQuestions[currentQuestionIndex];
            bool isCorrect = answer == question.correctAnswer;

            if (isCorrect)
            {
                score += 10;
                correctCount++;
            }
            else
            {
                wrongCount++;
            }

            OnAnswerResult?.Invoke(isCorrect, question.correctAnswer);
        }

        /// <summary>Bir sonraki soruya geçer veya quiz'i bitirir.</summary>
        public void NextQuestion()
        {
            currentQuestionIndex++;

            if (currentQuestionIndex < currentQuestions.Count)
            {
                OnQuestionReady?.Invoke(
                    currentQuestions[currentQuestionIndex],
                    currentQuestionIndex,
                    currentQuestions.Count
                );
            }
            else
            {
                OnQuizComplete?.Invoke(score, correctCount, wrongCount);
            }
        }

        // ==================== SORU ÜRETİMİ ====================

        private List<QuizQuestion> GenerateQuestions()
        {
            var discoveredFish = DiscoveredFishManager.Instance.GetDiscoveredFish();
            var questions = new List<QuizQuestion>();

            foreach (var fish in discoveredFish)
            {
                // Habitat sorusu
                questions.Add(CreateQuestion(
                    $"{fish.displayName} nerede yaşar?",
                    fish.habitat,
                    discoveredFish.Where(f => f != fish).Select(f => f.habitat).ToList(),
                    fish,
                    QuestionCategory.Habitat
                ));

                // Beslenme sorusu
                questions.Add(CreateQuestion(
                    $"{fish.displayName} ne ile beslenir?",
                    fish.diet,
                    discoveredFish.Where(f => f != fish).Select(f => f.diet).ToList(),
                    fish,
                    QuestionCategory.Diet
                ));

                // Bilimsel ad sorusu
                if (!string.IsNullOrEmpty(fish.scientificName))
                {
                    questions.Add(CreateQuestion(
                        $"{fish.displayName} balığının bilimsel adı nedir?",
                        fish.scientificName,
                        discoveredFish.Where(f => f != fish).Select(f => f.scientificName).ToList(),
                        fish,
                        QuestionCategory.ScientificName
                    ));
                }

                // İlginç bilgi → hangi balık sorusu
                if (!string.IsNullOrEmpty(fish.funFact))
                {
                    questions.Add(CreateQuestion(
                        $"Aşağıdaki bilgi hangi balığa aittir?\n\"{fish.funFact}\"",
                        fish.displayName,
                        discoveredFish.Where(f => f != fish).Select(f => f.displayName).ToList(),
                        fish,
                        QuestionCategory.FunFact
                    ));
                }

                // Boy sorusu
                if (!string.IsNullOrEmpty(fish.sizeInfo))
                {
                    questions.Add(CreateQuestion(
                        $"Hangi balık {fish.sizeInfo} boyutlarındadır?",
                        fish.displayName,
                        discoveredFish.Where(f => f != fish).Select(f => f.displayName).ToList(),
                        fish,
                        QuestionCategory.Size
                    ));
                }
            }

            // Karıştır ve sınırla
            Shuffle(questions);
            return questions.Take(questionsPerQuiz).ToList();
        }

        private QuizQuestion CreateQuestion(
            string text,
            string correct,
            List<string> wrongPool,
            FishData fish,
            QuestionCategory category)
        {
            var options = new List<string> { correct };

            // Gerçek yanlış cevapları ekle (diğer keşfedilmiş balıklardan)
            var wrongs = wrongPool
                .Where(w => !string.IsNullOrEmpty(w) && w != correct)
                .Distinct()
                .ToList();
            Shuffle(wrongs);
            options.AddRange(wrongs.Take(3));

            // Yeterli yanlış cevap yoksa jenerik cevaplar ekle
            var generics = GetGenericAnswers(category);
            int attempts = 0;
            while (options.Count < 4 && attempts < generics.Count)
            {
                string g = generics[attempts];
                if (!options.Contains(g))
                    options.Add(g);
                attempts++;
            }

            Shuffle(options);

            return new QuizQuestion
            {
                questionText = text,
                correctAnswer = correct,
                allOptions = options,
                relatedFish = fish,
                category = category
            };
        }

        /// <summary>
        /// Yeterli keşfedilmiş balık yoksa kullanılacak jenerik yanlış cevaplar.
        /// </summary>
        private List<string> GetGenericAnswers(QuestionCategory category)
        {
            switch (category)
            {
                case QuestionCategory.Habitat:
                    return new List<string>
                    {
                        "Mercan resifleri",
                        "Derin okyanus dibi",
                        "Tatlı su gölleri",
                        "Kutup denizleri",
                        "Tropik nehirler"
                    };
                case QuestionCategory.Diet:
                    return new List<string>
                    {
                        "Plankton",
                        "Yosun ve algler",
                        "Küçük kabuklular",
                        "Deniz yıldızları",
                        "Su bitkileri"
                    };
                case QuestionCategory.ScientificName:
                    return new List<string>
                    {
                        "Amphiprion ocellaris",
                        "Betta splendens",
                        "Hippocampus kuda",
                        "Pterois volitans",
                        "Mola mola"
                    };
                case QuestionCategory.FunFact:
                case QuestionCategory.Size:
                case QuestionCategory.Identification:
                    return new List<string>
                    {
                        "Palyaço Balığı",
                        "Yunus",
                        "Denizatı",
                        "Balon Balığı",
                        "Ahtapot"
                    };
                default:
                    return new List<string> { "Bilinmiyor", "Diğer", "Hiçbiri" };
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
