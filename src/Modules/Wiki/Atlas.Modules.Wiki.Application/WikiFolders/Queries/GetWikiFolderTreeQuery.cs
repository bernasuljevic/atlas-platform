using Atlas.Modules.Wiki.Application.WikiFolders;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiFolders.Queries;

/// <summary>
/// DepartmentName BİLEREK zorunlu bir parametre - "hangi departmanın ağacını
/// gösteriyorum" sorusu istemciden geliyor (kendi departmanını ya da başka bir
/// departmanı gezebilir), ama SONUÇTA neyin görüneceği (Handler'daki
/// isFullAccess ayrımı) istemciden değil ICurrentUserAccessor'dan belirleniyor -
/// GetWikiPagesQuery'deki AYNI güvenlik deseni (bkz. Ders #10).
/// </summary>
public record GetWikiFolderTreeQuery(string DepartmentName) : IRequest<WikiFolderTreeDto>;
