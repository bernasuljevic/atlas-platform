using Atlas.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Notifications.Api;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("'Redis' bağlantı dizesi appsettings.json'da bulunamadı.");

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

        // WikiPageCreatedEventHandler, Infrastructure projesinde yaşıyor - MediatR'a
        // "bu assembly'yi de tara, INotificationHandler implementasyonlarını bul" diyoruz.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<WikiPageCreatedEventHandler>();
        });

        return services;
    }
}