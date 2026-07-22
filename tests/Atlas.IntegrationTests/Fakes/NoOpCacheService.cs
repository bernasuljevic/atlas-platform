using Atlas.Shared.Caching.Abstractions;

namespace Atlas.IntegrationTests.Fakes;

// Gerçek Redis'e bağlanmıyor - her GetAsync "cache miss" (null) döner. Bunu
// kullanmazsak GetWikiPagesQueryHandler gerçek, testler arasında paylaşılan bir
// Redis instance'ına yazıp okurdu - bir testin verisi diğerini kirletebilirdi.
public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
