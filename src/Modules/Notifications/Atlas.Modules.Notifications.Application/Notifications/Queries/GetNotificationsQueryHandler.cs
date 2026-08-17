using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Notifications.Application.Notifications.Queries;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationEntryDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IWikiVisibilityChecker _visibilityChecker;

    public GetNotificationsQueryHandler(
        INotificationRepository notificationRepository, ICurrentUserAccessor currentUser,
        IWikiVisibilityChecker visibilityChecker)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _visibilityChecker = visibilityChecker;
    }

    public async Task<IReadOnlyList<NotificationEntryDto>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var viewerUserId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;

        // GÜVENLİK (Ders #10 sınıfından bir hatayı BAŞTAN önlemek için): AI
        // aramasıyla AYNI desen - ham kayıtları FİLTRESİZ çekip, HER birini
        // istek sahibinin departmanına/Admin durumuna göre süzüyoruz. "take*4"
        // kadar aday çekiliyor çünkü filtreden sonra bazıları elenecek -
        // SearchByMeaningQueryHandler'daki AYNI "yeterince aday çek" gerekçesi.
        // GetRecentAsync artık AYRICA TargetUserId'yi de DB seviyesinde
        // filtreliyor (broadcast + bana hedeflenmiş) - bkz. repository.
        var candidates = await _notificationRepository.GetRecentAsync(viewerUserId, request.Take * 4, cancellationToken);

        var visible = VisibleNotificationsHelper
            .FilterVisible(candidates, _visibilityChecker, _currentUser.Department, _currentUser.IsAdmin)
            .Take(request.Take)
            .ToList();

        // Okuma durumu (2026-08-17) - giriş yapmamış bir istek için (viewerUserId
        // null) her şey "okunmamış" sayılıyor, ama bu endpoint zaten
        // RequireAuthorization() ile korunuyor (bkz. NotificationsEndpoints),
        // bu dal pratikte hiç tetiklenmiyor - sadece savunma amaçlı.
        var readIds = viewerUserId is null
            ? new HashSet<Guid>()
            : await _notificationRepository.GetReadNotificationIdsAsync(
                viewerUserId.Value, visible.Select(n => n.Id).ToList(), cancellationToken);

        return visible
            .Select(n => new NotificationEntryDto(
                n.Id, n.EventType, n.ResourceId, n.Title, n.DepartmentName, n.Visibility, n.ActorEmail,
                n.CreatedAtUtc, readIds.Contains(n.Id)))
            .ToList();
    }
}
