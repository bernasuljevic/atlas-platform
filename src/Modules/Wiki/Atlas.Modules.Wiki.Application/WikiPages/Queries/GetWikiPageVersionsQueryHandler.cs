using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPageVersionsQueryHandler : IRequestHandler<GetWikiPageVersionsQuery, IReadOnlyList<WikiPageVersionSummaryDto>?>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly IWikiPageVersionRepository _wikiPageVersionRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetWikiPageVersionsQueryHandler(
        IWikiPageRepository wikiPageRepository, IWikiPageVersionRepository wikiPageVersionRepository,
        ICurrentUserAccessor currentUser)
    {
        _wikiPageRepository = wikiPageRepository;
        _wikiPageVersionRepository = wikiPageVersionRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WikiPageVersionSummaryDto>?> Handle(
        GetWikiPageVersionsQuery request, CancellationToken cancellationToken)
    {
        var page = await _wikiPageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return null;

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        // GetWikiPageByIdQueryHandler'daki AYNI görünürlük kuralı - Id'yi
        // bilmek görebilmek anlamına gelmiyor.
        if (!page.IsVisibleTo(viewerDepartment, viewerIsAdmin))
            return null;

        var versions = await _wikiPageVersionRepository.GetByWikiPageIdAsync(page.Id, cancellationToken);

        return versions
            .Select(v => new WikiPageVersionSummaryDto(v.VersionNumber, v.Title, v.EditedByEmail, v.EditedAtUtc))
            .ToList();
    }
}
