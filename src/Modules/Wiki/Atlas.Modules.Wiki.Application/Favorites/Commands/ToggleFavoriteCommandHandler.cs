using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Favorites.Commands;

public class ToggleFavoriteCommandHandler : IRequestHandler<ToggleFavoriteCommand, bool>
{
    private readonly IUserPageFavoriteRepository _favoriteRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleFavoriteCommandHandler(
        IUserPageFavoriteRepository favoriteRepository,
        ISender sender,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _favoriteRepository = favoriteRepository;
        _sender = sender;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Favorilere eklemek için giriş yapmış olmalısınız.");

        // CreateCommentCommandHandler'daki AYNI gerekçe: bir sayfayı favoriye
        // ekleyebilmek o sayfayı GÖREBİLMEYİ gerektiriyor - aksi halde
        // erişilemeyen ("Sadece Departman") bir sayfanın ID'si tahmin edilip
        // favoriye eklenmeye çalışılarak sayfanın VARLIĞI sızdırılabilirdi.
        await EnsurePageIsVisibleAsync(request.WikiPageId, cancellationToken);

        var userId = _currentUser.UserId.Value;
        var existing = await _favoriteRepository.GetAsync(userId, request.WikiPageId, cancellationToken);

        if (existing is not null)
        {
            await _favoriteRepository.RemoveAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        var favorite = UserPageFavorite.Create(userId, request.WikiPageId);
        await _favoriteRepository.AddAsync(favorite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsurePageIsVisibleAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var allPages = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);
        var page = allPages.FirstOrDefault(p => p.Id == pageId);

        var isVisible = page is not null && WikiVisibilityRules.IsVisibleTo(
            Enum.Parse<WikiVisibility>(page.Visibility), page.DepartmentName, _currentUser.Department, _currentUser.IsAdmin);

        if (!isVisible)
            throw new ArgumentException("Sayfa bulunamadı.", nameof(pageId));
    }
}
