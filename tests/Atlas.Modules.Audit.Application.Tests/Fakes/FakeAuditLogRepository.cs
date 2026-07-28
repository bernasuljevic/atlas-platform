using Atlas.Modules.Audit.Application.Abstractions;
using Atlas.Modules.Audit.Application.AuditLog;

namespace Atlas.Modules.Audit.Application.Tests.Fakes;

// Gerçek DB'ye hiç dokunmadan Handler'ı izole test edebilmek için - sadece
// Handler'ın repository'e HANGİ değerleri geçirdiğini kaydediyor, gerçek bir
// filtreleme/sayfalama mantığı taşımıyor (bu EfAuditLogRepository'nin işi,
// integration test seviyesinde doğrulanıyor).
public class FakeAuditLogRepository : IAuditLogRepository
{
    public string? LastAction { get; private set; }
    public DateTime? LastFromUtc { get; private set; }
    public DateTime? LastToUtc { get; private set; }
    public int LastPageNumber { get; private set; }
    public int LastPageSize { get; private set; }

    public Task<PagedResult<AuditLogEntryDto>> GetPagedAsync(
        string? action, DateTime? fromUtc, DateTime? toUtc, int pageNumber, int pageSize,
        CancellationToken cancellationToken)
    {
        LastAction = action;
        LastFromUtc = fromUtc;
        LastToUtc = toUtc;
        LastPageNumber = pageNumber;
        LastPageSize = pageSize;

        return Task.FromResult(new PagedResult<AuditLogEntryDto>([], pageNumber, pageSize, 0));
    }
}
