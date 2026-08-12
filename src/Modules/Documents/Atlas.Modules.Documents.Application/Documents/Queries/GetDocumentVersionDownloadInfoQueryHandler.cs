using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

public class GetDocumentVersionDownloadInfoQueryHandler
    : IRequestHandler<GetDocumentVersionDownloadInfoQuery, DocumentDownloadInfoDto?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _documentVersionRepository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public GetDocumentVersionDownloadInfoQueryHandler(
        IDocumentRepository documentRepository, IDocumentVersionRepository documentVersionRepository,
        IWikiVisibilityChecker visibilityChecker, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _documentVersionRepository = documentVersionRepository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<DocumentDownloadInfoDto?> Handle(
        GetDocumentVersionDownloadInfoQuery request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return null;

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        if (!_visibilityChecker.IsVisibleTo(document.Visibility.ToString(), document.DepartmentName, viewerDepartment, viewerIsAdmin))
            return null;

        var version = await _documentVersionRepository.GetByDocumentIdAndVersionNumberAsync(
            document.Id, request.VersionNumber, cancellationToken);
        if (version is null)
            return null;

        return new DocumentDownloadInfoDto(version.StorageKey, version.ContentType, version.OriginalFileName);
    }
}
