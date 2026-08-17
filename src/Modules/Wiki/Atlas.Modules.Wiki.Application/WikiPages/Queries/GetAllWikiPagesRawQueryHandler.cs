using Atlas.Modules.Wiki.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetAllWikiPagesRawQueryHandler : IRequestHandler<GetAllWikiPagesRawQuery, List<WikiPageDto>>
{
    private readonly IWikiPageRepository _wikiPageRepository;

    public GetAllWikiPagesRawQueryHandler(IWikiPageRepository wikiPageRepository)
    {
        _wikiPageRepository = wikiPageRepository;
    }

    public async Task<List<WikiPageDto>> Handle(GetAllWikiPagesRawQuery request, CancellationToken cancellationToken)
    {
        var pagesFromDb = await _wikiPageRepository.GetAllAsync(cancellationToken);

        return pagesFromDb
            .Select(p => new WikiPageDto(
                p.Id, p.Title, p.Content, p.DepartmentName,
                p.Visibility.ToString(), p.CreatedByUserId, p.CreatedAtUtc,
                p.FolderId, p.UpdatedAtUtc, p.CreatedByEmail, p.Tags, p.CurrentVersionNumber))
            .ToList();
    }
}
