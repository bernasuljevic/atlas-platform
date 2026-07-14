using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

/// <summary>
/// DİKKAT: "CreatedByUserId" burada YOK. Kullanıcı bunu kendisi göndermiyor -
/// çünkü kullanıcının kim olduğunu istemciye güvenerek asla belirlememeliyiz.
/// Bunu Handler, ICurrentUserAccessor üzerinden kendisi bulacak.
/// </summary>
public record CreateWikiPageCommand(
    string Title,
    string Content,
    string DepartmentName,
    string Visibility) : IRequest<Guid>;
