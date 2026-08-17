using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Modules.Notifications.Domain.Entities;
using Atlas.Shared.Contracts;

namespace Atlas.Modules.Notifications.Application.Notifications;

// GetNotificationsQueryHandler/GetUnreadNotificationCountQueryHandler/
// MarkAllNotificationsReadCommandHandler'ın ÜÇÜ de "adayları çek, departman/
// görünürlük kuralına göre süz" işini yapıyordu (2026-08-17) - Ders #21'in
// AYNI "bir kuralı sadece bir yerde değiştirip diğer tüketicilerini unutmak"
// sınıfından bir riski BAŞTAN önlemek için TEK bir yere çıkarıldı.
public static class VisibleNotificationsHelper
{
    // Departman/Admin filtresinden SONRA "take" kadar geri kalması BEKLENEN
    // bir aday havuzu - GetNotificationsQueryHandler'daki "take*4" gerekçesiyle
    // AYNI, ama okunmamış SAYISI/hepsini-okundu-işaretle gibi "tüm son
    // bildirimleri" kapsaması gereken işlemler için daha geniş, sabit bir
    // pencere kullanılıyor.
    public const int UnboundedCandidateWindow = 100;

    public static IReadOnlyList<NotificationEntry> FilterVisible(
        IReadOnlyList<NotificationEntry> candidates, IWikiVisibilityChecker visibilityChecker,
        string? viewerDepartment, bool viewerIsAdmin)
    {
        return candidates
            .Where(n => visibilityChecker.IsVisibleTo(n.Visibility, n.DepartmentName, viewerDepartment, viewerIsAdmin))
            .ToList();
    }
}
