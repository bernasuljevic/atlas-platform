using MediatR;

namespace Atlas.Modules.Notifications.Application.Notifications.Queries;

/// <summary>
/// "Diğerlerinin yazdıkları" / bildirim geçmişi (kullanıcı isteği, 2026-08-15,
/// Medium'un sağ sütunundaki "Staff Picks" benzeri bir akış referans alındı).
/// Take BİLEREK küçük bir varsayılana (10) sahip - bu bir sayfalanan tam liste
/// DEĞİL, ana sayfanın sağ sütunundaki KISA bir özet akışı.
/// </summary>
public record GetNotificationsQuery(int Take = 10) : IRequest<IReadOnlyList<NotificationEntryDto>>;
