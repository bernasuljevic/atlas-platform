using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Notifications.Application.Notifications.Queries;

public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IWikiVisibilityChecker _visibilityChecker;

    public GetUnreadNotificationCountQueryHandler(
        INotificationRepository notificationRepository, ICurrentUserAccessor currentUser,
        IWikiVisibilityChecker visibilityChecker)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _visibilityChecker = visibilityChecker;
    }

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return 0;

        var candidates = await _notificationRepository.GetRecentAsync(
            _currentUser.UserId, VisibleNotificationsHelper.UnboundedCandidateWindow, cancellationToken);

        var visible = VisibleNotificationsHelper.FilterVisible(
            candidates, _visibilityChecker, _currentUser.Department, _currentUser.IsAdmin);

        var readIds = await _notificationRepository.GetReadNotificationIdsAsync(
            _currentUser.UserId.Value, visible.Select(n => n.Id).ToList(), cancellationToken);

        return visible.Count(n => !readIds.Contains(n.Id));
    }
}
