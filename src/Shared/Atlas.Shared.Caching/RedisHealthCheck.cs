using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Atlas.Shared.Caching;

// AspNetCore.HealthChecks.Redis gibi ayrı bir paket eklemek yerine, zaten Singleton
// olarak kayıtlı IConnectionMultiplexer'ı kullanan basit bir PING kontrolü yazdık -
// ekstra bir bağımlılık/sürüm uyuşmazlığı riski almadan aynı işi görüyor.
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await _connectionMultiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"PING {latency.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis'e PING atılamadı.", ex);
        }
    }
}
