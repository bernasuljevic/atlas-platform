using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPageVersionByNumberQueryHandler : IRequestHandler<GetWikiPageVersionByNumberQuery, WikiPageVersionDto?>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly IWikiPageVersionRepository _wikiPageVersionRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetWikiPageVersionByNumberQueryHandler(
        IWikiPageRepository wikiPageRepository, IWikiPageVersionRepository wikiPageVersionRepository,
        ICurrentUserAccessor currentUser)
    {
        _wikiPageRepository = wikiPageRepository;
        _wikiPageVersionRepository = wikiPageVersionRepository;
        _currentUser = currentUser;
    }

    public async Task<WikiPageVersionDto?> Handle(GetWikiPageVersionByNumberQuery request, CancellationToken cancellationToken)
    {
        var page = await _wikiPageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return null;

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        if (!page.IsVisibleTo(viewerDepartment, viewerIsAdmin))
            return null;

        var version = await _wikiPageVersionRepository.GetByWikiPageIdAndVersionNumberAsync(
            page.Id, request.VersionNumber, cancellationToken);

        if (version is null)
            return null;

        return new WikiPageVersionDto(
            version.VersionNumber, version.Title, version.Content, version.Visibility, version.Tags,
            version.EditedByEmail, version.EditedAtUtc);
    }
}
