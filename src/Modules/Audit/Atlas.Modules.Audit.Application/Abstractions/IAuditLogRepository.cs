using Atlas.Modules.Audit.Application.AuditLog;

namespace Atlas.Modules.Audit.Application.Abstractions;

public interface IAuditLogRepository
{
    // "details" kısmi eşleşme (Contains) ile aranıyor - tam eşleşme değil,
    // "bu başlığı içeren kayıtları göster" senaryosu için.
    Task<PagedResult<AuditLogEntryDto>> GetPagedAsync(
        string? details,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
