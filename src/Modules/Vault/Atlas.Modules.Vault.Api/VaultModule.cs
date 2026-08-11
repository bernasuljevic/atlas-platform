using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Modules.Vault.Application.PasswordEntries.Commands;
using Atlas.Modules.Vault.Infrastructure.Encryption;
using Atlas.Modules.Vault.Infrastructure.Persistence;
using Atlas.Shared.CQRS.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Vault.Api;

public static class VaultModule
{
    public static IServiceCollection AddVaultModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ayrı bir veritabanı DEĞİL - Auth/Wiki/Audit ile AYNI SQL Server
        // veritabanını (AtlasPlatform), kendi "vault" şemasıyla paylaşıyor.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'DefaultConnection' bağlantı dizesi appsettings.json'da bulunamadı.");

        services.AddDbContext<VaultDbContext>(options => options.UseSqlServer(connectionString));

        // Data Protection - şifrelerin GERİ ÇÖZÜLEBİLİR (encryption, Auth
        // modülündeki Pbkdf2PasswordHasher'ın TERSİNE - o TEK YÖNLÜ) olarak
        // saklanması için. Anahtarlar YEREL DİSKE, %LOCALAPPDATA% altına
        // kalıcı - Directory.Build.props'un bin/obj için yaptığı taşımayla
        // AYNI gerekçe (Ders #16): proje klasörü OneDrive ile senkronize
        // ediliyor, senkronize bir klasöre kritik anahtar dosyaları yazmak
        // riskli olurdu. NOT: bu, TEK instance'lık yerel geliştirme için
        // doğru varsayılan - birden fazla instance/Docker'da paylaşılan bir
        // depolama (mounted volume ya da DB-backed key ring) gerekirdi,
        // bilinçli olarak Faz 7'nin bu ilk gününün kapsamı DIŞINDA bırakıldı.
        var keysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AtlasPlatformDataProtectionKeys");
        services.AddDataProtection()
            .SetApplicationName("AtlasPlatform")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        services.AddScoped<IPasswordEntryRepository, EfPasswordEntryRepository>();

        // Singleton - IDataProtectionProvider'ın kendisi de Singleton (framework
        // bunu böyle kaydediyor), CreateProtector() maliyeti bir kere ödensin
        // diye Encryptor da Singleton (FakeEmbeddingService'le AYNI gerekçe -
        // durumsuz, dış bir kaynağa (DB/HTTP) bağlı değil).
        services.AddSingleton<IPasswordEncryptor, DataProtectionPasswordEncryptor>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreatePasswordEntryCommand>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

            // AuditBehavior - Create/Update/Delete (ve Gün 2'de Reveal) hepsi
            // IAuditableCommand implemente ediyor, bu tek satır hepsini otomatik
            // devreye sokuyor (WikiModule'daki AYNI desen).
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<CreatePasswordEntryCommand>();

        return services;
    }

    /// <summary>
    /// Host, VaultDbContext'in varlığından habersiz - sadece bu metodu çağırır
    /// (Auth/Wiki/AI/Audit'teki MigrateXDatabase ile aynı desen).
    /// </summary>
    public static void MigrateVaultDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VaultDbContext>();

        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
        }
    }
}
