using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

public class GetDocumentDownloadInfoQueryHandler : IRequestHandler<GetDocumentDownloadInfoQuery, DocumentDownloadInfoDto?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public GetDocumentDownloadInfoQueryHandler(
        IDocumentRepository documentRepository, IWikiVisibilityChecker visibilityChecker, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<DocumentDownloadInfoDto?> Handle(GetDocumentDownloadInfoQuery request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (document is null)
            return null;

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        if (!_visibilityChecker.IsVisibleTo(document.Visibility.ToString(), document.DepartmentName, viewerDepartment, viewerIsAdmin))
            return null;

        return new DocumentDownloadInfoDto(document.StorageKey, document.ContentType, document.OriginalFileName);
    }
}
