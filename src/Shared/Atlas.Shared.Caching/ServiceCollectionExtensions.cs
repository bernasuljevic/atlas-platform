using Atlas.Shared.Caching.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Atlas.Shared.Caching;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string 'ConnectionStrings:Redis' appsettings.json içinde bulunamadı.");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");

        return services;
    }
}