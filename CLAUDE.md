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
      bypass etti). **Henüz bir frontend arama kutusu YOK** - WikiBoard.jsx'e
      eklenmedi, sadece backend API hazır.

## İzlenecek teknik borç (henüz aksiyon gerektirmiyor, büyürse ele alınmalı)

- `FakeCurrentUserAccessor`, `Atlas.Modules.AI.Application.Tests` ve
  `Atlas.Modules.Wiki.Application.Tests`'te neredeyse birebir kopya (tek fark
  opsiyonel `userId` parametresi). 2 kopya - henüz "üç kural" eşiğine gelmedi,
  ama üçüncü bir test projesi (örn. Auth.Application.Tests) aynı fake'i
  isterse, paylaşılan bir `Atlas.Shared.Testing` projesine taşınmalı.
- `WikiBoard.jsx` 350 satıra yaklaştı - liste, oluşturma, silme, detay dialogu
  ve JWT çözme tek component'te. Gün 5'te arama kutusu da eklenirse bölünmeyi
  (örn. ayrı `WikiPageTable`/`CreateWikiPageDialog` component'leri) düşünmenin
  vakti gelir.
- `WikiVisibilityRules.IsVisibleTo`'ya eklenen `viewerIsAdmin` bool parametresi
  şu an temiz ama ölçeklenmiyor - ÜÇÜNCÜ bir rol (örn. "Departman Yöneticisi")
  eklenirse bir `UserRole` enum'ı ya da capability seti'ne geçmek gerekir
  (şimdiden yapmak YAGNI ihlali olurdu).

## Sırada ne var

1. **AI Semantik Arama - Gün 6/6:** Integration test (sayfa oluştur → embed
   edildiğini doğrula → ara → sonuçta çık) + haftalık mimari retro (Outbox
   Pattern teknik borcu dahil - bkz. Gün 3 notu, ayrıca bu oturumdaki
   "İzlenecek teknik borç" listesi de gözden geçirilmeli).
2. Frontend'e arama kutusu eklenmesi - backend hazır (`GET /api/ai/search`)
   ama WikiBoard.jsx'te (ya da ayrı bir component'te) hiç kullanılmıyor henüz.
3. Gerçek embedding/LLM sağlayıcısına geçiş (API key'ler gelince) - sadece
   `IEmbeddingService`'in DI kaydını değiştirmek yeterli olacak şekilde tasarlandı.

## Endpoint referansı

- `POST /api/auth/register` (email, fullName, password, department?) → açık
- `POST /api/auth/login` (email, password) → açık, döner: `{accessToken, refreshToken}` ya da 401
- `POST /api/auth/refresh` (refreshToken) → döner: yeni `{accessToken, refreshToken}` ya da 401
- `GET /api/auth/users` → sadece Admin rolü
- `GET /api/wiki/pages` → açık, DepartmentOnly filtresi artık query'den DEĞİL,
  gönderilen token'daki (varsa) department claim'inden otomatik uygulanır
- `POST /api/wiki/pages` (title, content, departmentName, visibility: Public|DepartmentOnly) → token gerektirir.
  departmentName normal kullanıcı için YOK SAYILIR (departman her zaman JWT'den
  zorlanır) - sadece Admin gönderdiği departmanı seçebilir.
- `DELETE /api/wiki/pages/{id}` → token gerektirir. Admin HER sayfayı, normal
  kullanıcı SADECE kendi oluşturduğunu silebilir (aksi halde 403).
- `GET /api/ai/search?q=...&topN=5` (topN opsiyonel, varsayılan 5) → token
  gerektirir, sonuçlar departman görünürlük kuralına göre filtrelenir (Admin bypass eder).
- `/hubs/notifications` (SignalR Hub) → Wiki'de yeni sayfa eklenince "WikiPageCreated" mesajı yayınlanır

İlk kurulumda otomatik oluşan admin: `admin@atlas.local` / `Admin123!` (Admin rolüyle,
SADECE tablo ilk kez boşken - tablo doluysa tekrar oluşturulmaz).

Detaylı notlar için `README.md`'ye bak (Bölüm 10'a kadar güncel).