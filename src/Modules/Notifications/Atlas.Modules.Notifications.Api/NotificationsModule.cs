using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Modules.Notifications.Api;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        // SignalR, ASP.NET Core'a dahil - ekstra NuGet paketi gerekmiyor.
        // AddSignalR(): Hub altyapısını (bağlantı yönetimi, mesajlaşma) DI'a kaydeder.
        services.AddSignalR();

        return services;
    }
}