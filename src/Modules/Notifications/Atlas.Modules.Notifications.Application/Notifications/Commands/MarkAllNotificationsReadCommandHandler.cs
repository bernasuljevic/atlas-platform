using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Notifications.Application.Notifications.Commands;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IWikiVisibilityChecker _visibilityChecker;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notificationRepository, ICurrentUserAccessor currentUser,
        IWikiVisibilityChecker visibilityChecker)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _visibilityChecker = visibilityChecker;
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Bildirimleri okundu işaretlemek için giriş yapmış olmalısınız.");

        var candidates = await _notificationRepository.GetRecentAsync(
            _currentUser.UserId, VisibleNotificationsHelper.UnboundedCandidateWindow, cancellationToken);

        var visibleIds = VisibleNotificationsHelper
            .FilterVisible(candidates, _visibilityChecker, _currentUser.Department, _currentUser.IsAdmin)
            .Select(n => n.Id)
            .ToList();

        await _notificationRepository.MarkAllAsReadAsync(_currentUser.UserId.Value, visibleIds, cancellationToken);
    }
}
