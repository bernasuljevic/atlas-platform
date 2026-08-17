# Atlas Platform — Proje Hafızası

## Bu projenin amacı

Kullanıcının .NET / modüler monolith mimarisini **öğrenerek** inşa ettiği bir
öğrenme + staj defteri projesi. Kurumsal wiki + AI fikrine dayanıyor (orijinal
ilham: SubMed Platform mimarisi - departman bazlı wiki + AI katmanı). AI
modülünün embedding ingestion + semantik arama akışı sahte (Fake) bir
embedding servisiyle uçtan uca çalışıyor (bkz. "AI Semantik Arama" günlük
ilerleme kayıtları) - gerçek bir sağlayıcıya (Voyage AI, OpenAI vb.) geçiş
API key'ler gelince yapılacak, tasarım gereği sadece `IEmbeddingService`'in
DI kaydını değiştirmek yeterli olacak.

**Hedef büyüdü (2026-07-27):** Artık sadece öğrenme defteri değil - kullanıcı bu
projeyi GitHub portföyünde ve iş görüşmelerinde gösterebileceği, kurumsal
seviyede bir ürün haline getirmek istiyor. Bkz. aşağıdaki "Mentor modu" - bundan
sonraki yeni özellikler kullanıcı tarafından yazılacak, Claude sadece
yönlendiriyor.

## ÇOK ÖNEMLİ — Nasıl çalışmalısın

**GÜNCEL ÇALIŞMA MODU (2026-07-27'den itibaren, 2026-07-27'de bir kez revize
edildi) — Anlatarak-geliştirme modu:**
Kullanıcı artık kodu GitHub portföyünde iş görüşmesi seviyesinde gösterebilmek
istiyor. Saf Socratic/ipucu-only mod (kod hiç yazma, sadece soru sor) bir kez
denendi ama kullanıcı için çok yavaş kaldı - "sen kodları yaz ama bana da
anlatarak git önemli olanları bilmem gerekenleri" diyerek modu netleştirdi.
Şu an geçerli kural:

- Kodu SEN yazıyorsun - ama her önemli kararı (neden bu tasarım, hangi
  alternatif elendi, hangi tuzağa dikkat) kısaca anlatarak ilerle. Önemli
  olmayan syntax detaylarını anlatarak boğma - sadece "bilmesi gereken" kısma
  odaklan.
- Yeni bir özellik istendiğinde onu tek seferde verme - 4-6 güne böl, her günün
  TEK bir mantıksal hedefi olsun, her gün bir öncekine bağlı olsun (gerçek bir
  şirkette geliştiriliyormuş gibi).
- Bir özelliğe başlamadan önce NEDEN o özelliği yaptığımızı açıkla; teknik
  olarak sırada başka bir şey daha mantıklıysa bunu gerekçeleriyle söyle,
  kullanıcının önerisini sorgusuz kabul etme.
- Her haftanın sonunda mimariyi tekrar değerlendir - teknik borç oluşmuşsa
  belirt, gerekirse refactoring görevi ekle.
- Küçük öğretici oyuncak örnekler değil, gerçek şirket pratiğine uygun
  (kurumsal) görevler ver.

Genel ilkeler (her modda geçerli):
- Yeni bir kavram tanıtırken 1-2 cümleyle ne işe yaradığını anlat.
- Her anlamlı adımdan sonra `dotnet build`/`dotnet test` ile doğrula, sonra
  commit önerisi sun.

## Mimari - Modüler Monolith

src/Shared/ → Kernel (Entity base), Contracts (modüller arası interface'ler/event'ler),
              CQRS (MediatR pipeline behaviors), Caching (Redis - ICacheService)
src/Modules/Auth/ → Domain → Application → Infrastructure → Api (4 katman)
src/Modules/Wiki/ → aynı 4 katman deseni
src/Modules/Notifications/ → aynı 4 katman deseni, SignalR Hub Infrastructure'da yaşıyor
src/Modules/AI/ → aynı 4 katman deseni, iskelet + Domain modeli kuruldu (embedding/LLM
                  entegrasyonu henüz yok - API key bekleniyor)
src/Host/Atlas.Api/ → composition root, sadece her modülün *.Api projesine referans verir
tests/ → xUnit test projeleri (Atlas.Modules.Wiki.Domain.Tests, Atlas.Modules.Auth.Domain.Tests)
Web/ → npm workspaces monorepo (Web/package.json, "apps/*" + "packages/*")
  Web/apps/platform/ → React (Vite) frontend, shadcn/ui (Tailwind v4 + Base UI preset)
  Web/packages/ui/ → paylaşılan @atlas/ui paketi - tüm shadcn/ui bileşenleri
                     (Button/Card/Input/Label/Textarea/Badge/RadioGroup/Table/
                     Dialog + lib/utils.js) burada yaşıyor, platform bunları
                     "@atlas/ui/button" gibi import ediyor (package.json'daki
                     "exports" alanı sayesinde)


**Katman kuralı:** Domain framework'ten habersiz. Application "nasıl saklanır"ı
bilmez, sadece interface (Abstractions/I*.cs) kullanır. Infrastructure gerçek
implementasyonu yazar (EF Core, JWT, hash'leme). Api, modülün DI kayıt
mekanizmasını (`AddXModule()`) ve MediatR endpoint'lerini barındırır.

**Modüller arası iletişim kuralı:** Modüller birbirinin Domain/Application/
Infrastructure'ına ASLA referans vermez. Sadece `Shared.Contracts`'taki
interface'ler ve **domain event'ler** üzerinden konuşurlar (Wiki, Auth'u tanımaz,
sadece `ICurrentUserAccessor`'ı tanır; Notifications, Wiki'yi tanımaz, sadece
`Shared.Contracts`'taki `WikiPageCreatedEvent`'i dinler). **İstisna:** Veritabanı
seviyesinde foreign key gerekiyorsa (örn. `WikiPage.CreatedByUserId` → `User.Id`),
bu EF Core'un C# API'siyle değil, migration içinde **ham SQL** ile eklenir - kod
seviyesindeki ayrım korunur, veritabanı seviyesinde tutarlılık sağlanır.

**Veritabanı:** İki ayrı fiziksel veritabanı, modül gruplarına göre:
- SQL Server Express (`.\SQLEXPRESS`, Windows Authentication) - Auth + Wiki,
  ayrı şemalar (`auth.*`, `wiki.*`), tek veritabanı (`AtlasPlatform`).
- PostgreSQL + pgvector (Docker, `docker-compose.yml`) - AI modülü, şema `ai.*`,
  veritabanı `AtlasAi`. Host portu **5433** (5432 değil - bu makinede native bir
  PostgreSQL Windows servisi zaten 5432'yi dinliyor, çakışma yaşandı).

appsettings.json → `ConnectionStrings:DefaultConnection` (SQL Server),
`ConnectionStrings:Postgres` (PostgreSQL), `ConnectionStrings:Redis`, ve
`Jwt:Key`/`Issuer`/`Audience` - hepsi dolu olmadan uygulama ilgili endpoint'te hata verir.

**Redis / cache:** `Shared.Caching` projesi, `ICacheService` interface'i +
`RedisCacheService` implementasyonu. `GetWikiPagesQuery` sonuçları cache'leniyor.
Docker'da ayrı bir container (`atlas-redis`), kalıcı volume YOK (cache verisi
kaybolursa DB'den yeniden üretilir, bu bilinçli bir tercih).

**Bildirimler:** `Notifications` modülü, SignalR Hub (`NotificationsHub`) ile
gerçek zamanlı bildirim gönderiyor. Wiki'de yeni sayfa eklenince
`WikiPageCreatedEvent` (Shared.Contracts, MediatR `INotification`) yayınlanıyor,
`WikiPageCreatedEventHandler` (Notifications.Infrastructure) bunu dinleyip
SignalR üzerinden bağlı React istemcilerine anlık bildirim gönderiyor.

**Kimlik doğrulama:** JWT tabanlı. `Pbkdf2PasswordHasher` (salt + 100k iterasyon)
şifreleri hash'liyor. Access token claim'leri: NameIdentifier, Email, Name, Role,
(varsa) department. `User.Role` (Member/Admin enum) → `.RequireRole("Admin")` ile
korunan endpoint'ler var. Access token ömrü 15 dakika (eskiden 8 saatti) - uzun
oturum artık `RefreshToken` (Auth.Domain, `auth.RefreshTokens` tablosu) ile sağlanıyor:
`POST /api/auth/login` `{accessToken, refreshToken}` döndürür, `POST /api/auth/refresh`
"rotation" deseniyle (her kullanımda eski token iptal edilir, yenisi üretilir) yeni
bir çift verir. `Jwt:Key` artık appsettings.json'da DEĞİL, User Secrets'ta
(`dotnet user-secrets set "Jwt:Key" "..."` - Development ortamında otomatik yüklenir,
`Properties/launchSettings.json` bunu garanti ediyor).

**AI modülü:** `WikiPageEmbedding` entity (AI.Domain) - `WikiPageId`, `ChunkIndex`/
`ChunkText`, denormalize `Title`/`DepartmentName`/`Visibility` (Wiki'nin
görünürlük kuralını uygulayabilmek için), `Embedding` (`Pgvector.Vector`, boyut
1024 - Voyage AI'a uygun), `CreatedAtUtc`. `AiDbContext` (AI.Infrastructure)
PostgreSQL'e `Npgsql.EntityFrameworkCore.PostgreSQL` + `Pgvector.EntityFrameworkCore`
ile bağlanıyor, `HasPostgresExtension("vector")` migration'ın "vector"
extension'ını otomatik `CREATE EXTENSION` etmesini sağlıyor. Wiki sayfası
oluşunca `WikiPageCreatedEvent` üzerinden otomatik chunk'lanıp embed ediliyor
(`GenerateWikiPageEmbeddingsCommand`); `SearchWikiPagesByMeaningQuery` doğal
dil sorgusunu embed edip pgvector `CosineDistance` ile en yakın chunk'ları
buluyor, Wiki'nin departman görünürlük kuralına göre filtreliyor. Henüz bir
HTTP endpoint'i yok (Gün 5'te gelecek).

## Öğrenilen dersler (tekrar sorgulama, test edildi)

1. **Service Lifetime kuralı:** Repository dış kaynağı (DB bağlantısı)
   sarmalıyorsa → Scoped. Repository kendisi veri deposuysa → Singleton
   (artık geçersiz - tüm repository'ler EF Core'a geçti, hepsi Scoped).
2. `.NET 10` kullanılıyor, yeni projelerde TargetFramework net10.0 olmalı.
3. Her yeni proje `Atlas.sln`'e eklenmeli (`dotnet sln add`).
4. Birden fazla DbContext varken migration komutlarında `--context` şart.
5. Migration process'i (`dotnet build`/`dotnet run`) kapatılmadan yeni migration
   eklenmeye çalışılırsa "dosya kilitli" hatası alınır - önce
   `Get-Process -Name "Atlas.Api" | Stop-Process -Force`.
6. Foreign key eklerken var olan "yetim" veri (referans verdiği kayıt silinmiş)
   migration'ı başarısız kılar - önce LEFT JOIN ile tespit edip düzeltilmeli.
7. React tarafında CORS: backend'de `AddCors`/`UseCors("AllowReactApp")`
   sadece `http://localhost:5173`'e izin veriyor - React portu değişirse
   burası da güncellenmeli.
8. **Shared.EventBus ayrı bir proje olarak açılmadı** - MediatR'ın `IPublisher`'ı
   zaten in-process event bus işlevi görüyor, ekstra bir sarmalayıcı katman
   gereksiz görüldü (bilinçli sadeleştirme).
9. Aynı makinede birden fazla veritabanı motoru varsa (örn. native kurulu
   PostgreSQL servisi + Docker'daki pgvector container'ı) ikisi de aynı portu
   (5432) dinlemeye çalışabilir - host bağlantıları yanlış olana gidip
   "password authentication failed" gibi yanıltıcı bir hata verir. Çözüm:
   Docker container'ının host portunu değiştirmek (`docker-compose.yml`),
   native servisi kapatmaktan daha güvenli ve geri alınabilir.
10. **GÜVENLİK - bulunup düzeltildi (2026-07-22):** `GET /api/wiki/pages`'teki
    departman filtresi eskiden istemcinin `?department=X` query string'iyle
    gönderdiği bir değere güveniyordu, sadece `IsAuthenticated` kontrol
    ediliyordu - kullanıcının GERÇEKTEN o departmana ait olup olmadığı hiç
    sorgulanmıyordu. Sonuç: giriş yapmış herhangi bir kullanıcı, departman
    adını tahmin ederek (`?department=IK` gibi) başka departmanların
    DepartmentOnly sayfalarını okuyabiliyordu (canlı olarak doğrulandı: yeni
    kayıt olan, IK ile hiçbir ilgisi olmayan bir kullanıcı IK'ya özel bir
    sayfayı görebildi). Düzeltme: `User` entity'sine gerçek bir `Department`
    alanı eklendi, JWT'ye imzalı bir `department` claim'i gömülüyor,
    `GetWikiPagesQuery` artık istemciden hiçbir departman parametresi almıyor -
    `ICurrentUserAccessor.Department` (token'dan) tek doğruluk kaynağı. Ayrıca
    React'ın `getWikiPages()` çağrısı hiç Authorization header'ı göndermiyordu
    (bu da düzeltilmeden önce departman özelliğinin UI'da zaten hiç gerçek
    anlamda çalışmadığı anlamına geliyordu) - bu da düzeltildi.
11. **SINIRLAMA - shadcn CLI, npm workspace paket adını alias olarak çözemiyor:**
    `components.json`'daki `aliases.ui`'ı `"@atlas/ui"` yapıp `npx shadcn add`
    çalıştırınca "Could not resolve the following aliases... ui. Configure path
    aliases in tsconfig.json or imports in package.json" hatası alındı (denendi,
    doğrulandı). CLI, `jsconfig.json`'daki `paths` eşlemesini tanımıyor - sadece
    `tsconfig.json` (TypeScript projeleri) ya da package.json'ın `imports`
    alanını (Node'un `#foo` iç import özelliği) destekliyor gibi görünüyor, bu
    proje JS (jsconfig.json) kullandığı için bu yollardan hiçbiri açık değil.
    Bu yüzden `aliases.ui`/`aliases.utils` platform'un kendi `@/components/ui`
    ve `@/lib/utils` yoluna GERİ alındı - yeni bir shadcn bileşeni eklerken
    akış şöyle: `npx shadcn add <bileşen>` (platform'a yazar) → `git mv` ile
    `Web/packages/ui/src/`'e taşı → içindeki `@/lib/utils` import'unu
    `./lib/utils`'e (ve varsa component-içi `@/components/ui/X` import'larını
    `./X`'e) elle düzelt. `jsconfig.json`'daki `@atlas/ui/*` path eşlemesi
    sadece IDE'nin (autocomplete/go-to-definition) `@atlas/ui/...` import'larını
    anlaması için tutuldu - shadcn CLI'nin yazma hedefini etkilemiyor.
12. **Çalışan bir Atlas.Api process'i User Secrets değişikliğini görmez:**
    `Jwt:Key` User Secrets'a eklendikten/değiştirildikten SONRA hâlâ çalışan
    eski bir process, konfigürasyonu yeniden okumaz - `Encoding.UTF8.GetBytes(null)`
    her istekte `ArgumentNullException` (→ 500 "Value cannot be null. (Parameter 's')")
    fırlatır, register/login dahil HER endpoint (hatta `/health`) aynı hatayı verir.
    Çözüm Ders #5'teki ile aynı: `Get-Process -Name "Atlas.Api" | Stop-Process -Force`
    sonra `dotnet run` ile yeniden başlat.
13. **Güvenlik kuralı YAZMA tarafında da tekrar kontrol edilmeli, sadece OKUMA
    tarafında değil:** Ders #10'daki departman açığı düzeltildikten sonra bile
    `CreateWikiPageCommandHandler` sayfanın `DepartmentName`'ini hâlâ istemciden
    alıyordu - IK'daki bir kullanıcı `departmentName: "IT"` göndererek IT'nin
    alanına sayfa ekleyebiliyordu (canlı doğrulandı, 2026-07-27). Genel ders:
    "istemciden gelen bir alana güvenme" kuralını bir CRUD kaynağının SADECE
    GET'inde uygulayıp POST/PUT/DELETE'inde unutmak kolay - ikisi de aynı
    şekilde denetlenmeli.
14. **Gerçek bir veritabanına karşı "yer tutucu" (placeholder) bir SQL sorgusu
    ÇALIŞTIRILMAMALI - önce tam sorgu yazılıp öyle çalıştırılmalı:**
    Yetim embedding'leri temizlerken yazılan bir ara adım sorgusundaki
    `WHERE NOT EXISTS (... WHERE FALSE)` koşulu her satır için her zaman true
    çıkıyordu - bu, hedeflenen ~36 yetimin yanında hâlâ geçerli olan tüm
    embedding'leri de sildi (2026-07-27, canlı yaşandı). Kurtarma
    `ReindexWikiPagesCommand` eklenerek yapıldı. Genel ders: "şimdilik
    böyle bırakayım, sonra düzeltirim" diye ara bir sorguyu gerçek veriye
    karşı çalıştırmak yerine, DELETE gibi geri alınamaz bir işlemden önce
    her zaman önce bir SELECT ile etkilenecek satırları görmeli.
15. **Sıfır büyüklüklü (zero-magnitude) bir vektör, cosine distance'ta NaN
    üretir ve JSON serileştirmesini çökertir:** `FakeEmbeddingService.Normalize()`
    hiç kelime içermeyen bir chunk için (örn. sadece "????????") 0'a bölmeyi
    önlemek amacıyla normalize ETMEDEN sıfır vektör döndürüyordu (kendi
    içinde güvenli) - ama bu sıfır vektör pgvector'ın `<=>` operatörüyle
    karşılaştırılınca NaN üretiyor, `System.Text.Json` NaN'ı yazamayıp TÜM
    isteği çökertiyordu (2026-07-27, canlı doğrulandı, küçük veri setlerinde
    tetiklendi - büyük bir tabloda bu satır genelde TopN dışında kalıp fark
    edilmiyordu). Genel ders: bir embedding/vektör pipeline'ında "anlamsız
    girdi" (boş, sadece noktalama) durumunu HER ZAMAN düşün - normalize
    etmemek yetmez, o veriyi hiç kaydetmemek gerekir.
16. **Claude Code'un araç (Bash/PowerShell) üzerinden yazdığı bir User Secret,
    kullanıcının KENDİ interaktif terminaline görünmeyebilir:** 2026-07-28'de
    saatlerce süren bir "Jwt:Key bulunamıyor" teşhisinin GERÇEK kök sebebi bu
    çıktı - Claude'un Bash aracından çalıştırdığı `dotnet user-secrets set`
    diskteki `secrets.json`'ı doğru yazıyordu ve Claude'un KENDİ açtığı
    Bash/PowerShell process'lerinden (`dotnet user-secrets list`, `dotnet run`)
    her seferinde doğru okunuyordu (aynı `%APPDATA%` yolu, aynı Windows
    kullanıcısı görünmesine rağmen) - ama kullanıcının GERÇEK, kendi açtığı
    interaktif PowerShell penceresinden çalıştırılan `dotnet user-secrets list`
    "No secrets configured" diyordu, `dotnet run` da tutarlı biçimde
    `Encoding.UTF8.GetBytes(null)` hatasıyla çöküyordu - basit bir "stale
    process" ya da "eksik derleme" değildi, defalarca doğrulandı. Kesin çözüm:
    kullanıcının secret'ı KENDİ terminalinden (`dotnet user-secrets set ...`)
    yeniden yazması - bundan sonra kendi `dotnet run`'ı da sorunsuz çalıştı.
    Olası açıklama (bu ortama özgü, doğrulanmadı ama en olası): bu kurumun
    (nku.edu.tr) OneDrive/profil politikası zaten Masaüstü'nü OneDrive'a
    yönlendiriyor (proje yolu `OneDrive - nku.edu.tr\Masaüstü\...`) - böyle bir
    kurumsal Klasör Yönlendirme (Folder Redirection) politikası bazen
    `%APPDATA%\Roaming`'i de senkronize/redirect eder, bu da araç tarafından
    başlatılan process'lerle kullanıcının kendi kabuğu arasında GEÇİCİ bir
    tutarsızlık penceresi açabiliyor. Genel ders: "Claude bir dosyayı X yoluna
    yazdı ve KENDİ komutuyla doğruladı" DEMEK, "kullanıcının kendi terminali de
    aynı şeyi görecek" DEMEK DEĞİLDİR - özellikle User Secrets gibi kullanıcı
    profiline bağlı, kurumsal senkronizasyon politikalarının araya girebileceği
    bir mekanizma söz konusuysa. Böyle bir tutarsızlık şüphesi varsa, en hızlı
    kesin teşhis: kullanıcıdan AYNI doğrulama komutunu KENDİ terminalinde
    çalıştırmasını istemek (Claude'un kendi doğrulaması yeterli kanıt değil).

    (Bu araştırma sırasında DENENİP GERİ ALINAN bir yan yol: `.AddJwtBearer(
    options => {...})` içindeki `configuration["Jwt:Key"]` okumasını "lazy"den
    "eager"e (AddAuthModule çağrılırken, `Build()`'den önce) çevirmek - teoride
    `IOptionsMonitor`'ın `Lazy<T>` önbelleklemesinin İLK hatayı kalıcı hale
    getirme riskini ortadan kaldıracaktı, ama PRATİKTE integration testlerini
    KIRDI: `WebApplicationFactory`nin test için enjekte ettiği `Jwt:Key`
    override'ı bu erken noktada henüz `configuration`'a uygulanmamış oluyor -
    `JwtTokenGenerator` (lazy, request zamanında okuyan) token'ı DOĞRU test
    anahtarıyla imzalarken, eager okunan bu lambda YANLIŞ/eksik bir anahtarla
    doğruluyordu, imza uyuşmazlığı her korumalı endpoint'te 401 üretiyordu.
    Geri alındı - lazy okuma orijinal haliyle korundu. Genel ders: teorik bir
    kırılganlığı "sağlamlaştırmak" için yapılan bir değişikliği, o
    kırılganlığın gerçek kanıtı OLMADAN (sadece "olabilir" diye) uygulamak
    riskli - burada gerçek kanıt zaten farklı bir yerdeydi (User Secrets
    görünürlüğü), o yüzden bu "sağlamlaştırma" hem gereksizdi hem de kendi
    başına yeni, gerçek bir regresyona yol açtı. Böyle bir değişiklik
    yapılacaksa mutlaka integration test suite'i (`dotnet test
    tests/Atlas.IntegrationTests`) çalıştırılıp doğrulanmalı - sadece
    Domain testleriyle yetinmek bu regresyonu YAKALAYAMADI.)

    (Yan not: bu araştırma sırasında `bin`/`obj`'un OneDrive senkronize
    klasörünün içinde yaşamasının ayrı, gerçek bir risk olduğu da görüldü -
    büyük ölçekli bir "clean rebuild" sırasında bir kez MSB3030 dosya
    kopyalama hatası da alınmıştı. Bu yüzden `Directory.Build.props` eklenip
    `bin`/`obj` `%LOCALAPPDATA%\AtlasPlatformBuild\`'a taşındı - bu JWT
    hatasının asıl sebebi değildi ama yine de kalıcı, faydalı bir hijyen
    iyileştirmesi olarak korundu.)
17. **HTML `<input type="date">` saatsiz bir DateTime gönderir - "Bitiş" tarihi
    filtrelerinde `<=` kullanmak o günü SESSİZCE dışlar:** Audit Log'daki
    "Bitiş" filtresi hiç çalışmıyordu (2026-07-28, kullanıcı bildirdi) -
    `toUtc` her zaman `00:00:00` ile geliyordu, `OccurredAtUtc <= toUtc`
    o günün SADECE gece yarısı anını kapsıyordu, gerçek kayıtlar (ör. 15:51)
    hiç eşleşmiyordu. Düzeltme: `toUtc.Date.AddDays(1)` (bir sonraki günün
    başlangıcı) ÜST SINIR olarak, `<` (küçüktür, eşit değil) ile kullanılmalı -
    "Bitiş: X" kullanıcı için her zaman "X gününün TAMAMI" anlamına gelir.
    Aynı hata AI aramasına tarih filtresi eklenirken tekrar yazılmasın diye
    aynı düzeltme oraya da baştan uygulandı.
18. **Npgsql, `Kind=Unspecified` bir `DateTime`'ı `timestamp with time zone`
    sütunuyla karşılaştırmayı REDDEDİYOR - SQL Server aynı durumda sessizce
    çalışıyor:** AI aramasına tarih filtresi eklenince (`GET /api/ai/search?
    fromUtc=...`), ASP.NET Core'un query string binder'ının ürettiği
    `Kind=Unspecified` bir `DateTime`, Postgres'teki `WikiPageEmbedding.
    CreatedAtUtc` ("timestamp with time zone") ile karşılaştırılınca "Cannot
    write DateTime with Kind=Unspecified... only UTC is supported" hatasıyla
    400 dönüyordu (canlı doğrulandı) - AYNI desen SQL Server'daki Audit log
    filtresinde HİÇ sorun çıkarmamıştı, çünkü `datetime2` Kind'ı hiç
    umursamıyor. Düzeltme: `DateTime.SpecifyKind(value, DateTimeKind.Utc)` ile
    Postgres'e gönderilmeden hemen önce. Genel ders: Postgres/Npgsql, SQL
    Server'a göre `DateTime.Kind` konusunda çok daha katı - bir filtre/sorgu
    SQL Server'da sorunsuz çalıştı diye Postgres'te de çalışacağı anlamına
    gelmiyor, ikisi ayrı ayrı test edilmeli.
19. **Backend'in serileştirdiği UTC zaman damgaları veritabanına göre TUTARSIZ:**
    SQL Server kaynaklı alanlar (`AuditLogEntry.OccurredAtUtc`, `WikiPage.
    CreatedAtUtc`) JSON'a "Z" SİZ yazılıyor (Kind bilgisi SQL Server'da
    kayboluyor), Postgres kaynaklı alanlar (`WikiPageEmbedding.CreatedAtUtc`,
    AI arama sonuçlarında) "Z" İLE yazılıyor (Npgsql, Kind=Utc'yi koruyor).
    Bir yerde işe yarayan sabit bir `+ "Z"` düzeltmesi (bkz. Ders'in üstündeki
    Audit log çözümü), başka bir yerde (AI arama sonuçları) "...ZZ" üretip
    tarihi JavaScript'te "Invalid Date"e çeviriyordu (canlı doğrulandı, kod
    push edilmeden ÖNCE fark edildi). Çözüm: paylaşılan `dateUtils.js` /
    `formatUtcTimestamp()` - "Z" SADECE stringin sonunda yoksa ekleniyor,
    her iki kaynaktan gelen değerle de doğru çalışıyor.
20. **`dotnet ef migrations add`, hedef DbContext'in kendisiyle hiç ilgisi
    olmayan bir sebepten (Redis erişilemez) başarısız olabilir:** Documents
    modülüne (SQL Server, `.\SQLEXPRESS`) yeni bir migration eklenirken
    (2026-08-12) "Unable to resolve service for type DbContextOptions..."
    hatası alındı - kök sebep DbContext'te değil, Docker Desktop'ın arka
    planda kapanmış olmasıydı (Redis/Postgres container'ları duruyordu).
    `dotnet ef migrations add --startup-project src/Host/Atlas.Api`,
    hedef DbContext SQL Server'a bağlı olsa bile `Program.cs`'in TAMAMINI
    (dolayısıyla `AddCaching()`'in Redis bağlantısını da) kurmaya çalışıyor -
    Redis erişilemeyince tüm host inşası çöküyor, hata mesajı da ilk bakışta
    DbContext'in kendisinde bir sorun varmış gibi görünüyor (yanıltıcı).
    Genel ders: bir modülün migration'ı SQL Server'a bağlı olsa bile, o an
    Docker'da çalışması gereken HER şeyin (Redis, Postgres) ayakta olması
    gerekiyor - `docker ps` ile hızlıca kontrol edip gerekirse
    `docker compose start postgres redis` ile düzeltilebilir (Ders #9'daki
    gibi tüm stack'i değil, sadece bu iki servisi başlatmak yeterli, native
    SQL Server'a dokunmuyor).
21. **Yeni bir production kuralı eklendiğinde, o akışı kullanan HER test
    yardımcı fonksiyonu (helper) elden geçirilmeli - "derlendi" yeterli bir
    kanıt değil:** Kullanıcı `LoginCommandHandler`'a bağımsız olarak bir
    e-posta doğrulama zorunluluğu (`if (!user.EmailVerified) throw ...`)
    eklemiş (2026-08-03 migration) - ama register+login yapan integration
    test dosyalarındaki (`WikiEndpointsTests`, `AiSearchEndpointsTests`,
    `OutboxIntegrationTests`, `AuthEndpointsTests`) `RegisterAndLoginAsync`
    helper'ları hiç güncellenmemiş. Sonuç: bu kural eklendiği andan itibaren
    HEPSİ login adımında 403 alıp kırılıyordu - ama kimse fark etmedi, çünkü
    CI zaten "Category=Integration" testlerini atlıyor (gerçek Postgres/
    Redis'e ihtiyaç duyduğundan, bkz. `.github/workflows/ci.yml`) ve
    `dotnet test tests/Atlas.IntegrationTests` en son ne zaman elle
    çalıştırılmış belli değil. Documents pipeline'ı için YENİ bir integration
    test dosyası yazılırken (P4 Gün 6, 2026-08-12) kendi testlerim de AYNI
    şekilde 403 alınca ortaya çıktı - benim P4 işimin bir parçası değildi,
    bağımsız keşfedilen bir regresyondu. Düzeltme: `AuthTestHelper.
    RegisterVerifyAndLoginAsync` (AuthDbContext test host'unda InMemory
    olduğu için doğrulama kodunu gerçek bir e-posta kutusu açmadan doğrudan
    DB'den okuyup `POST /api/auth/verify-email`'e gönderiyor) - dört dosya da
    buna geçirildi, `AuthEndpointsTests`'e regresyonun kendisini kilitleyen
    yeni bir test eklendi. Genel ders: Ders #15/#19'daki "bir kuralı SADECE
    bir yerde değiştirip diğer tüketicilerini unutmak" hatasının bir başka
    türü - burada tüketici production kodu değil, test altyapısıydı; production
    kodu (register/login akışının kendisi) hiçbir zaman bozuk değildi, SADECE
    onu doğrulayan testler görünmez şekilde kırılmıştı. "Testler mevcut, o
    zaman güvenlik ağı sağlam" varsayımı, o testlerin GERÇEKTEN çalıştırıldığı
    (ve CI'ın onları atlamadığı) doğrulanmadan yapılmamalı.
22. **EF Core'un `ExecuteDeleteAsync`/`ExecuteUpdateAsync`'i InMemory
    sağlayıcısında ÇALIŞMIYOR - gerçek SQL Server'a karşı yerelde test etmek
    bunu YAKALAYAMAZ:** Wiki Version History'nin `EfWikiPageVersionRepository.
    DeleteAllForWikiPageAsync`'i ilk yazıldığında `ExecuteDeleteAsync`
    kullanıyordu - yerelde (gerçek `.\SQLEXPRESS`'e karşı) hem curl/sqlcmd hem
    tarayıcı testinde SORUNSUZ çalıştı, hiçbir hata vermedi. PR #19'un CI'ında
    (2026-08-17) `OutboxIntegrationTests.SayfaSilininceOlusanOutboxMesaji_
    DogruEventTipindeYaziliyor` `DELETE /api/wiki/pages/{id}`'de 500 alıp
    kırıldı - kök sebep: integration test host'u (`AtlasApiFactory.cs`)
    `WikiDbContext`'i (Auth/Wiki/Audit/Documents gibi) EF Core InMemory
    sağlayıcısına çeviriyor (SADECE Vault/AI gerçek veritabanına bağlı kalıyor,
    bkz. yukarıdaki "InMemory'ye çevrilme" notları) - InMemory sağlayıcısı
    `ExecuteDelete`/`ExecuteUpdate`'i DESTEKLEMİYOR, `InvalidOperationException`
    fırlatıyor. Düzeltme: `ExecuteDeleteAsync` yerine `ToListAsync()` +
    `RemoveRange()` (AddAsync'teki AYNI "stage et, çağıranın `SaveChangesAsync`'i
    beklesin" deseni) - hem InMemory'de HEM gerçek SQL Server'da çalışıyor,
    ÜSTELİK asıl istenen atomikliği de sağlıyor (`ExecuteDeleteAsync` zaten
    HEMEN/AYRI çalışıyordu, `DeleteWikiPageCommandHandler`'ın sonundaki TEK
    `SaveChangesAsync`'in dışında kalıyordu - bu da ayrı, gizli bir
    atomiklik kusuruydu, sadece CI'ın InMemory sağlayıcısı sayesinde daha
    erken yakalandı). Genel ders: **gerçek bir veritabanına karşı yerel test
    "yeterince gerçek" değildir** - bu proje kasıtlı olarak bazı modüllerde
    InMemory (hız için) bazılarında gerçek DB (Vault/AI, davranış doğruluğu
    için) kullanıyor; EF Core'un bulk `Execute*` API'leri gibi provider'a özgü
    davranış farklılıkları SADECE CI'ın (ya da InMemory'nin) kullandığı
    provider'a karşı ortaya çıkabilir - "yerelde sorunsuz çalıştı" tek başına
    yeterli bir doğrulama değil, mümkünse HER İKİ sağlayıcıya karşı da (ya da
    en azından CI'ınkine karşı) test edilmeli.

## Şu ana kadar tamamlananlar

- [x] Auth modülü: User (Email/FullName/PasswordHash/Role), Register/Login
      Command'ları, GetAllUsersQuery, MediatR+LoggingBehavior
- [x] Wiki modülü: WikiPage (Title/Content/DepartmentName/Visibility/CreatedByUserId),
      `IsVisibleTo()` Domain'de, CreateWikiPageCommand, GetWikiPagesQuery
- [x] EF Core: AuthDbContext/WikiDbContext, migration'lar, kalıcılık doğrulandı
- [x] JWT login: Pbkdf2PasswordHasher, JwtTokenGenerator, HttpCurrentUserAccessor
      (FakeCurrentUserAccessor kaldırıldı)
- [x] Foreign key: wiki.WikiPages.CreatedByUserId → auth.Users.Id (raw SQL migration)
- [x] Authorization: GET /api/auth/users → RequireRole("Admin"),
      POST /api/wiki/pages → RequireAuthorization (GET wiki bilinçli olarak açık)
- [x] Rol bazlı yetkilendirme: User.Role (Member/Admin), JWT Role claim
- [x] React frontend (Web/apps/platform/): login, wiki listesi/oluşturma,
      görünürlük seçimi, yükleme durumları, çıkış yap, access+refresh token
      localStorage'da kalıcı, 401 alınca otomatik refresh (bkz. api.js).
      (Eski "departman filtresi" input'u kaldırıldı - departman artık
      kullanıcı tarafından seçilebilir değil, JWT'deki gerçek departmandan
      otomatik geliyor, bkz. Öğrenilen dersler #10.)
- [x] Test projeleri: Atlas.Modules.Wiki.Domain.Tests + Atlas.Modules.Auth.Domain.Tests
      (xUnit), WikiPage.IsVisibleTo() ve User.Create() validasyonları için
- [x] Redis cache: Shared.Caching, ICacheService/RedisCacheService,
      GetWikiPagesQuery sonuçları cache'leniyor
- [x] Notifications modülü: SignalR Hub, WikiPageCreatedEvent ile Wiki'den
      Notifications'a olay bildirimi, React'ta anlık bildirim gösteriliyor
- [x] AI modülü iskeleti: WikiPageEmbedding Domain modeli, PostgreSQL/pgvector
      bağlantısı, migration uygulandı ve doğrulandı (embedding üretimi yok henüz)
- [x] Monorepo restructure: web/ → Web/apps/platform + Web/packages/ui,
      npm workspaces kuruldu
- [x] shadcn/ui kurulumu (Tailwind v4, Base UI preset) - sitenin orijinal
      özel tasarımı (mor/koyu tema) korunacak şekilde entegre edildi
- [x] JWT key rotasyonu + User Secrets (appsettings.json'daki sızmış key kaldırıldı)
- [x] Refresh token mekanizması: access token 15dk, rotation destekli /api/auth/refresh
- [x] Wiki GET güvenlik açığı bulundu ve düzeltildi: departman artık JWT'den
      geliyor, istemciden gelen bir query parametresine güvenilmiyor (Öğrenilen
      dersler #10)
- [x] React Router: /login, /wiki, / (redirect), ProtectedRoute deseni,
      AuthContext (token state + eski App.jsx event listener'ları)
- [x] shadcn bileşenleri Web/packages/ui'ye taşındı, @atlas/ui paketi olarak
      gerçekten paylaşılabilir hale geldi (bkz. Öğrenilen dersler #11 -
      shadcn CLI'nin bu monorepo yapısındaki sınırlaması)
- [x] CQRS pipeline'ına `ValidationBehavior` (FluentValidation) eklendi:
      `RegisterUserCommandValidator`, `LoginCommandValidator`,
      `CreateWikiPageCommandValidator`. `GlobalExceptionHandler` artık
      `FluentValidation.ValidationException`'ı ayrı yakalayıp alan bazlı
      `ValidationProblemDetails` (`errors: {alan: [mesajlar]}`) dönüyor -
      Domain'in `ArgumentException` fırlatan son-hat validasyonu hâlâ duruyor,
      bu behavior sadece isteği Handler'a ulaşmadan, daha temiz bir hata
      mesajıyla erken kesiyor.
- [x] CQRS pipeline'ına `CachingBehavior` eklendi: `GetWikiPagesQueryHandler`'daki
      elle yazılmış "cache'e bak, yoksa DB'den çek, cache'e yaz" mantığı, generic
      bir `ICacheableQuery<TResponse>` marker interface + `CachingBehavior<,>`'a
      taşındı. **Önemli güvenlik ayrımı:** Cache'lenen sınıf `GetWikiPagesQuery`
      DEĞİL, onun çağırdığı ayrı bir iç Query olan `GetAllWikiPagesRawQuery` -
      çünkü `GetWikiPagesQuery` kullanıcının departmanına göre FİLTRELENMİŞ bir
      sonuç döndürüyor, bunu olduğu gibi cache'lemek bir kullanıcının filtrelenmiş
      görünümünün başka bir kullanıcıya sızmasına yol açardı (Öğrenilen dersler
      #10'daki departman güvenlik açığıyla aynı sınıf hata). `GetAllWikiPagesRawQuery`
      filtresiz, ham veriyi döndürüyor - filtreleme hâlâ `GetWikiPagesQueryHandler`'da,
      istek bazlı olarak yapılıyor.

- [x] Notifications modülünün SignalR Hub'ına Redis backplane eklendi
      (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`,
      `AddSignalR().AddStackExchangeRedis(ConnectionStrings:Redis)`). Tek
      instance'lı Development kurulumunda görünür bir davranış farkı yok, ama
      artık uygulama birden fazla instance ile (load balancer arkasında)
      çalıştırılırsa bir instance'a bağlı istemci başka bir instance'da oluşan
      "WikiPageCreated" olayını da alabiliyor - öncesinde her instance sadece
      kendi bağlı istemcilerini biliyordu.

- [x] **AI Semantik Arama - Gün 1/6 (anlatarak-geliştirme modu):** Mimari
      tasarım kararları verildi ve uygulandı.
      `IEmbeddingService` (`AI.Application/Abstractions`) - batch, sıra garantili
      tek metotlu bir soyutlama, `IPasswordHasher` ile aynı desen (henüz
      implementasyonu yok, Gün 2'de sahte/lokal bir servisle gelecek).
      `WikiPageEmbedding` entity'sine `ChunkIndex`/`ChunkText` eklendi -
      `EmbeddingDimension = 1024` sabiti artık Domain'de TEK yerde tanımlı,
      hem entity'nin fail-fast validasyonu hem EF configuration'ı (`vector(1024)`
      sütun tipi) buradan türetiliyor. `WikiPageId` üzerindeki tekil index,
      `(WikiPageId, ChunkIndex)` composite index'iyle değiştirildi (bir sayfanın
      chunk'larını sıralı çekebilmek için). Migration oluşturulup PostgreSQL'e
      uygulandı.

- [x] **AI Semantik Arama - Gün 2/6:** `TextChunker` (AI.Domain/Chunking) -
      `WikiVisibilityRules` ile aynı desen (durumsuz static sınıf), sabit boyutlu
      + üst üste binen (overlap) kayan pencere ile metni chunk'lara bölüyor;
      `overlap >= chunkSize` durumu (sonsuz döngüye yol açardı) baştan
      reddediliyor. `FakeEmbeddingService` (AI.Infrastructure/Embeddings) -
      "feature hashing" tekniğiyle (kelimeleri MD5 ile 1024 kovaya hash'leyip
      normalize ederek) TAMAMEN rastgele olmayan, ortak kelimesi çok olan
      metinlerin gerçekten birbirine yakın vektör ürettiği deterministik bir
      sahte embedding servisi. `string.GetHashCode()` KULLANILMADI çünkü .NET
      onu process başına farklı tuzlar (hash randomization) - MD5 girdiye göre
      sabit bir çıktı garantiliyor. `AIModule.cs`'e `IEmbeddingService` DI kaydı
      eklendi (Singleton - durumsuz, dış kaynağa bağlı değil). Yeni bir test
      projesi (`Atlas.Modules.AI.Infrastructure.Tests`) açıldı - Infrastructure
      katmanını doğrudan test eden ilk proje.

- [x] **AI Semantik Arama - Gün 3/6:** Wiki sayfası oluşunca otomatik embedding
      üretimi uçtan uca çalışıyor (canlı doğrulandı). `WikiPageCreatedEvent`'e
      `Content` eklendi (AI'ın embedding üretmek için sayfanın asıl metnine
      ihtiyacı var, Wiki'nin DB'sine geri sorgu atmak modül izolasyonunu
      ihlal ederdi). `GenerateWikiPageEmbeddingsCommand` (AI.Application) -
      TextChunker + IEmbeddingService + yeni `IWikiPageEmbeddingRepository`
      soyutlamasını orkestra ediyor. `WikiPageCreatedEventHandler`
      (AI.Infrastructure) - Notifications'ın aynı event'i dinleyen handler'ıyla
      birebir aynı desen, AI bu event'in ikinci abonesi.
      **Bulunan gerçek bug:** `AIModule.cs`'te MediatR sadece AI.Application
      assembly'sini tarıyordu, `WikiPageCreatedEventHandler` AI.Infrastructure'da
      olduğu için hiç kayıt olmuyordu (event sessizce hiç dinlenmiyordu) -
      `RegisterServicesFromAssemblyContaining<WikiPageCreatedEventHandler>()`
      eklenerek düzeltildi.
      **Bilinçli teknik borç (Gün 6 retrospektifinde tekrar ele alınacak):**
      MediatR'ın `IPublisher`'ı tüm abonelerini AYNI senkron çağrı zincirinde
      çalıştırıyor - wiki sayfası oluşturma isteği artık HEM Notifications'ın
      SignalR yayınını HEM AI'ın chunk'lama+embedding+Postgres'e yazma işlemini
      bitirmesini bekliyor, ve iki farklı veritabanı (SQL Server + Postgres)
      arasında hiçbir atomicity garantisi yok. `WikiPageCreatedEventHandler`
      (AI.Infrastructure) bu yüzden kendi içinde try/catch ile hatayı yutuyor
      (loglayıp durduruyor) - aksi halde embedding üretimi başarısız olduğunda
      wiki sayfası SQL Server'a zaten kaydedilmiş olmasına rağmen istemciye
      500 dönerdi. Bu, en başta konuşulan Transactional Outbox Pattern'in
      çözdüğü problem - kalıcı, retry edilebilir bir tetikleyici yerine
      "en iyi çaba" (best-effort) bir tetikleyici kullanıyoruz şu an.

- [x] **AI Semantik Arama - Gün 4/6:** `SearchWikiPagesByMeaningQuery` + Handler
      (AI.Application) yazıldı - sorguyu embed edip `IWikiPageEmbeddingRepository.
      FindNearestAsync` ile pgvector `CosineDistance` sıralamasıyla en yakın
      chunk'ları çekiyor, sayfa başına en yakın chunk'ı seçip (`GroupBy` +
      `MinBy(Distance)`) skora göre sıralıyor. `WikiPageEmbedding`'e denormalize
      `Title`/`DepartmentName`/`Visibility` eklendi (migration uygulandı) -
      arama sonucunu Wiki'ye geri sorgu atmadan gösterebilmek VE görünürlük
      filtresini uygulayabilmek için. Yeni `IWikiVisibilityChecker`
      (Shared.Contracts) - Wiki'nin görünürlük kuralı AI tarafından, modül
      izolasyonu bozulmadan ödünç alınıyor (`ICurrentUserAccessor` ile aynı
      desen). Yeni `Atlas.Modules.AI.Application.Tests` projesi - Handler elle
      yazılmış fake'lerle izole test edildi (mocking kütüphanesi eklemeden);
      en kritik test departman güvenlik filtresini doğruluyor. **Henüz bir HTTP
      endpoint'i YOK** - bu bilerek Gün 5'e bırakıldı (erken atlanmadı).

- [x] **Admin rolü tüm departmanları görebilir/yazabilir (bypass):**
      `ICurrentUserAccessor.IsAdmin` eklendi (mevcut Role claim'inden okunuyor).
      `WikiVisibilityRules.IsVisibleTo` artık bir `viewerIsAdmin` parametresi
      alıyor (varsayılan `false`) - Admin ise departman sınırı görmezden
      geliniyor. Kural TEK yerde tanımlı olduğu için Wiki listesi, AI arama
      VE sayfa oluşturma/silme hepsi bu bypass'ı otomatik aldı.

- [x] **Wiki sayfası oluşturmada departman artık istemciden değil JWT'den
      geliyor** - Ders #13'teki yazma tarafı açığının düzeltmesi. Normal
      kullanıcı sadece kendi departmanına yazabiliyor, Admin istediği
      departmanı seçebiliyor (okuma tarafındaki bypass'la simetrik).
      Departmansız bir normal kullanıcı artık hiç sayfa oluşturamıyor (anlaşılır
      400 hatası). Bu yüzden `CreateWikiPageCommandValidator`'daki eski
      `DepartmentName` NotEmpty kuralı KALDIRILDI - artık zararlı hale gelmişti
      (normal kullanıcı departmanı BİLEREK boş gönderiyor, Handler'ın override
      etmesi için; bu kural isteği Handler'a hiç ulaşmadan reddediyordu).

- [x] **Wiki sayfası silme + cache invalidation:** `DeleteWikiPageCommand`/Handler
      - Admin HER sayfayı, normal kullanıcı SADECE kendi oluşturduğunu silebiliyor
      (`UnauthorizedAccessException` → yeni 403 dalı, `GlobalExceptionHandler`).
      Bu sırada fark edilen ayrı bir gecikme sorunu: `GetAllWikiPagesRawQuery`'nin
      30sn'lik cache'i ne oluşturmada ne silmede temizlenmiyordu. `CachingBehavior`'ın
      simetriği olan generic bir `ICacheInvalidatingCommand` + `CacheInvalidationBehavior`
      eklendi (Shared.CQRS) - artık Create/Delete Handler çalıştıktan sonra ilgili
      cache anahtarını kendiliğinden temizliyor.

- [x] **Register sayfası eklendi (frontend'de daha önce HİÇ yoktu):** Departman
      seçimi serbest metin değil, sabit bir listeden (`departments.js`, tek
      doğruluk kaynağı) - RadioGroup ile (WikiBoard'daki Görünürlük seçimiyle
      aynı desen). Register'da email zaten kayıtlıysa artık anlamsız bir 500
      yerine anlaşılır bir 400 dönüyor (`RegisterUserCommandValidator`'a
      `IUserRepository` enjekte edilip async `MustAsync` kuralı eklendi).

- [x] **Wiki listesi UI iyileştirmesi:** İçerik kolonu artık 80 karakterden
      uzun metinleri kesiyor (satırlar dev boyutlara şişmiyordu), satıra
      tıklanınca tam içeriği gösteren salt-okunur bir detay Dialog'u açılıyor.
      `jwt.js` eklendi - JWT payload'ı (Role/department/nameidentifier) SADECE
      UI kararları için (buton/alan göster-gizle) çözülüyor, gerçek yetkilendirme
      her zaman backend'de.

- [x] **AI Semantik Arama - Gün 5/6:** `GET /api/ai/search?q=...&topN=5`
      eklendi (token gerektiriyor - sonuçlar zaten departmana göre filtreleniyor,
      anonim istek kafa karıştırıcı olurdu). `SearchWikiPagesByMeaningQueryValidator`
      (FluentValidation) - boş sorgu, 500 karakterden uzun sorgu, TopN aralık
      dışı (1-50) erken 400 ile kesiliyor. Swagger otomatik yakaladı, ekstra
      bir şey yapmaya gerek kalmadı. Canlı doğrulandı: alakalı bir Türkçe
      sayfa gerçek bir pozitif skorla (0.52) en üstte çıktı, alakasızlar 0
      skorla altta kaldı; departman görünürlük filtresi arama sonuçlarında
      da doğru çalıştı (IT kullanıcısı IK'nın gizli sayfasını göremedi, Admin
      bypass etti).

- [x] **Semantik arama için frontend UI:** `WikiSearch.jsx` - bilerek AYRI bir
      component (WikiBoard.jsx'e eklenmedi, o zaten 350 satıra yaklaşmıştı).
      Arama sonucu her satır başlık/departman/skor (yüzde, cosine mesafesinden
      çevrilmiş) + chunk metnini gösteriyor. Tarayıcıda canlı doğrulandı.

- [x] **Arama sonucuna tıklanınca tam sayfa açılıyor:** `GET /api/wiki/pages/{id}`
      eklendi (aynı görünürlük kuralı burada da uygulanıyor - Id'yi bilmek
      görebilmek anlamına gelmiyor). `WikiSearch.jsx`'teki her sonuç artık
      WikiBoard'daki detay dialoguyla aynı desende tıklanınca açılıyor.

- [x] **Silinen sayfanın embedding'i de temizleniyor (hayalet arama sonucu
      bug'ı):** `WikiPageDeletedEvent` eklendi (WikiPageCreatedEvent'in silme
      tarafındaki karşılığı) - AI artık bir sayfa silinince kendi
      embedding'lerini de temizliyor. Bunsuz, silinen bir sayfa arama
      sonuçlarında sonsuza kadar "hayalet" olarak çıkmaya devam ediyordu
      (canlı doğrulandı).

- [x] **Sıfır-vektör NaN çökmesi + admin reindex aracı:** İçeriği sadece
      noktalama işaretlerinden oluşan bir chunk (örn. "????????") sıfır vektör
      üretiyordu, bu da pgvector'ın cosine distance hesabında NaN'a yol açıp
      TÜM arama isteğini çökertiyordu (canlı doğrulandı, küçük veri setlerinde
      tetiklendi). Kaynağında (anlamsız chunk artık kaydedilmiyor) VE savunma
      amaçlı (NaN/Infinity sonuçlar filtreleniyor) düzeltildi.
      `POST /api/wiki/reindex` (Admin) eklendi - var olan tüm sayfalar için
      embedding'leri yeniden üretiyor.

- [x] **Integration testler artık kendi ürettikleri AI verisini temizliyor:**
      `AtlasApiFactory`'nin AI'ın Postgres'ini InMemory'e çevirmemesi (bilinçli
      tasarım - gerçek ingestion'ı test edebilmek için), her test çalıştırmasının
      geride kalıcı "yetim" embedding bırakmasına yol açıyordu (36+ birikmişti,
      canlı doğrulandı). Testler artık oluşturdukları sayfaların id'lerini
      try/finally ile takip edip sadece kendi verilerini temizliyor.

## Transactional Outbox Pattern (Gün 6 retrospektifinde ayrı bir özellik olarak açıldı)

AI Semantik Arama'nın Gün 3/Gün 6 notlarındaki teknik borç (MediatR'ın
`IPublisher`'ı event'i in-process, KALICI OLMAYAN şekilde yayınlıyor - process
WikiPage kaydedildikten hemen sonra çökerse event sonsuza kadar kaybolur,
ayrıca wiki sayfası oluşturma isteği AI'ın embedding üretimini bitirmesini
bekliyor) artık kendi 5 günlük planıyla ele alınıyor - küçük bir yama değil,
gerçek bir mimari değişiklik olduğu için.

**Plan:**
1. Altyapı: `OutboxMessage` entity + migration + `IOutboxWriter` soyutlaması. ✅ (bugün)
2. Atomik yazma: Command Handler'lar `IPublisher.Publish` yerine `IOutboxWriter.Enqueue`
   kullanacak, WikiPage'in KENDİSİYLE aynı `SaveChanges` çağrısında (atomiklik).
3. Arka plan işleyici (`IHostedService`, polling) - işlenmemiş mesajları okuyup
   gerçek `Publish`'i tetikler. Notifications/AI'ın mevcut handler'ları HİÇ değişmeden çalışır.
4. Hata/retry stratejisi.
5. Integration test + wrap-up.

- [x] **Gün 1/5:** `OutboxMessage` (Wiki.Domain) - `EventType`
      (`AssemblyQualifiedName`, arka plan işleyicinin deserialize edebilmesi
      için) + `Payload` (JSON) + `ProcessedAtUtc` (null = henüz işlenmedi) +
      `Attempts`/`LastError` (Gün 4'teki retry için). Migration uygulandı
      (`wiki.OutboxMessages`, `ProcessedAtUtc` üzerinde index). `IOutboxWriter`
      (Wiki.Application) + `EfOutboxWriter` (Wiki.Infrastructure) - `Enqueue()`
      BİLEREK senkron ve kendi `SaveChanges`'ini çağırmıyor, sadece change
      tracker'a ekliyor; asıl atomiklik Gün 2'de Handler'ların WikiPage için
      zaten yapacağı `SaveChangesAsync` ile AYNI anda sağlanacak.

- [x] **Gün 2/5:** `CreateWikiPageCommandHandler`/`DeleteWikiPageCommandHandler`
      artık `IPublisher.Publish` kullanmıyor - `IOutboxWriter.Enqueue` ile AYNI
      değişiklik kümesine bir `OutboxMessage` ekliyor. `IWikiPageRepository.AddAsync`/
      `DeleteAsync` artık kendi `SaveChangesAsync()`'ini ÇAĞIRMIYOR (sadece stage
      ediyor) - yeni `IUnitOfWork`/`EfUnitOfWork`, WikiPage + Outbox mesajını TEK
      bir `SaveChanges`'te (atomik) yazıyor.

  ~~⚠️ BİLİNÇLİ, GEÇİCİ ARA DURUM~~ - **Gün 3 ile ÇÖZÜLDÜ**, aşağı bkz.

- [x] **Gün 3/5:** `OutboxProcessor` (Wiki.Infrastructure/Outbox, `BackgroundService`) -
      5 saniyede bir işlenmemiş `OutboxMessage`'ları okuyup `Payload`'ı `EventType`'a
      (`AssemblyQualifiedName`) göre deserialize edip gerçek `IPublisher.Publish`'i
      TETİKLİYOR, başarılıysa `MarkProcessed()` çağırıyor. Notifications/AI'ın
      event handler'ları HİÇ DEĞİŞMEDİ - Outbox sadece NE ZAMAN yayınlanacağını
      değiştirdi. Singleton bir `BackgroundService`'in Scoped `WikiDbContext`/
      `IPublisher`'a erişmesi gerektiği için her turda `IServiceScopeFactory`
      ile yeni bir scope açılıyor.

      Gün 2'de `[Skip]` edilen 2 integration test geri açıldı - artık "eventual
      consistency" bekleyen bir retry helper kullanıyorlar (poll interval + pay,
      10sn timeout) çünkü ingestion artık senkron değil. **Uygulama artık Gün 2'nin
      bıraktığı yarım kalmış durumdan çıktı, tekrar tam işlevsel** - canlı
      doğrulandı: sayfa oluşturulduktan hemen sonra aramada yok, ~6 saniye sonra
      tam skoruyla çıkıyor.

- [x] **Gün 4/5:** Hata/retry stratejisi - "dead letter" mantığı eklendi.
      Yeni migration/kolon GEREKMEDİ - `Attempts` zaten vardı. `OutboxMessage`'a
      `MaxAttempts = 5` sabiti + `IsDeadLettered` (hesaplanan, `ProcessedAtUtc
      is null && Attempts >= MaxAttempts`) eklendi. `OutboxProcessor`'ın sorgusu
      `Attempts < MaxAttempts` filtresiyle daraltıldı - eşiği aşan bir mesaj
      sorgudan düşüyor (bir daha hiç denenmiyor) ama satır DB'de SİLİNMEDEN,
      `Attempts`/`LastError` ile hâlâ görünür/incelenebilir duruyor (istenen
      tam olarak buydu: "bir daha hiç denenmesin, sadece görünür kalsın").
      Dead letter'a düşen mesaj için ayrı bir `LogWarning` eklendi ("artık
      sessizce kayboldu değil, birinin bakması gerekiyor" sinyali). Backoff
      (bekleme süresini kademeli artırma) BİLEREK eklenmedi - 5sn'lik sabit
      poll interval'ın kendisi zaten denemeler arasına doğal bir boşluk
      koyuyor, ek karmaşıklık şimdilik YAGNI.

- [x] **Gün 5/5:** Integration test + wrap-up. Yeni `OutboxIntegrationTests.cs` -
      gerçek bir "process SaveChanges ortasında çöktü" senaryosu integration
      testte simüle edilemez, ama HTTP isteği 200 döndüğü anda hem `WikiPage`
      hem `OutboxMessage`'ın (doğru `EventType`/`Payload` ile) DB'de var
      olduğunu doğrudan doğruluyor - bu, atomikliğin test edilebilir kısmı.
      **Bilinçli tasarım kararı:** Test, mesajın `ProcessedAtUtc`'sinin NULL
      OLMASINI beklemiyor - `OutboxProcessor` test host'unda da gerçek bir
      `BackgroundService` olarak çalıştığı için mesaj birkaç saniye içinde
      işlenebilir, bunu bekleyen bir assertion flaky olurdu. Sadece satırın
      doğru içerikle VAR OLDUĞUNU kontrol ediyor.

      **Bu günün asıl bulgusu bir regresyondu:** Gün 4 sırasında (üretimdeki
      "Jwt:Key bulunamıyor" krizini araştırırken, bkz. Ders #16) `AddAuthModule`
      içinde `configuration["Jwt:Key"]`'i lazy'den eager okumaya çeviren bir
      "sağlamlaştırma" denenmişti - bu integration testleri SESSİZCE kırmıştı
      (`WikiEndpointsTests` dahil, sadece Domain testleri çalıştırıldığı için
      fark edilmemişti). `dotnet test tests/Atlas.IntegrationTests` çalıştırılınca
      hepsi 401 ile başarısız oldu - kök sebep bulunup (`WebApplicationFactory`'nin
      test config override'ı o erken noktada henüz uygulanmamış oluyordu) eager
      okuma geri alındı, testler tekrar yeşile döndü (bkz. Ders #16'nın sonundaki
      not). **Genel ders:** bir config/DI değişikliğinden sonra sadece ilgili
      Domain testlerini değil, TÜM solution'ı (`dotnet test Atlas.sln`) çalıştırmak
      gerekiyor - regresyon başka bir katmanda (burada: test host'un HTTP
      pipeline'ında) çıkabiliyor.

      Tüm solution (`dotnet test Atlas.sln`) yeşil: Domain/Application/
      Infrastructure testleri + 10 integration test, toplam ~70+ test.

**Transactional Outbox Pattern artık TAMAMLANDI (Gün 1-5):** OutboxMessage
entity → atomik yazma (IUnitOfWork) → arka plan işleyici (OutboxProcessor) →
dead-letter/retry sınırı → integration test. Wiki artık AI'ın (veya
Notifications'ın) var olduğundan habersiz kalmaya devam ederken, event
teslimatı crash-safe ve atomik.

## İzlenecek teknik borç (henüz aksiyon gerektirmiyor, büyürse ele alınmalı)

- ~~`FakeCurrentUserAccessor` kopyası~~ - 2026-07-28'de `Atlas.Shared.Testing`'e
  taşınarak ÇÖZÜLDÜ (bkz. Portföy sertleştirme yol haritası, 3. adım).
- ~~`WikiBoard.jsx`'in tek component'te dört sorumluluğu~~ - 2026-07-28'de
  `CreateWikiPageDialog`/`WikiPageTable`'a bölünerek ÇÖZÜLDÜ (bkz. Portföy
  sertleştirme yol haritası, 3. adım).
- `WikiVisibilityRules.IsVisibleTo`'ya eklenen `viewerIsAdmin` bool parametresi
  şu an temiz ama ölçeklenmiyor - ÜÇÜNCÜ bir rol (örn. "Departman Yöneticisi")
  eklenirse bir `UserRole` enum'ı ya da capability seti'ne geçmek gerekir
  (şimdiden yapmak YAGNI ihlali olurdu).

## Portföy sertleştirme yol haritası (2026-07-28, Outbox Pattern bitince açıldı)

Kullanıcıyla birlikte kararlaştırıldı - sırayla: 1) CI/CD, 2) Observability,
3) Teknik borç ödeme, 4) Yeni kullanıcı-görünür özellik.

- [x] **1) CI/CD pipeline:** `.github/workflows/ci.yml` - push/PR'da otomatik
      build+test (backend) ve build+lint (frontend). Integration testleri
      (`Atlas.IntegrationTests`) CI'da ÇALIŞTIRILMIYOR - gerçek SQL Server/
      Postgres/Redis'e ihtiyaç duyuyorlar, servis container'larıyla CI'a
      taşımak ayrı bir iş olarak bırakıldı (bilinçli kapsam sınırı). Bunun
      yerine tüm integration test sınıflarına `[Trait("Category",
      "Integration")]` eklendi, CI `--filter "Category!=Integration"` ile
      sadece Domain/Application/Infrastructure testlerini (dış bağımlılığı
      olmayan, ~70 test) çalıştırıyor - bu sayede yeni bir test projesi
      eklendiğinde CI dosyasını elle güncellemeye gerek kalmıyor, sadece
      doğru trait'i eklemek yeterli. README'ye durum rozeti eklendi.
- [x] **2) Observability:** Serilog (`Serilog.AspNetCore`) varsayılan
      `Microsoft.Extensions.Logging`'in yerini aldı. Yeni `CorrelationIdMiddleware`
      (Atlas.Api/Observability) - pipeline'ın EN BAŞINDA, her isteğe bir
      `X-Correlation-Id` kazandırıyor (istemci gönderirse onu kullanır, yoksa
      üretir, yanıta da geri yazar) ve `LogContext.PushProperty` ile Serilog'un
      "ambient" bağlamına ekliyor. Sonuç: o istek sırasında oluşan HER log
      satırı (EF Core sorguları, `LoggingBehavior`'ın CQRS logları, Exception
      Handler) hiçbir mevcut log çağrısı DEĞİŞTİRİLMEDEN aynı ID'yi otomatik
      taşıyor - canlı doğrulandı (`X-Correlation-Id: login-test-456` gönderilen
      bir login isteğinin TÜM log satırları, DB sorgularından CQRS'e kadar,
      bu ID'yi taşıdı). `UseSerilogRequestLogging()` ile her isteğin sonunda
      method/path/status/süre içeren tek bir özet satır da ekleniyor.
      appsettings üzerinden değil kod içinde yapılandırıldı (tek ortam için
      appsettings şeması eklemek şimdilik gereksiz karmaşıklık olurdu).
- [x] **3) Teknik borç ödeme:** `WikiBoard.jsx` (~350 satır) `CreateWikiPageDialog`/
      `WikiPageTable`'a bölündü - her biri kendi form/dialog/hata state'ini
      kendi yönetiyor, parent'a sadece "liste değişti, yenile" callback'iyle
      haber veriyor. Yan etki: silme/listeleme hataları artık kullanıcıya
      gösteriliyor (eskiden aynı error state'i sadece oluşturma dialogunun
      içinde render ediliyordu). Tarayıcıda uçtan uca doğrulandı. Ayrıca
      `FakeCurrentUserAccessor` kopyası yeni `Atlas.Shared.Testing` projesine
      taşındı (diğer test-özel fake'ler kendi projelerinde kaldı, sadece bu
      genuinely aynı olan implementasyon paylaşıldı).
- [ ] **4) Yeni kullanıcı-görünür özellik: Audit log** (üçüncü rol yerine
      seçildi - daha net kapsamlı, mevcut mimariyle - CachingBehavior/
      CacheInvalidationBehavior deseniyle - doğal örtüştüğü için tercih
      edildi; üçüncü rol daha büyük bir ürün kararı gerektirdiği için
      YAGNI gerekçesiyle ertelenmeye devam ediyor).
  - [x] **Gün 1/3 - Domain modeli + pipeline behavior:** Yeni
        `Atlas.Modules.Audit` modülü (Domain/Infrastructure/Api, `audit.*`
        şeması - Auth/Wiki ile AYNI SQL Server veritabanı, ayrı bir DB
        gerekmedi). `AuditLogEntry` entity - UserId/UserEmail BİLEREK
        denormalize (WikiPageEmbedding'in Title/DepartmentName'i
        denormalize etmesiyle aynı gerekçe - kullanıcı silinse/değişse
        bile audit kaydı o anki gerçeği göstermeye devam etsin).
        Cross-module iletişim `ICurrentUserAccessor`/`IWikiVisibilityChecker`
        ile AYNI desen: `IAuditLogWriter` (Shared.Contracts) + yeni bir
        marker interface `IAuditableCommand` (Shared.CQRS/Behaviors,
        `ICacheInvalidatingCommand` ile aynı yapı) + `AuditBehavior`
        (`CacheInvalidationBehavior` ile birebir aynı iskelet - generic
        constraint `where TRequest : notnull, IAuditableCommand` sayesinde
        MediatR bu behavior'ı SADECE işaretlenen komutlar için pipeline'a
        sokuyor). `CreateWikiPageCommand`/`DeleteWikiPageCommand` ilk
        denetlenen eylemler oldu - Create'te `AuditResourceId` BİLEREK null
        (yeni sayfanın ID'si Handler çalışana kadar bilinmiyor), bu durumda
        `AuditBehavior` `TResponse`'un kendisini (Guid dönüyorsa) kaynak
        ID'si sayıyor; Delete'te ID zaten Command'da olduğu için doğrudan
        dolduruluyor. Audit yazımı BEST-EFFORT - `AuditBehavior`'daki
        try/catch, başarısız bir audit yazımının asıl işlemi (Handler zaten
        tamamlandı) etkilemesini/istemciye 500 dönmesini engelliyor (AI'ın
        `WikiPageCreatedEventHandler`'ının kendi hatasını yutmasıyla aynı
        gerekçe). Canlı doğrulandı: bir sayfa oluşturulup silindi,
        `audit.AuditLogEntries`'de doğru `UserEmail`/`Action`/`ResourceId`
        ile (ikisi de AYNI sayfanın ID'siyle eşleşen) iki satır oluştu.
  - [x] **Gün 2/3 - `GET /api/audit-log` endpoint'i (Admin) + filtreleme:**
        Yeni `Atlas.Modules.Audit.Application` katmanı - `GetAuditLogQuery`
        (Details/FromUtc/ToUtc HEPSİ opsiyonel, PageNumber/PageSize) +
        `IAuditLogRepository` soyutlaması. `EfAuditLogRepository` filtre/
        sayfalamayı DB SEVİYESİNDE yapıyor (Wiki'nin `GetWikiPagesQueryHandler`'ının
        "tüm veriyi çek, bellekte filtrele" yaklaşımının BİLEREK aksine) -
        audit log zamanla büyümesi beklenen, tüm satırlarını cache'lemenin/
        belleğe çekmenin mantıklı olmadığı bir tablo. Endpoint sadece Admin
        rolü (`AuthEndpoints`'teki `GET /users` ile aynı desen - audit log'un
        kendisi "kim ne yaptı" taşıdığı için normal bir kullanıcının başkasının
        işlemlerini görmesi ayrı bir gizlilik sorunu olurdu). **Gün 3'ten sonra
        değişti (2026-07-28, kullanıcı geri bildirimi):** filtre başlangıçta
        Action'a (tam eşleşme) göreydi, ama Action sadece iki sabit değerden
        biri olduğu için pratik değildi - "hangi sayfa" sorusuna cevap
        vermiyordu. `Details` (sayfa başlığı) üzerinden KISMİ eşleşme
        (`Contains`) aramaya çevrildi - "bu sayfayla ilgili tüm işlemleri
        göster" gibi gerçek bir ihtiyaca karşılık geliyor. Canlı doğrulandı:
        filtresiz liste (en yeni önce, doğru `totalCount`/`totalPages`),
        `?details=...` kısmi eşleşmesi 28 kayıt arasından ilgili sayfanın
        Created+Deleted satırlarını doğru buldu, Admin olmayan kullanıcı 403,
        token'sız istek 401.
  - [x] **Gün 3/3:** Frontend'de audit log görüntüleme sayfası. Yeni
        `AuditLogPage.jsx` - eylem/tarih aralığı filtresi + sayfalama,
        `WikiBoard.jsx`'e sadece Admin'in gördüğü bir "Audit Log" linki
        eklendi (gerçek yetkilendirme zaten backend'de `RequireRole
        ("Admin")` - buradaki kontrol WikiPageTable'daki "Sil" butonuyla
        aynı desen, sadece UI kararı). Tarayıcıda uçtan uca doğrulandı:
        Admin girişiyle liste/filtre çalışıyor, Member hesabıyla sayfa
        "yetkin yok" mesajı gösteriyor.
  - [x] **PR incelemesinde bulunan eksiklik - `Details` alanı:** `AuditLogEntry`
        sadece bir `ResourceId` (GUID) tutuyordu - özellikle `WikiPage.Deleted`
        kayıtlarında bu anlamsız kalıyordu, çünkü sayfa gerçekten silindiği
        için title'a başka HİÇBİR yerden erişilemiyordu ("ne silindiğini nereden
        bileceğiz" sorusu PR incelemesi sırasında ortaya çıktı). `WikiPageEmbedding`'in
        Title denormalizasyonuyla AYNI gerekçeyle yeni bir `Details` alanı
        eklendi. `IAuditableCommand.AuditDetails` BİLEREK settable (get-only
        değil) - `AuditResourceId`'nin aksine, title çoğu zaman Command
        oluşturulduğu anda değil (Delete'te SADECE PageId var), Handler
        içinde (silmeden ÖNCE ilgili kaydı çektikten sonra) ortaya çıkıyor;
        Handler `Handle()` içinde bu alanı dolduruyor, `AuditBehavior`
        `next()` sonrası okuyor - aynı Command nesnesi pipeline boyunca
        taşındığı için bu mutasyon güvenle görülüyor. Migration uygulandı,
        frontend'e "Detay" sütunu eklendi. Canlı doğrulandı: bir sayfa
        oluşturulup silindi, hem Created hem Deleted audit satırında doğru
        başlık (`"Detail Alani Testi"`) göründü.

**Portföy sertleştirme yol haritası artık TAMAMLANDI (1-4).** Audit log
özelliği baştan sona bitti: domain modeli → pipeline behavior → endpoint →
frontend sayfası.

## Portföy yol haritası bitince açılan ek işler (2026-07-28)

Kullanıcıyla birlikte üç aday belirlendi: Docker Compose tam paketleme,
SignalR bildirim UX düzeltmesi, rate limiting. Cuma'ya kadar hedeflendi,
üçü de bitti - paralel/bağımsız branch'lerde ilerledi (Docker Compose PR #2,
SignalR toast PR #3, rate limiting PR #4, audit log Gün 3 + Details
düzeltmesi PR #1), hepsi merge edildi.

- [x] **Docker Compose tam paketleme:** `docker compose up --build` artık
      HER ŞEYİ (SQL Server, Postgres+pgvector, Redis, backend API, frontend)
      tek komutla ayağa kaldırıyor - önceden sadece Redis+Postgres vardı,
      SQL Server Express native kurulu olmalıydı, `npm install`/User Secrets
      elle ayarlanmalıydı. Yeni `Dockerfile`'lar (Atlas.Api + Web/apps/platform),
      `docker-compose.yml`'ye `sqlserver`/`atlas-api`/`atlas-web` servisleri
      eklendi. **Native dev kurulumuna (appsettings.json, User Secrets) hiç
      dokunulmadı** - konteyner servisi kendi bağlantı dizelerini/JWT
      anahtarını ortam değişkenleriyle (`ConnectionStrings__*`, `Jwt__*`)
      override ediyor, ikisi tamamen bağımsız. Host portları (5173/5000)
      native kurulumla AYNI tutuldu ki CORS hiç değişmesin. Canlı doğrulandı:
      sıfırdan (boş volume) başlatıldı, migration'lar otomatik uygulandı,
      admin otomatik seed edildi, giriş/wiki/audit log uçtan uca çalıştı.
- [x] **SignalR bildirim UX düzeltmesi:** Yeni wiki sayfası bildirimi eskiden
      akışı tamamen kilitleyen bir `alert()` popup'ıydı - `sonner` tabanlı bir
      toast'a geçirildi (`Web/packages/ui/src/sonner.jsx`, Ders #11'deki
      alışılmış shadcn-CLI-sonra-taşı akışıyla eklendi). `next-themes`
      bağımlılığı BİLEREK kaldırıldı - proje Next.js değil, sabit tek bir
      koyu tema kullanıyor, `Toaster`'da doğrudan `theme="dark"` sabitlendi.
      Tarayıcıda uçtan uca doğrulandı.
- [x] **Rate limiting:** ASP.NET Core'un yerleşik
      `Microsoft.AspNetCore.RateLimiting` middleware'i (ekstra paket
      gerekmedi). İki anahtar-bazlı (partitioned) politika - `"login"` (IP
      bazlı, dakikada 5 - brute-force'a karşı, kullanıcı bazlı olamaz çünkü
      login sırasında kimlik henüz bilinmiyor) ve `"ai-search"` (JWT
      NameIdentifier bazlı, dakikada 20 - embedding çağrısı + vector arama
      "ucuz" bir işlem değil). `UseRateLimiter()` BİLİNÇLİ OLARAK
      `UseAuthorization()`'dan SONRA - `ai-search` politikası
      `HttpContext.User`'ı okuyor. Canlı doğrulandı: login'e 7 istekten
      6./7.'si 429, ai-search'e 22 istekten 21./22.'si 429. Entegrasyon
      testleri etkilenmedi (her test sınıfı kendi rate limiter sayaçlarını
      alıyor - ayrı `WebApplicationFactory` instance'ı).

## 6 maddelik özellik listesinin eksik kalan 3 parçası (2026-08-04)

Ana sayfa/Wikipedia görünümü işi bittikten sonra kullanıcı orijinal 6 maddelik
listeyi ("bunlardan eksik olan var mı") tekrar sordu. Denetim sonucu GERÇEK,
aksiyon gerektiren sadece 3 eksik çıktı (geri kalanı ya tamamlanmıştı ya da
bilinçli olarak ertelenmişti) - kullanıcı üçüne de "evet" diyerek onayladı,
önerilen sırayla yapıldı:

- [x] **(a) Link penceresinde arama/filtreleme:** `WikiEditorPage.jsx`'teki
      link ekleme penceresi eskiden mevcut klasör ağacından çıkarılan SABİT,
      filtrelenemeyen bir sayfa listesi gösteriyordu - departman büyüdükçe
      kullanışsız hale geliyordu. Üst bardaki aramayla (bkz. WikiLayout) AYNI
      hafif öneri endpoint'i (`getWikiSearchSuggestions`, debounce'lu) burada
      da kullanılmaya başlandı - artık TÜM görünür sayfalar arasında (sadece
      mevcut departmanın klasör ağacı değil) arama yapılabiliyor, backend'in
      görünürlük filtresi burada da otomatik uygulanıyor.
- [x] **(b) Kırmızı link (red link) mekanizması:** Wikipedia'nın "henüz
      yazılmamış makaleye bağlantı" fikri - link aramasında eşleşen bir sayfa
      YOKSA, "'X' adında bir sayfa yok - kırmızı bağlantı olarak ekle" seçeneği
      çıkıyor. Var olan `wiki:GUID` hedefinden FARKLI olarak `wiki-new:Başlık`
      (URL-encoded) sözdizimi kullanılıyor - hedef sayfa henüz yok, GUID'i de
      yok. `markdown.jsx`'in render katmanı bu hedefi kırmızı, kesikli alt
      çizgili bir bağlantı olarak gösteriyor (bkz. `INLINE_PATTERN`'in
      `wiki-new:` dalı), tıklanınca `/wiki/new?title=...`'a gidiyor.
      `WikiEditorPage`, `useSearchParams` ile bu `title` parametresini okuyup
      Başlık alanını ÖNCEDEN dolduruyor (sadece oluşturma modunda anlamlı,
      edit modunda zaten gerçek başlık fetch'le geliyor). Tamamen frontend -
      backend değişikliği gerekmedi. Canlı doğrulandı: sayfa oluşturuldu,
      render edildi, kırmızı linke tıklanınca doğru başlıkla dolu "Yeni Sayfa"
      ekranına gidildi.
- [x] **(c) Etiket (tag) sistemi:** Ayrı bir `Tag` entity'si/many-to-many
      ilişki BİLEREK kurulmadı - tek gerçek ihtiyaç "arama sırasında eşleşme",
      ilişkisel bir model bunun için YAGNI olurdu. `WikiPage.Tags` (Domain) -
      virgülle ayrılmış, TEK bir nullable string sütun (`nvarchar(300)`,
      migration uygulandı). Normalizasyon (`NormalizeTags`) Domain'de TEK
      yerde: trim + küçük harf + tekrarsız (`"React, react ,DevOps"` →
      `"react,devops"`) - hem `Create()` hem `Update()` bunu kullanıyor, PUT
      istediği zaman etiketleri boşaltıp yeniden yazabiliyor. Command/DTO/
      endpoint/validator zincirinin TAMAMINA (`CreateWikiPageCommand`,
      `UpdateWikiPageCommand`/`UpdateWikiPageRequest`, `WikiPageDto`,
      `GetAllWikiPagesRawQueryHandler`, `GetWikiPageByIdQueryHandler`) opsiyonel
      bir `Tags` alanı eklendi - hiçbiri KIRILMADI çünkü hepsi ya nullable ya
      da varsayılan değerli. `SearchWikiPageSuggestionsQueryHandler`'a
      ÜÇÜNCÜ bir eşleşme katmanı eklendi: başlık > ETİKET > içerik (bir etiket
      eşleşmesi başlıktan daha zayıf ama içerikte geçen bir kelimeden daha
      güçlü bir sinyal). `WikiEditorPage.jsx`'e "Etiketler" adlı tek bir metin
      alanı (virgülle ayrılmış ham girdi) eklendi, `WikiArticlePage.jsx`
      etiketleri küçük badge'ler olarak gösteriyor. Ayrı bir "etikete göre
      gözat" sayfası BİLEREK eklenmedi - birleşik arama (üst bar) zaten
      etiketleri kapsıyor, kapsamı gereksiz büyütmemek için. Canlı doğrulandı:
      `"Kubernetes, DevOps , kubernetes"` girdisi `"kubernetes,devops"`'a
      normalize edildi, sayfada badge olarak doğru göründü, "devops" (ne
      başlıkta ne içerikte geçen bir kelime) ile arama yapılınca sayfa doğru
      şekilde bulundu.

## Şifre Kasası (Vault) modülü (2026-08-11, Faz 7 tamamlandı - Gün 1-3)

Yeni `Atlas.Modules.Vault` modülü (Domain/Application/Infrastructure/Api,
kendi `vault.*` şeması - Auth/Wiki/Audit ile AYNI SQL Server veritabanı).
`PasswordEntry` WikiPage'e hiç bağlı değil, kullanıcının kendi parola/erişim
bilgilerini tuttuğu tamamen bağımsız bir varlık.

**Şifreleme:** ASP.NET Core'un kendi Data Protection API'si
(`IDataProtectionProvider` - yeni bir NuGet paketi DEĞİL,
`Microsoft.AspNetCore.App` framework referansı yeterli). Anahtarlar
`%LOCALAPPDATA%\AtlasPlatformDataProtectionKeys`'de disk üzerinde kalıcı
(proje klasörü OneDrive senkronizasyonunda - Ders #16'daki User Secrets
riskiyle aynı gerekçeyle oraya yazılmıyor). **Bilinçli, dokümante sınırlama:**
bu production-grade bir password manager (zero-knowledge, client-side
master-password-türetilmiş anahtar, HSM) DEĞİL - sunucu her zaman şifreyi
çözebilir (dahili/trusted-IT-admin modeli), anahtarlar tek instance'ta disk
üzerinde. İç kullanım için makul, gerçek bir ticari password manager'la
karıştırılmamalı.

**Yetkilendirme:** owner-or-Admin (Audit log'un "kim görebilir" kısıtından
FARKLI - burada normal kullanıcı SADECE KENDİ kayıtlarını görür/yönetir,
Admin hepsini). `GetPasswordEntryByIdQuery` "varlığı gizle" deseninde
(null→404), Update/Delete/Reveal throw-based (400/403). **Reveal BİLEREK bir
Command** (Query değil) - `AuditBehavior`'dan geçip audit'lensin diye
("PasswordEntry.Revealed" - kimin ne zaman hangi kaydı gördüğü iz bırakıyor).
`passwordGenerator.js` - `Math.random` DEĞİL `crypto.getRandomValues`
tabanlı, belirsiz karakterleri (0/O, 1/l vb.) hariç tutuyor, her kategoriden
en az bir karakter garantiliyor.

**Frontend:** `/vault`, `/vault/new`, `/vault/:id/edit` (Wiki İÇERİĞİ
DEĞİLLER, top-level route - Audit Log'la aynı gerekçe, ama herkese görünür,
Audit Log'un aksine). `VaultPage.jsx` (liste, kategori filtresi, maskeli
parolalar + Göster/Kopyala - reveal edilen parola state'te cache'leniyor,
gereksiz tekrar-reveal audit kirliliği yaratmasın diye), `VaultEntryFormPage.jsx`
(tam sayfa oluştur/düzenle, edit modunda parola alanı BOŞ başlıyor - eski
şifreli değeri hiç istemciye göndermiyoruz).

**Yapısal garanti:** Vault ASLA `WikiPageCreatedEvent` yayınlamıyor - AI/
arama pipeline'ına hiç girmiyor (bir filtre kuralı değil, mimari olarak o
event'i hiç tetiklemediği için garanti).

## Kapsamlı Geliştirme Paketi (2026-08-11/12, plan dosyası: `crispy-sauteeing-kettle.md`)

Kullanıcı Atlas'ı "wiki sayfalarının olduğu bir site" olmaktan çıkarıp
kapsamlı bir şirket bilgi platformuna dönüştürmek istedi (zengin blok
editörü + gerçek dosya sistemi + gerçek Favoriler/Pinler). 19 bölümlük bir
spec olduğu için önce 3 paralel Explore ajanı + 1 Plan ajanıyla mevcut
mimari uçtan uca analiz edildi, sonra 7 faza (P1-P7) bölünüp sırayla
uygulanmaya başlandı - kullanıcının "hepsini tek seferde bitirme, günlere
yay, önemli şeyleri anlat" talimatına göre.

- [x] **P1 - Favoriler/Pinler (gerçek backend):** Eskiden TAMAMEN
      localStorage'daydı (cihazlar arası senkron olmuyordu, erişimi kaybedilen
      bir sayfa sessizce listede kalmaya devam ediyordu), HomePage'deki
      butonlar dekoratif/disabled'dı. `UserPageFavorite`/`UserPagePin`
      (Wiki.Domain) - İKİ AYRI tablo, bir sayfa aynı anda hem favori hem pin
      olabilir. `ToggleFavoriteCommand`/`TogglePinCommand` BİLEREK
      audit'lenmiyor (güvenlik açısından önemsiz bir eylem). `GetFavoritePagesQuery`/
      `GetPinnedPagesQuery` mevcut `WikiVisibilityRules`'u uyguluyor - erişim
      sonradan kaybedilirse liste sessizce küçülür. `/wiki/favorites`,
      `/wiki/pinned` (Wiki İÇERİĞİ oldukları için `WikiLayout` altında nested -
      Vault/Documents'ın top-level kararının AKSİNE).
- [x] **P2 - Editör blok genişletmesi v2:** Faz 1'in (callout/divider/
      checklist/inline-code) devamı, AYNI mimari karar korunarak (içerik
      modeli hâlâ düz markdown string, JSON blok modeline GEÇİLMEDİ, yeni bir
      editör kütüphanesi EKLENMEDİ). Video bloğu (`:::video`...`:::` - YouTube
      URL'si otomatik `youtube-nocookie.com` iframe'ine, mp4/webm/mov
      `<video>` etiketine, tanınmayan kaynak sade bir linke düşüyor). Hizalı
      resim bloğu (`:::image-left/center/right` - float ile metin görselin
      etrafından dolanıyor). "/" slash-command menüsü (`SlashCommandMenu.jsx` -
      satır başında `/` yazınca 8 blok tipini listeliyor, mevcut Link/Resim
      popover deseniyle AYNI sabit pozisyonda; `onMouseDown`+`preventDefault`
      kullanıyor, `onClick` DEĞİL - textarea'nın focus'unu kaybetmemek için).
      `document:GUID` içerik-referans bloğu BİLEREK bu fazdan çıkarıldı
      (Documents modülü henüz yoktu, test edilemez bir ölü link olurdu) -
      P5'e ertelendi.
- [x] **P3 - Documents modülü temeli:** Yeni `Atlas.Modules.Documents`
      modülü, Vault'un 4 katmanlı yapısını taklit ediyor (kendi `documents.*`
      şeması, AYNI SQL Server veritabanı). `Document` entity WikiPage'e FK'siz,
      tamamen bağımsız - kendi `DepartmentName`/`Visibility` alanları var
      (WikiPage'inkiyle aynı semantik). **Güvenli depolama:** dosyalar
      `wwwroot` DIŞINDA (`%LOCALAPPDATA%\AtlasPlatformDocuments`),
      `UseStaticFiles` HİÇ KULLANILMIYOR - tek erişim yolu authenticated
      `GET /api/documents/{id}/download`. `StorageKey` HİÇBİR ZAMAN kullanıcı
      girdisinden türetilmiyor (`IFileStorageService.SaveAsync` kendi GUID
      tabanlı anahtarını üretir, çağıran bir key ÖNERMİYOR bile) - path
      traversal yapısal olarak imkânsız, bir filtre/sanitizasyon değil.
      Public `DocumentDto` `StorageKey` İÇERMİYOR - indirme endpoint'i içinde
      kullanılan ayrı, internal bir `DocumentDownloadInfoDto` var. Liste/
      detay/indirme yetkilendirmesi MEVCUT `IWikiVisibilityChecker`'ı
      (Shared.Contracts) DOĞRUDAN tekrar kullanıyor - yeni bir görünürlük
      arayüzü icat edilmedi. Update/Delete owner-or-admin (Vault deseni),
      silme diskteki dosyayı da temizliyor. Pozitif uzantı allowlist'i
      (Document/Presentation/Spreadsheet/Data/Image/Video/Audio/Archive
      kategorileri) - reddetme listesi DEĞİL, izin listesi. SHA-256 içerik
      hash'i P6'nın duplicate-detection'ı için şimdiden kolon olarak açıldı
      (ikinci bir migration'dan kaçınmak için, davranış henüz yok).
      Frontend: `DocumentLibraryPage`/`DocumentUploadPage` (sürükle-bırak,
      TAM SAYFA - Dialog DEĞİL, `WikiBoard.jsx`'in Dialog'dan uzaklaşma
      tarihiyle tutarlı)/`DocumentDetailPage`. İndirme JWT'yi URL'e KOYMUYOR -
      authenticated fetch + blob + `URL.createObjectURL` ile tarayıcı indirmesi.
- [x] **P4 - Document processing pipeline (Gün 1-6, TAMAMLANDI):**
  - Gün 1: `TextChunker`, AI.Domain'den yeni paylaşılan `Atlas.Shared.Text`
    projesine taşındı (davranış değişmedi, saf statik algoritma) - artık hem
    AI hem Documents AYNI chunking algoritmasını kullanıyor, kopya kod yok.
  - Gün 2: Wiki'nin Transactional Outbox Pattern'i (`OutboxMessage`/
    `IOutboxWriter`/`IUnitOfWork`/`OutboxProcessor`, 5sn poll, 5 deneme sonrası
    dead-letter) Documents'a BİREBİR kopyalandı - Wiki'nin kendi Gün 1-5'lik
    tarihini tekrar yaşamadan aynı olgunluğa tek adımda ulaştı.
  - Gün 3: `DocumentUploadedEvent`/`DocumentChunksReadyEvent`/
    `DocumentDeletedEvent` (Shared.Contracts) + `IDocumentProcessor` arayüzü
    (`CanProcess(extension)`/`ExtractAsync`, `IEnumerable<>` DI ile
    first-match-wins) + `PlainTextDocumentProcessor` (txt/md/csv/json/xml/
    yaml/sql/log) + `DocumentUploadedEventHandler` (Documents.Infrastructure).
    **AI modülünde daha önce yaşanan hata BAŞTAN önlendi:** `DocumentsModule.cs`'e
    `RegisterServicesFromAssemblyContaining<UploadDocumentCommand>()`'ın
    YANINA `RegisterServicesFromAssemblyContaining<DocumentUploadedEventHandler>()`
    da eklendi - MediatR ilk çağrı SADECE Application assembly'sini tarar,
    handler Infrastructure'da yaşadığı için ikinci satır olmasaydı event
    sessizce hiç dinlenmezdi (bkz. AI Semantik Arama Gün 3'teki AYNI bug).
    Handler mantığı: `MarkExtracting()` + ayrı bir erken `SaveChanges` (yavaş
    bir extraction gerçekten "Extracting" olarak görünsün diye) → processor
    seç (yoksa `NotSupportedException`) → metni çıkar (boşsa hata) → chunk'la →
    `DocumentChunksReadyEvent`'i outbox'a ekle → `MarkReady()`; hata durumunda
    `MarkFailed(ex.Message)`, ASLA rethrow YOK (sonsuz Outbox retry'ı önlüyor).
  - Gün 4: `PdfDocumentProcessor` (**Docnet.Core**, PDFium sarmalayıcısı, MIT),
    `OpenXmlWordDocumentProcessor` (SADECE .docx - eski ikili .doc'u
    AÇAMIYOR, OpenXml SDK'nın yapısal sınırı, .doc/.rtf/.odt bilerek Failed'a
    düşüyor), `OpenXmlPresentationDocumentProcessor` (slayt metni + speaker
    notes, "Slayt N:" önekiyle - P5'te arama sonucunda hangi slayttan
    geldiği görünsün diye), `OpenXmlSpreadsheetDocumentProcessor` (sheet adı +
    satır/hücre metni, `SharedStringTable` çözümlemesiyle - Excel metin
    hücrelerini genelde doğrudan saklamıyor, bir index saklıyor).
    **TEDARİK ZİNCİRİ GÜVENLİĞİ bulgusu:** ilk seçilen `UglyToad.PdfPig`'in
    NuGet'teki sürüm geçmişi ("1.7.0-custom-5", sahip "grinay") GitHub'daki
    RESMİ release listesiyle (v0.1.8...v0.1.15) TUTARSIZDI - kurulmadı,
    `AskUserQuestion` ile kullanıcıya sorulup Docnet.Core'a geçildi (sürüm
    geçmişi önce doğrulandı). 4 gerçek dosya (pdf/docx/pptx/xlsx, programatik
    üretildi) yüklenip hepsinin doğru metni çıkardığı canlı doğrulandı
    (Outbox payload'ları SQL'den doğrudan incelenerek).
  - Gün 5: `ReprocessDocumentCommand` + `POST /api/documents/{id}/reprocess` -
    Wiki'nin `POST /api/wiki/reindex`'iyle AYNI gerekçe (Failed bir belgeyi
    elle yeniden tetikleyebilme) ama Admin-only bulk DEĞİL, owner-or-Admin,
    TEK bir belgeyi hedefliyor (Delete/Update ile aynı yetki deseni). Handler
    yeni bir extraction akışı yazmıyor - var olan StorageKey/ContentType/
    FileExtension ile `DocumentUploadedEvent`'i Outbox'a yeniden yazıyor, zaten
    var olan `DocumentUploadedEventHandler` bunu ilk yüklemedekiyle birebir
    aynı şekilde işliyor. Extracting durumundaki bir belge için erken 400 -
    çift tıklamanın aynı belgeyi iki kez kuyruğa sokmasını engelliyor.
  - Gün 6: `DocumentsProcessingIntegrationTests` - Outbox atomikliği +
    eventual-consistency (Ready/Failed geçişi + `DocumentChunksReadyEvent`) +
    Reprocess'in owner-or-Admin/"hala işleniyor" guard'ı/yeniden kuyruklama
    davranışı. `DocumentsDbContext` de Auth/Wiki/Audit ile AYNI gerekçeyle
    InMemory'e çevrildi (`AtlasApiFactory`).
    **Bu günün asıl bulgusu, P4'ün bir parçası OLMAYAN, bağımsız bir
    regresyondu:** kullanıcının ayrıca eklediği e-posta doğrulama zorunluluğu
    (bkz. Ders #21) register+login yapan TÜM integration testleri (Wiki/
    AiSearch/Outbox/Auth) login adımında 403 ile kırmıştı - CI bu kategoriyi
    zaten atladığı için (gerçek Postgres/Redis'e ihtiyaç duyduğundan) fark
    edilmemişti. Yeni `AuthTestHelper.RegisterVerifyAndLoginAsync` (InMemory
    `AuthDbContext`'ten doğrulama kodunu doğrudan okuyup `POST /api/auth/
    verify-email`'e gönderiyor) dört dosyaya da uygulandı, `dotnet test
    Atlas.sln` yeniden yeşile döndü (135+ test).

  Tüm değişiklikler `feature/document-processing-pipeline` branch'inde,
  3 ayrı commit'te (Gün 1-4 / regresyon düzeltmesi / Gün 5-6).

- [x] **P5 - Documents→AI/RAG entegrasyonu (Gün 1-4, TAMAMLANDI,
      `feature/documents-ai-rag-integration` branch'inde):**
  - Gün 1: `DocumentEmbedding` (AI.Domain) - `WikiPageEmbedding`'e paralel
    ama AYRI bir tablo (WikiPageId/DocumentId farklı kimlik uzayları,
    "polymorphic" tek tablo İCAT EDİLMEDİ). `EmbeddingDimension` sabiti
    (1024) yeni bir `EmbeddingDimensions` statik sınıfına çıkarıldı - iki
    entity de (Wiki+Document) artık TEK bir yerden türüyor.
    `IDocumentEmbeddingRepository`/`EfDocumentEmbeddingRepository` -
    `IWikiPageEmbeddingRepository`'nin birebir kopyası. Migration uygulandı.
  - Gün 2: `GenerateDocumentEmbeddingsCommand` - Wiki'nin karşılığından TEK
    farkı: TextChunker'ı KENDİSİ ÇAĞIRMIYOR, zaten bölünmüş `ChunkTexts`
    alıyor (chunking Documents.Infrastructure'da yapıldı).
    `DocumentChunksReadyEventHandler`/`DocumentDeletedEventHandler`
    (AI.Infrastructure) - `WikiPageCreatedEventHandler`/
    `WikiPageDeletedEventHandler` ile birebir aynı best-effort desen.
  - Gün 3: `SearchWikiPagesByMeaningQuery` → `SearchByMeaningQuery`
    (`AI.Application/Search/Queries`'e taşındı, `WikiSearchResultDto` →
    `SemanticSearchResultDto` ile `SourceType`+`ResourceId`) - artık İKİ
    kaynaktan (Wiki+Documents) aday çekip AYNI `IWikiVisibilityChecker`
    kuralıyla filtreleyip TEK bir listede skora göre birleştiriyor. Endpoint
    (`/api/ai/search`) değişmedi. **İki gerçek bug canlı integration testte
    yakalandı:** (1) iki repository çağrısını `Task.WhenAll` ile "aynı anda"
    başlatmak, AYNI DI scope'undaki Scoped `AiDbContext` üzerinde "a second
    operation was started" hatasıyla HER istekte patlıyordu (EF Core
    DbContext thread-safe değil) - sıralı `await`e çevrildi. (2) yeni uçtan
    uca testin 500ms'lik poll aralığı "ai-search" rate limit'ini (dakikada
    20) aşıp 429 alıyordu (iki ayrı Outbox turu toplam ~10sn sürüyor,
    Wiki'nin tek-hop'lu eşdeğerinden daha uzun) - 3sn'ye çıkarıldı.
  - Gün 4: `WikiSearch.jsx` birleşik sonuçları (kaynak-tipi ikonuyla, BookOpen/
    FileText) gösteriyor, tıklanınca `sourceType`'a göre `/wiki/:id` ya da
    `/documents/:id`'ye gidiyor. P2'de ertelenen `document:GUID` içerik-
    referans bloğu (`markdown.jsx`) bağlandı - `wiki:`den görsel olarak
    AYRIŞTIRILDI (küçük dosya ikonu). Bunu besleyen yeni
    `SearchDocumentSuggestionsQuery` (Documents.Application,
    `GET /api/documents/search-suggestions`) - Wiki'nin var olan endpoint'i
    Documents'a bağımlı KILINMADI (modül izolasyonu), bunun yerine
    WikiEditorPage'in link penceresi İKİ öneri endpoint'ini
    `Promise.allSettled` ile birlikte çağırıp frontend'de birleştiriyor.

  `dotnet test Atlas.sln`: tüm testler (135+ Domain/Application/
  Infrastructure + 19 Integration testi) yeşil.

- [x] **P6 - Belge versiyonlama + toplu yükleme (Gün 1-5, TAMAMLANDI,
      `feature/documents-versioning-bulk-upload` branch'inde):**
  - Gün 1: `DocumentVersion` (Documents.Domain) - `Document`'a FK İLE
    BAĞLI DEĞİL (bu modülde bile FK'ler tercih edilmiyor, temizlik DB
    cascade'ine değil Handler'ın orkestrasyonuna bırakılıyor).
    `CreatedByUserId`/`CreatedByEmail` BİLİNÇLİ bir sadeleştirme - içeriği
    İLK YAZAN değil, o versiyonu DEĞİŞTİREN kişi (orijinal yükleyici zaten
    `Document.CreatedByUserId`'de duruyor). `(DocumentId, VersionNumber)`
    composite unique index. Migration uygulandı.
  - Gün 2: `Document.ReplaceFile` - Status BİLEREK Uploaded'a dönüyor
    (içerik değişti, eski extraction/embedding geçersiz).
    `UploadNewDocumentVersionCommand` - Handler ÖNCE mevcut dosyayı bir
    `DocumentVersion`'a snapshot'layıp SONRA `ReplaceFile`'ı çağırıyor,
    ardından var olan `DocumentUploadedEvent`'i (P4'ten beri var olan
    pipeline'ı YENİDEN KULLANARAK) Outbox'a yazıyor - yeni bir extraction
    akışı icat edilmedi (`ReprocessDocumentCommand`'daki AYNI fikir). Yeni
    endpoint'ler: `POST/GET /api/documents/{id}/versions`,
    `GET .../versions/{versionNumber}/download`. `DeleteDocumentCommandHandler`
    artık versiyon geçmişindeki HER dosyayı da diskten temizliyor.
  - Gün 3: `UploadDocumentCommandHandler` artık aynı `ContentHash`'e sahip
    GÖRÜNÜR bir belge varsa uyarı döndürüyor - YÜKLEMEYİ ENGELLEMİYOR.
    Başka departmanın `DepartmentOnly` belgesiyle eşleşme SESSİZCE yok
    sayılıyor (varlığını bile sızdırmamak için, Ders #10'daki AYNI sınıf
    hata). Yanıt `Guid`'den `UploadDocumentResult`'a değişti - bu,
    `AuditBehavior`'ın "TResponse Guid'se onu ResourceId say" varsayımını
    kırdığı için `AuditResourceId` get-only'den SETTABLE'a çevrildi
    (`AuditDetails`'le AYNI desen) - `AuditBehavior`'ın kendisine
    DOKUNULMADI.
  - Gün 4: `DocumentUploadPage.jsx` artık birden fazla dosya kabul ediyor
    (Visibility/Departman/Açıklama/Etiket ORTAK, Title sadece TEK dosyada
    gösteriliyor, her dosya kendi adından başlık alıyor, SIRAYLA yükleniyor).
    `DocumentDetailPage.jsx`'e versiyon geçmişi + "Yeni Versiyon Yükle"
    formu eklendi - versiyon listesi belge state'inden AYRI bir effect'te
    çekiliyor, yeni versiyon sonrası state iyimser GÜNCELLENMİYOR (sunucudan
    yeniden çekiliyor).
  - Gün 5: `DocumentVersioningIntegrationTests` (5 test) - versiyon
    arşivleme+indirme, owner-or-Admin/"hâlâ işleniyor" guard'ları,
    duplicate-detection'ın görünürlük filtresiyle birlikte doğru çalıştığı.

  `dotnet test Atlas.sln`: tüm testler yeşil (24 Integration testi dahil).

- [x] **P7 - Güvenlik sertleştirme (Gün 1-3, TAMAMLANDI,
      `feature/security-hardening` branch'inde) - "Kapsamlı Geliştirme
      Paketi"nin SON fazı:**
  - Gün 1: `POST /api/vault/entries/{id}/reveal` artık rate-limitli
    (dakikada 10, kullanıcı bazlı) - login/ai-search/email-verification
    zaten korunuyordu, reveal gerçek bir boşluktu. Amaç maliyet DEĞİL VERİ
    SIZINTISI riski (çalınmış bir token'la toplu "reveal" script'i).
    "ai-search" politikasıyla BİREBİR aynı `RateLimitPartition` deseni.
  - Gün 2: `IMalwareScanner` (Documents.Application/Abstractions) +
    `NoOpMalwareScanner` (Documents.Infrastructure) - `IEmbeddingService`nin
    "Fake-önce" felsefesiyle AYNI desen. `UploadDocumentCommandHandler`/
    `UploadNewDocumentVersionCommandHandler` dosyayı DİSKE YAZMADAN ÖNCE
    tarıyor - kirli bulunursa (bugün asla) 400, Document hiç oluşmuyor.
    Gerçek bir tarayıcıya geçişte değişecek TEK yer DI kaydı olacak.
  - Gün 3: `docker-compose.yml`'e `DocumentStorage__RootPath=/data/documents`
    override'ı + `atlas-documents-data` kalıcı volume'ü eklendi -
    Postgres/SQL Server'ın ZATEN kanıtlanmış aynı deseni. Öncesinde bu
    override hiç yoktu, `docker compose down` yüklenen belgeleri sessizce
    siliyordu (DB satırları kalıp artık var olmayan bir dosyaya işaret
    ediyordu).

**Sonradan (2026-08-12, Test & CI sertleştirme paketinin Gün 1'i) tam bir
"up --build" döngüsüyle CANLI DOĞRULANDI:** `docker compose up -d --build`
ile tüm stack sıfırdan ayağa kaldırıldı, bir kullanıcı kaydedilip
doğrulandı, bir belge yüklendi (indirildi, byte-birebir eşleşti,
`status: "Ready"` - embedding pipeline'ı da container içinde sorunsuz
çalıştı). Sonra `docker compose down` (volume'ler SİLİNMEDEN) + `docker
compose up -d` ile TÜM container'lar (sqlserver dahil) sıfırdan yeniden
oluşturuldu - aynı belge tekrar indirildi, içerik byte-birebir AYNIYDI.
Kod değişikliği gerekmedi, sadece P7 Gün 3'ün doğru çalıştığının kanıtıydı.

  Test/lint yeşil (docker-compose.yml değişikliği .NET/JS kodunu
  etkilemiyor, ayrı bir test gerektirmedi).

**"Kapsamlı Geliştirme Paketi" artık TAMAMEN BİTTİ (P1-P7).**

## Test & CI Sertleştirme (2026-08-12, API key'ler gelene kadar sırada ne var
sorusuna kullanıcının 4 seçeneğin HEPSİNİ seçmesiyle açıldı)

`chore/test-ci-hardening` branch'inde, 4 günlük bir paket:

- [x] **Gün 1 - Docker Compose belge volume'ü canlı doğrulandı:** P7 Gün
      3'teki `atlas-documents-data` volume'ü tam bir `docker compose up -d
      --build` döngüsüyle test edildi - belge yüklendi, TÜM container'lar
      (sqlserver dahil) `docker compose down` (volume'ler silinmeden) +
      `up -d` ile sıfırdan yeniden oluşturuldu, dosya byte-birebir aynı
      kaldı. Kod değişikliği gerekmedi.
- [x] **Gün 2 - Integration testleri CI'a taşındı:** Yeni "Integration
      Tests" job'ı (backend'den ayrı, paralel) - SQL Server/Postgres/Redis
      servis container'ları + health check'ler, `ConnectionStrings__*`
      ortam değişkeni override'ları (appsettings.json'daki yerel `.\SQLEXPRESS`
      Windows Authentication CI'da çalışamaz). **CI'a taşınınca BULUNAN
      GERÇEK BUG:** ilk çalıştırmada 6 test "Database 'AtlasPlatform'
      already exists" hatasıyla patladı - xUnit'in varsayılan test sınıfı
      paralelliği, her sınıfın kendi `AtlasApiFactory`'sinin (dolayısıyla
      gerçek SQL Server'a bağlı `VaultDbContext` migration'ının - Vault
      BİLEREK InMemory'e çevrilmiyor) AYNI ANDA "veritabanı yok, oluşturayım"
      durumuna düşüp `CREATE DATABASE` için yarışmasına yol açtı. Yerel
      geliştirmede hiç görülmedi çünkü "AtlasPlatform" veritabanı zaten
      haftalardır var (Migrate() idempotent) - CI'da veritabanı HER
      ÇALIŞTIRMADA sıfırdan olduğu için yarış ortaya çıktı. Çözüm:
      `xunit.runner.json` (`parallelizeTestCollections: false`) - test
      sınıflarının paralel çalışması kapatıldı (yerelde 24/24 test hâlâ
      geçiyor, 30s'den 45s'e çıktı). Branch protection'a "Integration
      Tests" üçüncü zorunlu check olarak eklendi.

  Gün 1-2 için PR #13 açıldı (kullanıcının "sadece bu adımı push'la" istisnai
  onayıyla - GitHub Actions'ı yerel simüle edecek bir araç olmadığı için CI
  değişikliğinin GERÇEK bir Actions çalıştırmasıyla doğrulanması
  gerekiyordu).

- [x] **Gün 3 - Documents modülüne Application-katmanı unit test projesi:**
      Yeni `Atlas.Modules.Documents.Application.Tests` - `Atlas.Modules.AI.
      Application.Tests`'in aynı deseni (elle yazılmış fake'ler, mocking
      kütüphanesi YOK). `FakeDocumentRepository`/`FakeDocumentVersionRepository`/
      `FakeFileStorageService`/`FakeMalwareScanner`/`FakeOutboxWriter`/
      `FakeUnitOfWork`/`FakeWikiVisibilityChecker` - 7 fake, DocumentsDbContext'in
      InMemory'e çevrilip GERÇEK bir integration test'in ihtiyaç duyduğu
      SQL Server/Docker olmadan Handler'ları izole test edebilmek için.
      18 test: `UploadDocumentCommandHandler` (departmansız kullanıcı hatası,
      malware taraması başarısız olunca HİÇBİR ŞEY kalıcı olmuyor, başarılı
      yüklemenin event'i kuyruğa ekliyor, ContentHash duplicate tespiti hem
      görünür hem GİZLİ - farklı departman - senaryosu), `UploadNewDocumentVersionCommandHandler`
      (not-found, owner-olmayan 403, "hâlâ işleniyor" guard'ı, başarılı
      snapshot+replace, Admin başkasının belgesini versiyonlayabiliyor),
      `DeleteDocumentCommandHandler` (not-found, 403, TÜM versiyon
      dosyalarının temizlenmesi, Admin bypass), `ReprocessDocumentCommandHandler`
      (not-found, 403, guard, var olan StorageKey'le yeniden kuyruklama).
      **Bu testlerin asıl değeri integration testlerin YERİNE geçmek değil -
      integration testler hâlâ gerçek SQL Server'a karşı Outbox/EF Core
      davranışını doğruluyor (bkz. P4 Gün 6), bu yeni proje SADECE Handler'ın
      İŞ MANTIĞINI (kim neyi yapabilir, hangi durumda hangi hata) saniyeler
      içinde, Docker'a hiç ihtiyaç duymadan test ediyor** - günlük geliştirme
      döngüsünde çok daha hızlı bir geri bildirim katmanı. `dotnet test
      Atlas.sln` yeniden çalıştırıldı: TÜMÜ yeşil (Domain/Application/
      Infrastructure testleri + 18 yeni test + 24 Integration testi).

- [x] **Gün 4-5 - Frontend'e otomatik test altyapısı (Vitest + React Testing
      Library):** Backend'in "hızlı, dış bağımlılıksız test katmanı" fikrinin
      (bkz. Gün 3) frontend karşılığı - şimdiye kadar frontend'in HİÇ otomatik
      testi yoktu, sadece `npm run lint`/`npm run build` + tarayıcıda elle
      doğrulama vardı. `vitest`/`@testing-library/react`/`@testing-library/
      jest-dom`/`@testing-library/user-event`/`jsdom` eklendi - ayrı bir
      `vitest.config.js` DEĞİL, var olan `vite.config.js`'e bir `test` bloğu
      (Vitest zaten Vite'ın kendi config'ini okuyabiliyor, ayrı bir dosya
      gereksiz bir kopya olurdu). `src/test/setup.js` - jest-dom matcher'larını
      (`toBeInTheDocument()` vb.) `expect()`'e ekliyor.

      İki katman test yazıldı: (1) **saf mantık** - `dateUtils.test.js`
      (Ders #19'daki "...ZZ" bug'ının regresyon testi - bu düzeltme daha önce
      SADECE manuel gözlemle korunuyordu), `jwt.test.js` (JWT claim çözümleme,
      Türkçe karakter senaryosu dahil), `passwordGenerator.test.js`
      (uzunluk/kategori garantisi/belirsiz karakter hariç tutma/gerçek
      rastgelelik). (2) **gerçek bir component render'ı** - `WikiSearch.test.jsx`,
      BİLEREK eklendi çünkü sadece pure-function testleri "test altyapısı
      kuruldu" iddiasını TAM kanıtlamıyordu (jsdom/RTL'in DOM'a gerçekten
      render edip kullanıcı etkileşimini simüle ettiğini göstermiyordu).
      `../api`'deki `searchByMeaning` `vi.mock` ile taklit edildi, component
      `MemoryRouter` ile sarmalandı (`useNavigate` bir Router context'i
      gerektiriyor). 4 senaryo: wiki sayfası + belge sonucunun `sourceType`'a
      göre AYRIŞTIRILMASI (ikon + hedef rota + skor yüzdesi), boş sonuç mesajı,
      API hata durumunda hata mesajı (sonuç listesi render EDİLMİYOR), boş
      sorguyla submit'in arama tetiklemediği (buton zaten disabled).

      **Kendi test kodumda bulunan bir hata (proje kodunda değil):**
      `jwt.test.js`'in `fakeToken` yardımcı fonksiyonu ilk halinde doğrudan
      `btoa(JSON.stringify(obj))` çağırıyordu - Türkçe karakter (ş/ğ/ü/ö/ç/İ)
      içeren bir payload'da `InvalidCharacterError` fırlattı, çünkü `btoa()`
      SADECE Latin-1 (0-255) kabul ediyor. `TextEncoder` ile önce gerçek
      UTF-8 baytlarına çevirip SONRA base64'leyecek şekilde düzeltildi -
      `jwt.js`'in kendisi bu durumu zaten doğru işliyordu (decode tarafında),
      hata sadece test fixture'ımdaydı.

      CI'ın `frontend` job'ına yeni bir "Test" adımı eklendi (`npm run test
      --workspace=web`, Lint ile Build arasında) - ayrı bir job AÇILMADI,
      backend'in Integration testlerinin aksine bu testlerin dış bağımlılığı
      (Docker/gerçek DB) yok, aynı job'da hızlıca çalışıyor. Yerelde
      doğrulandı: `npx vitest run` → 4 dosya, 17 test, hepsi yeşil;
      `npm run lint`/`npm run build` de temiz.

  Dört gün de (Gün 1-5) tamamlandı, PR #13'e eklendi, push edildi ve
  **merge edildi (2026-08-12, `master`'a fast-forward)** - üç zorunlu CI
  check'i de (Backend, Frontend, Integration Tests) yeşildi. Merge sonrası
  yerel `master` senkronize edildi, `chore/test-ci-hardening` local branch
  silindi.

**Test & CI Sertleştirme paketi artık TAMAMEN BİTTİ (Gün 1-5) ve `master`'da.**

## Documents modülüne toplu (bulk) reindex eklendi (2026-08-12)

LLM key geçişine hazırlık denetimi sırasında bulunan gerçek bir eksik
kapatıldı: Wiki'nin `POST /api/wiki/reindex`'i (embedding sağlayıcısı
değişince var olan TÜM sayfaları yeniden işleten Admin aracı) vardı, ama
Documents'ta eşdeğeri hiç yoktu - `POST /api/documents/{id}/reprocess` SADECE
tek bir belgeyi hedefliyordu (owner-or-Admin, "bu belge Failed kaldı" senaryosu
için baştan BİLEREK bulk yapılmamıştı, bkz. `ReprocessDocumentCommand`'ın
orijinal yorumu). Yeni `ReindexDocumentsCommand`/Handler + `POST
/api/documents/reindex` (Admin-only) bu boşluğu kapatıyor - ikisi birbirinin
YERİNE geçmiyor, farklı senaryolara hizmet ediyor.

**Wiki'nin reindex'inden TEK mimari fark:** Wiki'nin `ReindexWikiPagesCommand`'ı
Outbox Pattern'den ÖNCE yazıldığı için hâlâ `IPublisher.Publish` kullanıyor
(retrofit edilmedi) - Documents'ın reindex'i BİLEREK `IOutboxWriter` kullanıyor,
çünkü modülün geri kalanı (Upload/Delete/Reprocess) zaten Outbox'a yazıyor;
yüzlerce belgeyi senkron `Publish` ile işlemek, Outbox'ın çözdüğü "atomiklik/
crash-safety" garantisini bulk bir işlemde tekrar kaybetmek olurdu. Durum
(Ready/Failed/Extracting) fark etmeksizin TÜM belgeler için `DocumentUploadedEvent`
tek bir `SaveChangesAsync`'te (atomik) kuyruğa yazılıyor - `DocumentUploadedEventHandler`
(zaten var olan, Gün 3'ten beri değişmeyen) bunları ilk yüklemedekiyle birebir
aynı şekilde işleyip Extracting→Ready/Failed geçişini kendisi yapıyor. İki yeni
unit test (`Atlas.Modules.Documents.Application.Tests`, var olan Fake'lerle) +
tüm solution (`dotnet test Atlas.sln --filter "Category!=Integration"`) yeşil.

## Voyage AI embedding entegrasyonu - key olmadan yapılabilecek TÜM hazırlık (2026-08-12)

Kullanıcı "şirket key'i sonra verecek, o gelmeden ne yapılabilir" diye sordu -
madde 1'deki (a)-(e) listesinden key'e bağlı OLMAYAN her şey şimdiden yazılıp
test edildi. **DI kaydı hâlâ `FakeEmbeddingService`'te** - hiçbir davranış
değişmedi, bu bilerek "kanatta bekleyen, anahtarı çevirmeyi bekleyen" bir
implementasyon.

- **`VoyageEmbeddingService`** (AI.Infrastructure/Embeddings) - Voyage AI'ın
  `POST /v1/embeddings` sözleşmesine göre yazıldı (endpoint/alan adları resmi
  dokümantasyondan doğrulandı, tahmin edilmedi). Batch bölme: Voyage tek
  istekte en fazla 1000 metin kabul ediyor, `texts.Chunk(1000)` ile bölünüyor
  (`.NET`'in kendi `Chunk()`'ı yeterliydi, elle bir bölme algoritması
  YAZILMADI). Toplam token bütçesi (modele göre 120K-1000K) BİLEREK ayrıca
  hesaplanmıyor - `truncation: true` Voyage'ın aşırı uzun TEK bir metni
  kendisinin kesmesini sağlıyor, tam bir tokenizer eklemek şimdilik YAGNI.
  Sıra garantisi (`IEmbeddingService`'in "çıktı[i] = girdi[i]" sözleşmesi)
  Voyage'ın dönüş sırasına GÜVENMEDEN, her elemanın kendi döndürdüğü `index`
  alanına göre doğru pozisyona yazılmasıyla sağlanıyor.
- **Retry:** 429/5xx/network/timeout GEÇİCİ sayılıp üstel geri çekilmeyle
  (1sn, 2sn) en fazla 3 deneme yapılıyor; 400/401 gibi KALICI hatalar hiç
  tekrar denenmeden fırlatılıyor (geçersiz bir key'le her çağrının 3 katı
  gereksiz istek atmaması için). Polly gibi bir kütüphane EKLENMEDİ - tek bir
  dış çağrı noktası için elle yazılmış bir döngü yeterliydi (gereksiz
  bağımlılık eklememe ilkesi).
- **Fail-fast boyut kontrolü:** dönen vektör `EmbeddingDimensions.Standard`
  (1024) ile eşleşmiyorsa hemen hata fırlatılıyor - Ders #15'teki (sıfır-vektör
  → NaN → tüm arama isteği çöktü) sınıftan bir hatanın pgvector'a ulaşmadan
  yakalanması.
- **`VoyageAiOptions`** - `ApiKey` appsettings.json'da DEĞİL (Jwt:Key'le AYNI
  gerekçe, Ders #16), `Model`/`BaseUrl` appsettings.json'da (`"VoyageAi"`
  bölümü, gizli değiller). `AIModule.cs`'e `AddHttpClient<VoyageEmbeddingService>`
  (typed client, Authorization header'ı DI çözümlenirken Options'tan kuruluyor)
  + `Configure<VoyageAiOptions>` eklendi - **key BOŞKEN bile** uygulama
  sorunsuz açılıyor (canlı doğrulandı: `dotnet run`, `/health` → 200), sadece
  gerçek bir çağrı yapılırsa Voyage 401 döner.
- **Testler key GEREKTİRMİYOR:** `FakeHttpMessageHandler` (mocking kütüphanesi
  yok, projenin kendi Fake deseni) ile `HttpClient` gerçek ağa hiç çıkmadan
  test ediliyor - 6 yeni test (boş girdi, sıra garantisi/ters index, 1000+
  metnin bölünmesi, 429'da retry, 401'de retry YOK, yanlış boyutta fail-fast).
  `dotnet test Atlas.sln --filter "Category!=Integration"` yeşil (Documents
  bulk reindex'ten sonra: 138 test).

**Key geldiğinde yapılacaklar (runbook):**
1. `dotnet user-secrets set "VoyageAi:ApiKey" "..."` (KENDİ terminalinden -
   Ders #16, Claude'un çalıştırdığı bir komut kullanıcının kendi shell'ine
   görünmeyebilir).
2. `AIModule.cs`'te TEK satır: `AddSingleton<IEmbeddingService, FakeEmbeddingService>()`
   → `AddScoped<IEmbeddingService, VoyageEmbeddingService>()` (Singleton
   DEĞİL - artık `HttpClient` gibi dış bir kaynağı sarmalıyor, bkz. "Service
   Lifetime kuralı").
3. `Model`/`output_dimension` kararını (appsettings.json'daki `"VoyageAi:Model"`)
   gerçek key'e karşı doğrula - `EmbeddingDimensions.Standard`in (1024) seçilen
   modelin gerçek çıktısıyla eştiği canlı test edilmeli.
4. `POST /api/wiki/reindex` + `POST /api/documents/reindex` (Admin) - var olan
   TÜM Fake-üretimi embedding'leri gerçek sağlayıcıyla yeniden üret.
5. Bir arama yapıp sonuçların anlam benzerliğine göre (kelime örtüşmesine göre
   DEĞİL) sıralandığını canlı doğrula.

## Makale okunabilirliği - "Okuma Süresi" eklendi (2026-08-12)

Şirketten (Rıdvan) gelen eski bir geri bildirim ("Medium gibi birkaç siteyi
kontrol edip belki ek özellikler ekleyebiliriz") denetlendi. **Önemli
düzeltme:** İçindekiler (TOC, scroll-spy ile aktif başlık takibi, mobilde
çekmece), Okuma Ayarları (yazı boyutu/satır genişliği/satır aralığı/tema,
`ReadingSettingsPanel.jsx`) ve Tam Ekran Okuma Modu **ZATEN VARDI**
(`WikiArticlePage.jsx`, 2026-08-07 tarihli birden fazla kullanıcı geri
bildirimi turuyla inşa edilmiş) - bu, CLAUDE.md'nin "Şu ana kadar
tamamlananlar" listesine hiç girmemiş, GERÇEK bir dokümantasyon boşluğuydu
(kod var, kayıt yok). Denetimde tek somut EKSİK bulundu: Medium'un "X dakika
okuma" göstergesinin bir karşılığı yoktu.

`readingTime.js` (yeni, `dateUtils.js` ile AYNI desen - saf fonksiyon + kendi
Vitest testi) - 200 kelime/dakika varsayımıyla kaba bir tahmin üretiyor,
kod bloklarını/`:::` blok işaretlerini/link URL'lerini (SADECE link metnini
sayıyor) kelime sayısına KATMIYOR - aksi halde uzun bir kod örneği ya da uzun
bir URL süreyi yapay olarak şişirirdi. `WikiArticlePage.jsx`'teki mevcut
"Bilgi Kutusu"na (Departman/Erişim/Oluşturan/Tarih'in yanına) "Okuma Süresi"
satırı olarak eklendi - yeni bir UI alanı İCAT EDİLMEDİ, var olan desene
uyduruldu. Canlı doğrulandı: "Atlas Platformu Geliştirici Kılavuzu" (en uzun
sayfa) için "~4 dk" gösterdi, doğru sırada (Tarih'ten sonra, Etiketler'den
önce) render edildi. 7 yeni test (`readingTime.test.js`) + tüm frontend test
suite'i (24/24) yeşil.

## UI/UX Denetimi ve Uygulanan Düzeltmeler (2026-08-12)

Kullanıcının "yeşil + krem + kahve tonları, sarı yok, sade, kompakt sidebar"
hedefine göre mevcut frontend'in canlı DOM/CSS incelemesi yapıldı (kod hiç
değiştirilmeden önce - `getComputedStyle` ile ölçülen gerçek renk/spacing/
font değerleri, ekran görüntüsü bu ortamda alınamadığı için). **Önemli
bulgu:** Light mode ZATEN hedefteki krem+yeşil paleti taşıyordu (`--bg:
#f9f6ef`, `--brand-accent: #1b4d3e`, sarı hiç yok) - asıl kopukluk dark
mode'daydı. Denetim raporunun tamamı onaylandıktan sonra 5 madde uygulandı:

- [x] **Dark mode paleti ısıtıldı + gerçek bir `--primary` çelişkisi
      düzeltildi:** Dark mode eskiden soğuk bir slate/grafit paletiydi
      (`--bg: #1b2025`, mavimsi) - light mode'un krem/kahve karakteriyle hiç
      akrabalığı yoktu. `--bg`/`--page-bg`/`--border`/`--code-bg`/`--text`/
      `--text-h` AYNI koyuluk/luminance seviyesi korunarak (WCAG kontrastları
      bozulmasın diye) mavi-griden sıcak kahve/espresso ailesine çekildi
      (`--bg: #1e1710`, `--page-bg: #16110b` vb.). **Denetim sırasında bulunan
      GERÇEK bug:** `index.css`'te İKİ AYRI `.dark` bloğu vardı (biri elle
      yazılmış, öbürü shadcn init'inden kalma, hiç birleştirilmemiş) -
      `--primary` üç farklı yerde üç farklı değer taşıyordu (`#34d399`,
      `#24654f`), CSS kaskad kuralı gereği SONRAKİ blok kazanıyordu, yani
      ikisinden biri hiç render edilmeyen ölü koddu; ayrıca ikisi de
      WCAG-doğrulanmış `--brand-accent`'le (`#1d8660`, bkz. eski kontrast
      düzeltmesi notu) UYUŞMUYORDU. Üçü de tek bir değere (`#1d8660`)
      birleştirildi - iki `.dark` bloğunun kendisi BİLEREK birleştirilmedi
      (daha büyük bir mimari temizlik, bu denetimin kapsamı dışında
      bırakıldı), ama artık hangisinin kazandığının önemi kalmadı.
- [x] **H1/H2/H3 gövde metninin (16px) üzerine çıkarıldı:** H2 (15px) gövde
      metninden KÜÇÜKTÜ, sadece kalın yazı tipi hiyerarşiyi taşıyordu -
      Wikipedia'nın kendi tipografisiyle (H2 gözle görülür büyük) karşılaştırınca
      ters bir hiyerarşiydi. `HEADING_SIZES` (markdown.jsx): 21/15/12 →
      24/19/17px. H4-H6 BİLEREK dokunulmadı (orijinal tasarım kararı onları
      H3'ün altında küçük bir "etiket" tonunda tutmaktı, denetim SADECE
      H1-H3'ü işaretlemişti).
- [x] **Ana sayfa hero başlığı büyütüldü, ama ÖLÇÜLÜ:** `text-lg` (15.75px) →
      `text-xl` (17.5px) - `text-2xl`'e SIÇRANMADI, çünkü bu satır zaten
      "Karşılama alanı küçültülsün" (2026-08-07 spec'i) kararıyla bilinçli
      olarak ince tutulmuştu; audit'in "hero zayıf" bulgusuyla o geçmiş
      kararı ÇELİŞTİRMEDEN tek kademelik bir denge bulundu.
- [x] **Makale sağ paneli (Bilgi Kutusu + Okuma Ayarları) daraltıldı:**
      260/280px → 200/240px (`GRID_TEMPLATES`, `WikiArticlePage.jsx`) - panel
      içeriği (Departman/Erişim/Oluşturan/Tarih/Okuma Süresi/Etiketler) zaten
      o genişliği doldurmuyordu, içerik sütunu bu daralmadan +40px kazandı.
      TOC genişliğine (200/220px) BİLEREK dokunulmadı, denetim SADECE sağ
      paneli "gereksiz geniş" işaretlemişti.
- [x] **Sarı/amber kontrolü:** `CALLOUT_CONFIG`'de (markdown.jsx) hiç sarı/amber
      YOK - "warning"/"error" ikisi de düz `"red"` kullanıyor. Doğrulandı,
      düzeltme gerekmedi (hedef sadece "sarı kullanılmamalı" diyordu).

**Denetimde "bug" olarak işaretlenip SONRADAN YANLIŞ ÇIKAN bir bulgu:**
İçindekiler'in "genişlet" düğmesinin grid kolonunu büyütmediği düşünülmüştü -
uygulamaya geçerken TEMİZ bir sayfa yüklemesinde YENİDEN test edildi ve
DOĞRU çalıştığı görüldü (220px'e genişliyor). Muhtemelen ilk ölçüm sırasında
bir zamanlama/stale-state sorunu vardı - "düzeltme" YAPILMADI, çünkü
düzeltilecek gerçek bir sorun yoktu. Genel ders: bir "bug" bulgusunu
düzeltmeye geçmeden önce TEMİZ bir ortamda yeniden üretmeye çalışmak gerekir -
burada bu adım atılıp gerçek olmayan bir düzeltmeden kaçınıldı.

Tüm değişiklikler canlı doğrulandı (light+dark mod, `getComputedStyle` ile
gerçek piksel/hex değerleri okunarak) + `dotnet build` gerekmedi (sadece
frontend) + `npm run lint`/`npm run build`/`npx vitest run` (24/24) yeşil.

## "Modernize et" turu (2026-08-12, denetim raporunun hemen ardından)

Kullanıcı "siteyi aç ve daha modernize et" dedi - denetim raporunun ("sade
kal, abartma") sınırları İÇİNDE, iki somut ekleme yapıldı:

- [x] **Sticky header:** `WikiLayout.jsx`'teki `<header>` eskiden
      `position: static` idi - sayfa kaydırılınca arama/tema/bildirim gibi
      her an erişilebilir olması gereken kontroller TAMAMEN gözden
      kayboluyordu. Artık `sticky top-0 z-20`, opak `var(--bg)` zemini
      koruyor (backdrop-blur/yarı-saydamlık BİLEREK eklenmedi - "sade" hedefine
      gereksiz bir gösteriş olurdu). Canlı doğrulandı: 800px scroll sonrası
      `header.getBoundingClientRect().top` hâlâ 0.
- [x] **Ana sayfa kartlarına hover mikro-etkileşimi:** `ArticleCard`
      (HomePage.jsx) zaten `hover:shadow-md` taşıyordu - buna küçük bir
      kaldırma (`hover:-translate-y-0.5`) ve sınır renginin yeşile dönmesi
      (`hover:border-[var(--brand-accent-border)]`) eklendi. Bunu yapabilmek
      için sınır rengi inline `style`'dan Tailwind class'ına taşınmak
      ZORUNDAYDI - inline style, AYNI özellik için `:hover` pseudo-class'ından
      BAĞIMSIZ olarak her zaman dış CSS kuralını ezer, bu yüzden eskisi gibi
      `style={{ borderColor: "var(--border)" }}` kalsaydı hover rengi hiç
      görünmezdi.

**Doğrulama sınırlaması (dürüstçe not edilsin):** Hover mikro-etkileşimini bu
ortamın otomasyon aracıyla (simüle edilmiş fare hareketi) GÖRSEL olarak
doğrulayamadım - `element.matches(':hover')` `true` dönmesine rağmen
`getComputedStyle` değişmedi. Ama bu ortamın kendi sınırlaması olduğu
doğrulandı: benim EKLEMEDİĞİM, koddan ÖNCEDEN var olan `hover:shadow-md`
sınıfı da AYNI testte tepki vermedi - yani gerçek bir regresyon değil, CDP
tabanlı simüle hover'ın bu ortamda `:hover` stil çözümlemesini gerçek fare
gibi tetiklememesi. CSS kuralının kendisi (`sheet.cssRules` içinde doğru
selector/specificity ile) doğru derlendiği ayrıca doğrulandı. `npm run lint`/
`npm run build`/`npx vitest run` (24/24) yeşil, sticky header (layout/scroll
davranışı gerçekten test edilebilir olduğu için) tam doğrulandı.

## Eksik-özellik listesi - Gün 1: Okuma ilerleme çubuğu + arama derin link (2026-08-12)

19 bölümlük "Atlas İçerik Sistemi" spec'inin tekrar denetlenmesinde bulunan
10+ eksikten (bkz. o denetimin özeti) en düşük riskli, tamamen frontend olan
ikisiyle başlandı - CLAUDE.md'nin "büyük özellik birden fazla küçük adıma
bölünsün" kuralına göre, hepsi tek seferde YAPILMADI.

- [x] **Okuma İlerleme Çubuğu** - `WikiArticlePage.jsx`'te makalenin
      tepesi/altı viewport'a göre hesaplanan, sticky header'ın (50px) hemen
      altına sabitlenen ince (2px) bir çubuk. Kısa sayfalarda (tek ekrana
      sığan) hiç GÖSTERİLMİYOR - "gereksiz UI elementiyle doldurma" spec
      notuna uyuluyor. Tam ekran okuma modu BİLEREK kapsam dışı (kendi ayrı
      `overflow-y-auto` scroll konteynerini kullanıyor, window scroll değil -
      ayrı bir izleme mantığı gerektirirdi).
- [x] **Arama sonucundan ilgili bölüme derin link** - `WikiSearch.jsx`
      artık eşleşen chunk metnini `navigate(..., { state: { chunkText } })`
      ile taşıyor, `WikiArticlePage` chunk'ın ham içerikteki konumunu bulup
      (`markdown.jsx`'e eklenen `lineIndex` alanı sayesinde) EN YAKIN ÖNCEKİ
      başlığa kaydırıyor - backend'e YENİ BİR ALAN EKLENMEDİ (chunk<->başlık
      ilişkisi backend'de hiç saklanmıyor, TextChunker sabit boyutlu pencere
      kullanıyor, başlık sınırlarını bilmiyor). **Canlı testte bulunan gerçek
      bir hata:** snippet'i chunk'ın BAŞINDAN almak yanlıştı - bir chunk
      sıkça bir bölüm SINIRINI ortadan kesiyor (ilk cümlesi ÖNCEKİ bölümün
      son cümlesi, geri kalanı YENİ bölüm), bu yüzden yanlış (bir önceki)
      başlığa kaydırıyordu. Düzeltme: snippet chunk'ın ORTASINDAN alınıyor -
      bir chunk'ın "asıl konusu" neredeyse hep ortasında, sınır-kesme etkisi
      sadece uçlarda oluyor. Canlı doğrulandı (gerçek bir arama yapılıp
      tıklanarak): "departman bazlı görünürlük" araması artık doğru şekilde
      "Departman Bazlı Erişim" başlığını aktif işaretliyor (önceden yanlışlıkla
      bir önceki başlığı - "CQRS Komutu" - işaretliyordu).

**Doğrulama sınırlaması (dürüstçe not edilsin):** Bu ortamın Browser
pane'i "compositing" yapmıyor (screenshot da bu yüzden çalışmıyor) - bu
yüzden `scrollIntoView({behavior:"smooth"})` GERÇEKTEN kaydırmıyor (aynı
hedefe `behavior:"instant"` ile manuel test edilince ANINDA çalıştığı
kanıtlandı - kod doğru, gerçek kullanıcılarda sorunsuz çalışacak). Okuma
ilerleme çubuğunun matematiği de aynı şekilde manuel `scroll` event'i
tetiklenerek doğrulandı (`window.scrollTo()` bu ortamda gerçek bir `scroll`
event'i tetiklemiyor - başka bir ortam kısıtlaması, kodun kendisi değil).
`npm run lint`/`npm run build`/`npx vitest run` (24/24) yeşil.

## Medium-vari "+" düğmesi (2026-08-12, kullanıcının Medium ekran görüntüsü isteği)

Kullanıcı Medium'un editöründeki "+" düğmesini (tıklanınca resim/video/kod/
embed gibi blok tiplerini gösteren bir satır) örnek gösterip "bizde de olsun"
dedi. **Yeni bir ekleme mekanizması İCAT EDİLMEDİ** - `WikiEditorPage.jsx`'te
zaten var olan `SlashCommandMenu`/`SLASH_ITEMS` (satır başında "/" yazınca
açılan blok menüsü, Faz 2'den beri var) TEK gerçek eksikti: görünür,
keşfedilebilir bir tetikleyicisi yoktu, sadece "/" kısayolunu BİLENLER
kullanabiliyordu.

- [x] **Araç çubuğunun İLK öğesi olarak yuvarlak bir "+" düğmesi eklendi**
      (`handlePlusButtonClick`) - diğer dikdörtgen `Button`'lardan BİLEREK
      görsel olarak ayrışıyor (yuvarlak, `--brand-accent` renginde) ki
      Medium'daki gibi "keşfedilebilir, farklı bir şey" hissi versin. Var
      olan ~15 butonluk uzun araç çubuğuna DOKUNULMADI (kaldırılan/gizlenen
      hiçbir şey yok) - "+" SADECE aynı blok menüsüne "/" yazmaya gerek
      kalmadan ek bir yol.
- [x] **`handleSlashSelect` iki tetikleyiciyi de destekleyecek şekilde
      dallandırıldı:** "/" ile açıldığında `slashTriggerPosRef.current` dolu
      oluyor (kaldırılacak bir "/" karakteri var), "+" düğmesiyle açıldığında
      BİLEREK `null` (kaldırılacak bir tetikleyici karakter yok, doğrudan
      `applyToolbarInsert` ile mevcut imleç konumuna ekleniyor). **Canlı
      testte bulunacaktı ama ÖNCEDEN fark edilip önlendi:** `handleSlashSelect`
      eskiden `triggerPos === null` durumunda SESSİZCE hiçbir şey yapmadan
      dönüyordu (`if (!el || triggerPos === null) return;`) - "+" düğmesi bu
      koşulu tetiklediği için, düzeltilmeseydi düğme görünüşte çalışır ama
      hiçbir şey EKLEMEZDİ.

Canlı doğrulandı: "+" düğmesi menüyü açıyor, bir blok seçilince doğru
sözdizimi içeriğe ekleniyor ("Kod Bloğu" seçilince `` ```\nkod\n``` `` doğru
eklendi), menü kapanıyor; AYRICA eski "/" tetiklemesi de (regresyon
kontrolü) hâlâ birebir aynı şekilde çalışıyor - "/" karakteri doğru
kaldırılıp yerine blok ekleniyor. `npm run lint`/`npm run build`/
`npx vitest run` (24/24) yeşil.

## Kalıcı Bildirim Geçmişi - Gün 1/2: Backend (2026-08-15)

Kullanıcı Medium'un sağ sütunundaki "Write" kartını + "Staff Picks" akışını
örnek gösterip "bildirim için de böyle bir şey ekleyelim" dedi.
`AskUserQuestion` ile netleştirildi: sadece kozmetik bir "yakında" kartı DEĞİL,
**gerçek, kalıcı bir bildirim geçmişi** isteniyor. Notifications modülü
2026-08-15'e kadar TAMAMEN ephemeral'dı (`Class1.cs` placeholder'ları hâlâ
duruyordu - Domain/Application katmanları hiç kullanılmamıştı) - sadece
SignalR ile anlık toast, hiçbir yerde saklama yoktu. Bu modüle ilk kez
gerçek bir Domain/Application/Infrastructure katmanı eklendi.

- [x] **`NotificationEntry` (Notifications.Domain)** - AuditLogEntry'nin AYNI
      denormalizasyon desenini taşıyor (Title/DepartmentName/Visibility/
      ActorEmail kopyalanıyor, Auth/Wiki'nin tablolarına referans YOK).
      **Kritik olan asıl gerekçe süs değil:** DepartmentName/Visibility
      buradan, `GetNotificationsQueryHandler`'ın Wiki listesi/AI aramasıyla
      AYNI `IWikiVisibilityChecker`'ı uygulayabilmesi İÇİN var - bu alanlar
      olmasaydı, DepartmentOnly bir sayfanın oluşturulduğu bilgisi (başlığıyla
      birlikte) o departmanda OLMAYAN kullanıcılara da sızardı (Ders #10'daki
      SINIFTAN bir hata, bu sefer BAŞTAN önlendi).
- [x] **`WikiPageCreatedEvent` genişletildi:** `CreatedByEmail` (Notifications'ın
      "kim oluşturdu" göstermesi için, "Content" alanının eklenme gerekçesiyle
      AYNI desen) + `IsReindexReplay = false` (varsayılan). **Bu ikinci alan
      olmadan bulunacak GERÇEK bir bug BAŞTAN önlendi:** `POST /api/wiki/reindex`
      var olan TÜM sayfalar için bu event'i yeniden yayınlıyor (embedding
      sağlayıcısı değişince AI'ın yeniden işlemesi için) - AI'ın handler'ı
      için sorun değil ama Notifications'ın YENİ kalıcı geçmişi için BÜYÜK
      bir sorun olurdu: bir reindex çalıştırmak, haftalar önce oluşturulmuş
      HER sayfa için "az önce oluşturuldu" gibi SAHTE kayıtlar ekleyip
      geçmişi anlamsızlaştırırdı. `WikiPageCreatedEventHandler`
      (Notifications.Infrastructure) artık `IsReindexReplay` true ise hem
      SignalR toast'ını HEM kalıcı kaydı ATLIYOR.
- [x] **Kalıcı yazma best-effort** - AI'ın embedding-üretim handler'ıyla AYNI
      gerekçe (try/catch, rethrow YOK, sadece `LogWarning`) - bir DB yazma
      hatası SignalR toast'ının gönderilmesini ENGELLEMEMELİ.
- [x] **`GetNotificationsQuery` + `GET /api/notifications?take=10`** (token
      gerektiriyor - AI arama endpoint'iyle AYNI gerekçe, sonuçlar zaten
      departmana göre filtreleniyor). `NotificationsDbContext` - Audit/Vault/
      Documents ile AYNI SQL Server veritabanı (`AtlasPlatform`), kendi
      `notifications.*` şeması. Migration uygulandı.

**Canlı doğrulandı (üç ayrı senaryo, gerçek kullanıcılarla):**
1. Public bir sayfa oluşturulunca doğru veriyle (title/departman/actor email)
   kalıcı kayıt oluştu.
2. **Güvenlik testi (en kritik olan):** DepartmentOnly bir IK sayfası
   oluşturuldu - IT departmanındaki (email doğrulanmış) bir test kullanıcısı
   `GET /api/notifications` çağırınca SADECE Public sayfayı gördü, IK'nın
   gizli bildirimi listede HİÇ YOKTU. Admin ise ikisini de gördü (bypass).
3. `POST /api/wiki/reindex` (19 sayfa) tetiklendi - bildirim sayısı reindex
   ÖNCESİ ve SONRASI birebir aynı (2) kaldı, `IsReindexReplay` doğru
   çalıştığı kanıtlandı.

`dotnet build Atlas.sln` (0 uyarı/hata) + `dotnet test Atlas.sln --filter
"Category!=Integration"` (regresyon yok, `WikiPageCreatedEvent`'i doğrudan
oluşturan hiçbir test dosyası bulunmadı - constructor değişikliği güvenliydi).
Test verisi (2 sayfa + 2 bildirim kaydı) canlı doğrulama sonrası temizlendi.

**Gün 2 (frontend) TAMAMLANDI (2026-08-15):**

- [x] **`WritePromptCard`** - Medium'un "+ Just start writing" kartının
      ÇEKİRDEK fikri alındı, dekoratif illüstrasyon/ekstra linkler BİLEREK
      alınmadı ("Medium'dan özellik alınabilir ama Atlas'ın tasarımı
      Medium'un kopyası olmamalı" ilkesi) - tek satır, tıklanınca `/wiki/new`.
- [x] **`NotificationsPanel`** - `DiscussionPanel`'in AYNI "self-contained,
      kendi verisini kendi çeken" deseni, `GetNotificationsQuery`'yi (Gün 1)
      kullanıyor. Hiçbir yetkilendirme mantığı İÇERMİYOR - backend zaten
      filtrelenmiş veriyi döndürüyor, aynı "gerçek yetkilendirme her zaman
      backend'de" ilkesi.
- [x] **HomePage'in ana içerik alanı 2 sütuna bölündü** (`xl:grid-cols-[1fr_300px]`)
      - sol/geniş sütun makale ızgarası (3'ten 2 sütuna indirildi, sidebar'a
      yer açmak için), sağ/dar sütun (Yazmaya Başla + Bildirimler + Son
      Güncellemeler + Popüler Kategoriler) `xl:sticky`. `xl` ALTINDA sidebar
      `hidden` DEĞİL - DOM sırası gereği makalelerin altına doğal olarak
      akıyor (WikiArticlePage'in TOC/panel'indeki AYNI "dar ekranda gizleme
      yerine akıt" tercihi).

Canlı doğrulandı: gerçek bir sayfa oluşturulup Bildirimler panelinde doğru
veriyle (`admin@atlas.local yeni bir sayfa ekledi` + başlık + departman/tarih)
göründüğü, tıklanınca doğru sayfaya gittiği, "Yazmaya başla" kartının
`/wiki/new`'e gittiği, grid'in gerçekten iki sütun oluşturduğu (`789.667px
300px`) DOM üzerinden teyit edildi. `npm run lint`/`npm run build`/
`npx vitest run` (24/24) yeşil. Test verisi temizlendi.

**"Kalıcı Bildirim Geçmişi" özelliği artık TAMAMEN BİTTİ (Gün 1-2).**

## Eksik-özellik listesi - Gün 2: Görsel resize + fullscreen/lightbox (2026-08-17)

Gün 1'deki (okuma ilerleme çubuğu + arama derin linki) devamı - listenin
"A) Hızlı, düşük risk, sadece frontend" grubundaki son madde.

- [x] **Resize - serbest piksel sürükleme BİLİNÇLİ OLARAK EKLENMEDİ.**
      Bir `<textarea>` tabanlı düz-metin editöründe fare ile sürükleyip tam
      piksel değerini markdown'a yazmak, hem mouse-tracking hem "editördeki
      taslak = yayınlanan görünüm" pixel-perfect bir önizleme gerektirirdi -
      bu editörün "gerçek bir contenteditable/blok editörü yok" mimarisiyle
      uyuşmazdı. Bunun yerine `HEADING_SIZES`/`WikiVisibilityRules`'daki AYNI
      felsefe: üç sabit, isimlendirilmiş boyut (Küçük/Orta/Büyük).
      `:::image-{sol|orta|sağ}` sözdizimine isteğe bağlı bir `-{small|medium|
      large}` eki eklendi (`IMAGE_ALIGN_SIZE_CLASSES`, markdown.jsx) - eki
      OLMAYAN eski içerik hâlâ eşleşiyor, `medium`'a düşüyor (GERİYE DÖNÜK
      UYUMLULUK bozulmadı, migration/veri dönüşümü gerekmedi). Editöre
      ikinci bir `<select>` (boyut) eklendi, mevcut hizalama select'inin
      yanına.
- [x] **Fullscreen/lightbox** - Tam Ekran Okuma Modu'nun (WikiArticlePage.jsx)
      AYNI `fixed inset-0 z-50` deseni, tutarlı bir "bu uygulamada tam ekran
      overlay böyle görünür" dili. `ImageBlock` VE `AlignedImageBlock`'un
      İKİSİ de kendi yerel `isOpen` state'ini tutuyor - paylaşılan bir
      Context/global state İCAT EDİLMEDİ (aynı anda en fazla bir görsel
      açık olabilir, yerel state yeterli). Büyütme ikonu SADECE hover'da
      görünüyor ("sade" hedefine göre, görseli her zaman bir ikonla
      kirletmemek için).

Canlı doğrulandı: `:::image-left-large` + `:::image-center-small` içeren
gerçek bir sayfa oluşturuldu, render edilen `<figure>`'ların computed
`max-width`/`float` değerleri (440px float-left, 480px ortalı) doğru
eşleşti; bir görsele tıklanınca lightbox doğru `src` ile açıldı, kapatma
düğmesiyle kapandığı doğrulandı; editördeki boyut `<select>`'i test edilip
doğru sözdiziminin (`:::image-right-small\n![Açıklama](https://...)\n:::`)
üretildiği teyit edildi. `npm run lint`/`npm run build`/`npx vitest run`
(24/24) yeşil. Test verisi temizlendi.

**Eksik-özellik listesinin "A" grubu (hızlı, düşük risk, sadece frontend)
artık TAMAMEN BİTTİ** - okuma ilerleme çubuğu, arama derin linki, görsel
resize/lightbox. Sırada "B) Orta, mevcut desenleri tekrar kullanıyor" grubu
var: Wiki sayfaları için Version History + Autosave/Draft göstergesi.

## Eksik-özellik listesi - B Grubu, Gün 1: Wiki Version History backend (2026-08-17)

"B) Orta, mevcut desenleri tekrar kullanıyor" grubunun ilk maddesi. Yeni bir
tasarım İCAT EDİLMEDİ - Documents modülünün P6'daki `DocumentVersion`/
`UploadNewDocumentVersionCommandHandler` deseni ("önce mevcut hâli
snapshot'la, SONRA üzerine yaz") BİREBİR Wiki'ye taşındı.

- [x] **`WikiPage.CurrentVersionNumber`** - Domain'de yeni bir alan, `Update()`
      her çağrıldığında artıyor. `WikiPage.cs`'in KENDİSİ hiçbir zaman eski
      bir versiyonu tutmuyor - HER ZAMAN en güncel hâli taşıyor, geçmiş
      SADECE ayrı bir tabloda yaşıyor (bkz. altta).
- [x] **`WikiPageVersion` (Domain, YENİ entity)** - `DocumentVersion`'ın
      birebir karşılığı: `WikiPage`'e FK İLE BAĞLI DEĞİL (bu projede FK'ler
      sadece Wiki'nin cross-module ham-SQL migration'ındaki istisnai durumda
      var - temizlik DB cascade'ine değil Handler'ın orkestrasyonuna
      bırakılıyor). `EditedByUserId`/`EditedByEmail` BİLİNÇLİ bir
      sadeleştirme - içeriği İLK YAZAN değil, o versiyonu DEĞİŞTİREN kişi
      (orijinal yazar zaten `WikiPage.CreatedByUserId`'de duruyor).
      `(WikiPageId, VersionNumber)` composite unique index. Migration
      (`AddWikiPageVersions`) uygulandı - **dikkat edilen bir detay:**
      `CurrentVersionNumber` kolonunun migration'daki `defaultValue`'su `0`
      DEĞİL `1` olarak ayarlandı, çünkü Domain'deki in-memory varsayılan da
      `1` - migration'dan ÖNCE var olan (hiç düzenlenmemiş) sayfalar da
      "1. versiyon"da sayılmalı, `0` olsaydı bu sayfalar için tutarsız/yanlış
      bir başlangıç değeri olurdu.
- [x] **`UpdateWikiPageCommandHandler`** artık `page.Update(...)` çağrısından
      HEMEN ÖNCE mevcut (o ana kadar güncel olan) hâli bir
      `WikiPageVersion.CreateSnapshot(...)`'a alıp kaydediyor -
      `UploadNewDocumentVersionCommandHandler`'daki "önce snapshot, SONRA
      ReplaceFile" sırasıyla AYNI. **`DeleteWikiPageCommandHandler`** artık
      sayfa silinince `IWikiPageVersionRepository.DeleteAllForWikiPageAsync`
      ile geçmişteki TÜM versiyonları da temizliyor (`DeleteDocumentCommandHandler`'ın
      "versiyon dosyalarını da diskten temizle" gerekçesiyle AYNI - burada
      disk yok, sadece DB satırı, ama "yetim" veri bırakmama ilkesi aynı).
      Bu Outbox ÜZERİNDEN DEĞİL, Handler içinde doğrudan yapılıyor - versiyon
      geçmişi tamamen Wiki modülünün kendi iç verisi, başka bir modül
      dinlemiyor.
- [x] **`GetWikiPageVersionsQuery`/`GetWikiPageVersionByNumberQuery`** -
      `GetWikiPageByIdQueryHandler`'daki AYNI "varlığı gizle" deseni (null
      dönerse 404) + AYNI `page.IsVisibleTo(viewerDepartment, viewerIsAdmin)`
      görünürlük kuralı - Id'yi bilmek geçmişi görebilmek anlamına gelmiyor.
      Versiyon listesi SADECE ESKİ (arşivlenmiş) versiyonları döndürüyor -
      güncel hâl zaten `GET /api/wiki/pages/{id}`'in kendisinde.
- [x] **`RestoreWikiPageVersionCommand`** - owner-or-Admin (Update/Delete ile
      AYNI yetki deseni, throw-based 403/400 - Restore da bir düzenleme
      eylemi). Handler ÖNCE geri dönülmeden HEMEN ÖNCEki hâli YENİ bir
      snapshot olarak arşive ekliyor, SONRA hedef versiyonun içeriğini
      `page.Update(...)`'e veriyor - "geri dönmek" hiçbir hâli sessizce
      kaybetmiyor, kendisi de versiyonlanabilir bir eylem. `IAuditableCommand`
      ile audit'leniyor ("WikiPage.VersionRestored").
- [x] **3 yeni endpoint:** `GET /api/wiki/pages/{id}/versions`,
      `GET /api/wiki/pages/{id}/versions/{versionNumber}`,
      `POST /api/wiki/pages/{id}/versions/{versionNumber}/restore`.

**Canlı doğrulandı (curl + sqlcmd ile uçtan uca):** bir sayfa oluşturulup iki
kez güncellendi (v1→v2→v3), versiyon listesi doğru sırayla (`[2, 1]` - v3
güncel olduğu için listede YOK) döndü, eski versiyonların içeriği/etiketleri
doğru okundu. v1'e restore edilince: sayfa v1'in içeriğine döndü,
`CurrentVersionNumber` 4'e çıktı (3 DEĞİL - restore da bir versiyon
ilerletir), restore edilmeden HEMEN ÖNCEki hâl (v3) otomatik olarak yeni bir
snapshot'a (versiyon 3) dönüştü - versiyon listesi artık `[3, 2, 1]`. Audit
log'da `WikiPage.VersionRestored` doğru `Details` ("Versiyon Testi v1 (v1
sürümüne geri döndürüldü)") ile kayıtlı. Güvenlik testleri: aynı departmandan
sahibi-olmayan bir kullanıcı versiyonları GÖREBİLDİ ama restore denemesi 403
aldı; başka departmandan (IK) bir kullanıcı `GET .../versions` VE
`GET .../versions/{n}` için 404 aldı (varlık gizlendi) - restore denemesi ise
403 döndü (Update/Delete'in ZATEN taşıdığı, mutasyon komutlarının throw-based
olup "varlığı gizle"mediği kuralla TUTARLI, yeni bir açık DEĞİL). Sayfa
silinince 4 versiyon satırının TAMAMININ da temizlendiği doğrulandı. 13 yeni
unit test (`RestoreWikiPageVersionCommandHandlerTests`,
`GetWikiPageVersionsQueryHandlerTests`, `GetWikiPageVersionByNumberQueryHandlerTests`)
+ `dotnet test Atlas.sln --filter "Category!=Integration"` yeşil (regresyon
yok - `UpdateWikiPageCommandHandlerTests`/`DeleteWikiPageCommandHandlerTests`'in
`CreateHandler` yardımcıları yeni `IWikiPageVersionRepository` parametresini
alacak şekilde güncellendi). Test verisi (1 sayfa + 3 kullanıcı) temizlendi.

## Eksik-özellik listesi - B Grubu, Gün 2: Wiki Version History frontend (2026-08-17)

`DocumentDetailPage.jsx`'teki versiyon geçmişi listesinin (P6) fikrini
taşıdı, ama Documents'ın "İndir" düğmesi yerine burada "Önizle + geri dön"
var - Wiki'nin içeriği (markdown) İNDİRİLECEK bir dosya değil, DOĞRUDAN
görüntülenebilir.

- [x] **Yeni "Geçmiş" sekmesi** - `WikiArticlePage.jsx`'in var olan "Madde"/
      "Tartışma" sekme desenine ÜÇÜNCÜ bir sekme olarak eklendi (yeni bir
      Dialog/route İCAT EDİLMEDİ, `activeTab` state'i zaten vardı).
- [x] **`WikiVersionHistoryPanel.jsx` (YENİ, kendi kendine yeten bileşen)** -
      `DiscussionPanel.jsx`'le AYNI desen (kendi state'ini, kendi veri
      çekmesini yönetiyor, parent'a sadece `onRestored` callback'iyle haber
      veriyor). Bir versiyon satırına tıklanınca İÇİNDE genişleyip
      `renderWikiMarkdown` ile SALT-OKUNUR bir önizleme gösteriyor - AYRI bir
      Dialog/route AÇILMADI (bu projede içerik görüntüleme Dialog'dan tam
      sayfaya kaydı, bkz. WikiPageTable'ın eski detay dialogunun kaldırılma
      gerekçesi - satır-içi genişleme bu felsefeyle daha tutarlı). "Bu
      sürüme geri dön" düğmesi SADECE `canRestore` (owner-or-Admin, parent'tan
      geliyor) true ise gösteriliyor - backend zaten 403 ile reddediyor, bu
      sadece UI'da gereksiz bir düğme göstermemek için.
- [x] **3 yeni `api.js` fonksiyonu** - `getWikiPageVersions`/
      `getWikiPageVersionByNumber`/`restoreWikiPageVersion`, `updateWikiPage`
      ile AYNI 401→refresh→tekrar dene deseni.
- [x] **Restore sonrası state güncellemesi** - `DocumentDetailPage`'in "yeni
      versiyon yüklendi" akışındaki AYNI gerekçeyle iyimser (optimistic) bir
      güncelleme YAPILMADI - `handleVersionRestored`, sayfayı sunucudan
      YENİDEN çekip `page` state'ini tazeliyor, "Madde" sekmesi bir sonraki
      bakışta gerçek (restore edilmiş) içeriği gösteriyor.

**Canlı doğrulandı (gerçek tarayıcı etkileşimiyle, admin girişiyle):** var
olan bir sayfa ("Blok Editörü Test Sayfası") düzenlenip bir versiyon
oluşturuldu, "Geçmiş" sekmesinde doğru göründü; satıra tıklanınca
ESKİ (düzenlemeden önceki) içerik doğru render edildi; "Bu sürüme geri dön"
tıklanınca - **ortamın `window.confirm()`'ü CDP üzerinden otomatik
reddettiği fark edildi** (bu ortamın bilinen bir sınırlaması, bu projenin
`handleDelete` gibi diğer `window.confirm()` kullanan akışlarıyla AYNI
davranış) - `window.confirm` geçici olarak `true` döndürecek şekilde
override edilip TEKRAR denendi: restore doğru çalıştı, "Madde" sekmesi
ANINDA (sayfa yenilemeden) eski içeriği gösterdi, "Geçmiş" sekmesi
pre-restore hâli otomatik olarak yeni bir versiyon (2) olarak arşivledi.
Test sırasında oluşan versiyon satırları + `CurrentVersionNumber` sqlcmd ile
temizlenip sayfa test-öncesi hâline döndürüldü. `npm run lint`/`npm run
build`/`npm run test` (24/24) yeşil - yeni kod hiçbir yeni uyarı/hata
eklemedi.

**"Eksik-özellik listesi B grubu"nun ilk maddesi (Wiki Version History) artık
TAMAMEN BİTTİ (Gün 1-2, backend+frontend).** Sırada grubun ikinci maddesi:
Autosave/Draft göstergesi.

## Eksik-özellik listesi - B Grubu, Gün 3: Autosave/Draft göstergesi (2026-08-17)

B grubunun ikinci ve son maddesi - "mevcut desenleri tekrar kullanıyor"
temasına rağmen bu sefer YENİ bir mimari karar gerekiyordu: taslak nereye
yazılacak?

**Mimari karar - backend'e HİÇ dokunulmadı, tamamen `localStorage`:**
Gün 1-2'de bitirdiğimiz `WikiPageVersion` GERÇEK, kaydedilmiş bir geçmiş
tutuyor - Autosave'in amacı bunun TAMAMEN FARKLISI: sadece tarayıcı-yerel bir
kazayı (yanlışlıkla sekme kapatma, "Vazgeç"e basma, tarayıcı çökmesi) telafi
etmek. Bu, theme/Okuma Ayarları/TOC-panel durumu gibi projenin ZATEN
`localStorage`'da tuttuğu "cihaza özel, senkron gerekmeyen" veri kategorisiyle
BİREBİR aynı. Bir backend `Draft` entity'si (yeni tablo/endpoint/sahiplik
kuralı/temizlik mantığı) hem gereksiz karmaşıklık olurdu hem de gerçek
versiyon geçmişiyle kavramsal olarak karışırdı - "taslak karalama" ile
"kaydedilmiş sürüm" aynı tabloda YAŞAMAMALI.

- [x] **`WikiEditorPage.jsx`'e autosave** - `AUTOSAVE_DEBOUNCE_MS` (1500ms)
      sonra title/content/tags/visibility/folderId/department (SADECE
      oluşturma modunda) `localStorage`'a yazılıyor. Anahtar şeması:
      düzenleme modunda `wiki-draft-edit-{pageId}` (sayfalar birbirini
      EZMESİN), oluşturma modunda TEK bir `wiki-draft-new` (theme/reading
      settings'teki AYNI "tek global anahtar" basitliği - aynı anda birden
      fazla "yeni sayfa" taslağı YAGNI).
- [x] **Taslak kurtarma banner'ı** - sayfa açılışında (fetch tamamlandıktan/
      red-link prefill'inden HEMEN SONRA) var olan bir taslak, o anki state'ten
      HERHANGİ bir alanda farklıysa "Kaydedilmemiş bir taslak bulundu... geri
      yüklemek ister misin?" banner'ı çıkıyor - "Geri Yükle" / "Yok say".
      **Kritik sıralama detayı:** `draftCheckDoneRef` (bir state DEĞİL, bir ref)
      kullanıcı bu karara VARANA kadar otomatik kaydetmeyi BLOKLUYOR - aksi
      halde fetch'ten gelen İLK state değişikliği, kullanıcının henüz
      GÖRMEDİĞİ bir taslağın üzerine sessizce yazardı (canlı test edilerek
      doğrulanan bir tasarım kararı, kod yazılırken baştan düşünüldü).
- [x] **"Vazgeç" taslağı SİLMİYOR** - sadece gerçek bir kayıt (`handleSave`
      başarılı olunca) taslağı temizliyor. Yanlışlıkla "Vazgeç"e basan bir
      kullanıcı, sayfaya geri döndüğünde taslağını hâlâ bulabiliyor - bu,
      autosave'in "kazaya karşı güvenlik ağı" olma amacıyla tutarlı (kazara
      Vazgeç de bir kaza sayılıyor).
- [x] **Durum göstergesi** - Kaydet/Vazgeç düğmelerinin yanında "Taslak
      kaydedildi · HH:MM:SS" (sade, tek satır metin - yeni bir UI elementi
      İCAT EDİLMEDİ).

**Canlı doğrulandı (gerçek tarayıcı etkileşimiyle):** hem oluşturma hem
düzenleme modunda - yazıp 2sn beklenince taslak `localStorage`'a doğru
içerikle yazıldı, gösterge göründü; kaydetmeden başka bir sayfaya gidip geri
dönülünce banner doğru çıktı; "Geri Yükle" tıklanınca alanlar taslaktan
doğru dolduruldu; "Yok say" tıklanınca hem banner kapandı hem `localStorage`
temizlendi VE alanlar mevcut (fetch edilmiş/boş) hâlinde kaldı; gerçek bir
"Yayınla"/"Kaydet" sonrası taslağın `localStorage`'dan silindiği doğrulandı.
**Test sırasında kendi test script'imde bulunan bir hata (ürün kodunda
değil):** ilk düzenleme-modu testinde textarea'ya `computer` aracıyla
yazdırılan ek metin GERÇEKTE textarea'ya ulaşmamıştı (muhtemelen stale bir
element referansı) - banner'ın "görünmediği" ilk gözlem BU YÜZDENDİ, ürün
kodunda bir eksiklik değildi; native input setter + `dispatchEvent`
kullanılarak yeniden denenince (içeriğin gerçekten değiştiği doğrulanarak)
banner beklendiği gibi doğru çıktı. Test verisi (oluşturulan sayfa + tüm
`wiki-draft-*` anahtarları) temizlendi. `npm run lint`/`build`/`test`
(24/24) yeşil - yeni kod hiçbir yeni uyarı/hata eklemedi.

**"Eksik-özellik listesi B grubu" artık TAMAMEN BİTTİ (Wiki Version History
Gün 1-2 + Autosave/Draft Gün 3).**

## Eksik-özellik listesi - C Grubu, Gün 1: Vimeo/Loom embed desteği (2026-08-17)

C grubu ("Video Merkezi/Medya Kütüphanesi") başlamadan önce kapsam kullanıcıyla
netleştirildi (`AskUserQuestion`) - üç seçenek sunuldu: (a) sadece embed+galeri,
gerçek dosya depolama YOK (önerilen), (b) yukarıdakine ek gerçek video dosyası
yükleme (Documents'a benzer yeni bir depolama katmanı), (c) sadece embed,
galeri YOK. Kullanıcı (a)'yı seçti - video transkript indeksleme zaten D
grubunda AYRI bir madde olduğu için, C grubunun gerçek dosya depolamaya
girmesi kapsam karışıklığı yaratırdı.

- [x] **`VideoBlock` (markdown.jsx) Vimeo/Loom tanıyacak şekilde genişletildi** -
      YouTube-only mimari DEĞİŞTİRİLMEDİ, SADECE genişletildi: sıralama
      YouTube→Vimeo→Loom→dosya→düz-link (eskisiyle AYNI "önce özel servisler,
      sonra genel dosya, en sonda düz link" mantığı). `VIMEO_PATTERN`
      (`vimeo.com/123...` ve zaten embed formatındaki `player.vimeo.com/
      video/123...` ikisini de yakalıyor) + `LOOM_PATTERN` (`loom.com/share/...`
      ve `loom.com/embed/...` ikisini de yakalıyor). Backend'e HİÇ dokunulmadı -
      bu tamamen bir render-katmanı genişlemesi.
      **Tanınmayan bir URL hâlâ kırık bir gömme DENEMİYOR**, sade bir
      "Videoyu Aç" linkine düşüyor - eski davranış korundu.
- [x] Editördeki video düğmesi/slash-command placeholder metni güncellendi
      ("YouTube/Vimeo/Loom linki ya da video dosyası URL'si") - kullanıcı
      artık desteklenen üç servisin farkında.

Canlı doğrulandı: YouTube+Vimeo+Loom+tanınmayan-URL içeren 4 blok taşıyan
gerçek bir sayfa oluşturuldu, render edilen `<iframe>` `src`'leri doğru
embed URL'lerine (`youtube-nocookie.com/embed/...`, `player.vimeo.com/
video/...`, `loom.com/embed/...`) dönüştüğü, tanınmayan URL'in düz linke
düştüğü, alt yazıların (figcaption) doğru göründüğü DOM üzerinden teyit
edildi. `npm run lint`/`build`/`test` (24/24) yeşil. Test verisi temizlendi.

## Eksik-özellik listesi - C Grubu, Gün 2: Video Merkezi galeri sayfası (2026-08-17)

C grubunun ikinci ve son maddesi. Backend'e YENİ bir endpoint EKLENMEDİ -
Wiki'nin zaten departman-görünürlüğüne göre filtrelenmiş döndürdüğü sayfa
listesi (`GetWikiPagesQuery`, `GetWikiPagesQueryHandler`'ın "tüm veriyi çek,
bellekte filtrele" ZATEN KABUL EDİLMİŞ ölçek varsayımıyla AYNI) istemci
tarafında taranıp `:::video` blokları çıkarılıyor - görünürlük kuralı VERİYİ
backend'den ALIRKEN zaten uygulanmış oluyor.

- [x] **`videoExtraction.js` (YENİ, saf fonksiyon)** - `dateUtils.js`/
      `readingTime.js` ile AYNI desen. `renderWikiMarkdown`'ın `:::video`
      blok algılama mantığıyla (markdown.jsx) BİREBİR aynı kural (ilk dolu
      satır URL, kalanı alt yazı) - TAM markdown render'ını tekrar üretmeden
      sadece video bloklarını buluyor. 8 yeni Vitest testi.
- [x] **`VideoBlock` (markdown.jsx) `export` edildi** - embed algılama
      mantığının (YouTube/Vimeo/Loom/dosya/düz-link) TEK bir yerde yaşamaya
      devam etmesi için galeri sayfası bunu DOĞRUDAN tekrar kullanıyor, AYNI
      mantığı ikinci bir yerde KOPYALAMIYOR (ör. "hangi ikon/etiket" kararı
      için bile ayrı bir regex seti YAZILMADI - kart placeholder'ı BİLEREK
      kaynak-agnostik, sade bir Video ikonu kullanıyor).
- [x] **`VideoCenterPage.jsx` (YENİ sayfa, `/wiki/videos`)** - Favoriler/
      Pinlenenler'le AYNI gerekçeyle Wiki İÇERİĞİ (top-level DEĞİL, `/wiki`
      altında nested). "Lazy play" - `VideoCard` tıklanana kadar iframe HİÇ
      render EDİLMİYOR (bir galeride onlarca YouTube/Vimeo/Loom iframe'ini
      baştan yüklemek hem yavaş hem gereksiz ağ trafiği olurdu), tıklanınca
      `VideoBlock` inline render ediliyor + kaynak sayfaya link. `pageSize=100`
      (backend'in `Math.Clamp` üst sınırı) ile `totalPages` kadar sıralı
      istek atılıyor - Wiki'nin ana liste sayfasının ZATEN kabul ettiği
      "büyük ölçekte optimize değil" tradeoff'uyla tutarlı.
- [x] `HomePage.jsx`'in Hızlı Erişim şeridine "Video Merkezi" düğmesi
      eklendi (Favoriler/Pinlenenler/Audit Log'un yanına).

**Canlı doğrulandı:** IT departmanında Public bir video sayfası + IK
departmanında DepartmentOnly bir video sayfası oluşturuldu - IT'li normal
bir kullanıcı galeride SADECE IT'nin videosunu gördü (IK'nınki hiç
görünmedi, güvenlik testi), Admin ikisini de gördü (bypass). "Lazy play"
doğrulandı: sayfa açılışında `<iframe>` sayısı 0, bir karta tıklanınca TAM
1 iframe doğru embed URL'iyle (`player.vimeo.com/video/...`) render edildi,
"Kaynak: {sayfa başlığı}" linki doğru göründü. `/wiki/videos` linkinin
Hızlı Erişim şeridinde doğru çalıştığı teyit edildi. `npm run lint`/`build`/
`test` (32/32) yeşil. Test verisi (2 sayfa + 1 kullanıcı) temizlendi.

**"Eksik-özellik listesi C grubu" artık TAMAMEN BİTTİ (Vimeo/Loom Gün 1 +
Video Merkezi galerisi Gün 2).** Sırada D grubu var: link/embed otomatik
algılama, video transkript indeksleme, Vault paylaşım modeli - henüz hiç
başlanmadı.

## Görsel Tasarım Yenileme - Teal/Cyan + Turuncu Palet (2026-08-17)

Eksik-özellik listesinden (D grubu) BAĞIMSIZ, kullanıcının bir referans
mockup ekran görüntüsü (koyu tema, teal/cyan + turuncu gradient vurgulu bir
Atlas Wiki ana sayfa tasarımı) paylaşıp "aynı olsun" demesiyle açıldı. Ayrı
bir branch'te (`design/teal-cyan-homepage-redesign`, `master`'dan) - D
grubunun devam eden branch'leriyle (`feature/video-link-autodetect`,
`feature/vault-sharing`) İLGİSİZ, o yüzden karışmasınlar diye bilerek ayrı
tutuldu. 2 güne bölündü - Gün 1 (bu bölüm) palet+logo, Gün 2 ana sayfanın
kendisi (hero/öne çıkan makale/belgeler widget'ı/video widget'ı/istatistik
grafiği/footer).

**Gün 1 - Renk paleti + logo:**

- [x] **`index.css` TAMAMEN yeniden renklendirildi** - eski yeşil+krem+kahve
      paleti (aylar süren WCAG-doğrulama emeğiyle kurulmuştu) yerine teal/cyan
      (ANA etkileşim rengi, eski yeşille AYNI ROL - link/buton/rozet/focus
      ring) + turuncu (YENİ, SADECE ikincil/gradient bir vurgu - `--accent-warm`,
      `--brand-accent`'in yerine GEÇMİYOR). **Eski paletin WCAG emeği BOŞA
      GİTMEDİ** - AYNI yöntem (tahminle değil, gerçek kontrast oranı
      hesaplanıp doğrulanarak, bkz. scratchpad'teki `contrast.js`) burada da
      uygulandı: koyu modda `--brand-accent` (#0d828f) beyazla 4.56:1, sayfa
      zeminiyle 4.06:1 - eski #1d8660 düzeltmesinin (4.53/4.01) BİREBİR AYNI
      "iki ucu da mümkün olduğunca yükseğe çek" dengesiyle seçildi. Açık mod
      için de aynı titizlik (#0c7c92: beyazla 4.87:1, zeminle 4.54:1).
      `--accent-warm` turuncusu da AYNI şekilde doğrulandı (koyu #c2570f
      beyazla 4.50:1, açık #ad4a0d beyazla 5.58:1). Yeni `--gradient-hero`
      token'ı (teal→turuncu) Gün 2'nin hero bölümü için hazırlandı.
- [x] **`AtlasLogo.jsx` (YENİ, SVG bileşen)** - eski `logo.png` (statik PNG,
      yeşil blob) yeni paletle renk UYUMSUZLUĞUNA düştüğü için (CSS
      değişkenleriyle yeniden renklendirilemiyordu) yerine geçti. Header'da
      ZATEN `h-7 w-7 rounded-full`'a kırpıldığı için (yazı okunaklı
      değildi) TAM "ATLAS WIKI" yazısını piksel piksel yeniden çizmek yerine
      basit bir marka işareti (organik blob + "A" harfi, `var(--brand-accent)`→
      `var(--accent-warm)` gradient'i) tercih edildi - SVG olduğu için CSS
      custom property'lerini DOĞRUDAN okuyor, açık/koyu temada otomatik
      doğru renklere geçiyor (favicon.png - ayrı bir dosya - bu değişikliğin
      kapsamı DIŞINDA bırakıldı, istenirse ayrı ele alınır).

**Canlı doğrulandı (gerçek tarayıcıda, CSS custom property'lerin GERÇEKTEN
render edilen değerlerini okuyarak - sadece dosyanın "doğru yazıldığını"
değil):** koyu modda `body`'nin `background-color`'ı `rgb(10, 20, 32)`
(`--page-bg`) ile birebir eşleşti, logo SVG'sinin gradient durak renkleri
`rgb(13, 130, 143)`→`rgb(194, 87, 15)` (tam beklenen teal→turuncu) çıktı,
aktif sekme alt çizgisi doğru teal rengi taşıdı; tema değiştirilip açık
moda geçilince TÜM değerler (body bg, brand-accent, accent-warm, text/text-h)
doğru açık-mod karşılıklarına döndüğü teyit edildi. `npm run lint`/`build`/
`test` (32/32 - bu branch `master`'dan, D grubunun henüz merge edilmemiş
`isRecognizedVideoUrl` testlerini İÇERMİYOR, beklenen) yeşil.

**Gün 2 - Ana sayfanın kendisi:**

Kullanıcının referans mockup'ındaki TÜM bölümler eklendi - hiçbiri dekoratif/
sahte veri DEĞİL, hepsi zaten var olan backend endpoint'lerinden (ya da
küçük, güvenli bir backend genişlemesinden) besleniyor:

- [x] **`HeroSection`** - `--gradient-hero` (Gün 1) zeminli karşılama alanı +
      GERÇEK, çalışan bir arama kutusu. Submit olunca `/wiki/pages?q=...`'a
      yönlendiriyor. **Bu sırada bulunan gerçek bir "bağlanmamış uç" (dangling
      wiring) düzeltildi:** `WikiSearch.jsx`'in `initialQuery` prop'u + kendi
      useEffect'i ZATEN vardı (yorumunda "üst bardaki arama kutusundan
      yönlendirildiğinde" diye açıkça yazıyordu) ama HİÇBİR yer bu URL
      parametresini okumuyordu - `WikiBoard.jsx`'e `useSearchParams` eklenip
      tamamlandı. Hero'ya İKİNCİ bir arama mekanizması İCAT EDİLMEDİ, var
      olan akış TAMAMLANDI.
- [x] **`FeaturedArticleCard`** ("Öne Çıkan Makale") - en yeni sayfa büyük bir
      kart olarak tekrar vurgulanıyor. Ayrı bir "editör seçimi" alanı İCAT
      EDİLMEDİ (backend'de yok, eklemek YAGNI olurdu).
- [x] **"Son Eklenen Makaleler" 9'dan 4'e indirildi** (kullanıcı isteği: "ilk
      sayfada örnek olarak 3-5 tane olsun") - Öne Çıkan Makale ZATEN en
      yeniyi gösterdiği için, bu bölüm SIRADAKİ 4'ü gösteriyor (aynı sayfa
      iki kez görünmüyor). "Tümünü Gör" zaten vardı.
- [x] **`SimplePageListPanel`** (Favorilere Eklenenler + Pinlenenler, "ayrı
      bölüm" olarak) - var olan `getFavoritePages`/`getPinnedPages`'i
      kullanan tek, esnek bir bileşen - dört ayrı liste bileşeni YAZILMADI.
- [x] **`DocumentsWidget`** ("Belgeler") - Documents modülünden gerçek veri,
      format-özel ikonlar `documentIcons.js`'ten (DocumentDetailPage'in
      ZATEN kullandığı harita - ikinci bir ikon eşlemesi İCAT EDİLMEDİ).
- [x] **`RecentVideosWidget`** ("Videolar/Eğitimler") - `VideoCenterPage`'in
      (Eksik-özellik listesi C grubu) AYNI `extractVideosFromContent`'ini
      tekrar kullanıyor, ikinci bir video-algılama YAZILMADI. VideoCenterPage'in
      AKSİNE TÜM sayfalar değil, son ~20 sayfalık bir dilim taranıyor (ana
      sayfa widget'ı için yeterli, tam galeri zaten `/wiki/videos`'ta).
- [x] **`DiscussionsWidget`** ("Tartışmalar") - platform GENELİNE ait yorumlar
      (`getComments(token)`, `pageId=null` - "Anasayfa Tartışması" sekmesiyle
      AYNI veri kaynağı). "Tartışmaya Katıl" ayrı bir sayfaya DEĞİL, var olan
      "Tartışma" sekmesine geçiyor - ikinci bir tartışma sayfası İCAT
      EDİLMEDİ.
- [x] **"Kategoriler" restyle edildi** - var olan `popularTags` verisi artık
      küçük bir ikon-rozet ızgarası (2 sütun) olarak gösteriliyor.
- [x] **`DepartmentDonutChart`** ("İstatistikler") - **backend'e küçük, güvenli
      bir alan eklendi:** `WikiDashboardDto.DepartmentBreakdown`
      (`GetWikiDashboardQueryHandler`'ın ZATEN bellekte tuttuğu
      `visiblePages`'ten türetiliyor, `PopularTags`'le AYNI desen, yeni bir
      sorgu/endpoint YOK) - donut grafiği SAHTE/rastgele veri GÖSTERMEDİ,
      gerçek departman dağılımını gösteriyor. Harici bir grafik kütüphanesi
      EKLENMEDİ - CSS `conic-gradient` + `color-mix()` ile saf bir donut
      (brand-accent'in azalan opaklık tonları, Gün 1'deki "kategorik çoklu
      renk yerine sade kalma" kararıyla tutarlı).
- [x] **`HomeFooter`** - SADECE gerçek linkler (mockup'taki "SSS"/"Destek
      Talebi"/sahte sosyal medya ikonları gibi karşılığı OLMAYAN dekoratif
      linkler BİLEREK EKLENMEDİ - projenin baştan beri sürdürdüğü "dekoratif/
      çalışmayan bir şey gösterme" ilkesi).

**Canlı doğrulandı (gerçek tarayıcı etkileşimiyle, hem koyu hem açık modda):**
TÜM bölümler doğru veriyle render edildi (Favoriler/Pinlenenler/Belgeler
gerçek kayıtlar gösterdi, İstatistikler donut'u gerçek departman dağılımını
- IT %78, Engineering %11, IK %11 - doğru çizdi). Hero arama kutusu uçtan uca
test edildi: "sunucu bakım" yazılıp gönderilince `/wiki/pages?q=sunucu%20bak%C4%B1m`'a
yönlendirdi VE WikiSearch otomatik çalışıp GERÇEK, alakalı sonuçlar
("Sunucu Bakım ve İzleme Rehberi") döndürdü. Videolar/Tartışmalar widget'ları
başlangıçta veri olmadığı için (doğru şekilde) hiç görünmüyordu - birer test
kaydı (video sayfası + platform-geneli yorum) eklenip widget'ların GERÇEKTEN
çalıştığı kanıtlandı, sonra temizlendi. Hero gradient'i ve donut grafiğinin
`conic-gradient`/`color-mix()`'i hem koyu hem açık modda doğru renklere
(gerçek render edilen CSS değerleri okunarak) geçtiği teyit edildi.
`npm run lint`/`build`/`test` (32/32) + `dotnet test Atlas.sln --filter
"Category!=Integration"` yeşil (regresyon yok - `WikiDashboardDto`'ya yeni
alan eklemek hiçbir testi kırmadı, bu DTO'ya referans veren test yoktu).

**"Görsel Tasarım Yenileme" artık TAMAMEN BİTTİ (Gün 1-2, palet+logo+ana
sayfa).**

## Ana sayfa takibi: "Son Eklenen Makaleler" carousel'ı + bildirim temizliği bug'ı (2026-08-17)

Görsel Tasarım Yenileme'nin hemen ardından, kullanıcı referans mockup'ıyla
canlı siteyi karşılaştırırken iki ayrı iş ortaya çıktı - biri planlı bir
UX isteği, öbürü kullanıcının fark ettiği gerçek bir veri temizliği
sorunundan doğan bağımsız bir bug avı.

- [x] **"Son Eklenen Makaleler" nokta-sayfalamalı carousel'a çevrildi**
      (`design/homepage-recent-articles-followup` branch'i, `master`'dan) -
      mockup'taki kart ızgarasının altındaki `• • •` işaretlerinin gerçek
      karşılığı. `AskUserQuestion` ile netleştirildi: kullanıcı "sayfa
      içinde genişleme" DEĞİL, gerçek bir carousel/kaydırmalı görünüm istedi.
      Backend'e `GetWikiDashboardQuery`'ye `ItemsPerSection`'dan AYRI bir
      `RecentlyAddedCount` parametresi eklendi (`GET /api/wiki/dashboard?
      recentlyAddedCount=13`) - SADECE "Son Eklenen Makaleler" havuzunu
      büyütüyor, `recentlyUpdated`/`departmentSpecific`'i şişirmiyor
      (`departmentSpecific`'in listesi frontend'de zaten hiç render
      edilmiyor). `RecentArticlesCarousel` (HomePage.jsx) - Öne Çıkan Makale
      + 3 sayfa x 4 kart (2x2 grid), nokta düğmelerine tıklamak SADECE
      component state'ini değiştiriyor, navigasyon YOK - "Tümünü Gör" linki
      (başka sayfaya gider) AYRICA duruyor. Canlı doğrulandı (gerçek
      backend'e karşı, `javascript_tool` ile): 3 nokta doğru render edildi,
      her tıklama URL değiştirmeden farklı 4 kart getirdi.

- [x] **Bulunan gerçek bug - yetim bildirim kayıtları:** Kullanıcı ana
      sayfadaki Bildirimler panelinde silinmiş sayfalara ait "hayalet"
      kayıtlar fark etti. Kök sebep: `WikiPageDeletedEvent` yayınlanınca AI
      kendi embedding'lerini temizliyordu (bkz. AI Semantik Arama bölümü)
      ama **Notifications modülü bu event'i hiç dinlemiyordu** -
      `WikiPageCreatedEventHandler`'ın yazdığı kalıcı `NotificationEntry`
      kaydı, sayfa silinince sonsuza kadar "yetim" olarak tabloda kalıyordu.
      Düzeltme: AI'ın `WikiPageDeletedEventHandler`'ıyla BİREBİR aynı desen -
      yeni `WikiPageDeletedEventHandler` (Notifications.Infrastructure) +
      `INotificationRepository.DeleteAllForResourceAsync` (Ders #22'deki
      "InMemory `ExecuteDelete`'i desteklemiyor" güvenli deseniyle -
      `ToListAsync`+`RemoveRange`). Aynı assembly'de yaşadığı için (AI/
      Documents'taki gibi ikinci bir MediatR assembly kaydı GEREKMEDİ,
      `WikiPageCreatedEventHandler` zaten Infrastructure'ı tarıyordu).
      Canlı doğrulandı (gerçek create+delete + Outbox'ın 5sn'lik turu
      beklenerek): sayfa oluşunca bildirim oluştu, silinince bildirim de
      silindi.
      **Test sırasında bulunan, düzeltilmeyen (bilinçli) ikincil bir
      gözlem:** `dotnet test tests/Atlas.IntegrationTests` çalıştırılınca 2
      yeni yetim bildirim daha oluştu - testler kendi WikiPage'lerini
      İZOLE bir InMemory `WikiDbContext`'te oluşturup siliyor (bkz.
      `AtlasApiFactory`), ama Notifications GERÇEK SQL Server'a yazıyor
      (Vault/AI/Documents ile AYNI "bilerek InMemory'e çevrilmeyen" grup) -
      testin `finally` bloğundaki DELETE, `WikiPageDeletedEvent`'i InMemory
      Outbox'a enqueue ediyor ama test host'u OutboxProcessor'ın bir
      SONRAKİ 5sn'lik turunu beklemeden kapanabiliyor, bu da bildirim
      temizliğinin o test çalıştırması için hiç tetiklenmemesine yol
      açabiliyor. Bu, AI'ın embedding'leri için ZATEN bilinen/kabul edilmiş
      bir sınıf soruna BENZER (bkz. "Integration testler artık kendi
      ürettikleri AI verisini temizliyor") - düşük hacimli (test başına en
      fazla birkaç satır), kendi kendine büyümeyen bir sızıntı, teorik bir
      "sağlamlaştırma" (ör. test teardown'a bir flush/wait eklemek) DENENMEDİ
      çünkü Ders #16'nın sonundaki notla AYNI gerekçe: kanıtlanmamış bir
      kırılganlığı "düzeltmeye" çalışmak yeni bir regresyon riski taşır.
      Gerekirse (hacim gerçekten büyürse) AI'ın test-verisi-takip deseni
      (try/finally ile oluşturulan ID'leri izleyip temizleme) buraya da
      uygulanabilir - şimdilik YAGNI.

- [x] **Veri temizliği (kullanıcı isteğiyle, canlı DB üzerinde, önce SELECT
      ile doğrulanarak - Ders #14):** `notifications.NotificationEntries`'de
      birikmiş 11 yetim kayıt (haftalar süren test/doğrulama oturumlarından)
      + yukarıdaki düzeltmeyi test ederken oluşan 2 yeni yetim kayıt
      silindi. AI embedding'leri/Favoriler/Pinler/Belgeler/Vault tabloları
      da kontrol edildi - hepsi zaten temizdi (yetim veri yok). Ayrıca
      `auth.Users`'ta haftalar/ayların birikimi ~20 otomatik-test deseniyle
      (tarih/random suffix'li e-posta) oluşturulmuş hesap silindi -
      `wiki.WikiPages.CreatedByUserId` FK'siyle (Ders'in tek istisnai FK'ı)
      korunan 4 hesap (gerçek içerik yazmış test kullanıcıları:
      `browser-test-1`/`ik-calisan-yeni`/`test-login`/`test-shadcn`) VE 5
      gerçek/örnek kullanıcı (`admin`/`admin2`/`ahmet`/`esra`/`mehmet`)
      BİLEREK silinmeden bırakıldı - önce bir `SELECT ... WHERE Email NOT
      IN (...)` ile silinecek tam liste gösterilip kullanıcı onayı alındı.

`dotnet build`/`dotnet test Atlas.sln --filter "Category!=Integration"`
(regresyon yok) + `dotnet test tests/Atlas.IntegrationTests` (24/24) +
`npm run lint`/`build`/`test` (32/32) yeşil.

## Sırada ne var

1. Gerçek embedding/LLM sağlayıcısına geçiş (API key'ler gelince) - sadece
   `IEmbeddingService`'in DI kaydını değiştirmek yeterli olacak şekilde tasarlandı
   (bu, API key'ler gelene kadar bloklanmış durumda). **Güncel durum
   (2026-08-12):** yukarıdaki "Voyage AI embedding entegrasyonu" bölümüne bkz -
   key'e bağlı OLMAYAN TÜM hazırlık (gerçek Infrastructure sınıfı, batch/retry/
   fail-fast mantığı, HttpClient altyapısı, testler, Wiki+Documents bulk
   reindex) bitti. Kalan tek şey gerçekten key'e bağlı: (a) User Secrets'a
   key'in girilmesi, (b) DI kaydının tek satır değişmesi, (c) `Model`/boyut
   kararının gerçek key'e karşı doğrulanması, (d) toplu reindex'in
   tetiklenmesi - yukarıdaki runbook. **Not (2026-07-28,
   kullanıcı gözlemi):** Arama şu an "başlığa göre eşleşiyormuş" hissi
   verebiliyor - kod tarafında bu YANLIŞ, canlı test edilip kanıtlandı
   (`SearchByMeaningQueryHandler` sadece `ChunkText`/vektöre bakıyor,
   `Title` skora hiç girmiyor, sadece görüntüleme alanı - bkz.
   `GenerateWikiPageEmbeddingsCommandHandler`'daki `TextChunker.Chunk(request.Content)`,
   Title hiç chunk'lanmıyor). Gerçek sebep `FakeEmbeddingService`'in kaba
   kelime-hash'leme yöntemi - insanlar başlığı içeriğin özeti gibi yazdığı
   için başlık/içerik kelimeleri doğal olarak örtüşüyor, bu da "başlık
   etkisi" gibi GÖRÜNÜYOR ama değil. Gerçek bir embedding sağlayıcısına
   geçilince (anlam benzerliğine dayanacağı için) bu his kendiliğinden
   azalacak/netleşecek - bu satırdaki geçişin dışında AYRI bir kod
   değişikliği GEREKMİYOR, sadece geçiş tamamlanınca doğal olarak düzelecek
   bir gözlem olarak not düşüldü.
2. Portföy sertleştirme yol haritası, Cuma'ya kadar hedeflenen 3 ek iş
   (Docker Compose, SignalR toast, rate limiting) VE orijinal 6 maddelik
   özellik listesinin denetimde bulunan 3 gerçek eksiği (link arama, kırmızı
   link, etiketler - yukarıdaki bölüme bkz.) hepsi tamamlandı.
3. **"Kapsamlı Geliştirme Paketi" TAMAMEN BİTTİ ve TAMAMI merge edildi
   (yukarıdaki bölüme bkz.):** P1 (Favoriler/Pinler), P2 (Editör v2), P3
   (Documents temeli) merge edildi (PR #6). P4 (belge işleme pipeline'ı,
   Gün 1-6) merge edildi (PR #7, bkz. Ders #21'deki regresyon düzeltmesi de
   aynı PR'da). P5 (Documents→AI/RAG entegrasyonu, Gün 1-4) merge edildi
   (PR #8). P6 (belge versiyonlama + toplu yükleme, Gün 1-5) merge edildi
   (PR #9). **P7 (güvenlik sertleştirme, Gün 1-3) merge edildi (PR #10).**
   Paketin 19 bölümlük orijinal spec'inin TAMAMI `master`'da.
4. **README.md portföy seviyesinde güncellendi ve merge edildi** (PR #11/#12,
   2026-08-12) - Audit/Vault/Documents/AI modülleri dahil projenin GÜNCEL
   tam mimarisini yansıtıyor (eskiden "Bölüm 19 - Sayfalama"da kalmış bir
   öğrenme günlüğüydü).
5. **Test & CI Sertleştirme paketi TAMAMLANDI ve merge edildi (PR #13,
   yukarıdaki bölüme bkz.):** integration testler CI'a taşındı (CI'a
   taşınınca gerçek bir bug bulundu - xUnit test sınıfı paralelliği, bkz.
   ilgili bölüm), Documents modülüne hızlı bir Application-katmanı unit test
   projesi eklendi (18 test), frontend'e ilk kez otomatik test altyapısı
   (Vitest + React Testing Library, 17 test) kuruldu.

**Şu an bloklanmış olan (madde 1) dışında net, önceden planlanmış bir
sonraki adım YOK** - hem "Kapsamlı Geliştirme Paketi"nin 19 bölümlük orijinal
spec'i hem de "API key'ler gelene kadar ne yapalım" sorusuna açılan Test & CI
paketi tamamen bitti. Bir sonraki özellik/yön kullanıcıyla birlikte
kararlaştırılmalı - varsayılan bir öncelik YOK.

**AI Semantik Arama artık TAMAMLANDI (Gün 1-6):** Domain modeli → chunking/fake
embedding → otomatik ingestion → arama Query'si + görünürlük filtresi →
API endpoint'i → integration test + haftalık retro (bu retro sırasında
Transactional Outbox Pattern kendi 5 günlük özelliği olarak açıldı, yukarıya bkz.).

## Endpoint referansı

- `POST /api/auth/register` (email, fullName, password, department?) → açık
- `POST /api/auth/login` (email, password) → açık, döner: `{accessToken, refreshToken}` ya da 401
- `POST /api/auth/refresh` (refreshToken) → döner: yeni `{accessToken, refreshToken}` ya da 401
- `GET /api/auth/users` → sadece Admin rolü
- `GET /api/wiki/pages` → açık, DepartmentOnly filtresi artık query'den DEĞİL,
  gönderilen token'daki (varsa) department claim'inden otomatik uygulanır
- `GET /api/wiki/pages/{id}` → açık, aynı görünürlük kuralı burada da uygulanır
  (Id'yi bilmek görebilmek anlamına gelmiyor - başka departmanın DepartmentOnly
  sayfasının Id'si tahmin edilse bile 404 döner). Arama sonucuna tıklanınca
  tam sayfayı göstermek için kullanılıyor.
- `POST /api/wiki/pages` (title, content, departmentName, visibility: Public|DepartmentOnly, folderId?, tags?)
  → token gerektirir. departmentName normal kullanıcı için YOK SAYILIR (departman
  her zaman JWT'den zorlanır) - sadece Admin gönderdiği departmanı seçebilir.
  tags virgülle ayrılmış ham metin, Domain'de normalize edilir (bkz. "6 maddelik
  özellik listesi" bölümündeki (c) notu). `PUT /api/wiki/pages/{id}` da AYNI
  tags alanını kabul ediyor (departmentName HARİÇ, geri kalan alanlarla aynı).
- `DELETE /api/wiki/pages/{id}` → token gerektirir. Admin HER sayfayı, normal
  kullanıcı SADECE kendi oluşturduğunu silebilir (aksi halde 403).
- `POST /api/wiki/reindex` → sadece Admin rolü. Var olan TÜM sayfalar için
  AI'ın embedding'lerini yeniden üretir (`WikiPageCreatedEvent`'i toplu
  yeniden yayınlayarak) - bir bakım hatası ya da embedding sağlayıcısı
  değişikliği sonrası kullanılacak bir admin aracı.
- `GET /api/ai/search?q=...&topN=5&fromUtc=...&toUtc=...` (topN/fromUtc/toUtc
  opsiyonel, varsayılan topN=5) → token gerektirir, sonuçlar departman
  görünürlük kuralına göre filtrelenir (Admin bypass eder). fromUtc/toUtc
  verilirse, mesafe sıralamasından ÖNCE embedding'in oluşturulma zamanına
  göre daraltır - normal semantik aramaya EK, isteğe bağlı bir filtre. P5'ten
  itibaren SADECE wiki sayfalarını DEĞİL, Documents'ın da chunk'larını
  arıyor - her sonuç `{sourceType: "WikiPage"|"Document", resourceId, ...}`
  taşıyor.
- `GET /api/documents/search-suggestions?q=...` → açık (görünürlük filtresi
  otomatik), `GET /api/wiki/search-suggestions`'ın Documents karşılığı -
  başlık/etiket üzerinde hafif, gerçek-zamanlı arama (içerik YOK, Document
  kendi çıkarılmış metnini saklamıyor). WikiEditorPage'in link penceresi
  ikisini birlikte çağırıyor.
- `GET /api/audit-log?details=...&fromUtc=...&toUtc=...&pageNumber=1&pageSize=20`
  (hepsi opsiyonel, `details` kısmi eşleşme/`Contains`) → sadece Admin rolü.
  `WikiPage.Created`/`WikiPage.Deleted` eylemlerini kaydediyor (bkz.
  AuditBehavior, Shared.CQRS).
- `/hubs/notifications` (SignalR Hub) → Wiki'de yeni sayfa eklenince "WikiPageCreated" mesajı yayınlanır
- `POST /api/wiki/pages/{id}/favorite`, `POST /api/wiki/pages/{id}/pin` → token
  gerektirir, toggle (varsa kaldırır, yoksa ekler), audit'lenmez.
- `GET /api/wiki/favorites`, `GET /api/wiki/pinned` → token gerektirir, mevcut
  görünürlük kuralı uygulanır (erişimi kaybedilen sayfa listeden sessizce düşer).
- `GET /api/vault/entries`, `GET /api/vault/entries/{id}` → token gerektirir,
  owner-or-Admin (detay: varlığı gizle/404).
- `POST /api/vault/entries`, `PUT /api/vault/entries/{id}`, `DELETE /api/vault/entries/{id}`
  → token gerektirir, owner-or-Admin (throw-based 403).
- `POST /api/vault/entries/{id}/reveal` → token gerektirir, owner-or-Admin,
  audit'lenir ("PasswordEntry.Revealed" - Reveal bilerek bir Command),
  kullanıcı bazlı rate-limitli (dakikada 10, P7 Gün 1).
- `POST /api/documents/upload` (multipart: file + title/description/visibility/
  departmentName?/tags?) → token gerektirir, `IFormFile`/`[FromForm]` (JSON-bound
  record DEĞİL - minimal API'nin dosya yükleme mecburiyeti). Yanıt:
  `{id, duplicateOfDocumentId?, duplicateOfTitle?}` - son ikisi doluysa
  (P6 Gün 3) aynı içerikli GÖRÜNÜR başka bir belge var demektir, YÜKLEMEYİ
  ENGELLEMEZ, sadece bilgilendirir.
- `GET /api/documents` (paged), `GET /api/documents/{id}` → açık, görünürlük
  kuralı `IWikiVisibilityChecker` ile uygulanır (detay: varlığı gizle/404).
- `GET /api/documents/{id}/download` → token gerektirir (tek dosya erişim yolu,
  `UseStaticFiles` yok).
- `PUT /api/documents/{id}`, `DELETE /api/documents/{id}` → token gerektirir,
  owner-or-Admin (throw-based 403), silme diskteki dosyayı da temizler.
- `POST /api/documents/{id}/reprocess` → token gerektirir, owner-or-Admin.
  Var olan StorageKey/ContentType ile `DocumentUploadedEvent`'i Outbox'a
  yeniden yazar - "bu TEK belge Failed kaldı" senaryosu için (bulk/Admin-only
  DEĞİL). Extracting durumundaki bir belge için 400.
- `POST /api/documents/reindex` → sadece Admin rolü. `POST /api/wiki/reindex`'in
  Documents karşılığı - var olan TÜM belgeler için (durum fark etmeksizin)
  `DocumentUploadedEvent`'i toplu olarak Outbox'a yeniden yazar - embedding
  sağlayıcısı değişikliği sonrası kullanılacak bir bakım aracı.
  `reprocess`'in YERİNE geçmiyor, ayrı bir senaryoya hizmet ediyor.
- `POST /api/documents/{id}/versions` (multipart: file) → token gerektirir,
  owner-or-Admin. Mevcut dosyayı bir `DocumentVersion`'a arşivleyip yenisini
  Document'a yazar, `CurrentVersionNumber` artar, Status Uploaded'a döner
  (yeniden işlenecek). Extracting durumundaki bir belge için 400.
- `GET /api/documents/{id}/versions` → açık, görünürlük filtresi otomatik.
  SADECE ESKİ versiyonları döndürür (güncel versiyon `GET /api/documents/{id}`'in
  `currentVersionNumber` alanında).
- `GET /api/documents/{id}/versions/{versionNumber}/download` → token
  gerektirir, `GET /api/documents/{id}/download`'ın belirli bir eski versiyon
  için karşılığı.

İlk kurulumda otomatik oluşan admin: `admin@atlas.local` / `Admin123!` (Admin rolüyle,
SADECE tablo ilk kez boşken - tablo doluysa tekrar oluşturulmaz).

**`README.md` 2026-08-12'de güncellendi** - eskiden "Bölüm 19 — Sayfalama"da
kalmış bir öğrenme günlüğüydü (AI Semantik Arama, Outbox Pattern, Audit log,
Vault, Documents, "Kapsamlı Geliştirme Paketi" P1-P7'den hiç bahsetmiyordu).
Artık Bölüm 27'ye kadar (özet seviyesinde, gün-gün DEĞİL - her büyük özellik
için birkaç satırlık bir "Bölüm") güncel; ayrıca giriş paragrafı ve mimari
ağacı (Audit/Vault/Documents modülleri, Atlas.Shared.Text) da yenilendi.
**Yine de bu dosya (CLAUDE.md) projenin TEK eksiksiz kaynağı** - README
bilerek portföy/genel bakış seviyesinde tutuldu (gün-gün kırılım, canlı
bulunan/düzeltilen gerçek bug'lar, mimari kararların tam gerekçeleri burada,
"Şu ana kadar tamamlananlar"/"Kapsamlı Geliştirme Paketi"/"Endpoint
referansı" bölümlerinde). README'yi tekrar güncellerken bu dengeyi (özet
vs. tam detay) koru - CLAUDE.md'nin bir kopyasına dönüştürme.