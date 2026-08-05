using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiFolders.Queries;

public class GetWikiFolderTreeQueryHandler : IRequestHandler<GetWikiFolderTreeQuery, WikiFolderTreeDto>
{
    private readonly IWikiFolderRepository _wikiFolderRepository;
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetWikiFolderTreeQueryHandler(
        IWikiFolderRepository wikiFolderRepository,
        IWikiPageRepository wikiPageRepository,
        ICurrentUserAccessor currentUser)
    {
        _wikiFolderRepository = wikiFolderRepository;
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
    }

    public async Task<WikiFolderTreeDto> Handle(GetWikiFolderTreeQuery request, CancellationToken cancellationToken)
    {
        // GetWikiPagesQueryHandler'daki AYNI güvenlik deseni: departman/admin
        // bilgisi istemciden DEĞİL, ICurrentUserAccessor'dan (JWT) geliyor.
        // Kendi departmanını (ya da Admin herhangi birini) gezen kullanıcı TAM
        // erişim alır, başka bir departmanı gezen ise Public'e budanmış bir
        // görünüm alır (bkz. WikiVisibilityRules ile aynı kural, sınıf bazında).
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;
        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var isFullAccess = viewerIsAdmin
            || (viewerDepartment is not null
                && string.Equals(viewerDepartment, request.DepartmentName, StringComparison.OrdinalIgnoreCase));

        var folders = await _wikiFolderRepository.GetByDepartmentAsync(request.DepartmentName, cancellationToken);
        var pages = await _wikiPageRepository.GetByDepartmentAsync(request.DepartmentName, cancellationToken);

        if (!isFullAccess)
        {
            pages = pages.Where(p => p.Visibility == WikiVisibility.Public).ToList();
        }

        var visibleFolders = isFullAccess ? folders : PruneToFoldersContainingVisiblePages(folders, pages);

        var pagesByFolderId = pages
            .Where(p => p.FolderId is not null)
            .ToLookup(p => p.FolderId!.Value);

        var childrenByParentId = visibleFolders
            .Where(f => f.ParentFolderId is not null)
            .ToLookup(f => f.ParentFolderId!.Value);

        WikiFolderNodeDto BuildNode(WikiFolder folder) => new(
            folder.Id,
            folder.Name,
            childrenByParentId[folder.Id].OrderBy(f => f.Name).Select(BuildNode).ToList(),
            pagesByFolderId[folder.Id].OrderByDescending(p => p.CreatedAtUtc).Select(ToPageSummary).ToList());

        var rootFolders = visibleFolders
            .Where(f => f.ParentFolderId is null)
            .OrderBy(f => f.Name)
            .Select(BuildNode)
            .ToList();

        var unfiledPages = pages
            .Where(p => p.FolderId is null)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(ToPageSummary)
            .ToList();

        return new WikiFolderTreeDto(rootFolders, unfiledPages);
    }

    // Başka bir departmanın ağacını gezen bir kullanıcı için: sadece görünür
    // (Public) en az bir sayfaya ulaşmak için gereken ata klasörleri tutuyoruz -
    // boş/kilitli klasör kabukları hiç görünmüyor. Her sayfanın klasör zincirini
    // köke kadar yürüyoruz; bir klasör zaten kümedeyse (daha önce başka bir
    // sayfa tarafından eklenmişse) o noktadan yukarısı ZATEN kümede demektir,
    // tekrar yürümeye gerek yok.
    private static List<WikiFolder> PruneToFoldersContainingVisiblePages(
        IReadOnlyList<WikiFolder> folders, IReadOnlyList<WikiPage> visiblePages)
    {
        var foldersById = folders.ToDictionary(f => f.Id);
        var neededFolderIds = new HashSet<Guid>();

        foreach (var page in visiblePages.Where(p => p.FolderId is not null))
        {
            var currentId = page.FolderId;
            while (currentId is not null && neededFolderIds.Add(currentId.Value))
            {
                currentId = foldersById.TryGetValue(currentId.Value, out var folder) ? folder.ParentFolderId : null;
            }
        }

        return folders.Where(f => neededFolderIds.Contains(f.Id)).ToList();
    }

    private static WikiPageSummaryDto ToPageSummary(WikiPage page) =>
        new(page.Id, page.Title, page.Visibility.ToString(), page.CreatedAtUtc);
}
