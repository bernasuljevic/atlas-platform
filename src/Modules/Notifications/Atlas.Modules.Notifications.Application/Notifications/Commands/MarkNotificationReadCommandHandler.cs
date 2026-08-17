using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Notifications.Application.Notifications.Commands;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notificationRepository, ICurrentUserAccessor currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Bir bildirimi okundu işaretlemek için giriş yapmış olmalısınız.");

        await _notificationRepository.MarkAsReadAsync(request.NotificationId, _currentUser.UserId.Value, cancellationToken);
    }
}
