using MediatR;

namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// Bir Query bu interface'i implemente ederse CachingBehavior otomatik devreye girer -
/// tıpkı ValidationBehavior'ın "IValidator&lt;T&gt; var mı?" diye sormasına benzer bir
/// desen. CacheKey/CacheDuration'ı Query'nin kendisi belirliyor çünkü "bu veriyi hangi
/// anahtarla, ne kadar süre cache'lemeli" bilgisi modülün kendi iş kuralı - generic
/// behavior bunu bilemez, sadece uygular.
/// </summary>
public interface ICacheableQuery<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }

    TimeSpan CacheDuration { get; }
}
