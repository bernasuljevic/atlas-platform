using System.Net.Http.Headers;
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
using Microsoft.Extensions.Options;

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

        // VoyageAi:ApiKey appsettings.json'da DEĞİL - User Secrets'tan/ortam
        // değişkeninden geliyor (Jwt:Key'le AYNI gerekçe, bkz. VoyageAiOptions).
        services.Configure<VoyageAiOptions>(configuration.GetSection("VoyageAi"));

        // HAZIR AMA HENÜZ DEVREDE DEĞİL: VoyageEmbeddingService'in kendisi VE
        // typed HttpClient kaydı API key gelmeden de yazılıp test edilebiliyordu
        // (bkz. VoyageEmbeddingServiceTests - sahte bir HttpMessageHandler'la,
        // gerçek ağ çağrısı hiç yapılmadan). BaseAddress/Authorization header'ı
        // burada, DI çözümlenirken (yani key User Secrets'a girilince otomatik)
        // kuruluyor - key boşsa Authorization header'ı hiç eklenmiyor, Voyage
        // 401 döner (fail-fast, DI çözümlemesi asla patlamaz).
        services.AddHttpClient<VoyageEmbeddingService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<VoyageAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);

            if (!string.IsNullOrEmpty(options.ApiKey))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        });

        // GEÇİCİ: API key gelince değişecek TEK satır burası -
        // AddSingleton<IEmbeddingService, FakeEmbeddingService>() ->
        // AddSingleton<IEmbeddingService, VoyageEmbeddingService>() (yukarıdaki
        // AddHttpClient<VoyageEmbeddingService> zaten typed client'ı Scoped/
        // Transient olarak yönetiyor - IEmbeddingService kaydını AddSingleton
        // DEĞİL AddScoped yapmak gerekecek, çünkü VoyageEmbeddingService artık
        // HttpClient gibi dış bir kaynağı sarmalıyor; bkz. CLAUDE.md "Service
        // Lifetime kuralı"). FakeEmbeddingService hiçbir dış kaynağa bağlı
        // olmadığı için Singleton güvenli.
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
