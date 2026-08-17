using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Notifications.Domain.Entities;

/// <summary>
/// Bir bildirimin BELİRLİ bir kullanıcı tarafından okunduğunu işaretliyor
/// (2026-08-17, "gerçek bildirim sistemi" isteği). PasswordEntryShare'deki
/// AYNI desen - NotificationEntry'ye FK İLE BAĞLI DEĞİL (bu projede FK'ler
/// sadece Wiki'nin cross-module ham-SQL migration'ındaki istisnai durumda
/// var), (NotificationEntryId, UserId) composite unique index.
///
/// AYRI bir tablo olmasının gerekçesi: NotificationEntry PAYLAŞILAN
/// (broadcast) bir kayıt - AYNI bildirimi 10 farklı kullanıcı görebiliyor,
/// her biri KENDİ okuma durumunu bağımsız tutmalı. Okundu bilgisini
/// NotificationEntry'nin ÜZERİNE (tek bir IsRead bool) eklemek YANLIŞ
/// olurdu - "kimin okuduğu" sorusuna cevap veremezdi.
/// </summary>
public class NotificationRead : Entity<Guid>
{
    public Guid NotificationEntryId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime ReadAtUtc { get; private set; }

    private NotificationRead() { }

    private NotificationRead(Guid id, Guid notificationEntryId, Guid userId, DateTime readAtUtc) : base(id)
    {
        NotificationEntryId = notificationEntryId;
        UserId = userId;
        ReadAtUtc = readAtUtc;
    }

    public static NotificationRead Create(Guid notificationEntryId, Guid userId)
    {
        if (notificationEntryId == Guid.Empty)
            throw new ArgumentException("NotificationEntryId boş olamaz.", nameof(notificationEntryId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId boş olamaz.", nameof(userId));

        return new NotificationRead(Guid.NewGuid(), notificationEntryId, userId, DateTime.UtcNow);
    }
}
