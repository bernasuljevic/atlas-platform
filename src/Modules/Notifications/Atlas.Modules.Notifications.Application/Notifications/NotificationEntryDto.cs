namespace Atlas.Modules.Notifications.Application.Notifications;

public record NotificationEntryDto(
    Guid Id, string EventType, Guid ResourceId, string Title, string DepartmentName, string Visibility,
    string? ActorEmail, DateTime CreatedAtUtc, bool IsRead);
