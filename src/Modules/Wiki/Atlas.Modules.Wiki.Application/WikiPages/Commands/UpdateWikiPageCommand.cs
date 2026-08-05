using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

/// <summary>
/// CreateWikiPageCommand'da düzenlenebilen alanlarla AYNI küme (Title/Content/
/// Visibility/FolderId) - DepartmentName BİLEREK YOK, bir sayfanın hangi
/// departmana ait olduğu oluşturulduktan sonra değişmiyor (departman, Ders
/// #10/#13'teki güvenlik kararlarının dayandığı sabit bir gerçek - bunu
/// düzenleme akışına açmak ayrı, dikkatli bir tasarım kararı gerektirirdi).
/// </summary>
public record UpdateWikiPageCommand(
    Guid PageId,
    string Title,
    string Content,
    string Visibility,
    Guid? FolderId,
    string? Tags = null) : IRequest, ICacheInvalidatingCommand, IAuditableCommand
{
    public string CacheKeyToInvalidate => "wiki-pages:all";

    // Delete'teki AYNI gerekçe - ID zaten Command'ın kendisinde biliniyor.
    public string AuditAction => "WikiPage.Updated";
    public string? AuditResourceId => PageId.ToString();
    public string? AuditDetails { get; set; }
}
