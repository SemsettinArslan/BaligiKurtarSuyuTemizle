using System.Collections.Generic;
using UnityEngine;

namespace BalikKurtar.SuTemizligi
{
    /// <summary>
    /// Çöp objeleri için Object Pooling ve rastgele spawn sistemi.
    /// 
    /// Inspector'dan ayarlanan spawn alanı içinde belirli sayıda çöpü
    /// rastgele pozisyon, rotasyon ve ölçek ile sahneye yerleştirir.
    /// Temizlenen çöpler pool'a geri döner ve tekrar kullanılabilir.
    /// 
    /// Kullanım:
    /// 1. Boş bir GameObject'e ekleyin
    /// 2. trashPrefabs dizisine çöp prefab'lerini atayın
    /// 3. trashBinTarget'a çöp kutusunu atayın
    /// 4. Spawn alanını (spawnCenter, spawnSize) ayarlayın
    /// 5. spawnCount ile kaç çöp spawn olacağını belirleyin
    /// </summary>
    public class TrashPoolManager : MonoBehaviour
    {
        public static TrashPoolManager Instance { get; private set; }

        // ==================== PREFAB AYARLARI ====================

        [Header("Çöp Prefab'leri")]
        [Tooltip("Spawn edilecek çöp prefab'leri. Rastgele seçilir.")]
        [SerializeField] private GameObject[] trashPrefabs;

        [Header("Çöp Kutusu")]
        [Tooltip("Tüm çöplerin uçacağı çöp kutusu Transform'u.")]
        [SerializeField] private Transform trashBinTarget;

        // ==================== SPAWN AYARLARI ====================

        [Header("Spawn Ayarları")]
        [Tooltip("Her oyun başlangıcında spawn edilecek çöp sayısı.")]
        [SerializeField, Range(3, 50)] private int spawnCount = 10;

        [Tooltip("Spawn alanının merkez noktası (World Space).")]
        [SerializeField] private Vector3 spawnCenter = Vector3.zero;

        [Tooltip("Spawn alanının boyutu (X = genişlik, Y = yükseklik, Z = derinlik).")]
        [SerializeField] private Vector3 spawnSize = new Vector3(20f, 0.5f, 20f);

        [Tooltip("Çöpler arası minimum mesafe (üst üste binmeyi önler).")]
        [SerializeField, Range(0.5f, 5f)] private float minSpacing = 1.5f;

        [Tooltip("Uygun pozisyon bulunamazsa maksimum deneme sayısı.")]
        [SerializeField] private int maxPlacementAttempts = 30;

        [Header("Rastgele Varyasyon")]
        [Tooltip("Rastgele Y-ekseni rotasyonu uygulansın mı?")]
        [SerializeField] private bool randomRotation = true;

        // ==================== POOL AYARLARI ====================

        [Header("Pool Ayarları")]
        [Tooltip("Başlangıçta oluşturulacek ekstra pool objesi sayısı (buffer).")]
        [SerializeField, Range(0, 20)] private int poolBuffer = 5;

        // ==================== POOL VERİSİ ====================

        private readonly Dictionary<string, Queue<GameObject>> pool = new Dictionary<string, Queue<GameObject>>();
        private readonly List<TrashItem> activeTrash = new List<TrashItem>();
        private readonly List<Vector3> occupiedPositions = new List<Vector3>();
        private Transform poolParent;

        // ==================== PROPERTIES ====================

        /// <summary>Aktif (sahnedeki) çöp listesi.</summary>
        public List<TrashItem> ActiveTrash => activeTrash;

        /// <summary>Her seferinde spawn edilecek çöp sayısı.</summary>
        public int SpawnCount => spawnCount;

        // ==================== LIFECYCLE ====================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Pool objelerini tutan parent oluştur
            poolParent = new GameObject("_TrashPool").transform;
            poolParent.SetParent(transform);
        }

        private void Start()
        {
            InitializePool();
        }

        // ==================== POOL YÖNETİMİ ====================

        /// <summary>Başlangıçta pool'u prefab'lerle doldurur.</summary>
        private void InitializePool()
        {
            if (trashPrefabs == null || trashPrefabs.Length == 0)
            {
                Debug.LogError("[TrashPoolManager] trashPrefabs dizisi boş! Inspector'dan prefab ekleyin.");
                return;
            }

            int totalToCreate = spawnCount + poolBuffer;
            int prefabCount = trashPrefabs.Length;

            for (int i = 0; i < totalToCreate; i++)
            {
                // Her prefab'den eşit sayıda oluştur, sonra rastgele doldurmaya devam et
                GameObject prefab = trashPrefabs[i % prefabCount];
                CreatePoolObject(prefab);
            }

            Debug.Log($"[TrashPoolManager] Pool hazır: {totalToCreate} obje oluşturuldu ({prefabCount} farklı prefab).");
        }

        /// <summary>Bir prefab'den pool objesi oluşturur ve pool'a ekler.</summary>
        private GameObject CreatePoolObject(GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, poolParent);
            obj.SetActive(false);
            obj.name = prefab.name; // "(Clone)" kısmını kaldır

            // TrashItem bileşeni yoksa ekle
            if (!obj.TryGetComponent<TrashItem>(out _))
            {
                obj.AddComponent<TrashItem>();
            }

            // Pool'a ekle
            string key = prefab.name;
            if (!pool.ContainsKey(key))
                pool[key] = new Queue<GameObject>();

            pool[key].Enqueue(obj);
            return obj;
        }

        /// <summary>Pool'dan bir obje alır. Yoksa yeni oluşturur.</summary>
        private GameObject GetFromPool()
        {
            // Rastgele bir prefab tipi seç
            int randomIndex = Random.Range(0, trashPrefabs.Length);
            GameObject prefab = trashPrefabs[randomIndex];
            string key = prefab.name;

            // Pool'da varsa al
            if (pool.ContainsKey(key) && pool[key].Count > 0)
            {
                GameObject obj = pool[key].Dequeue();
                return obj;
            }

            // Pool boşsa: herhangi bir pool'da obje ara
            foreach (var kvp in pool)
            {
                if (kvp.Value.Count > 0)
                {
                    return kvp.Value.Dequeue();
                }
            }

            // Hiçbir pool'da yoksa yeni oluştur
            Debug.Log($"[TrashPoolManager] Pool genişletiliyor: {key}");
            GameObject newObj = CreatePoolObject(prefab);
            pool[key].Dequeue(); // Az önce eklenen objeyi al
            return newObj;
        }

        /// <summary>Temizlenen objeyi pool'a geri döndürür.</summary>
        public void ReturnToPool(TrashItem trash)
        {
            if (trash == null) return;

            activeTrash.Remove(trash);

            GameObject obj = trash.gameObject;
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);

            // Pool'a geri ekle
            string key = obj.name;
            if (!pool.ContainsKey(key))
                pool[key] = new Queue<GameObject>();

            pool[key].Enqueue(obj);
        }

        // ==================== SPAWN SİSTEMİ ====================

        /// <summary>
        /// Belirtilen sayıda çöpü spawn alanı içinde rastgele yerleştirir.
        /// WaterCleaningManager tarafından oyun başlangıcında çağrılır.
        /// </summary>
        public List<TrashItem> SpawnTrash()
        {
            return SpawnTrash(spawnCount);
        }

        /// <summary>Belirtilen sayıda çöpü spawn eder.</summary>
        public List<TrashItem> SpawnTrash(int count)
        {
            activeTrash.Clear();
            occupiedPositions.Clear();

            for (int i = 0; i < count; i++)
            {
                Vector3 position;
                bool positionFound = TryGetValidSpawnPosition(out position);

                if (!positionFound)
                {
                    Debug.LogWarning($"[TrashPoolManager] {i + 1}. çöp için uygun pozisyon bulunamadı, atlanıyor.");
                    continue;
                }

                GameObject obj = GetFromPool();
                if (obj == null)
                {
                    Debug.LogWarning("[TrashPoolManager] Pool'da obje kalmadı!");
                    break;
                }

                // Pozisyon ayarla
                obj.transform.SetParent(null); // Pool parent'dan çıkar
                obj.transform.position = position;

                // Rastgele rotasyon
                if (randomRotation)
                {
                    float randomY = Random.Range(0f, 360f);
                    obj.transform.rotation = Quaternion.Euler(0f, randomY, 0f);
                }

                // TrashItem'ı yapılandır
                TrashItem trash = obj.GetComponent<TrashItem>();
                if (trash != null)
                {
                    trash.SetTrashBinTarget(trashBinTarget);
                    trash.ResetForPool();
                }

                obj.SetActive(true);
                activeTrash.Add(trash);
                occupiedPositions.Add(position);
            }

            Debug.Log($"[TrashPoolManager] {activeTrash.Count} çöp spawn edildi.");
            return activeTrash;
        }

        /// <summary>Tüm aktif çöpleri pool'a geri gönderir.</summary>
        public void DespawnAll()
        {
            // Listeyi kopyala çünkü ReturnToPool liste'yi değiştiriyor
            var trashCopy = new List<TrashItem>(activeTrash);
            foreach (var trash in trashCopy)
            {
                if (trash != null && trash.gameObject.activeSelf)
                {
                    ReturnToPool(trash);
                }
            }
            activeTrash.Clear();
            occupiedPositions.Clear();
        }

        // ==================== POZİSYON HESAPLAMA ====================

        /// <summary>Spawn alanı içinde geçerli bir pozisyon bulmayı dener.</summary>
        private bool TryGetValidSpawnPosition(out Vector3 position)
        {
            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                position = GetRandomPointInArea();

                if (IsPositionValid(position))
                    return true;
            }

            // Son çare: spacing olmadan rastgele pozisyon
            position = GetRandomPointInArea();
            return true;
        }

        /// <summary>Spawn alanı içinde rastgele bir nokta döndürür.</summary>
        private Vector3 GetRandomPointInArea()
        {
            float x = spawnCenter.x + Random.Range(-spawnSize.x / 2f, spawnSize.x / 2f);
            float y = spawnCenter.y + Random.Range(-spawnSize.y / 2f, spawnSize.y / 2f);
            float z = spawnCenter.z + Random.Range(-spawnSize.z / 2f, spawnSize.z / 2f);

            return new Vector3(x, y, z);
        }

        /// <summary>Pozisyonun diğer çöplerle çakışıp çakışmadığını kontrol eder.</summary>
        private bool IsPositionValid(Vector3 position)
        {
            foreach (var occupied in occupiedPositions)
            {
                if (Vector3.Distance(position, occupied) < minSpacing)
                    return false;
            }
            return true;
        }

        // ==================== GIZMOS (Editörde Görselleştirme) ====================

        private void OnDrawGizmosSelected()
        {
            // Spawn alanını wireframe küp olarak göster
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
            Gizmos.DrawCube(spawnCenter, spawnSize);

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(spawnCenter, spawnSize);

            // Merkez noktasını küçük küre olarak göster
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(spawnCenter, 0.3f);
        }
    }
}
