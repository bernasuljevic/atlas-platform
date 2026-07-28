using Atlas.Modules.Audit.Application.Abstractions;
using Atlas.Modules.Audit.Application.AuditLog;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Audit.Infrastructure.Persistence;

public class EfAuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _context;

    public EfAuditLogRepository(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AuditLogEntryDto>> GetPagedAsync(
        string? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(e => e.Action == action);

        if (fromUtc is not null)
            query = query.Where(e => e.OccurredAtUtc >= fromUtc.Value);

        if (toUtc is not null)
            query = query.Where(e => e.OccurredAtUtc <= toUtc.Value);

        // Sayı ve sayfa AYRI sorgular - filtrelenmiş satırların TAMAMINI belleğe
        // çekmeden toplam sayıyı öğrenmek için (tablo büyüdükçe önemli).
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditLogEntryDto(e.Id, e.UserId, e.UserEmail, e.Action, e.ResourceId, e.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogEntryDto>(items, pageNumber, pageSize, totalCount);
    }
}
