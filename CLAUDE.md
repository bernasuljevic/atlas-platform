# Atlas Platform — Proje Hafızası

## Sırada ne var (kullanıcıyla konuşulan roadmap)

1. YARIM KALAN İŞ - JWT login: appsettings.json'a Jwt:Key/Issuer/Audience eklenmedi.
   Eklenmeden uygulama BAŞLARKEN ÇÖKER (AddJwtBearer null key ile patlar).
   İlk iş bu olmalı. Key en az 32 karakter olmalı (HMAC-SHA256 için).
2. Jwt config eklendikten sonra: mevcut admin kaydının hash'i eski placeholder
   formatında (Pbkdf2Verify tanımaz) - SSMS'ten admin.Users satırını silip
   uygulamayı yeniden başlatarak yeniden seed ettir (Admin123! şifresiyle).
3. POST /api/auth/login test et, dönen token ile POST /api/wiki/pages'i
   Authorization: Bearer <token> header'ıyla dene - HttpCurrentUserAccessor'ın
   gerçekten çalıştığını doğrula (artık admin'i sabit değil, token'daki kullanıcıyı görmeli).
4. Hassas endpoint'lere .RequireAuthorization() eklemeyi düşün (şu an her şey
   authentication olmadan da erişilebilir, middleware kayıtlı ama zorunlu değil).
5. dotnet watch ile hot-reload akışı
6. README.md güncellemesi bekliyor (Bölüm 4'te kalmış, EF Core + JWT notları eksik)

## Bu projenin amacı

Bu, kullanıcının .NET / modüler monolith mimarisini **öğrenerek** inşa ettiği bir
öğrenme projesi. Kurumsal wiki + AI fikrine dayanıyor (bkz. orijinal ilham:
SubMed Platform mimarisi - departman bazlı wiki + AI katmanı).

## ÇOK ÖNEMLİ — Nasıl çalışmalısın

Kullanıcı bu projeyi Claude (claude.ai) ile sıfırdan, adım adım, **öğreterek**
inşa etti. Şimdi Claude Code'a geçiyor çünkü dosyalara doğrudan erişimin var
ve kopyala-yapıştır döngüsü yorucuydu. AMA öğrenme hedefi hâlâ geçerli:

- Kod yazarken **neden** o kararı verdiğini kısaca açıkla (yorum olarak değil,
  konuşarak). Sessizce büyük değişiklikler yapıp "bitti" deme.
- Yeni bir kavram (örsn. bir NuGet paketi, bir tasarım deseni) tanıtırken
  1-2 cümleyle ne işe yaradığını anlat.
- Kullanıcı "neden böyle" diye sorarsa, kod göstermeden önce kavramı anlat.
- Küçük hatalarda (typo, eksik paket) kullanıcıya sormadan düzelt - ama
  mimari bir karar değiştiriyorsan (örn. bir katmanın sorumluluğunu
  değiştirmek) önce sor.
- Kullanıcı zaman zaman "bunu neden yaptık" diye geçmişe referans verebilir -
  aşağıdaki "Öğrenilen dersler" bölümü tam bunun için var.

## Mimari - Modüler Monolith

```
src/Shared/           → Kernel (Entity base), Contracts (modüller arası interface'ler), CQRS (MediatR pipeline behaviors)
src/Modules/Auth/      → Domain → Application → Infrastructure → Api (4 katman)
src/Modules/Wiki/      → aynı 4 katman deseni
src/Host/Atlas.Api/    → composition root, sadece her modülün *.Api projesine referans verir
```

**Katman kuralı:** Domain framework'ten habersiz. Application "nasıl saklanır"ı
bilmez, sadece interface (Abstractions/I*.cs) kullanır. Infrastructure gerçek
implementasyonu yazar. Api, modülün DI kayıt mekanizmasını (`AddXModule()`)
ve MediatR endpoint'lerini barındırır.

**Modüller arası iletişim kuralı:** Modüller birbirinin Domain/Application/
Infrastructure'ına ASLA referans vermez. Sadece `Shared.Contracts`'taki
interface'ler üzerinden konuşurlar (örn. Wiki, Auth'u tanımaz, sadece
`ICurrentUserAccessor`'ı tanır - implementasyonu Auth.Infrastructure sağlar,
DI container'da otomatik eşleşir).

**CQRS/MediatR:** Her modülün kendi Command/Query/Handler'ları var
(`Users/Commands`, `Users/Queries` gibi). `Shared.CQRS`'teki `LoggingBehavior`,
her modülün `AddXModule()` metodunda `cfg.AddOpenBehavior(...)` ile
kaydediliyor, tüm Command/Query'lerden otomatik geçiyor.

## Öğrenilen dersler (bunları tekrar sorgulama, zaten test edildi)

1. **Service Lifetime konusunda kritik bir kural:** Repository bir DIŞ
   KAYNAĞI (DB bağlantısı) sarmalıyorsa → `Scoped`. Repository KENDİSİ veri
   deposuysa (örn. bizim `InMemoryUserRepository`, `InMemoryWikiPageRepository`
   - içlerinde `ConcurrentDictionary`/`ConcurrentBag` instance alanı var) →
   `Singleton` olmalı, yoksa her HTTP isteğinde veri sıfırlanır (bunu canlı
   bug olarak yaşadık, ID'ler her istekte değişiyordu). Bu ikisi şu an
   Singleton. EF Core'a geçilince tekrar Scoped olacaklar.
2. **`ICurrentUserAccessor` şu an `FakeCurrentUserAccessor`** (Auth.Infrastructure)
   ile karşılanıyor - InMemoryUserRepository'deki ilk kullanıcıyı (admin)
   "giriş yapmış" sayıyor. Gerçek JWT gelince bu sınıf değişecek, Wiki hiç
   etkilenmeyecek.
3. `.NET 10` kullanılıyor (kullanıcının makinesinde bu kurulu, net8.0 değil).
   Yeni proje eklerken TargetFramework'ü net10.0 yap.
4. Her yeni proje `Atlas.sln`'e eklenmeli, yoksa Visual Studio/CLI görmüyor.

## Şu ana kadar tamamlananlar

- [x] Auth modülü: User entity, RegisterUserCommand, GetAllUsersQuery, MediatR+LoggingBehavior
- [x] Wiki modülü: WikiPage entity (Title/Content/DepartmentName/Visibility/CreatedByUserId),
      `IsVisibleTo()` yetkilendirme kuralı Domain'de, CreateWikiPageCommand, GetWikiPagesQuery
- [ ] Wiki modülü HENÜZ TEST EDİLMEDİ - kullanıcı Claude Code'a geçtiği anda
      test yapıyordu. İlk iş: `dotnet build` çalıştır, hataları düzelt,
      sonra `/api/wiki/pages` GET/POST endpoint'lerini test et.

## Sırada ne var (kullanıcıyla konuşulan roadmap)

1. Wiki modülünü test edip çalıştığından emin ol (yarım kalan iş)
2. EF Core ile gerçek veritabanına geçiş (Singleton hack'lerini kalıcı çöz)
3. JWT ile gerçek login (`FakeCurrentUserAccessor`'ı değiştir)
4. `dotnet watch` ile hot-reload akışı

## Endpoint referansı

- `GET /api/auth/users`, `POST /api/auth/register` (body: email, fullName, password)
- `GET /api/wiki/pages?department=X`, `POST /api/wiki/pages` (body: title, content, departmentName, visibility: "Public"|"DepartmentOnly")

Detaylı notlar için `README.md`'ye bak (her oturum sonunda güncellendi).
