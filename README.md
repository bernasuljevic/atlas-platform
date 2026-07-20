# Atlas Platform — Öğrenme Notları

Bu, modüler monolith mimarisiyle sıfırdan kurulan bir .NET öğrenme projesi.
**Auth** ve **Wiki** modülleri var, gerçek bir SQL Server veritabanı kullanıyor
(EF Core + migration'lar), ve JWT tabanlı gerçek bir login sistemi çalışıyor.

## Çalıştırmak için

Bilgisayarında [.NET 10 SDK](https://dotnet.microsoft.com/download) kurulu olmalı — projeler `net10.0` hedefliyor.
Kurulu mu diye kontrol et:

```bash
dotnet --version
```

`10.x` gibi bir şey görmelisin. Farklı bir major sürüm görürsen (örn. `8.x`), her `.csproj` dosyasındaki
`<TargetFramework>` satırını kendi kurulu sürümünle eşleştirmen gerekir — aksi halde derleme
başarılı olsa bile `dotnet run` "You must install or update .NET" hatası verir (bu ekipteki
en sık karşılaşılan hatalardan biri, SDK ile Runtime versiyonu karışıklığından kaynaklanıyor).

Ayrıca bilgisayarında SQL Server Express kurulu olmalı (`.\SQLEXPRESS` instance adıyla,
Windows Authentication). `src/Host/Atlas.Api/appsettings.json` içindeki `ConnectionStrings`
ve `Jwt` bloklarını kontrol et — ikisi de dolu olmalı, boşsa uygulama açılırken hata verir.

Sonra proje klasörüne gir ve:

```bash
cd atlas-platform
dotnet restore              # tüm projelerin bağımlılıklarını indirir
dotnet build                # her şeyin derlendiğini doğrular
dotnet run --project src/Host/Atlas.Api
```

Ya da geliştirme sırasında (kod kaydettikçe otomatik yeniden başlatma için):
```bash
dotnet watch --project src/Host/Atlas.Api
```

Terminalde şuna benzer bir çıktı göreceksin:

Now listening on: http://localhost:5xxx

O adresi tarayıcıda aç, şunları dene:

- `http://localhost:5xxx/` → sağlık kontrolü

- `http://localhost:5xxx/api/auth/users` → GET, **sadece Admin rolü** (`.RequireRole("Admin")`)
- `http://localhost:5xxx/api/auth/register` → POST, body: `{"email":"...","fullName":"...","password":"..."}`
- `http://localhost:5xxx/api/auth/login` → POST, body: `{"email":"...","password":"..."}` → `{"token":"..."}` ya da 401
- `http://localhost:5xxx/api/wiki/pages` → GET (herkese açık), POST (token gerektirir)

İlk kurulumda otomatik oluşan admin: `admin@atlas.local` / `Admin123!` (Admin rolüyle).
Not: Bu sadece veritabanı ilk kez boşken çalışır - eğer tabloda zaten kullanıcı
varsa admin tekrar oluşturulmaz. Gerekirse `POST /api/auth/register` ile yeni
bir kullanıcı kaydedip SSMS'ten `UPDATE auth.Users SET Role = 1 WHERE Email = '...'`
ile Admin yapabilirsin.

## Web arayüzü (React)

`web/` klasöründe basit bir React uygulaması var — login, wiki listesi/oluşturma,
departman filtresi. Çalıştırmak için:

```bash
cd web
npm install    # ilk seferde
npm run dev
```

`http://localhost:5173` adresinde açılır. **.NET API'nin (5080 portu) ayrıca ayakta
olması gerekir** — React sadece arayüz, tüm veriyi API'den çekiyor. CORS, backend'de
sadece `http://localhost:5173`'e izin verecek şekilde ayarlı (`Program.cs`).

## Bir şey çalışmazsa

- `dotnet restore` hata verirse internet bağlantını kontrol et (NuGet paket kaynağına erişim gerekiyor).
- `dotnet run` başlarken JWT ile ilgili bir `ArgumentNullException` verirse, `appsettings.json`'daki
  `Jwt:Key`/`Issuer`/`Audience` alanlarının dolu olduğunu kontrol et.
- Port çakışması olursa `--urls http://localhost:XXXX` parametresiyle farklı bir port belirt.
- Visual Studio kullanıyorsan `Atlas.sln` dosyasını aç, `Atlas.Api` projesini "Startup Project" yap, F5.

## Klasör yapısı neden böyle?

src/Shared/ → tüm modüllerin ortak kullandığı temel yapılar
Atlas.Shared.Kernel → Entity taban sınıfı
Atlas.Shared.Contracts → modüller arası interface'ler (örn. ICurrentUserAccessor)
Atlas.Shared.CQRS → MediatR pipeline behavior'ları (LoggingBehavior)
src/Modules/Auth/ ve src/Modules/Wiki/
*.Domain → saf iş kuralları, framework yok
*.Application → akış/orkestrasyon, "nasıl saklanır"ı bilmez
*.Infrastructure → gerçek saklama detayı (EF Core, JWT, hash'leme)
*.Api → modülün dışa açılan kapısı + DI kayıt mekanizması
src/Host/Atlas.Api → composition root, tüm modülleri birleştiren tek nokta

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

Yetkilendirme kuralı `WikiPage.IsVisibleTo()` metodunda, **Domain katmanında** yaşıyor -
Application sadece "görünür mü?" diye soruyor, kararı Domain veriyor.

## Bölüm 5 — EF Core ve gerçek veritabanı

Bellek-içi repository'ler (`InMemoryUserRepository`, `InMemoryWikiPageRepository`) kaldırıldı,
yerine `EfUserRepository`/`EfWikiPageRepository` geldi. Her modülün kendi `DbContext`'i
(`AuthDbContext`, `WikiDbContext`), ayrı SQL şemaları (`auth.*`, `wiki.*`), tek fiziksel
veritabanı (`AtlasPlatform`). Migration'lar `dotnet ef migrations add` ile oluşturuluyor,
birden fazla `DbContext` varsa `--context` parametresiyle hangisi kastedildiği belirtiliyor.
Host, migration'ları `app.MigrateAuthDatabase()` / `app.MigrateWikiDatabase()` ile uygulama
açılışında otomatik çalıştırıyor — Host, `AuthDbContext`'in var olduğunu bile bilmiyor.

Kalıcılık test edildi: uygulama tamamen yeniden başlatıldı, eklenen veri kaybolmadı
(bellek-içi versiyonda bu imkansızdı).

## Bölüm 6 — JWT ile gerçek login

`FakeCurrentUserAccessor` (sistemdeki ilk kullanıcıyı sabit "giriş yapmış" sayan geçici
köprü) kaldırıldı, yerine `HttpCurrentUserAccessor` geldi — artık gerçekten `HttpContext.User`
claim'lerinden (JWT token'dan) okuyor. Eklenen parçalar:

- `Pbkdf2PasswordHasher` — şifreler artık salt + 100.000 iterasyonla gerçekten hash'leniyor
- `JwtTokenGenerator` — `POST /api/auth/login` başarılı olduğunda imzalı bir JWT üretiyor
- `.RequireAuthorization()` — `GET /api/auth/users` artık token zorunlu kılıyor, diğer
  endpoint'ler (Wiki dahil) şimdilik açık (bilinçli bir tercih, ileride gözden geçirilebilir)

`appsettings.json`'a `Jwt:Key`/`Issuer`/`Audience` eklenmeden uygulama **her** endpoint'te
hata veriyordu (`UseAuthentication()` middleware'i her istekte token'ı doğrulamaya çalışıyor) -
ama `GlobalExceptionHandler` sayesinde çökme değil, düzgün 400 JSON yanıtı olarak.

## Bölüm 7 — Veri bütünlüğü ve Wiki authorization

`wiki.WikiPages.CreatedByUserId` artık `auth.Users.Id`'ye gerçek bir **foreign key**
ile bağlı. İki modül kod seviyesinde birbirini tanımadığı için (Wiki.Domain, Auth.Domain'e
referans vermiyor) bu kısıtlama EF Core'un otomatik API'siyle değil, migration içinde
**ham SQL** (`migrationBuilder.Sql(...)`) ile eklendi — "modüller kod seviyesinde ayrık,
veritabanı seviyesinde tutarlı" ilkesinin somut örneği.

`POST /api/wiki/pages` artık `.RequireAuthorization()` ile korunuyor (token zorunlu),
`GET /api/wiki/pages` bilinçli olarak açık kaldı — herkes public sayfaları görebilmeli.

## Bölüm 8 — React frontend

`web/` klasöründe Vite + React ile kurulan basit bir arayüz: login formu, wiki sayfası
listesi/oluşturma formu, departman filtresi. Backend'e CORS ile bağlanıyor
(`http://localhost:5173` özel olarak izinli). State yönetimi için ekstra kütüphane
kullanılmadı - `useState`/`useEffect` yeterli oldu, uygulamanın küçük olması sayesinde.

## Bölüm 9 — Rol bazlı yetkilendirme

`User` entity'sine `Role` alanı (`Member`/`Admin` enum) eklendi, JWT token'a
`ClaimTypes.Role` claim'i olarak gömülüyor. `GET /api/auth/users` artık
`.RequireRole("Admin")` ile korunuyor - Member rolündeki bir kullanıcı geçerli
token'la bile `403 Forbidden` alıyor (401'den farkı: kimliği biliniyor, sadece yetkisi yok).

## Bölüm 10 — Otomatik testler

İlk test projesi: `tests/Atlas.Modules.Wiki.Domain.Tests` (xUnit). `WikiPage.IsVisibleTo()`
için 7 test yazıldı - Domain katmanındaki iş kurallarının hiçbir veritabanı/HTTP
bağımlılığı olmadan, saniyenin altında test edilebildiğinin kanıtı. Çalıştırmak için:

```bash
dotnet test tests/Atlas.Modules.Wiki.Domain.Tests/Atlas.Modules.Wiki.Domain.Tests.csproj
```

## Sırada ne var?

1. Auth.Domain için de test projesi (`User.Create()` validasyonları)
2. Wiki endpoint'lerinde `GET`'in de departman bazlı ekstra korumaları düşünülebilir
3. Refresh token mekanizması (şu an token 8 saatte bir yeniden login gerektiriyor)
4. AI modülü (SubMed dokümanındaki orijinal fikir - RAG, embedding, LLM entegrasyonu) -
   büyük kapsamı nedeniyle ayrı bir haftaya bırakıldı
5. React tarafında: gerçek routing (React Router), daha kapsamlı stil