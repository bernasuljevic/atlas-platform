using Atlas.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Notifications.Api;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        // SignalR, ASP.NET Core'a dahil - ekstra NuGet paketi gerekmiyor.
        // AddSignalR(): Hub altyapısını (bağlantı yönetimi, mesajlaşma) DI'a kaydeder.
        services.AddSignalR();

        // WikiPageCreatedEventHandler, Infrastructure projesinde yaşıyor - MediatR'a
        // "bu assembly'yi de tara, INotificationHandler implementasyonlarını bul" diyoruz.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<WikiPageCreatedEventHandler>();
        });

        return services;
    }
}