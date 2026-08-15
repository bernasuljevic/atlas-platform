using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Notifications.Infrastructure.Persistence;

public class EfNotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _dbContext;

    public EfNotificationRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationEntry entry, CancellationToken ct = default)
    {
        await _dbContext.NotificationEntries.AddAsync(entry, ct);
        // Diğer modüllerin repository'lerinin AKSİNE (Outbox Pattern'e geçenler
        // SaveChanges'i çağırmıyor, IUnitOfWork'e bırakıyor) - burada bilerek
        // KENDİ SaveChanges'ini çağırıyor, tıpkı Vault'un P3-öncesi basit
        // deseni gibi. Gerekçe: bu yazma, WikiPageCreatedEventHandler'ın
        // KENDİ best-effort try/catch'i içinde, Wiki'nin KENDİ atomik
        // yazımından (WikiPage + Outbox mesajı) TAMAMEN AYRI bir veritabanına
        // (Notifications'ın kendi şeması, farklı bir DbContext) gidiyor - iki
        // ayrı DbContext zaten TEK bir transaction'a giremez, o yüzden
        // "atomik yaz, UnitOfWork'e bırak" deseninin burada hiçbir faydası yok.
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationEntry>> GetRecentAsync(int take, CancellationToken ct = default)
    {
        return await _dbContext.NotificationEntries
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
