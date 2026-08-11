using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Pins.Queries;

// GetFavoritePagesQueryHandler'ın birebir aynısı - bkz. o dosyadaki not.
public class GetPinnedPagesQueryHandler : IRequestHandler<GetPinnedPagesQuery, IReadOnlyList<WikiPageDto>>
{
    private readonly IUserPagePinRepository _pinRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;

    public GetPinnedPagesQueryHandler(
        IUserPagePinRepository pinRepository, ISender sender, ICurrentUserAccessor currentUser)
    {
        _pinRepository = pinRepository;
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WikiPageDto>> Handle(GetPinnedPagesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return [];

        var pins = await _pinRepository.GetByUserAsync(_currentUser.UserId.Value, cancellationToken);
        if (pins.Count == 0)
            return [];

        var allPages = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);
        var pagesById = allPages.ToDictionary(p => p.Id);

        var viewerDepartment = _currentUser.Department;
        var viewerIsAdmin = _currentUser.IsAdmin;

        return pins
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => pagesById.GetValueOrDefault(p.WikiPageId))
            .Where(page => page is not null && IsVisibleTo(page, viewerDepartment, viewerIsAdmin))
            .Select(page => page!)
            .ToList();
    }

    private static bool IsVisibleTo(WikiPageDto page, string? viewerDepartmentName, bool viewerIsAdmin)
    {
        var visibility = Enum.Parse<WikiVisibility>(page.Visibility);
        return WikiVisibilityRules.IsVisibleTo(visibility, page.DepartmentName, viewerDepartmentName, viewerIsAdmin);
    }
}
