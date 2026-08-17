using MediatR;

namespace Atlas.Modules.Notifications.Application.Notifications.Queries;

// Header'daki zil ikonundaki badge için (2026-08-17) - GetNotificationsQuery'nin
// (Take=10 ile SINIRLI) aksine, VisibleNotificationsHelper.UnboundedCandidateWindow
// kadar geniş bir pencerede sayıyor - "10'dan fazla okunmamış bildirimin varsa
// badge hâlâ doğru sayıyı göstersin" beklentisi.
public record GetUnreadNotificationCountQuery : IRequest<int>;
