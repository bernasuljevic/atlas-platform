using Atlas.Shared.Caching.Abstractions;
using MediatR;

namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// GetWikiPagesQueryHandler'da elle yazılmış "önce cache'e bak, yoksa DB'den çek,
/// sonra cache'e yaz" mantığının generic hali. TRequest constraint'i
/// ICacheableQuery&lt;TResponse&gt; olduğu için MediatR bu behavior'ı SADECE bunu
/// implemente eden Query'ler için pipeline'a sokuyor - diğer tüm Command/Query'ler
/// (constraint'i karşılamadıkları için) bu davranıştan hiç geçmiyor, elle bir
/// "if" yazmamıza gerek yok.
///
/// DİKKAT - GÜVENLİK: Bu behavior TResponse'un TAMAMINI cache'liyor. Bu yüzden
/// kullanıcıya özel (örn. departman filtresi uygulanmış) bir sonucu ASLA doğrudan
/// cache'letmemeli - aksi halde bir kullanıcının filtrelenmiş görünümü başka bir
/// kullanıcıya sızabilir (bkz. CLAUDE.md "Öğrenilen dersler #10"daki departman
/// güvenlik açığıyla aynı sınıf hata). Bu yüzden Wiki modülünde ICacheableQuery'yi
/// uygulayan sınıf, kullanıcıya özel filtreleme yapan GetWikiPagesQuery DEĞİL, onun
/// çağırdığı filtre öncesi ham veriyi döndüren ayrı bir iç Query (bkz.
/// GetAllWikiPagesRawQuery).
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
{
    private readonly ICacheService _cache;

    public CachingBehavior(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        var response = await next();

        await _cache.SetAsync(request.CacheKey, response, request.CacheDuration, cancellationToken);

        return response;
    }
}
