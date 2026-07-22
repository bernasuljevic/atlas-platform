using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Infrastructure.Persistence;
using Atlas.Shared.CQRS.Behaviors;
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

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<GetWikiPagesQuery>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

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
