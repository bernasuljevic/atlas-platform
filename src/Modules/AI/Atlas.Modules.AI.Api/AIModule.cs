using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Application.WikiPages.Commands;
using Atlas.Modules.AI.Infrastructure;
using Atlas.Modules.AI.Infrastructure.Embeddings;
using Atlas.Modules.AI.Infrastructure.Persistence;
using Atlas.Shared.CQRS.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.AI.Api;

public static class AIModule
{
    public static IServiceCollection AddAIModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("'Postgres' bağlantı dizesi appsettings.json'da bulunamadı.");

        // UseVector(): Npgsql'e "vector" PostgreSQL tipini Pgvector.Vector CLR tipiyle
        // eşleştirmesini söylüyor - bu olmadan Npgsql "vector" sütununu tanımaz.
        services.AddDbContext<AiDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.UseVector()));

        services.AddHealthChecks().AddDbContextCheck<AiDbContext>("postgresql");

        // GEÇİCİ: Gerçek bir sağlayıcı (Voyage AI, OpenAI vb.) API key'ler gelince
        // buraya bağlanacak - o zaman değişecek TEK satır burası. FakeEmbeddingService
        // hiçbir dış kaynağa (DB, HTTP) bağlı olmadığı, tamamen durumsuz (stateless)
        // olduğu için Singleton güvenli - her istek için yeni bir instance
        // oluşturmaya gerek yok (CLAUDE.md'deki "Service Lifetime kuralı"yla tutarlı:
        // dış bir kaynağı sarmalamıyorsa Singleton olabilir).
        services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();

        // AiDbContext'i sarmalıyor (dış kaynak) - Auth/Wiki'deki repository'lerle
        // aynı sebepten Scoped.
        services.AddScoped<IWikiPageEmbeddingRepository, EfWikiPageEmbeddingRepository>();

        // P5: Documents→AI/RAG entegrasyonu - WikiPageEmbedding'in AYNI deseni,
        // ayrı tablo/repository (bkz. DocumentEmbedding'deki "neden ayrı" notu).
        services.AddScoped<IDocumentEmbeddingRepository, EfDocumentEmbeddingRepository>();

        // AI modülünde MediatR ilk kez burada kayıt ediliyor (Gün 1'de fark ettiğimiz
        // eksiklik) - Auth/Wiki'deki AddMediatR bloklarıyla birebir aynı desen:
        // loglama + validasyon (henüz validator yok ama ValidationBehavior validator
        // bulamayınca no-op geçiyor, ileride eklenecek bir validator otomatik devreye girer).
        services.AddMediatR(cfg =>
        {
            // İKİ ayrı assembly taranıyor: GenerateWikiPageEmbeddingsCommand'ın
            // Handler'ı AI.Application'da yaşıyor, ama WikiPageCreatedEventHandler
            // (Wiki'nin event'ini dinleyen abone) AI.Infrastructure'da yaşıyor.
            // "RegisterServicesFromAssemblyContaining<T>" SADECE T'nin bulunduğu
            // assembly'yi tarıyor - referans edilen başka bir assembly'yi OTOMATİK
            // taramıyor. Bunu atlarsak (ilk denemede tam olarak bu oldu),
            // WikiPageCreatedEventHandler hiç kayıt olmaz ve Wiki'nin yayınladığı
            // event'i sessizce hiç dinlemeyen bir "hayalet" abone kalırdı.
            cfg.RegisterServicesFromAssemblyContaining<GenerateWikiPageEmbeddingsCommand>();
            cfg.RegisterServicesFromAssemblyContaining<WikiPageCreatedEventHandler>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<GenerateWikiPageEmbeddingsCommand>();

        return services;
    }

    /// <summary>
    /// Auth/Wiki'deki MigrateAuthDatabase/MigrateWikiDatabase ile aynı desen -
    /// Host, AiDbContext'in varlığını bilmiyor, sadece bu metodu çağırıyor.
    /// </summary>
    public static void MigrateAiDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        db.Database.Migrate();
    }
}
