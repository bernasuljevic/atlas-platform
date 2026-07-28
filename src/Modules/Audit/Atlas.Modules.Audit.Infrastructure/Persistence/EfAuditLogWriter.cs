using Atlas.Modules.Audit.Domain.Entities;
using Atlas.Shared.Contracts;

namespace Atlas.Modules.Audit.Infrastructure.Persistence;

/// <summary>
/// IAuditLogWriter'ın (Shared.Contracts) tek gerçek implementasyonu.
/// ICurrentUserAccessor'dan "kim" bilgisini kendisi çekiyor - AuditBehavior'ın
/// (Shared.CQRS) bunu bilmesine gerek yok, sadece "bu eylemi denetle" diyor.
/// KENDİ SaveChanges'ini çağırıyor (Wiki'nin transaction'ıyla atomik DEĞİL,
/// bilerek - audit yazımı best-effort bir yan etki, ana işlemi (zaten
/// tamamlanmış Handler) etkilememeli, bkz. AuditBehavior'daki try/catch).
/// </summary>
public class EfAuditLogWriter : IAuditLogWriter
{
    private readonly AuditDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public EfAuditLogWriter(AuditDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string? resourceId, string? details, CancellationToken cancellationToken)
    {
        var entry = AuditLogEntry.Create(_currentUser.UserId, _currentUser.Email, action, resourceId, details);

        _context.AuditLogEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
