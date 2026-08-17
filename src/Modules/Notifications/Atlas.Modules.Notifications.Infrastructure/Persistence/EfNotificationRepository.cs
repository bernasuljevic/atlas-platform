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

    public async Task<IReadOnlyList<NotificationEntry>> GetRecentAsync(
        Guid? viewerUserId, int take, CancellationToken ct = default)
    {
        return await _dbContext.NotificationEntries
            .AsNoTracking()
            .Where(n => n.TargetUserId == null || n.TargetUserId == viewerUserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);
    }

    // ExecuteDeleteAsync BILEREK kullanilmiyor - Ders #22 (EF Core InMemory
    // saglayicisi bulk Execute* API'lerini desteklemiyor). Bu DbContext su an
    // integration testlerde InMemory'e cevrilmiyor ama Wiki/Vault'un ayni
    // temizlik metotlarindaki AYNI guvenli deseni (ToListAsync + RemoveRange)
    // burada da tutarlilik icin koruyoruz.
    public async Task DeleteAllForResourceAsync(Guid resourceId, CancellationToken ct = default)
    {
        var entries = await _dbContext.NotificationEntries
            .Where(n => n.ResourceId == resourceId)
            .ToListAsync(ct);

        // Yetim NotificationRead satırları da temizleniyor (2026-08-17) -
        // AYNI "sayfa silinince ilişkili satırları da temizle" dersi, bu sefer
        // NotificationEntry -> NotificationRead ilişkisine uygulanıyor.
        var entryIds = entries.Select(e => e.Id).ToList();
        var reads = await _dbContext.NotificationReads
            .Where(r => entryIds.Contains(r.NotificationEntryId))
            .ToListAsync(ct);
        _dbContext.NotificationReads.RemoveRange(reads);

        _dbContext.NotificationEntries.RemoveRange(entries);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlySet<Guid>> GetReadNotificationIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken ct = default)
    {
        if (notificationIds.Count == 0)
            return new HashSet<Guid>();

        var readIds = await _dbContext.NotificationReads
            .AsNoTracking()
            .Where(r => r.UserId == userId && notificationIds.Contains(r.NotificationEntryId))
            .Select(r => r.NotificationEntryId)
            .ToListAsync(ct);

        return readIds.ToHashSet();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var alreadyRead = await _dbContext.NotificationReads
            .AnyAsync(r => r.NotificationEntryId == notificationId && r.UserId == userId, ct);
        if (alreadyRead)
            return;

        await _dbContext.NotificationReads.AddAsync(NotificationRead.Create(notificationId, userId), ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken ct = default)
    {
        if (notificationIds.Count == 0)
            return;

        var alreadyRead = await _dbContext.NotificationReads
            .Where(r => r.UserId == userId && notificationIds.Contains(r.NotificationEntryId))
            .Select(r => r.NotificationEntryId)
            .ToListAsync(ct);
        var alreadyReadSet = alreadyRead.ToHashSet();

        var newReads = notificationIds
            .Where(id => !alreadyReadSet.Contains(id))
            .Select(id => NotificationRead.Create(id, userId))
            .ToList();

        if (newReads.Count == 0)
            return;

        await _dbContext.NotificationReads.AddRangeAsync(newReads, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}
