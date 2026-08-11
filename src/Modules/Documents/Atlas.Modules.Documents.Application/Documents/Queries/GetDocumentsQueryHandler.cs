using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Application.Documents;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, PagedResult<DocumentDto>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public GetDocumentsQueryHandler(
        IDocumentRepository documentRepository, IWikiVisibilityChecker visibilityChecker, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);

        // GetWikiPagesQueryHandler'daki AYNI desen: departman JWT'den (imzalı
        // claim), istemciden gelen HİÇBİR değere güvenilmiyor. Anonim bir
        // ziyaretçi için effectiveDepartment null olur - sadece Public belgeler görünür.
        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        var visibleDocuments = allDocuments
            .Where(d => _visibilityChecker.IsVisibleTo(d.Visibility.ToString(), d.DepartmentName, viewerDepartment, viewerIsAdmin))
            .Select(d => d.ToDto());

        // DepartmentName/Status - görünürlük filtresinin YERİNE değil, ONUN
        // ÜZERİNE ek bir daraltma (ör. "sadece IT'nin belgelerini göster" gibi
        // bir Document Library filtresi - bir kullanıcı zaten görebildiği
        // belgeler arasında filtreliyor, başka departmanın belgesini bu yolla
        // GÖREMEZ).
        if (!string.IsNullOrWhiteSpace(request.DepartmentName))
            visibleDocuments = visibleDocuments.Where(d =>
                string.Equals(d.DepartmentName, request.DepartmentName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Status))
            visibleDocuments = visibleDocuments.Where(d =>
                string.Equals(d.Status, request.Status, StringComparison.OrdinalIgnoreCase));

        var orderedDocuments = visibleDocuments.OrderByDescending(d => d.CreatedAtUtc).ToList();

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var pageItems = orderedDocuments
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<DocumentDto>(pageItems, pageNumber, pageSize, orderedDocuments.Count);
    }
}
