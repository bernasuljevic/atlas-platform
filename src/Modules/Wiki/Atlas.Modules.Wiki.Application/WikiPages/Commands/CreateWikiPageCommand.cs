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
    string Visibility) : IRequest<Guid>, ICacheInvalidatingCommand, IAuditableCommand
{
    public string CacheKeyToInvalidate => "wiki-pages:all";

    // AuditResourceId BİLEREK null - yeni sayfanın ID'si Handler çalışana kadar
    // bilinmiyor. AuditBehavior, Handler'ın döndürdüğü Guid'i (page.Id) otomatik
    // olarak kaynak ID'si sayıyor (bkz. IAuditableCommand'daki not).
    public string AuditAction => "WikiPage.Created";
    public string? AuditResourceId => null;

    // Title zaten Command'ın kendisinde var - Handler yine de AYNI deseni
    // (AuditDetails'i Handle() içinde elle set etmek) kullanıyor, Delete'teki
    // ile tutarlı kalsın diye (aksi halde "bazen computed property, bazen
    // Handler'da set" gibi iki farklı kural olurdu).
    public string? AuditDetails { get; set; }
}
