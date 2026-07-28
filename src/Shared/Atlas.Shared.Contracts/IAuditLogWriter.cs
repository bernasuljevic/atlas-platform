namespace Atlas.Shared.Contracts;

/// <summary>
/// Audit modülünün DIŞARIYA açtığı TEK kapı - `Atlas.Shared.CQRS.Behaviors.
/// AuditBehavior` (Wiki gibi denetlenebilir komutları çalıştıran her modülün
/// pipeline'ına eklenen genel bir davranış) bunu çağırıyor, gerçek
/// implementasyonu (EfAuditLogWriter, Audit.Infrastructure'da) hiç bilmiyor.
/// Aynı desen: ICurrentUserAccessor/IWikiVisibilityChecker.
/// </summary>
public interface IAuditLogWriter
{
    Task LogAsync(string action, string? resourceId, string? details, CancellationToken cancellationToken);
}
