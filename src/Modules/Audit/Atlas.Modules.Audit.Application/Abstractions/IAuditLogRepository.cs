using Atlas.Modules.Audit.Application.AuditLog;

namespace Atlas.Modules.Audit.Application.Abstractions;

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLogEntryDto>> GetPagedAsync(
        string? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
