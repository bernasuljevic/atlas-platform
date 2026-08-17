using MediatR;

namespace Atlas.Modules.Notifications.Application.Notifications.Commands;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest;
