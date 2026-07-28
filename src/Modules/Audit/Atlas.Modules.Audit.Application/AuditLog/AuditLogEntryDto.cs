namespace Atlas.Modules.Audit.Application.AuditLog;

public record AuditLogEntryDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string? ResourceId,
    string? Details,
    DateTime OccurredAtUtc);
