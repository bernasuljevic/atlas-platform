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
        (Action/FromUtc/ToUtc HEPSİ opsiyonel, PageNumber/PageSize) +
        `IAuditLogRepository` soyutlaması. `EfAuditLogRepository` filtre/
        sayfalamayı DB SEVİYESİNDE yapıyor (Wiki'nin `GetWikiPagesQueryHandler`'ının
        "tüm veriyi çek, bellekte filtrele" yaklaşımının BİLEREK aksine) -
        audit log zamanla büyümesi beklenen, tüm satırlarını cache'lemenin/
        belleğe çekmenin mantıklı olmadığı bir tablo. Endpoint sadece Admin
        rolü (`AuthEndpoints`'teki `GET /users` ile aynı desen - audit log'un
        kendisi "kim ne yaptı" taşıdığı için normal bir kullanıcının başkasının
        işlemlerini görmesi ayrı bir gizlilik sorunu olurdu). Canlı doğrulandı:
        filtresiz liste (en yeni önce, doğru `totalCount`/`totalPages`),
        `?action=WikiPage.Created` filtresi doğru daraltıyor, Admin olmayan
        kullanıcı 403, token'sız istek 401.
  - [x] **Gün 3/3:** Frontend'de audit log görüntüleme sayfası. Yeni
        `AuditLogPage.jsx` - eylem/tarih aralığı filtresi + sayfalama,
        `WikiBoard.jsx`'e sadece Admin'in gördüğü bir "Audit Log" linki
        eklendi (gerçek yetkilendirme zaten backend'de `RequireRole
        ("Admin")` - buradaki kontrol WikiPageTable'daki "Sil" butonuyla
        aynı desen, sadece UI kararı). Tarayıcıda uçtan uca doğrulandı:
        Admin girişiyle liste/filtre çalışıyor, Member hesabıyla sayfa
        "yetkin yok" mesajı gösteriyor.

**Portföy sertleştirme yol haritası artık TAMAMLANDI (1-4).** Audit log
özelliği baştan sona bitti: domain modeli → pipeline behavior → endpoint →
frontend sayfası.

## Sırada ne var

1. Gerçek embedding/LLM sağlayıcısına geçiş (API key'ler gelince) - sadece
   `IEmbeddingService`'in DI kaydını değiştirmek yeterli olacak şekilde tasarlandı
   (bu, API key'ler gelene kadar bloklanmış durumda).
2. Portföy sertleştirme yol haritası tamamlandı - yeni bir yön/özellik
   kullanıcıyla birlikte kararlaştırılacak.

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
- `POST /api/wiki/pages` (title, content, departmentName, visibility: Public|DepartmentOnly) → token gerektirir.
  departmentName normal kullanıcı için YOK SAYILIR (departman her zaman JWT'den
  zorlanır) - sadece Admin gönderdiği departmanı seçebilir.
- `DELETE /api/wiki/pages/{id}` → token gerektirir. Admin HER sayfayı, normal
  kullanıcı SADECE kendi oluşturduğunu silebilir (aksi halde 403).
- `POST /api/wiki/reindex` → sadece Admin rolü. Var olan TÜM sayfalar için
  AI'ın embedding'lerini yeniden üretir (`WikiPageCreatedEvent`'i toplu
  yeniden yayınlayarak) - bir bakım hatası ya da embedding sağlayıcısı
  değişikliği sonrası kullanılacak bir admin aracı.
- `GET /api/ai/search?q=...&topN=5` (topN opsiyonel, varsayılan 5) → token
  gerektirir, sonuçlar departman görünürlük kuralına göre filtrelenir (Admin bypass eder).
- `GET /api/audit-log?action=...&fromUtc=...&toUtc=...&pageNumber=1&pageSize=20`
  (hepsi opsiyonel) → sadece Admin rolü. `WikiPage.Created`/`WikiPage.Deleted`
  eylemlerini kaydediyor (bkz. AuditBehavior, Shared.CQRS).
- `/hubs/notifications` (SignalR Hub) → Wiki'de yeni sayfa eklenince "WikiPageCreated" mesajı yayınlanır

İlk kurulumda otomatik oluşan admin: `admin@atlas.local` / `Admin123!` (Admin rolüyle,
SADECE tablo ilk kez boşken - tablo doluysa tekrar oluşturulmaz).

Detaylı notlar için `README.md`'ye bak (Bölüm 10'a kadar güncel).