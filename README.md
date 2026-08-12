# Atlas Platform — Öğrenme Notları

[![CI](https://github.com/bernasuljevic/atlas-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/bernasuljevic/atlas-platform/actions/workflows/ci.yml)

Modüler monolith mimarisiyle sıfırdan kurulan, .NET 10 + React tabanlı bir
kurumsal bilgi platformu. Kurumsal wiki + AI fikrine dayanıyor (orijinal
ilham: SubMed Platform mimarisi - departman bazlı wiki + AI katmanı), bir
öğrenme projesi olarak başladı, zamanla GitHub portföyünde gösterilebilecek
kurumsal seviyede bir ürüne dönüştü. Yedi modül var: **Auth** (JWT + refresh
token + e-posta doğrulama), **Wiki** (klasör ağacı, zengin blok editörü,
favoriler/pinler, etiketler), **Notifications** (SignalR, gerçek zamanlı),
**AI** (Wiki sayfaları + yüklenen belgeleri birlikte tarayan semantik arama),
**Audit** (denetim kaydı - kim ne zaman ne yaptı), **Vault** (kurum içi şifre
kasası, Data Protection API ile şifrelenmiş) ve **Documents** (belge
kütüphanesi - yükleme, metin çıkarımı, versiyonlama, AI aramasına otomatik
bağlanma). Gerçek bir SQL Server (Auth/Wiki/Audit/Vault/Documents, ayrı
şemalar) ve PostgreSQL/pgvector (AI embedding'leri) veritabanı, Redis cache,
SignalR gerçek zamanlı bildirim, JWT + refresh token tabanlı login, IP/kullanıcı
bazlı rate limiting, crash-safe event teslimatı (Transactional Outbox Pattern),
Serilog + correlation ID ile üretim benzeri bir gözlemlenebilirlik katmanı ve
GitHub Actions ile CI/CD var.

## Mimari özet

```
src/Shared/                          → tüm modüllerin ortak kullandığı temel yapılar
  Atlas.Shared.Kernel                → Entity taban sınıfı
  Atlas.Shared.Contracts             → modüller arası interface'ler/event'ler
  Atlas.Shared.CQRS                  → MediatR pipeline behavior'ları (Logging/Validation/Caching/Audit/CacheInvalidation)
  Atlas.Shared.Caching               → Redis cache (ICacheService/RedisCacheService)
  Atlas.Shared.Text                  → chunking algoritması (AI + Documents ortak kullanıyor)
src/Modules/Auth/                    → Domain → Application → Infrastructure → Api
src/Modules/Wiki/                    → aynı 4 katman deseni
src/Modules/Notifications/           → aynı 4 katman deseni, SignalR Hub Infrastructure'da
src/Modules/AI/                      → aynı 4 katman deseni - Wiki+Documents birleşik semantik arama
src/Modules/Audit/                   → aynı 4 katman deseni - denetim kaydı
src/Modules/Vault/                   → aynı 4 katman deseni - şifre kasası (Data Protection API)
src/Modules/Documents/               → aynı 4 katman deseni - belge kütüphanesi + işleme pipeline'ı
src/Host/Atlas.Api/                  → composition root, sadece her modülün *.Api projesine referans verir
tests/                               → xUnit unit testler (Domain/Application/Infrastructure) + Atlas.IntegrationTests
Web/                                 → npm workspaces monorepo
  Web/apps/platform/                 → React (Vite) frontend, shadcn/ui
  Web/packages/ui/                   → paylaşılan @atlas/ui paketi (Button/Card/Input/Dialog/Table/...)
docker-compose.yml                   → SQL Server + PostgreSQL/pgvector + Redis + backend + frontend
```

**Katman kuralı:** Domain framework'ten habersiz. Application "nasıl saklanır"ı
bilmez, sadece interface kullanır. Infrastructure gerçek implementasyonu yazar
(EF Core, JWT, hash'leme). Api, modülün DI kayıt mekanizmasını (`AddXModule()`)
ve endpoint'lerini barındırır. Modüller birbirinin Domain/Application/
Infrastructure'ına asla referans vermez - sadece `Shared.Contracts`'taki
interface'ler ve domain event'ler üzerinden konuşurlar.

## Hızlı başlangıç (tek komut, Docker Compose)

Projeyi sadece denemek/incelemek istiyorsan (aktif geliştirme yapmayacaksan)
en hızlı yol bu - SQL Server Express kurmana, User Secrets ayarlamana ya da
`npm install` çalıştırmana gerek yok. Tek gereksinim
[Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
docker compose up --build
```

Bu tek komut şunların HEPSİNİ ayağa kaldırır: SQL Server (Auth+Wiki+Audit+
Vault+Documents, ayrı şemalar), PostgreSQL+pgvector (AI embedding'leri),
Redis, backend API (migration'ları otomatik uygular) ve derlenmiş React
frontend'i (nginx ile sunuluyor). Yüklenen belgeler `atlas-documents-data`
adında kalıcı bir volume'de tutuluyor - `docker compose down` (volume'ler
silinmeden) sonrasında da kaybolmuyor. İlk çalıştırmada image'ları indirip
derlemek birkaç dakika sürebilir, sonrakiler çok daha hızlı.

Hazır olduğunda:
- Frontend: http://localhost:5173
- Backend: http://localhost:5000/health → `{"status":"Healthy",...}` görmelisin
- Varsayılan admin: `admin@atlas.local` / `Admin123!` (tablo ilk kez boşken otomatik oluşuyor)

Durdurmak için `Ctrl+C`, tamamen kaldırmak (volume'ler dahil) için
`docker compose down -v`.

**Not:** `docker-compose.yml`'deki SQL Server şifresi ve JWT imzalama anahtarı
BİLEREK bu compose stack'ine özel, gerçek bir sır DEĞİL (native geliştirme
kurulumundaki User Secrets'tan tamamen bağımsız) - sadece yerel/demo amaçlı.

## Çalıştırmak için (aktif geliştirme için - yerel kurulum)

Kod üzerinde çalışacaksan (debug, hızlı iterasyon, testler) bu yerel kurulum
daha pratik - IDE'nin debugger'ını doğrudan kullanabilirsin.

### 1. Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — `dotnet --version` ile `10.x` görmelisin.
- [Node.js](https://nodejs.org/) (npm ile birlikte) — React frontend için.
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — Redis ve PostgreSQL için.
- SQL Server Express (`.\SQLEXPRESS` instance adıyla, Windows Authentication) - Auth/Wiki/Audit/Vault/Documents verisi için (tek veritabanı, ayrı şemalar).

### 2. Docker container'larını başlat (Redis + PostgreSQL)

```bash
docker compose up -d
```

Bu, `atlas-redis` (port 6379) ve `atlas-postgres` (host portu **5433**, container
içinde 5432 - bu makinede native bir PostgreSQL Windows servisi zaten 5432'yi
dinlediği için host tarafı 5433'e alındı) container'larını ayağa kaldırır.
Doğrulamak için:

```bash
docker ps
```

`atlas-redis` ve `atlas-postgres` "Up" durumda görünmeli.

### 3. JWT key'ini User Secrets'a ekle (sadece ilk kurulumda)

`Jwt:Key` artık `appsettings.json`'da DEĞİL - repo public olduğu için secrets
oraya hiç yazılmıyor. İlk kurulumda kendi makinende bir key oluşturman gerekiyor:

```bash
cd src/Host/Atlas.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<en az 32 karakterlik rastgele bir string>"
```

`Properties/launchSettings.json` ortamı otomatik `Development` yapıyor, User
Secrets sadece bu ortamda yükleniyor.

### 4. Backend'i çalıştır

```bash
dotnet restore
dotnet build
dotnet run --project src/Host/Atlas.Api
```

Geliştirme sırasında (kod kaydettikçe otomatik yeniden başlatma için):

```bash
dotnet watch --project src/Host/Atlas.Api
```

Terminalde `Now listening on: http://localhost:5000` gibi bir çıktı göreceksin
(`launchSettings.json` varsayılan olarak 5000 portunu ayarlıyor).

### 5. Backend'i doğrula

- `http://localhost:5000/` → API ayakta mı (basit ping)
- `http://localhost:5000/health` → SQL Server + Redis + PostgreSQL'e gerçekten
  ulaşılabiliyor mu (`{"status":"Healthy","services":{"sqlserver":"Healthy",...}}`)
- `http://localhost:5000/swagger` → SignalR Hub (Notifications) hariç TÜM
  modüllerin (Auth/Wiki/AI/Audit/Vault/Documents) endpoint'lerini tarayıcıdan
  deneyebileceğin Swagger UI. Sağ üstteki **Authorize** butonuna login'den
  dönen `accessToken`'ı `Bearer <token>` formatında yapıştırarak korumalı
  endpoint'leri de deneyebilirsin.

### 6. React frontend'i çalıştır

Proje `Web/` altında npm workspaces ile kurulu (`Web/apps/platform` ana uygulama,
`Web/packages/ui` paylaşılan bileşen paketi):

```bash
cd Web
npm install    # ilk seferde, workspace kökünden
cd apps/platform
npm run dev
```

`http://localhost:5173` adresinde açılır. **.NET API'nin (5000 portu) ayrıca
ayakta olması gerekir** — CORS backend'de sadece `http://localhost:5173`'e
izin verecek şekilde ayarlı (`Program.cs`).

### Varsayılan admin kullanıcı

İlk kurulumda (Auth veritabanı tablosu ilk kez boşken) otomatik oluşturulur:

- Email: `admin@atlas.local`
- Şifre: `Admin123!`
- Rol: `Admin`

**Not:** Bu seed SADECE tablo tamamen boşken çalışır - tabloda zaten kullanıcı
varsa tekrar oluşturulmaz/güncellenmez. Gerekirse `POST /api/auth/register` ile
yeni bir kullanıcı kaydedip SSMS'ten `UPDATE auth.Users SET Role = 1 WHERE
Email = '...'` ile Admin yapabilirsin.

## Bir şey çalışmazsa

- `dotnet restore` hata verirse internet bağlantını kontrol et (NuGet paket kaynağına erişim gerekiyor).
- `dotnet run` başlarken `Jwt:Key` ile ilgili bir hata verirse, User Secrets adımını (3. adım) atlamış olabilirsin.
- `/health` endpoint'i `postgresql` veya `redis` için `Unhealthy` dönerse `docker ps` ile container'ların ayakta olduğunu kontrol et, `docker compose up -d` ile tekrar başlat.
- Port çakışması olursa `--urls http://localhost:XXXX` parametresiyle farklı bir port belirt (React tarafında `api.js`'teki `API_URL`'i de güncellemen gerekir).
- Visual Studio kullanıyorsan `Atlas.sln` dosyasını aç, `Atlas.Api` projesini "Startup Project" yap, F5.

## Testleri çalıştırmak için

```bash
dotnet test Atlas.sln
```

Bu, her modülün Domain/Application/Infrastructure katmanındaki unit testlerini
(veritabanı gerektirmez) ve `Atlas.IntegrationTests` projesini (gerçek HTTP
istekleriyle uçtan uca, EF Core InMemory provider kullanır - SQL Server'a
dokunmaz ama Docker'daki Postgres/Redis container'larının ayakta olmasını
bekler) çalıştırır.

## Bölüm 2 — DI Container ve Service Lifetime

`AddSingleton` / `AddScoped` / `AddTransient` arasındaki fark, gerçek bir bug'la öğrenildi:
bellek-içi bir repository (kendi verisini instance alanında tutan) `Scoped` yapılırsa, her
HTTP isteğinde veri sıfırlanır. Kural: repository bir **dış kaynağı** (DB bağlantısı gibi)
sarmalıyorsa `Scoped` doğrudur; repository **kendisi** veri deposuysa `Singleton` gerekir.
EF Core'a geçince tüm repository'ler gerçek anlamda `Scoped` oldu (DbContext dış kaynağı sarmalıyor).

## Bölüm 3 — CQRS ve MediatR

Her modülün kendi `Commands/` ve `Queries/` klasörleri var — `Command` = "ne isteniyor"
(veri taşıyıcı), `Handler` = "nasıl yapılıyor" (iş mantığı). `Shared.CQRS`'teki
`LoggingBehavior`, her modülün `AddXModule()` metodunda `cfg.AddOpenBehavior(...)` ile
kaydediliyor, tüm Command/Query'lerden otomatik geçiyor. Endpoint'ler `IMediator` kullanıyor,
Handler sınıflarını doğrudan tanımıyor.

## Bölüm 4 — Wiki modülü ve modüller arası iletişim

İkinci modül: `Atlas.Modules.Wiki.*`. En kritik nokta: Wiki, Auth'u **hiç tanımıyor** —
sadece `Shared.Contracts`'taki `ICurrentUserAccessor` interface'ini kullanıyor, gerçek
implementasyonu Auth.Infrastructure'da yaşıyor, DI container ikisini otomatik eşliyor.

Yetkilendirme kuralı `WikiVisibilityRules.IsVisibleTo()` metodunda, **Domain katmanında**
yaşıyor - Application sadece "görünür mü?" diye soruyor, kararı Domain veriyor.

## Bölüm 5 — EF Core ve gerçek veritabanı

Bellek-içi repository'ler kaldırıldı, yerine `EfUserRepository`/`EfWikiPageRepository` geldi.
Her modülün kendi `DbContext`'i (`AuthDbContext`, `WikiDbContext`), ayrı SQL şemaları
(`auth.*`, `wiki.*`), tek fiziksel veritabanı (`AtlasPlatform`). Migration'lar `dotnet ef
migrations add` ile oluşturuluyor, birden fazla `DbContext` varsa `--context` parametresiyle
hangisi kastedildiği belirtiliyor. Host, migration'ları `app.MigrateAuthDatabase()` /
`app.MigrateWikiDatabase()` ile uygulama açılışında otomatik çalıştırıyor — Host,
`AuthDbContext`'in var olduğunu bile bilmiyor.

## Bölüm 6 — JWT ile gerçek login

`FakeCurrentUserAccessor` kaldırıldı, yerine `HttpCurrentUserAccessor` geldi — artık
gerçekten `HttpContext.User` claim'lerinden (JWT token'dan) okuyor:

- `Pbkdf2PasswordHasher` — şifreler salt + 100.000 iterasyonla hash'leniyor
- `JwtTokenGenerator` — `POST /api/auth/login` başarılı olduğunda imzalı bir JWT üretiyor
- `.RequireAuthorization()` — korumalı endpoint'ler token zorunlu kılıyor

## Bölüm 7 — Veri bütünlüğü ve Wiki authorization

`wiki.WikiPages.CreatedByUserId` → `auth.Users.Id` foreign key'i, modüller kod
seviyesinde birbirini tanımadığı için EF Core'un otomatik API'siyle değil,
migration içinde **ham SQL** ile eklendi.

## Bölüm 8 — React frontend

`Web/apps/platform/` altında Vite + React ile kurulan bir arayüz: login formu,
wiki sayfası listesi/oluşturma formu. State yönetimi için ekstra kütüphane
kullanılmadı - `useState`/`useEffect` yeterli oldu.

## Bölüm 9 — Rol bazlı yetkilendirme

`User` entity'sine `Role` alanı (`Member`/`Admin` enum) eklendi, JWT token'a
`ClaimTypes.Role` claim'i olarak gömülüyor. `GET /api/auth/users` artık
`.RequireRole("Admin")` ile korunuyor.

## Bölüm 10 — Otomatik testler (Domain katmanı)

`tests/Atlas.Modules.Wiki.Domain.Tests`, `tests/Atlas.Modules.Auth.Domain.Tests`,
`tests/Atlas.Modules.AI.Domain.Tests` (xUnit) - Domain katmanındaki iş
kurallarının hiçbir veritabanı/HTTP bağımlılığı olmadan test edilebildiğinin kanıtı.

## Bölüm 11 — Redis cache

`Shared.Caching` projesi (`ICacheService`/`RedisCacheService`), `GetWikiPagesQuery`
sonuçlarını 30 saniyeliğine cache'liyor. Strateji: **ham veriyi (tüm sayfalar,
filtresiz) tek bir key'de cache'le**, departman filtresini ve sayfalamayı
her istekte bellekte uygula - böylece departman/sayfa kombinasyonu başına
ayrı bir cache key açılmıyor.

## Bölüm 12 — Notifications modülü (SignalR)

`Notifications` modülü, `NotificationsHub` (SignalR) ile gerçek zamanlı bildirim
gönderiyor. Wiki'de yeni sayfa eklenince `WikiPageCreatedEvent` (Shared.Contracts,
MediatR `INotification`) yayınlanıyor, `WikiPageCreatedEventHandler`
(Notifications.Infrastructure) bunu dinleyip SignalR üzerinden bağlı React
istemcilerine "WikiPageCreated" mesajı gönderiyor. Wiki, Notifications'ın var
olduğunu hiç bilmiyor - sadece paylaşılan event'i yayınlıyor.

## Bölüm 13 — AI modülü (iskelet)

`WikiPageEmbedding` entity (AI.Domain) - `WikiPageId`, `Embedding`
(`Pgvector.Vector`, boyut 1024 - Voyage AI'a uygun), `CreatedAtUtc`.
`AiDbContext` PostgreSQL'e `Npgsql.EntityFrameworkCore.PostgreSQL` +
`Pgvector.EntityFrameworkCore` ile bağlanıyor, migration "vector" extension'ını
otomatik `CREATE EXTENSION` ediyor. Henüz bir Command/Query yok - embedding
üretimi ve LLM entegrasyonu API key'ler gelince yapılacak.

## Bölüm 14 — Monorepo restructure ve shadcn/ui

`web/` → `Web/apps/platform` + `Web/packages/ui` (npm workspaces). shadcn/ui
(Tailwind v4 + Base UI preset) entegre edildi - sitenin orijinal mor/koyu
tasarımı korunacak şekilde. Öğrenilen ders: shadcn kendi `--accent` token'ını
sitenin ORİJİNAL mor `--accent` değişkeniyle aynı isimde `:root`'a ekledi -
sitenin değişkeni `--brand-accent` olarak yeniden adlandırılıp ayrıldı.

## Bölüm 15 — JWT key rotasyonu + Refresh token

Eski `Jwt:Key` public GitHub'a push edilmişti (sızmış sayılır) - User Secrets'a
taşındı, appsettings.json'dan silindi. Access token ömrü 8 saatten **15 dakikaya**
düşürüldü; uzun oturum artık `RefreshToken` (Auth.Domain, `auth.RefreshTokens`)
ile sağlanıyor: `POST /api/auth/login` `{accessToken, refreshToken}` döndürür,
`POST /api/auth/refresh` "rotation" deseniyle (her kullanımda eski token iptal
edilir) yeni bir çift verir. React tarafında `api.js`, 401 alınca otomatik
refresh deneyip isteği bir kez daha tekrarlıyor.

## Bölüm 16 — GÜVENLİK: Wiki GET departman filtresi düzeltmesi

`GET /api/wiki/pages`'teki departman filtresi eskiden istemcinin `?department=X`
query string'iyle gönderdiği bir değere güveniyordu - giriş yapmış herhangi bir
kullanıcı, departman adını tahmin ederek başka departmanların DepartmentOnly
sayfalarını okuyabiliyordu (canlı olarak doğrulandı ve düzeltildi). `User`
entity'sine gerçek bir `Department` alanı eklendi, JWT'ye imzalı bir `department`
claim'i gömülüyor, departman artık tamamen `ICurrentUserAccessor.Department`
(token'dan) ile belirleniyor - istemciden hiçbir parametre alınmıyor.

## Bölüm 17 — Swagger/OpenAPI

`/swagger` - Auth ve Wiki endpoint'lerini (Notifications hariç, SignalR Hub
olduğu için) belgeliyor. JWT Bearer authorize desteği var - Swagger UI'daki
"Authorize" butonuna token yapıştırınca tüm isteklere otomatik ekleniyor.

## Bölüm 18 — Integration testler

`tests/Atlas.IntegrationTests`, `WebApplicationFactory<Program>` ile gerçek
Program.cs'i (tüm modülleriyle) ayağa kaldırıyor. `AuthDbContext`/`WikiDbContext`
EF Core InMemory provider'a, `ICacheService` gerçek Redis yerine bir no-op
fake'e çevriliyor - testler izole ve deterministik. Kapsanan senaryolar:
register+login akışı, yetkisiz erişim reddi, wiki sayfası oluşturma+listeleme.

## Bölüm 19 — Sayfalama

`GetWikiPagesQuery`'ye `pageNumber`/`pageSize` eklendi. Cache stratejisi
değişmedi (Bölüm 11) - sayfalama, departman filtrelemesinden sonra bellekte
uygulanıyor. React tarafında "← Önceki"/"Sonraki →" butonları eklendi.

## Bölüm 20 — AI Semantik Arama

`TextChunker` (sabit boyutlu, üst üste binen pencerelerle metni parçalıyor) +
`FakeEmbeddingService` (kelimeleri MD5 ile hash'leyip deterministik bir vektör
üreten, gerçek bir sağlayıcıya kadar köprü görevi gören sahte embedding
servisi). Wiki sayfası oluşunca otomatik chunk'lanıp embed ediliyor,
`GET /api/ai/search?q=...` doğal dil sorgusunu embed edip pgvector'ın cosine
distance sıralamasıyla en anlamlı sonuçları döndürüyor - departman görünürlük
kuralı arama sonucuna da uygulanıyor. `IEmbeddingService` bilerek bir arayüz -
gerçek bir sağlayıcıya (Voyage AI, OpenAI vb.) geçiş sadece DI kaydını
değiştirmekle olacak şekilde tasarlandı.

## Bölüm 21 — Transactional Outbox Pattern

MediatR'ın in-process `IPublisher`'ı crash-safe değildi: bir sayfa
kaydedildikten hemen sonra process çökerse, o sayfanın embedding'i asla
üretilmezdi. `OutboxMessage` entity'si, sayfanın KENDİSİYLE aynı
`SaveChanges` çağrısında (atomik) yazılıyor; arka planda 5 saniyede bir
çalışan `OutboxProcessor` (`BackgroundService`) işlenmemiş mesajları okuyup
gerçek `Publish`'i tetikliyor. 5 başarısız denemeden sonra mesaj
dead-letter'a düşüyor - silinmiyor, `Attempts`/`LastError` ile görünür
kalmaya devam ediyor.

## Bölüm 22 — Observability + CI/CD

Serilog, varsayılan loglamanın yerini aldı. `CorrelationIdMiddleware`, her
isteğe bir `X-Correlation-Id` kazandırıp Serilog'un "ambient" bağlamına
ekliyor - o istek sırasında oluşan HER log satırı (EF Core sorguları, CQRS
logları, exception handler) aynı ID'yi otomatik taşıyor. GitHub Actions
(`.github/workflows/ci.yml`) her push/PR'da backend'i build+test ediyor
(gerçek bir veritabanı gerektiren Integration testleri hariç) ve frontend'i
build+lint ediyor.

## Bölüm 23 — Audit Log

`AuditBehavior` - generic bir MediatR pipeline behavior, sadece
`IAuditableCommand` işaretli komutlar için devreye giriyor (Wiki sayfası
oluşturma/silme gibi denetlenmesi gereken eylemler). `GET /api/audit-log`
(Admin) - eylem/tarih aralığı filtresi + sayfalama ile kim ne zaman ne
yaptığını gösteriyor.

## Bölüm 24 — Şifre Kasası (Vault)

Kurum içi parola/erişim bilgilerini tutan, Wiki'den tamamen bağımsız bir
modül. Şifreleme ASP.NET Core'un kendi Data Protection API'siyle (yeni bir
kütüphane eklenmedi). Owner-or-Admin yetkilendirme - normal kullanıcı sadece
kendi kayıtlarını görür. "Reveal" bilerek bir Command (Query değil) - kimin
ne zaman hangi parolayı gördüğü denetim izinde kalsın diye.

## Bölüm 25 — Docker Compose tam paketleme + Rate limiting

`docker compose up --build` artık SQL Server dahil HER ŞEYİ tek komutla
ayağa kaldırıyor - önceden SQL Server Express native kurulu olmalıydı.
Ayrıca IP/kullanıcı bazlı rate limiting eklendi: login (IP, dakikada 5),
AI arama (kullanıcı, dakikada 20), e-posta doğrulama kodu (IP, dakikada 10),
Vault reveal (kullanıcı, dakikada 10).

## Bölüm 26 — Documents modülü: belge kütüphanesi + AI/RAG entegrasyonu

Projenin en büyük tek parçası. PDF/DOCX/PPTX/XLSX/TXT gibi dosyalar
yükleniyor, arka planda (Transactional Outbox Pattern üzerinden) metinleri
çıkarılıyor (Docnet.Core, OpenXML SDK), parçalanıp AI'ın embedding
pipeline'ına bağlanıyor - `GET /api/ai/search` artık wiki sayfalarını VE
yüklenen belgeleri TEK bir birleşik sonuç listesinde döndürüyor. Ayrıca:
belge versiyonlama (eski dosyalar arşivde kalıp indirilebilir kalıyor),
çoklu dosya yükleme, aynı içerikli bir dosya tekrar yüklenirse (engellemeyen,
sadece bilgilendiren) bir uyarı.

## Bölüm 27 — Güvenlik sertleştirme

Vault'un "reveal" uç noktasına rate limit (Bölüm 25'e ek), `IMalwareScanner`
arayüzü (bugün no-op implementasyonlu, gerçek bir tarama motoruna DI-swap
ile bağlanmaya hazır - `IEmbeddingService`'le aynı felsefe), yüklenen
belgelerin Docker volume'de kalıcı olması (Bölüm 25'te eklenen Docker
Compose paketlemesinin bir eksiğiydi).

## Sırada ne var?

1. Gerçek bir embedding/LLM sağlayıcısına geçiş (Voyage AI, OpenAI vb.) -
   API key'ler gelince; `IEmbeddingService`'in DI kaydını değiştirmek
   yeterli olacak şekilde tasarlandı (bkz. Bölüm 20), bu yüzden şu an API
   key'ler gelene kadar bloklanmış durumda.

Bundan öncesi için (Documents modülünün tam gün-gün kırılımı, canlı
bulunan/düzeltilen gerçek bug'lar, mimari kararların gerekçeleri) proje
kökündeki `CLAUDE.md` dosyasına bakılabilir - bu README'nin tersine, o
dosya her oturumda güncelleniyor ve projenin tek eksiksiz kaynağı.
