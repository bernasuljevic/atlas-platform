using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Pins.Commands;

// ToggleFavoriteCommandHandler'ın birebir aynısı, sadece UserPagePin/
// IUserPagePinRepository üzerinden - iki ayrı, bağımsız toggle edilebilen
// tablo olduğu için (bkz. UserPagePin.cs'teki not) kod tekrarı bilinçli.
public class TogglePinCommandHandler : IRequestHandler<TogglePinCommand, bool>
{
    private readonly IUserPagePinRepository _pinRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public TogglePinCommandHandler(
        IUserPagePinRepository pinRepository,
        ISender sender,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _pinRepository = pinRepository;
        _sender = sender;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(TogglePinCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Sabitlemek için giriş yapmış olmalısınız.");

        await EnsurePageIsVisibleAsync(request.WikiPageId, cancellationToken);

        var userId = _currentUser.UserId.Value;
        var existing = await _pinRepository.GetAsync(userId, request.WikiPageId, cancellationToken);

        if (existing is not null)
        {
            await _pinRepository.RemoveAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        var pin = UserPagePin.Create(userId, request.WikiPageId);
        await _pinRepository.AddAsync(pin, cancellationToken);
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
