using MediatR;

namespace Atlas.Shared.Contracts;

/// <summary>
/// Wiki modülü yeni bir yorum oluşturulduğunda bu event'i yayınlar (2026-08-17,
/// "gerçek bildirim sistemi" isteği - "tartışmaya cevap geldiğinde bildirim
/// oluşsun"). Notifications modülü buna abone olup İLGİLİ kullanıcılara
/// (sayfa sahibi + önceki yorumcular) HEDEFLENMİŞ bir bildirim yazıyor.
///
/// RecipientUserIds - WikiPageCreatedEvent'in "Content" alanıyla AYNI gerekçe:
/// Notifications, Wiki'nin Comments tablosuna geri sorgu ATAMAZ (modüller
/// arası izolasyon kuralı) - "kimin bu tartışmaya daha önce katıldığı" sorusu
/// SADECE Wiki'nin (IWikiCommentRepository'ye erişimi olan) bilebileceği bir
/// şey, o yüzden CreateCommentCommandHandler bu listeyi ÖNCEDEN hesaplayıp
/// event'in içine koyuyor - Notifications sadece "bu kullanıcılara birer
/// bildirim yaz" der, KİME yazacağını kendisi bulmaz.
///
/// PageTitle/DepartmentName/Visibility - platform-geneli bir yorum (PageId
/// null) için DepartmentName/Visibility anlamsız - Visibility="Public" ile
/// dolduruluyor (WikiVisibilityRules.IsVisibleTo Public için departmanı hiç
/// sorgulamıyor, bkz. o metottaki kısa devre), bu yüzden DepartmentName için
/// herhangi bir yer tutucu değer (bkz. Handler) güvenli.
/// </summary>
public record WikiCommentCreatedEvent(
    Guid CommentId, Guid? PageId, string? PageTitle, string ContentExcerpt,
    Guid AuthorUserId, string? AuthorEmail, string DepartmentName, string Visibility,
    IReadOnlyList<Guid> RecipientUserIds, DateTime CreatedAtUtc) : INotification;
