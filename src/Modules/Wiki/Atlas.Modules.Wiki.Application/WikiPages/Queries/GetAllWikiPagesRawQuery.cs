using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

/// <summary>
/// GetWikiPagesQuery'nin DIŞINDAN çağrılmıyor (istemci bu Query'yi hiç bilmiyor) -
/// sadece GetWikiPagesQueryHandler'ın kendi içinde kullandığı bir uygulama detayı.
/// Departman filtresi UYGULANMADAN, veritabanındaki TÜM sayfaları döndürür - bu
/// yüzden cache'lenmesi güvenli (bkz. CachingBehavior'daki güvenlik notu):
/// filtreleme her zaman bu Query'nin SONUCU üzerinde, istek bazlı olarak yapılıyor.
/// </summary>
public record GetAllWikiPagesRawQuery : ICacheableQuery<List<WikiPageDto>>
{
    public string CacheKey => "wiki-pages:all";

    public TimeSpan CacheDuration => TimeSpan.FromSeconds(30);
}
