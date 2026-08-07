using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Infrastructure.Outbox;
using Atlas.Modules.Wiki.Infrastructure.Persistence;
using Atlas.Shared.Contracts;
using Atlas.Shared.CQRS.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Wiki.Api;

public static class WikiModule
{
    public static IServiceCollection AddWikiModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'DefaultConnection' bağlantı dizesi appsettings.json'da bulunamadı.");

        services.AddDbContext<WikiDbContext>(options => options.UseSqlServer(connectionString));

        // Artık Scoped - WikiDbContext gerçek bir bağlantıyı sarmalıyor,
        // InMemoryWikiPageRepository'deki gibi kendisi depo değil.
        services.AddScoped<IWikiPageRepository, EfWikiPageRepository>();
        services.AddScoped<IWikiFolderRepository, EfWikiFolderRepository>();
        services.AddScoped<IWikiCommentRepository, EfWikiCommentRepository>();

        // AI modülü (ve ileride Wiki'nin görünürlük kuralına ihtiyaç duyan başka
        // bir modül) bu arayüz üzerinden Wiki'nin kuralını KOPYALAMADAN kullanabiliyor.
        // Durumsuz (stateless) - Singleton güvenli.
        services.AddSingleton<IWikiVisibilityChecker, WikiVisibilityChecker>();

        // Transactional Outbox Pattern - Gün 1. WikiDbContext'i sarmalıyor
        // (dış kaynak) - Scoped. Gün 2'de Command Handler'lar IPublisher.Publish
        // yerine bunu kullanmaya başlayacak.
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();

        // Gün 2: WikiPage + OutboxMessage yazmalarını TEK bir SaveChanges'te
        // (atomik) birleştiren "unit of work" - bkz. IUnitOfWork'teki not.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Gün 3: Outbox'a yazılan mesajları periyodik okuyup gerçekten
        // yayınlayan arka plan işleyici - bkz. OutboxProcessor'daki not.
        // AddHostedService, uygulama başlarken otomatik başlatıyor.
        services.AddHostedService<OutboxProcessor>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<GetWikiPagesQuery>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

            // Sadece ICacheableQuery<T>'yi implemente eden Query'ler (şu an yalnızca
            // GetAllWikiPagesRawQuery) bu davranıştan geçer - constraint'i karşılamayan
            // Command/Query'ler için MediatR bu behavior'ı hiç devreye sokmuyor.
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));

            // CachingBehavior'ın tersi yönü - sadece ICacheInvalidatingCommand'ı
            // implemente eden Command'lar (CreateWikiPageCommand, DeleteWikiPageCommand)
            // bu davranıştan geçer, Handler çalıştıktan sonra ilgili cache anahtarını temizler.
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));

            // Portföy sertleştirme - 4. adım (Audit log): sadece IAuditableCommand'ı
            // implemente eden Command'lar (CreateWikiPageCommand, DeleteWikiPageCommand)
            // bu davranıştan geçer, Handler BAŞARIYLA tamamlandıktan sonra Audit
            // modülüne (IAuditLogWriter üzerinden, Wiki'nin haberi bile olmadan)
            // bir satır yazdırır.
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<GetWikiPagesQuery>();

        return services;
    }

    /// <summary>
    /// Host, WikiDbContext'in varlığından habersiz - sadece bu metodu çağırır.
    /// Wiki'nin seed edilecek bir başlangıç verisi yok (Auth'taki admin gibi),
    /// o yüzden sadece migration uyguluyoruz.
    /// </summary>
    public static void MigrateWikiDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WikiDbContext>();

        // Migrate(), EF Core InMemory provider'da desteklenmiyor (integration
        // testlerde kullanılıyor). ProviderName kontrolü, IsRelational()'dan
        // farklı olarak servis çözümlemesi gerektirmiyor (bkz. AuthModule.cs'teki
        // aynı not - IsRelational() burada güvenilir çalışmadı).
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
