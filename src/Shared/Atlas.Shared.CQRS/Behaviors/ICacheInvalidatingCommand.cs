namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// ICacheableQuery'nin tersi: bir Command bu interface'i implemente ederse
/// CacheInvalidationBehavior, Handler çalıştıktan SONRA belirtilen cache
/// anahtarını temizliyor. Bunsuz, bir yazma işleminden sonra CachingBehavior'ın
/// önbelleğe aldığı ham veri (bkz. GetAllWikiPagesRawQuery) süresi (30sn) dolana
/// kadar eski kalırdı - kullanıcı sildiği/eklediği bir sayfayı listede hemen
/// göremezdi. TResponse'a bağımlı değil (ValidationBehavior'daki "where TRequest :
/// notnull" ile aynı gerekçe) - Command'ın IRequest mi IRequest&lt;T&gt; mi
/// olduğu önemsiz, sadece bir "işaret" arayüzü.
/// </summary>
public interface ICacheInvalidatingCommand
{
    string CacheKeyToInvalidate { get; }
}
