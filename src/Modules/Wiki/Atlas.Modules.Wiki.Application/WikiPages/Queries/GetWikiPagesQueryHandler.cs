using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPagesQueryHandler : IRequestHandler<GetWikiPagesQuery, IReadOnlyList<WikiPageDto>>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetWikiPagesQueryHandler(IWikiPageRepository wikiPageRepository, ICurrentUserAccessor currentUser)
    {
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WikiPageDto>> Handle(GetWikiPagesQuery request, CancellationToken cancellationToken)
    {
        var allPages = await _wikiPageRepository.GetAllAsync(cancellationToken);

        // Giriş yapmamış bir kullanıcı departman parametresi gönderse bile,
        // bunu SESSİZCE yok sayıyoruz - hata fırlatmıyoruz, sadece filtreyi
        // devre dışı bırakıyoruz. Sonuç: giriş yapmamış herkes sadece Public
        // sayfaları görür, departman adını tahmin etmek işe yaramaz.
        var effectiveDepartment = _currentUser.IsAuthenticated ? request.ViewerDepartmentName : null;

        var visiblePages = allPages.Where(p => p.IsVisibleTo(effectiveDepartment));

        return visiblePages
            .Select(p => new WikiPageDto(
                p.Id, p.Title, p.Content, p.DepartmentName,
                p.Visibility.ToString(), p.CreatedByUserId, p.CreatedAtUtc))
            .ToList();
    }
}