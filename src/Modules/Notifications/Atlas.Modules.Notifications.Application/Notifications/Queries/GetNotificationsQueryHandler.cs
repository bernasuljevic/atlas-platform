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
        // GÜVENLİK (Ders #10 sınıfından bir hatayı BAŞTAN önlemek için): AI
        // aramasıyla AYNI desen - ham kayıtları FİLTRESİZ çekip, HER birini
        // istek sahibinin departmanına/Admin durumuna göre süzüyoruz. "take*4"
        // kadar aday çekiliyor çünkü filtreden sonra bazıları elenecek -
        // SearchByMeaningQueryHandler'daki AYNI "yeterince aday çek" gerekçesi.
        var candidates = await _notificationRepository.GetRecentAsync(request.Take * 4, cancellationToken);

        return candidates
            .Where(n => _visibilityChecker.IsVisibleTo(
                n.Visibility, n.DepartmentName, _currentUser.Department, _currentUser.IsAdmin))
            .Take(request.Take)
            .Select(n => new NotificationEntryDto(
                n.Id, n.EventType, n.ResourceId, n.Title, n.DepartmentName, n.Visibility, n.ActorEmail, n.CreatedAtUtc))
            .ToList();
    }
}
