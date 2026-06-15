# Balığı Kurtar: Suyu Temizle - Geliştirici ve Değişiklik Rehberi (Developer & Modification Guide)

Bu kılavuz, **"Balığı Kurtar: Suyu Temizle"** Karma Gerçeklik (MR) uygulamasında değişiklik yapmak isteyen ancak Unity, C# veya veri tabanları konusunda ön bilgisi olmayan kişilerin bile projeyi kolayca güncelleyebilmesi için adım adım hazırlanmıştır.

---

## 1. Proje Klasör Yapısı ve Bileşenleri

Projeyi Unity Editor ile açtığınızda **Project** panelinde karşılaşacağınız ana klasörler ve görevleri şunlardır:

*   **`Assets/Scenes` (Sahneler):** Uygulamanın farklı ekranlarını temsil eder.
    *   `MainMenu`: Karşılama ekranı, takım adı girişi ve lider tablosu.
    *   `SampleScene`: Balık kartlarının okutulduğu ve balıkların 3D göründüğü ana AR sahnesi.
    *   `WaterCleaningScene`: Denizi çöplerden temizlediğimiz mini oyun sahnesi.
*   **`Assets/Scripts` (Kodlar):** Oyunun mantığını çalıştıran C# kodları.
    *   `AR/`: Kart tarama ve balıkla dokunmatik etkileşim (döndürme, büyütme).
    *   `Data/`: Balık verilerinin tutulduğu şablonlar (ScriptableObject).
    *   `Managers/`: Ses, Firebase ve Quiz yönetim merkezleri.
    *   `SuTemizligi/`: Çöp havuzu ve temizlik mekanikleri.
    *   `UI/`: Ekran tasarımları ve buton animasyonları.
*   **`Assets/StreamingAssets` (Dış Yapılandırma):** Kod yazmadan değiştirilebilecek dış dosyalar.
    *   `firebase_config.txt`: Firebase veri tabanı adresini tutar.

---

## 2. Sık Yapılan Değişiklik Senaryoları (Adım Adım)

### Senaryo A: Yeni Bir Balık Türü Ekleme
Uygulamaya yeni bir balık kartı tanımlamak ve balığı ekranda göstermek için şu 3 adımı uygulayın:

#### 1. Balık Veri Kartını Oluşturma
1. Unity içinde `Assets/Scripts/Data` (veya verileri tuttuğunuz klasör) içine gidin.
2. Boş bir yere sağ tıklayın: `Create -> BalikKurtar -> Fish Data` yolunu izleyin.
3. Oluşan dosyaya balığın adını verin (örneğin: `AlabalikData`).
4. Sağdaki **Inspector** panelinde şu alanları doldurun:
   * **Fish Id:** Vuforia kartınızın (Image Target) adıyla *birebir aynı* olmalıdır (küçük harflerle ve Türkçe karaktersiz yazın, örn: `alabalik`).
   * **Display Name:** Ekranda görünecek isim (örn: `Gökkuşağı Alabalığı`).
   * **Scientific Name:** Latince bilimsel adı (örn: `Oncorhynchus mykiss`).
   * **Habitat, Diet, Fun Fact, Size Info:** Balıkla ilgili bilgileri yazın.
   * **Fish Image:** Balığın 2D simgesini sürükleyip bırakın (opsiyonel).
   * **Info Audio:** Balık okunduğunda çalacak ses kaydını (MP3/WAV) sürükleyin.

#### 2. Balık Veri Tabanına Ekleme
1. `Assets/Scripts/Data` (veya `Assets/Resources`) klasöründeki `FishDatabase` dosyasını bulun.
2. Inspector panelindeki **List** kısmına yeni oluşturduğunuz `AlabalikData` dosyasını sürükleyip ekleyin.

#### 3. Vuforia Image Target (Kart Tanımlama)
1. `SampleScene` sahnesini açın.
2. Sahneye yeni bir **Vuforia Image Target** ekleyin ve kart görselinizi (Image Target) seçin.
3. Bu Image Target objesinin altına balığın 3D modelini (Prefab) yerleştirin.
4. Image Target objesine tıklayıp Inspector'da `FishCardHandler` script'ini ekleyin ve oluşturduğunuz `AlabalikData` dosyasını bu script'e sürükleyin.

---

### Senaryo B: Quiz Sorularını Değiştirme veya Yeni Soru Ekleme
Uygulamada quiz sorularını değiştirmek için C# kodu yazmanıza veya harici bir veri tabanı ile uğraşmanıza **gerek yoktur**.
* **Nasıl Çalışır?** Quiz sistemi, öğrencilerin keşfettiği balıkların `FishData` kartlarındaki bilgilerden (Habitat, Diet, Fun Fact, Scientific Name vb.) otomatik olarak sorular üretir.
* **Soru Değiştirme:** Örneğin, Koi Balığı ile ilgili sorulan beslenme sorusunu değiştirmek istiyorsanız, sadece `KoiData` dosyasını açıp **Diet** (Beslenme) kısmındaki metni değiştirmeniz yeterlidir. Quiz sistemi otomatik olarak yeni metni kullanarak soruyu güncelleyecektir.

---

### Senaryo C: Liderlik Tablosu Veri Tabanını (Firebase) Değiştirme
Müzenizde veya okulunuzda yeni bir skor veri tabanı açmak istiyorsanız Unity projesini yeniden derlemeniz veya kod açmanız gerekmez.
1. Projenizin çıktı klasöründe (veya Unity içinde `Assets/StreamingAssets/` altında) bulunan `firebase_config.txt` dosyasını Notepad (Not Defteri) veya herhangi bir metin editörüyle açın.
2. Dosyanın içindeki eski Firebase URL'sini silip kendi Firebase Realtime Database URL'nizi yapıştırın. (Örnek: `https://proje-adi-default-rtdb.firebaseio.com/`).
3. Dosyayı kaydedin. Uygulama bir sonraki açılışında otomatik olarak bu yeni adrese skorları gönderecektir.

---

### Senaryo D: Su Temizliği Mini Oyunundaki Çöp Tiplerini veya Sayısını Değiştirme
1. `WaterCleaningScene` sahnesini açın.
2. Sahnedeki `WaterCleaningManager` veya `TrashPoolManager` objesine tıklayın.
3. Inspector panelinde yer alan:
   * **Spawn Count (Çöp Sayısı):** Oyunda aynı anda bulunacak maksimum çöp miktarını belirler. Bu sayıyı artırıp azaltabilirsiniz.
   * **Trash Prefabs:** Havuzdan rastgele üretilecek çöp objelerinin (plastik şişe, teneke kutu vb.) listesidir. Buraya yeni 3D çöp modelleri ekleyebilir veya çıkarabilirsiniz.

---

## 3. Uygulamayı Derleme ve Cihaza Yükleme (Build Alma)

Değişiklikleri tamamladıktan sonra uygulamanın Android tabletlerde çalışacak yeni bir sürümünü (`.apk`) almak için:
1. Unity üst menüsünden `File -> Build Settings` yolunu izleyin.
2. Platform listesinden **Android**'i seçin (Eğer seçili değilse *Switch Platform* butonuna basın).
3. **Build** butonuna basın ve APK dosyanızın kaydedileceği konumu seçin.
4. Derleme bittikten sonra oluşan `.apk` dosyasını USB kablosu yardımıyla Android tabletinize aktarıp kurun.

## 4. Projeyi Test Etme

1. Githubta bulunan resimler dosyasındaki resimleri okutarak projeyi test edebilirsiniz.

