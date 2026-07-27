using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

/// <summary>
/// DİKKAT: "CreatedByUserId" burada YOK. Kullanıcı bunu kendisi göndermiyor -
/// çünkü kullanıcının kim olduğunu istemciye güvenerek asla belirlememeliyiz.
/// Bunu Handler, ICurrentUserAccessor üzerinden kendisi bulacak.
///
/// ICacheInvalidatingCommand: bu Command SONRADAN fark edildi bir eksiklik -
/// yeni bir sayfa eklendiğinde GetAllWikiPagesRawQuery'nin 30 saniyelik
/// cache'i temizlenmiyordu, yeni sayfa listede görünene kadar bu süre kadar
/// beklemek gerekiyordu (silme özelliği eklenirken bu aynı sorunun DAHA
/// rahatsız edici bir versiyonu - "sildim ama hâlâ duruyor" - fark edilince
/// ikisi birlikte düzeltildi).
/// </summary>
public record CreateWikiPageCommand(
    string Title,
    string Content,
    string DepartmentName,
    string Visibility) : IRequest<Guid>, ICacheInvalidatingCommand
{
    public string CacheKeyToInvalidate => "wiki-pages:all";
}
