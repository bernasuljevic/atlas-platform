using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

/// <summary>
/// "viewerDepartmentName" şimdilik dışarıdan (query string ile) geliyor -
/// gerçek JWT login geldiğinde bunu da ICurrentUserAccessor'dan otomatik
/// alacağız, kullanıcının elle göndermesine gerek kalmayacak. Bugünlük
/// bu, yetkilendirme mantığını test edebilmemiz için geçici bir köprü.
/// </summary>
public record GetWikiPagesQuery(string? ViewerDepartmentName) : IRequest<IReadOnlyList<WikiPageDto>>;
