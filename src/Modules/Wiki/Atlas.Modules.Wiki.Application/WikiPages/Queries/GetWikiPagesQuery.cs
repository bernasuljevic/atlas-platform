using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

/// <summary>
/// GÜVENLİK DÜZELTMESİ: Bu Query eskiden "ViewerDepartmentName" adında bir
/// parametre alıyor, bunu istemcinin query string'inden (?department=X) elle
/// gönderdiği bir değerle dolduruyordu. Bu, giriş yapmış HERHANGİ bir kullanıcının
/// (kendi departmanı ne olursa olsun) sadece departman adını tahmin ederek başka
/// departmanların DepartmentOnly sayfalarını okuyabilmesine izin veriyordu -
/// gerçek bir yetkilendirme açığıydı (canlı olarak doğrulandı ve düzeltildi).
/// Artık Query'nin parametresi yok - Handler, departmanı doğrudan
/// ICurrentUserAccessor'dan (JWT'deki imzalı "department" claim'inden) okuyor.
/// </summary>
public record GetWikiPagesQuery : IRequest<IReadOnlyList<WikiPageDto>>;
