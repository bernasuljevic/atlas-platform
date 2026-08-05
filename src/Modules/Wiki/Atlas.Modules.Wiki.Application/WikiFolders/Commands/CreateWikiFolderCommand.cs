using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiFolders.Commands;

/// <summary>
/// CreateWikiPageCommand ile AYNI desen: DepartmentName normal bir kullanıcı
/// için Handler'da yok sayılır (departman her zaman JWT'den zorlanır), sadece
/// Admin istediği departmanda klasör açabilir. ParentFolderId null ise klasör
/// departmanın kök seviyesinde oluşturulur.
/// </summary>
public record CreateWikiFolderCommand(
    string Name,
    string DepartmentName,
    Guid? ParentFolderId) : IRequest<Guid>, IAuditableCommand
{
    // AuditResourceId BİLEREK null - CreateWikiPageCommand'daki AYNI gerekçe,
    // yeni klasörün ID'si Handler çalışana kadar bilinmiyor.
    public string AuditAction => "WikiFolder.Created";
    public string? AuditResourceId => null;
    public string? AuditDetails { get; set; }
}
