# Atlas Platform — Proje Hafızası

## Bu projenin amacı

Kullanıcının .NET / modüler monolith mimarisini **öğrenerek** inşa ettiği bir
öğrenme + staj defteri projesi. Kurumsal wiki + AI fikrine dayanıyor (orijinal
ilham: SubMed Platform mimarisi - departman bazlı wiki + AI katmanı). AI modülünün
Domain modeli ve PostgreSQL/pgvector altyapısı kuruldu; embedding üretimi/LLM
entegrasyonu API key'ler gelince yapılacak.

## ÇOK ÖNEMLİ — Nasıl çalışmalısın

Kullanıcı bu projeyi adım adım, **öğreterek** inşa ediyor. Zaman zaman staj
defteri için ilerleme yazıyor, o yüzden değişiklikler net, açıklanabilir ve
test edilmiş olmalı:

- Kod yazarken **neden** o kararı verdiğini kısaca açıkla, sessizce büyük
  değişiklikler yapıp "bitti" deme.
- Yeni bir kavram tanıtırken 1-2 cümleyle ne işe yaradığını anlat.
- Küçük hatalarda (typo, eksik using) sormadan düzelt - mimari karar
  değiştiriyorsan önce sor.
- Her anlamlı adımdan sonra `dotnet build` ile doğrula, sonra commit önerisi sun.

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
  Web/packages/ui/ → paylaşılan @atlas/ui paketi (şu an boş iskelet)


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

**AI modülü (iskelet):** `WikiPageEmbedding` entity (AI.Domain) - `WikiPageId`,
`Embedding` (`Pgvector.Vector`, boyut 1024 - Voyage AI'a uygun), `CreatedAtUtc`.
`AiDbContext` (AI.Infrastructure) PostgreSQL'e `Npgsql.EntityFrameworkCore.PostgreSQL`
+ `Pgvector.EntityFrameworkCore` ile bağlanıyor, `HasPostgresExtension("vector")`
migration'ın "vector" extension'ını otomatik `CREATE EXTENSION` etmesini sağlıyor.
Henüz bir Command/Query yok - embedding'i kim, ne zaman üretecek (Wiki sayfası
oluşturulunca mı, ayrı bir job ile mi) LLM entegrasyonu gelince netleşecek.

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

## Sırada ne var

1. AI modülüne embedding üretimi + LLM entegrasyonu (API key'ler gelince)
2. React: routing (React Router), Web/packages/ui'ye gerçek paylaşılan bileşenler taşınması

## Endpoint referansı

- `POST /api/auth/register` (email, fullName, password, department?) → açık
- `POST /api/auth/login` (email, password) → açık, döner: `{accessToken, refreshToken}` ya da 401
- `POST /api/auth/refresh` (refreshToken) → döner: yeni `{accessToken, refreshToken}` ya da 401
- `GET /api/auth/users` → sadece Admin rolü
- `GET /api/wiki/pages` → açık, DepartmentOnly filtresi artık query'den DEĞİL,
  gönderilen token'daki (varsa) department claim'inden otomatik uygulanır
- `POST /api/wiki/pages` (title, content, departmentName, visibility: Public|DepartmentOnly) → token gerektirir
- `/hubs/notifications` (SignalR Hub) → Wiki'de yeni sayfa eklenince "WikiPageCreated" mesajı yayınlanır

İlk kurulumda otomatik oluşan admin: `admin@atlas.local` / `Admin123!` (Admin rolüyle,
SADECE tablo ilk kez boşken - tablo doluysa tekrar oluşturulmaz).

Detaylı notlar için `README.md`'ye bak (Bölüm 10'a kadar güncel).