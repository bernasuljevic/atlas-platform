using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPageByIdQueryHandler : IRequestHandler<GetWikiPageByIdQuery, WikiPageDto?>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetWikiPageByIdQueryHandler(IWikiPageRepository wikiPageRepository, ICurrentUserAccessor currentUser)
    {
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
    }

    public async Task<WikiPageDto?> Handle(GetWikiPageByIdQuery request, CancellationToken cancellationToken)
    {
        var page = await _wikiPageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return null;

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        // GetWikiPagesQueryHandler'daki AYNI görünürlük kuralı - Id'yi bilmek
        // görebilmek anlamına gelmiyor.
        if (!page.IsVisibleTo(viewerDepartment, viewerIsAdmin))
            return null;

        return new WikiPageDto(
            page.Id, page.Title, page.Content, page.DepartmentName,
            page.Visibility.ToString(), page.CreatedByUserId, page.CreatedAtUtc,
            page.FolderId, page.UpdatedAtUtc, page.CreatedByEmail, page.Tags, page.CurrentVersionNumber);
    }
}
