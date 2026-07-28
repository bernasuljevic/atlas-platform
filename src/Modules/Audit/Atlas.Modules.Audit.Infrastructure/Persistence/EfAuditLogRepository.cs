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
        string? details,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogEntries.AsQueryable();

        // Tam eşleşme değil, KISMİ eşleşme (Contains) - "bu başlığı içeren
        // kayıtları göster" senaryosu için (bkz. GetAuditLogQuery'deki not).
        if (!string.IsNullOrWhiteSpace(details))
            query = query.Where(e => e.Details != null && e.Details.Contains(details));

        if (fromUtc is not null)
            query = query.Where(e => e.OccurredAtUtc >= fromUtc.Value);

        if (toUtc is not null)
        {
            // BULUNAN GERÇEK BUG (2026-07-28): frontend'deki <input type="date">
            // saat bilgisi olmadan (00:00:00) bir DateTime gönderiyor - "<= toUtc"
            // ile karşılaştırınca o günün SADECE gece yarısı anını kapsıyordu,
            // o gün içindeki (ör. 15:51) hiçbir kayıt eşleşmiyordu (canlı
            // doğrulandı - "Bitiş: bugün" seçilince her zaman "Kayıt bulunamadı"
            // dönüyordu). "Bitiş" tarihi seçildiğinde kullanıcı o günün TAMAMINI
            // kastediyor - bir sonraki günün başlangıcına kadar (hariç) genişletiyoruz.
            var exclusiveUpperBound = toUtc.Value.Date.AddDays(1);
            query = query.Where(e => e.OccurredAtUtc < exclusiveUpperBound);
        }

        // Sayı ve sayfa AYRI sorgular - filtrelenmiş satırların TAMAMINI belleğe
        // çekmeden toplam sayıyı öğrenmek için (tablo büyüdükçe önemli).
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditLogEntryDto(e.Id, e.UserId, e.UserEmail, e.Action, e.ResourceId, e.Details, e.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogEntryDto>(items, pageNumber, pageSize, totalCount);
    }
}
