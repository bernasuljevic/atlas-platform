using Atlas.IntegrationTests.Fakes;
using Atlas.Modules.Auth.Infrastructure.Persistence;
using Atlas.Modules.Wiki.Infrastructure.Persistence;
using Atlas.Shared.Caching.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atlas.IntegrationTests;

/// <summary>
/// Gerçek Program.cs'i (tüm modülleriyle) ayağa kaldırıyor, ama iki şeyi
/// test'e uygun hale getirmek için değiştiriyor:
///
/// 1. AuthDbContext/WikiDbContext gerçek SQL Server yerine EF Core InMemory
///    provider kullanıyor - her factory instance'ı kendi izole veritabanını
///    alıyor, testler birbirini kirletmiyor, gerçek DB de gerekmiyor.
/// 2. ICacheService gerçek Redis yerine NoOpCacheService (her zaman "miss") -
///    aksi halde testler paylaşılan gerçek bir Redis instance'ı üzerinden
///    birbirini etkileyebilirdi.
///
/// AI modülü (PostgreSQL) ve Notifications (SignalR) BİLEREK değiştirilmedi -
/// bu üç senaryo (register/login, yetkisiz erişim, wiki create+list) onlara
/// dokunmuyor. Bu da şu anlama geliyor: testleri çalıştırmak için Docker'daki
/// Postgres+Redis container'larının (docker-compose.yml) ayakta olması gerekiyor -
/// CI'a taşınırsa bu ikisi de aynı desenle (InMemory/fake) değiştirilmeli.
/// </summary>
public class AtlasApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbSuffix = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Jwt:Key'i User Secrets'tan değil, doğrudan burada veriyoruz - testler
        // geliştiricinin makinesindeki user-secrets kurulumuna bağımlı olmasın,
        // başka bir makinede/CI'da da hiçbir ek kurulum olmadan çalışsın.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-only-signing-key-never-used-elsewhere-32+chars",
                ["Jwt:Issuer"] = "AtlasPlatform",
                ["Jwt:Audience"] = "AtlasPlatformUsers",
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddAuthModule/AddWikiModule zaten SqlServer provider'ını DI'a kaydetmişti.
            // Aynı container'a InMemory provider'ı da eklersek EF Core "iki provider
            // aynı service provider'da olamaz" hatası veriyor - çözüm, InMemory için
            // TAMAMEN AYRI, kendi kendine yeten bir internal service provider kurmak
            // (Microsoft'un resmi WebApplicationFactory + EF Core test dokümanındaki desen).
            var inMemoryProviderServices = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseInMemoryDatabase($"AuthTestDb-{_dbSuffix}");
                options.UseInternalServiceProvider(inMemoryProviderServices);
            });

            services.RemoveAll<DbContextOptions<WikiDbContext>>();
            services.AddDbContext<WikiDbContext>(options =>
            {
                options.UseInMemoryDatabase($"WikiTestDb-{_dbSuffix}");
                options.UseInternalServiceProvider(inMemoryProviderServices);
            });

            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();
        });
    }
}
