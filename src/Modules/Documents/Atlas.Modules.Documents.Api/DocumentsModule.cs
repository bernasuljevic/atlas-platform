using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Infrastructure.Persistence;
using Atlas.Modules.Documents.Infrastructure.Storage;
using Atlas.Shared.CQRS.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Documents.Api;

// Gün 2'de sadece storage+persistence host'a bağlanmıştı (DbContext'in DI'dan
// çözülebilir olması, migration tooling'in çalışabilmesi için). Gün 3'te
// MediatR/FluentValidation kaydı eklendi - ilk Command (UploadDocumentCommand) geldi.
public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ayrı bir veritabanı DEĞİL - Auth/Wiki/Audit/Vault ile AYNI SQL Server
        // veritabanını (AtlasPlatform), kendi "documents" şemasıyla paylaşıyor.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'DefaultConnection' bağlantı dizesi appsettings.json'da bulunamadı.");

        services.AddDbContext<DocumentsDbContext>(options => options.UseSqlServer(connectionString));

        // RootPath appsettings.json'da BİLEREK YOK (override edilmek istenirse
        // orada tanımlanabilir) - varsayılan, Vault'un DataProtection anahtar
        // yoluyla AYNI gerekçeyle (proje klasörü OneDrive senkronizasyonunda)
        // %LOCALAPPDATA% altında hesaplanıyor.
        var storageRootPath = configuration["DocumentStorage:RootPath"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtlasPlatformDocuments");
        var maxFileSizeBytes = configuration.GetValue<long?>("DocumentStorage:MaxFileSizeBytes") ?? 50 * 1024 * 1024;

        services.AddSingleton(new FileStorageOptions { RootPath = storageRootPath, MaxFileSizeBytes = maxFileSizeBytes });

        // Singleton - FakeEmbeddingService/DataProtectionPasswordEncryptor'la
        // AYNI gerekçe: durumsuz, dış bir DB bağlantısına ihtiyacı yok (sadece
        // dosya sistemine yazıyor).
        services.AddSingleton<IFileStorageService, LocalDiskFileStorageService>();

        services.AddScoped<IDocumentRepository, EfDocumentRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<UploadDocumentCommand>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

            // Wiki/Vault'taki AYNI desen - sadece IAuditableCommand implemente
            // eden Command'lar (şimdilik yalnızca UploadDocumentCommand) bu
            // davranıştan geçiyor.
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<UploadDocumentCommand>();

        return services;
    }

    /// <summary>
    /// Host, DocumentsDbContext'in varlığından habersiz - sadece bu metodu
    /// çağırır (Auth/Wiki/AI/Audit/Vault'taki MigrateXDatabase ile aynı desen).
    /// </summary>
    public static void MigrateDocumentsDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();

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
