using Atlas.Modules.Wiki.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPagesQueryHandler : IRequestHandler<GetWikiPagesQuery, IReadOnlyList<WikiPageDto>>
{
    private readonly IWikiPageRepository _wikiPageRepository;

    public GetWikiPagesQueryHandler(IWikiPageRepository wikiPageRepository)
    {
        _wikiPageRepository = wikiPageRepository;
    }

    public async Task<IReadOnlyList<WikiPageDto>> Handle(GetWikiPagesQuery request, CancellationToken cancellationToken)
    {
        var allPages = await _wikiPageRepository.GetAllAsync(cancellationToken);

        // Yetkilendirme filtresi: Domain'deki IsVisibleTo() kuralını burada çağırıyoruz.
        // Handler "nasıl karar verileceğini" bilmiyor, sadece Domain'e soruyor.
        var visiblePages = allPages.Where(p => p.IsVisibleTo(request.ViewerDepartmentName));

        return visiblePages
            .Select(p => new WikiPageDto(
                p.Id, p.Title, p.Content, p.DepartmentName,
                p.Visibility.ToString(), p.CreatedByUserId, p.CreatedAtUtc))
            .ToList();
    }
}
