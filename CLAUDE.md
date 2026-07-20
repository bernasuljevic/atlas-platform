# Atlas Platform — Proje Hafızası

## Bu projenin amacı

Kullanıcının .NET / modüler monolith mimarisini **öğrenerek** inşa ettiği bir
öğrenme + staj defteri projesi. Kurumsal wiki + AI fikrine dayanıyor (orijinal
ilham: SubMed Platform mimarisi - departman bazlı wiki + AI katmanı). AI modülü
büyük kapsamı nedeniyle bilinçli olarak ayrı bir haftaya bırakıldı.

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

src/Shared/ → Kernel (Entity base), Contracts (modüller arası interface'ler), CQRS (MediatR pipeline behaviors)
src/Modules/Auth/ → Domain → Application → Infrastructure → Api (4 katman)
src/Modules/Wiki/ → aynı 4 katman deseni
src/Host/Atlas.Api/ → composition root, sadece her modülün *.Api projesine referans verir
tests/ → xUnit test projeleri (şu an: Atlas.Modules.Wiki.Domain.Tests)
web/ → React (Vite) frontend, ayrı bir npm projesi


**Katman kuralı:** Domain framework'ten habersiz. Application "nasıl saklanır"ı
bilmez, sadece interface (Abstractions/I*.cs) kullanır. Infrastructure gerçek
implementasyonu yazar (EF Core, JWT, hash'leme). Api, modülün DI kayıt
mekanizmasını (`AddXModule()`) ve MediatR endpoint'lerini barındırır.

**Modüller arası iletişim kuralı:** Modüller birbirinin Domain/Application/
Infrastructure'ına ASLA referans vermez. Sadece `Shared.Contracts`'taki
interface'ler üzerinden konuşurlar (Wiki, Auth'u tanımaz, sadece
`ICurrentUserAccessor`'ı tanır). **İstisna:** Veritabanı seviyesinde foreign key
gerekiyorsa (örn. `WikiPage.CreatedByUserId` → `User.Id`), bu EF Core'un C#
API'siyle değil, migration içinde **ham SQL** ile eklenir - kod seviyesindeki
ayrım korunur, veritabanı seviyesinde tutarlılık sağlanır.

**Veritabanı:** SQL Server Express (`.\SQLEXPRESS`, Windows Authentication),
her modülün kendi `DbContext`'i, ayrı şemalar (`auth.*`, `wiki.*`), tek fiziksel
veritabanı (`AtlasPlatform`). appsettings.json → `ConnectionStrings:DefaultConnection`
ve `Jwt:Key`/`Issuer`/`Audience` - ikisi de dolu olmadan uygulama her endpoint'te hata verir.

**Kimlik doğrulama:** JWT tabanlı. `Pbkdf2PasswordHasher` (salt + 100k iterasyon)
şifreleri hash'liyor. Token claim'leri: NameIdentifier, Email, Name, Role.
`User.Role` (Member/Admin enum) → `.RequireRole("Admin")` ile korunan endpoint'ler var.

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
- [x] React frontend (web/): login, wiki listesi/oluşturma, departman filtresi,
      görünürlük seçimi, yükleme durumları, çıkış yap
- [x] İlk test projesi: Atlas.Modules.Wiki.Domain.Tests (xUnit), 7 test,
      WikiPage.IsVisibleTo() için

## Sırada ne var

1. Auth.Domain için de test projesi (User.Create() validasyonları)
2. Refresh token mekanizması (şu an token 8 saatte bir yeniden login gerektiriyor)
3. AI modülü (SubMed dokümanındaki orijinal fikir) - ayrı bir haftaya bırakıldı
4. React: routing (React Router), daha kapsamlı stil
5. Wiki GET endpoint'i için departman bazlı ekstra koruma düşünülebilir

## Endpoint referansı

- `POST /api/auth/register` (email, fullName, password) → açık
- `POST /api/auth/login` (email, password) → açık, döner: `{token}` ya da 401
- `GET /api/auth/users` → sadece Admin rolü
- `GET /api/wiki/pages?department=X` → açık
- `POST /api/wiki/pages` (title, content, departmentName, visibility: Public|DepartmentOnly) → token gerektirir

İlk kurulumda otomatik oluşan admin: `admin@atlas.local` / `Admin123!` (Admin rolüyle,
SADECE tablo ilk kez boşken - tablo doluysa tekrar oluşturulmaz).

Detaylı notlar için `README.md`'ye bak (Bölüm 10'a kadar güncel).