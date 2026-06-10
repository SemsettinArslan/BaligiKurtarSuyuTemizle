using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace BalikKurtar.Managers
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string teamName;
        public int score;
        public long timestamp;
    }

    /// <summary>
    /// Firebase Realtime Database REST API aracılığıyla liderlik tablosu işlemlerini yönetir.
    /// URL bilgisini StreamingAssets altındaki firebase_config.txt dosyasından okur.
    /// </summary>
    public class FirebaseLeaderboardManager : MonoBehaviour
    {
        public static FirebaseLeaderboardManager Instance { get; private set; }

        private string databaseURL = "";
        private bool isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadDatabaseConfig();
        }

        /// <summary>
        /// Konfigürasyon dosyasından Firebase URL'sini yükler.
        /// </summary>
        private void LoadDatabaseConfig()
        {
            StartCoroutine(LoadDatabaseConfigCoroutine());
        }

        private IEnumerator LoadDatabaseConfigCoroutine()
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "firebase_config.txt");

            // Android'de StreamingAssets klasöründeki dosyalar sıkıştırılmış APK içinde olduğundan,
            // doğrudan C# File sınıfı (File.Exists / File.ReadAllText) ile okunamaz.
            // Bu yüzden UnityWebRequest veya Resources fallback kullanıyoruz.
            bool loadSuccess = false;

            // Platform Android ise veya URL WebRequest üzerinden okunmak isteniyorsa
            if (Application.platform == RuntimePlatform.Android || configPath.Contains("://"))
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Get(configPath))
                {
                    yield return webRequest.SendWebRequest();

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string fileContent = webRequest.downloadHandler.text.Trim();
                        if (ProcessConfigContent(fileContent))
                        {
                            loadSuccess = true;
                            Debug.Log($"[Firebase] URL StreamingAssets (UnityWebRequest) üzerinden başarıyla yüklendi: {databaseURL}");
                        }
                    }
                }
            }
            else
            {
                // Windows, Mac, Editor vb. platformlarda doğrudan hızlı File IO dene
                try
                {
                    if (File.Exists(configPath))
                    {
                        string fileContent = File.ReadAllText(configPath).Trim();
                        if (ProcessConfigContent(fileContent))
                        {
                            loadSuccess = true;
                            Debug.Log($"[Firebase] URL StreamingAssets (File IO) üzerinden başarıyla yüklendi: {databaseURL}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Firebase] File IO ile config okunurken hata oluştu: {e.Message}");
                }
            }

            // Eğer StreamingAssets'ten okuma başarısız olduysa, Resources klasöründen oku (Büyük kolaylık/Fallback)
            if (!loadSuccess)
            {
                TextAsset resourceConfig = Resources.Load<TextAsset>("firebase_config");
                if (resourceConfig != null)
                {
                    string fileContent = resourceConfig.text.Trim();
                    if (ProcessConfigContent(fileContent))
                    {
                        Debug.Log($"[Firebase] URL Resources/firebase_config üzerinden başarıyla yüklendi: {databaseURL}");
                    }
                    else
                    {
                        Debug.LogError("[Firebase] Resources/firebase_config dosyası geçerli bir URL içermiyor!");
                    }
                }
                else
                {
                    Debug.LogError("[Firebase] firebase_config.txt ne StreamingAssets'ten ne de Resources klasöründen yüklenemedi!");
                }
            }
        }

        private bool ProcessConfigContent(string content)
        {
            if (!string.IsNullOrEmpty(content) && content.StartsWith("http"))
            {
                databaseURL = content;
                if (!databaseURL.EndsWith("/"))
                {
                    databaseURL += "/";
                }
                isInitialized = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Firebase anahtarları için takım adını temizler.
        /// Firebase anahtarlarında ., $, #, [, ], / karakterlerine ve ASCII kontrol karakterlerine izin verilmez.
        /// </summary>
        public string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            // İzin verilmeyen karakterleri alt çizgiye çevirir
            string sanitized = Regex.Replace(key, @"[\.\$\#\[\]\/\x00-\x1F\x7F]", "_").Trim();
            // Türkçe karakterleri İngilizce karakterlere çevirebiliriz (opsiyonel ama url güvenliği için yararlı)
            sanitized = sanitized.Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u')
                                 .Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c')
                                 .Replace('İ', 'I').Replace('Ğ', 'G').Replace('Ü', 'U')
                                 .Replace('Ş', 'S').Replace('Ö', 'O').Replace('Ç', 'C')
                                 .Replace(" ", "_"); // Boşlukları alt çizgi yap
            return sanitized.ToLowerInvariant();
        }

        /// <summary>
        /// Belirtilen takım adının veritabanında zaten kayıtlı olup olmadığını asenkron kontrol eder.
        /// </summary>
        public void CheckTeamNameExists(string teamName, Action<bool> callback)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Firebase] URL yüklenemediği için takım adı kontrolü atlandı.");
                callback?.Invoke(false);
                return;
            }

            string sanitizedKey = SanitizeKey(teamName);
            if (string.IsNullOrEmpty(sanitizedKey))
            {
                callback?.Invoke(false);
                return;
            }

            StartCoroutine(CheckTeamNameExistsCoroutine(sanitizedKey, callback));
        }

        private IEnumerator CheckTeamNameExistsCoroutine(string sanitizedKey, Action<bool> callback)
        {
            string url = databaseURL + $"leaderboard/{sanitizedKey}.json";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text.Trim();
                    // Eğer düğüm boşsa Firebase "null" döner
                    bool exists = !string.IsNullOrEmpty(responseText) && responseText != "null" && responseText != "{}";
                    callback?.Invoke(exists);
                }
                else
                {
                    Debug.LogError($"[Firebase] Takım adı kontrol hatası: {request.error}");
                    // Hata durumunda ilerlemeyi engellememek için false dönebiliriz
                    callback?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// Oyuncu/Takım skorunu Firebase'e kaydeder veya günceller (PUT).
        /// </summary>
        public void SubmitScore(string teamName, int score, Action callback = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Firebase] Veritabanı URL'si yüklenemediği için skor gönderilemedi.");
                callback?.Invoke();
                return;
            }

            if (string.IsNullOrEmpty(teamName))
            {
                Debug.LogWarning("[Firebase] Takım adı boş olduğu için skor gönderilmedi.");
                callback?.Invoke();
                return;
            }

            string sanitizedKey = SanitizeKey(teamName);

            LeaderboardEntry entry = new LeaderboardEntry
            {
                teamName = teamName, // Orijinal ekran adını korur
                score = score,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            StartCoroutine(SubmitScoreCoroutine(sanitizedKey, entry, callback));
        }

        private IEnumerator SubmitScoreCoroutine(string key, LeaderboardEntry entry, Action callback)
        {
            // PUT isteği ile ilgili anahtardaki veriyi doğrudan günceller (veya yoksa ekler)
            string url = databaseURL + $"leaderboard/{key}.json";
            string jsonPayload = JsonUtility.ToJson(entry);

            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firebase] Skor başarıyla güncellendi: {entry.teamName} - {entry.score}");
                }
                else
                {
                    Debug.LogError($"[Firebase] Skor güncelleme hatası: {request.error}\nYanıt: {request.downloadHandler.text}");
                }

                // Arayüz güncellemesinin veri yazıldıktan sonra tetiklenmesi için callback çağrılır
                callback?.Invoke();
            }
        }

        /// <summary>
        /// En yüksek skorları Firebase'den çeker.
        /// </summary>
        public void GetTopScores(int limit, Action<List<LeaderboardEntry>> callback)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Firebase] Veritabanı URL'si yüklenemediği için liderlik tablosu çekilemedi.");
                callback?.Invoke(new List<LeaderboardEntry>());
                return;
            }

            StartCoroutine(GetTopScoresCoroutine(limit, callback));
        }

        private IEnumerator GetTopScoresCoroutine(int limit, Action<List<LeaderboardEntry>> callback)
        {
            // Firebase Realtime Database'de skora göre sıralamak için orderBy ve limitToLast parametrelerini kullanıyoruz.
            // Örn: leaderboard.json?orderBy="score"&limitToLast=10
            string url = databaseURL + $"leaderboard.json?orderBy=\"score\"&limitToLast={limit}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResult = request.downloadHandler.text;
                    List<LeaderboardEntry> entries = ParseFirebaseLeaderboard(jsonResult);
                    
                    // Firebase limitToLast kullandığımızda en küçükten en büyüğe doğru sıralı getirir.
                    // Liderlik tablosu için bunu tersine çeviriyoruz (en yüksek puan en üstte).
                    // Eğer iki takımın puanı eşitse, tarihi daha yeni olan (daha büyük timestamp) üstte yer alır.
                    var sortedEntries = entries
                        .OrderByDescending(e => e.score)
                        .ThenByDescending(e => e.timestamp)
                        .ToList();
                    
                    callback?.Invoke(sortedEntries);
                }
                else
                {
                    Debug.LogError($"[Firebase] Liderlik tablosu çekme hatası: {request.error}");
                    callback?.Invoke(new List<LeaderboardEntry>());
                }
            }
        }

        /// <summary>
        /// Firebase'den dönen push-ID'li veya benzersiz anahtarlı JSON sözlüğünü parse eder.
        /// </summary>
        private List<LeaderboardEntry> ParseFirebaseLeaderboard(string json)
        {
            List<LeaderboardEntry> list = new List<LeaderboardEntry>();

            if (string.IsNullOrEmpty(json) || json == "null" || json == "{}")
            {
                return list;
            }

            // Regex ile nested süslü parantez içlerini (her bir nesneyi) yakalarız.
            // Hem PushID hem de özel anahtar formatlarına uyar: {"sanitized_key":{"score":100,"teamName":"Grup A","timestamp":1653041234}}
            Regex objectRegex = new Regex(@"\{([^{}]+)\}");
            Regex scoreRegex = new Regex(@"""score""\s*:\s*(-?\d+)");
            Regex nameRegex = new Regex(@"""teamName""\s*:\s*""([^""]*)""");
            Regex timeRegex = new Regex(@"""timestamp""\s*:\s*(\d+)");

            foreach (Match m in objectRegex.Matches(json))
            {
                string content = m.Groups[1].Value;
                Match scoreMatch = scoreRegex.Match(content);
                Match nameMatch = nameRegex.Match(content);
                Match timeMatch = timeRegex.Match(content);

                if (scoreMatch.Success && nameMatch.Success)
                {
                    try
                    {
                        LeaderboardEntry entry = new LeaderboardEntry
                        {
                            score = int.Parse(scoreMatch.Groups[1].Value),
                            teamName = nameMatch.Groups[1].Value,
                            timestamp = timeMatch.Success ? long.Parse(timeMatch.Groups[1].Value) : 0
                        };
                        list.Add(entry);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Firebase] Entry parse hatası: {e.Message} (İçerik: {content})");
                    }
                }
            }

            return list;
        }
    }
}
