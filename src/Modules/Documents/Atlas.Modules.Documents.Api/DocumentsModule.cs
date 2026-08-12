using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Infrastructure;
using Atlas.Modules.Documents.Infrastructure.Outbox;
using Atlas.Modules.Documents.Infrastructure.Persistence;
using Atlas.Modules.Documents.Infrastructure.Processing;
using Atlas.Modules.Documents.Infrastructure.Storage;
using Atlas.Shared.CQRS.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Documents.Api;

// P3 Gün 2'de sadece storage+persistence host'a bağlanmıştı, Gün 3'te MediatR/
// FluentValidation kaydı eklendi. P4 Gün 2'de Outbox altyapısı eklendi (boş
// çalışıyordu). P4 Gün 3'te kuyruk ilk kez besleniyor: DocumentUploadedEvent +
// PlainTextDocumentProcessor + DocumentUploadedEventHandler (Infrastructure).
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

        // Transactional Outbox Pattern - Wiki'nin Gün 1-3'ünün BİREBİR kopyası.
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddHostedService<OutboxProcessor>();

        // IDocumentProcessor'lar - Singleton, FakeEmbeddingService'le AYNI gerekçe
        // (durumsuz, dış kaynağa bağlı değil). Her biri kendi uzantı kümesine
        // "evet" diyor (bkz. CanProcess), IEnumerable<IDocumentProcessor> hepsini
        // otomatik topluyor - yeni bir format desteği eklemek SADECE yeni bir
        // AddSingleton satırı, mevcut hiçbir processor'a dokunulmuyor.
        services.AddSingleton<IDocumentProcessor, PlainTextDocumentProcessor>();
        services.AddSingleton<IDocumentProcessor, PdfDocumentProcessor>();
        services.AddSingleton<IDocumentProcessor, OpenXmlWordDocumentProcessor>();
        services.AddSingleton<IDocumentProcessor, OpenXmlPresentationDocumentProcessor>();
        services.AddSingleton<IDocumentProcessor, OpenXmlSpreadsheetDocumentProcessor>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<UploadDocumentCommand>();

            // AI modülünün AIModule.cs'inde bulunan AYNI hatayı BURADA baştan
            // önlüyoruz: DocumentUploadedEventHandler Documents.Infrastructure'da
            // yaşıyor, RegisterServicesFromAssemblyContaining<UploadDocumentCommand>
            // SADECE Documents.Application assembly'sini tarar - bu satır
            // olmasaydı handler hiç kayıt olmaz, event sessizce hiç dinlenmezdi.
            cfg.RegisterServicesFromAssemblyContaining<DocumentUploadedEventHandler>();

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
