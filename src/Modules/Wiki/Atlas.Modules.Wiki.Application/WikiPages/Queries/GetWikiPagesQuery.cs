using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

/// <summary>
/// GÜVENLİK DÜZELTMESİ: Bu Query eskiden "ViewerDepartmentName" adında bir
/// parametre alıyor, bunu istemcinin query string'inden (?department=X) elle
/// gönderdiği bir değerle dolduruyordu. Bu, giriş yapmış HERHANGİ bir kullanıcının
/// (kendi departmanı ne olursa olsun) sadece departman adını tahmin ederek başka
/// departmanların DepartmentOnly sayfalarını okuyabilmesine izin veriyordu -
/// gerçek bir yetkilendirme açığıydı (canlı olarak doğrulandı ve düzeltildi).
/// Artık Query'nin departman parametresi yok - Handler, departmanı doğrudan
/// ICurrentUserAccessor'dan (JWT'deki imzalı "department" claim'inden) okuyor.
///
/// Sayfalama: PageNumber/PageSize sadece filtrelemeden SONRA, bellekte uygulanıyor -
/// cache stratejisi hâlâ "ham veriyi (tüm sayfalar) tek bir key'de cache'le" (bkz.
/// Handler'daki AllPagesCacheKey). Her sayfa/departman kombinasyonu için ayrı bir
/// cache key açmıyoruz - mevcut tasarımla tutarlı kalıyor.
/// </summary>
public record GetWikiPagesQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<WikiPageDto>>;
