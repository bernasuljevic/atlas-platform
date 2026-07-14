# Atlas Platform — Öğrenme Notları (Bölüm 1)

Bu, modüler monolith mimarisiyle sıfırdan kurulan bir .NET projesinin ilk iskeleti.
Şu an sadece **Auth modülü** var ve gerçek bir veritabanı yok (bellekte veri tutuyor).

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

Sonra proje klasörüne gir ve:

```bash
cd atlas-platform
dotnet restore              # tüm projelerin bağımlılıklarını indirir
dotnet build                # her şeyin derlendiğini doğrular
dotnet run --project src/Host/Atlas.Api
```

Terminalde şuna benzer bir çıktı göreceksin:

```
Now listening on: http://localhost:5xxx
```

O adresi tarayıcıda aç, şunları dene:

- `http://localhost:5xxx/` → sağlık kontrolü (API ayakta mı?)
- `http://localhost:5xxx/api/auth/users` → GET, MediatR üzerinden çalışan kullanıcı listesi
- `http://localhost:5xxx/api/auth/register` → POST, body: `{"email":"...","fullName":"...","password":"..."}`

İkinci adresi açtığında `admin@atlas.local` kullanıcısını JSON olarak görürsen,
**tüm katmanlar (Domain → Application → Infrastructure → Api → Host) doğru bağlanmış** demektir.

## Bir şey çalışmazsa

- `dotnet restore` hata verirse internet bağlantını kontrol et (NuGet paket kaynağına erişim gerekiyor).
- Port çakışması olursa `launchSettings.json` yok şu an, .NET otomatik boş bir port seçecek — terminaldeki
  "Now listening on" satırındaki adresi kullan.
- Visual Studio kullanıyorsan `Atlas.sln` dosyasını aç, `Atlas.Api` projesini "Startup Project" yap, F5.

## Klasör yapısı neden böyle?

Sohbet içinde adım adım anlattım, kısaca:

```
src/Shared/                          → tüm modüllerin ortak kullandığı temel yapılar
src/Modules/Auth/
    Atlas.Modules.Auth.Domain        → saf iş kuralları, framework yok
    Atlas.Modules.Auth.Application   → akış/orkestrasyon, "nasıl saklanır"ı bilmez
    Atlas.Modules.Auth.Infrastructure→ gerçek saklama detayı (şimdilik bellek)
    Atlas.Modules.Auth.Api           → modülün dışa açılan kapısı + DI kayıt mekanizması
src/Host/Atlas.Api                   → composition root, tüm modülleri birleştiren tek nokta
```

## Bölüm 2 notları — DI Container ve Service Lifetime (bugün öğrendiklerimiz)

`AuthModule.cs` içindeki şu satır kritik:

```csharp
services.AddScoped<IUserRepository, InMemoryUserRepository>();
```

`AddScoped` **doğru seçim** — her HTTP isteği kendi repository nesnesini alır, istekler
birbirine karışmaz. `AddSingleton` yapılırsa (deneyerek gördük) tüm istekler AYNI nesneyi
paylaşır — gerçek bir veritabanı bağlantısı olsaydı bu tehlikeli olurdu (race condition).

Üç seçenek özet:
- `AddSingleton` → uygulama boyunca 1 nesne, herkes paylaşır
- `AddScoped` → her HTTP isteği için 1 nesne (repository'ler için doğru tercih)
- `AddTransient` → her istendiğinde yeni nesne

## Bölüm 3 notları — CQRS ve MediatR (bugün eklenenler)

Eski `UserQueries` sınıfı kaldırıldı, yerine gerçek CQRS parçaları geldi:

```
Users/
  UserDto.cs                        → dışarıya dönecek veri şekli
  Queries/GetAllUsersQuery.cs       → "ne isteniyor" (veri taşıyıcı)
  Queries/GetAllUsersQueryHandler.cs→ "nasıl yapılıyor" (iş mantığı)
  Commands/RegisterUserCommand.cs
  Commands/RegisterUserCommandHandler.cs
```

Yeni proje: `Atlas.Shared.CQRS` — `LoggingBehavior` burada yaşıyor, MediatR pipeline'ının
her Command/Query'den otomatik geçen ortak parçası. `AuthModule.cs`'te tek satırla
(`cfg.AddOpenBehavior(...)`) bağlandı, hiçbir Handler'a elle loglama eklemedik.

Endpoint'ler artık `IMediator` kullanıyor, doğrudan Handler sınıflarını tanımıyor.

## Bölüm 4 notları — Wiki modülü ve modüller arası iletişim (bugün eklenenler)

İkinci modül eklendi: `Atlas.Modules.Wiki.*` (Domain/Application/Infrastructure/Api).
En kritik nokta: Wiki, Auth'u **hiç tanımıyor**. Sadece `Shared.Contracts`'taki
`ICurrentUserAccessor` interface'ini kullanıyor - gerçek implementasyonu
(`FakeCurrentUserAccessor`) Auth.Infrastructure'da yaşıyor, DI container ikisini
otomatik eşliyor.

Yeni endpoint'ler:
- `GET /api/wiki/pages?department=Engineering` → Public sayfalar + o departmana ait DepartmentOnly sayfalar
- `POST /api/wiki/pages` → body: `{"title":"...","content":"...","departmentName":"Engineering","visibility":"Public"}` (visibility: `Public` veya `DepartmentOnly`)

Yetkilendirme kuralı `WikiPage.IsVisibleTo()` metodunda, Domain katmanında yaşıyor -
Application sadece "görünür mü?" diye soruyor, kararı Domain veriyor.

`FakeCurrentUserAccessor`, gerçek JWT login gelene kadar geçici bir köprü -
şu an "giriş yapmış kullanıcı" olarak sistemdeki ilk kullanıcıyı (admin) kullanıyor.

## Sırada ne var?

Bir sonraki oturumda:
1. MediatR ile gerçek CQRS (Command/Query + Handler ayrımı)
2. `dotnet watch` ile hot-reload geliştirme akışı
3. İkinci modül: Wiki (departman bazlı içerik + yetkilendirme)
4. Auth modülüne gerçek JWT tabanlı login endpoint'i
