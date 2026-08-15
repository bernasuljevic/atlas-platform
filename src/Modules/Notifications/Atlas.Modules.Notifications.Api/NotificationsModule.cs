using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Modules.Notifications.Application.Notifications.Queries;
using Atlas.Modules.Notifications.Infrastructure;
using Atlas.Modules.Notifications.Infrastructure.Persistence;
using Atlas.Shared.CQRS.Behaviors;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Notifications.Api;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("'Redis' bağlantı dizesi appsettings.json'da bulunamadı.");

        // Audit/Vault/Documents ile AYNI SQL Server veritabanı (AtlasPlatform),
        // kendi "notifications" şemasıyla - 2026-08-15'e kadar bu modülün hiç
        // veritabanı bağlantısı yoktu (tamamen ephemeral SignalR), bkz. NotificationEntry.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'DefaultConnection' bağlantı dizesi appsettings.json'da bulunamadı.");

        services.AddDbContext<NotificationsDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<INotificationRepository, EfNotificationRepository>();

        // AddSignalR(): Hub altyapısını (bağlantı yönetimi, mesajlaşma) DI'a kaydeder.
        //
        // AddStackExchangeRedis(): Şu ana kadar Hub tek bir process (bu makinedeki tek
        // Atlas.Api instance'ı) içinde bağlı istemcileri hatırlıyordu - "WikiPageCreated"
        // mesajı sadece O ANDA çalışan process'e bağlı istemcilere gidiyordu. Uygulamayı
        // birden fazla instance ile (örn. yük dengeleme arkasında, load balancer) çalıştırırsak
        // her instance kendi istemci listesini tutar, B instance'ına bağlı bir istemci
        // A instance'ında oluşan bir Wiki sayfası bildirimini HİÇ ALAMAZ - çünkü olay A'da
        // yayınlanıyor, A sadece kendi bağlı istemcilerine gönderiyor. Redis backplane,
        // her instance'ın "SendAsync" çağrısını Redis üzerinden TÜM instance'lara yayınlanan
        // bir pub/sub mesajına çeviriyor - hangi istemci hangi instance'a bağlı olursa olsun
        // mesajı alıyor. Tek instance'lı (Development) kurulumda bu ekstra bir ağ atlaması
        // dışında görünür bir fark yaratmıyor, ama çoklu instance'a geçişin önünü açıyor.
        services.AddSignalR()
            .AddStackExchangeRedis(redisConnectionString);

        // İKİ ayrı assembly taranıyor - AI/Documents modüllerindeki AYNI bug'ı
        // (handler'ın yaşadığı assembly taranmadığı için event'in sessizce hiç
        // dinlenmemesi) BAŞTAN önlemek için: GetNotificationsQuery Application'da,
        // WikiPageCreatedEventHandler Infrastructure'da yaşıyor -
        // RegisterServicesFromAssemblyContaining<T> SADECE T'nin bulunduğu
        // assembly'yi tarar, ikinci satır OLMASAYDI handler kayıt olmazdı.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<GetNotificationsQuery>();
            cfg.RegisterServicesFromAssemblyContaining<WikiPageCreatedEventHandler>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        return services;
    }

    /// <summary>
    /// Host, NotificationsDbContext'in varlığından habersiz - sadece bu metodu
    /// çağırır (Auth/Wiki/AI/Audit/Vault/Documents'taki MigrateXDatabase ile
    /// aynı desen).
    /// </summary>
    public static void MigrateNotificationsDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

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