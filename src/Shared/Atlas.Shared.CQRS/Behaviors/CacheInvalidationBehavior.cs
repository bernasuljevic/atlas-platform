using Atlas.Shared.Caching.Abstractions;
using MediatR;

namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// CachingBehavior'ın tam tersi yönde çalışan sarmalayıcı - sadece
/// ICacheInvalidatingCommand'ı implemente eden Command'lar için MediatR bu
/// behavior'ı pipeline'a sokuyor (tıpkı CachingBehavior/ICacheableQuery
/// eşleşmesi gibi).
/// </summary>
public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICacheInvalidatingCommand
{
    private readonly ICacheService _cache;

    public CacheInvalidationBehavior(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Önce Handler çalışsın (yazma işlemi tamamlansın), SONRA cache temizlensin -
        // Handler bir hata fırlatırsa (örn. yetkisiz silme denemesi) cache'e hiç
        // dokunulmamış olur, gereksiz bir temizlik yapılmaz.
        var response = await next();

        await _cache.RemoveAsync(request.CacheKeyToInvalidate, cancellationToken);

        return response;
    }
}
