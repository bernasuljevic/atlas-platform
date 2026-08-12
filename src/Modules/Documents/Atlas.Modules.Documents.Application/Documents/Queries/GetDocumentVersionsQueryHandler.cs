using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

public class GetDocumentVersionsQueryHandler : IRequestHandler<GetDocumentVersionsQuery, IReadOnlyList<DocumentVersionDto>?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _documentVersionRepository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public GetDocumentVersionsQueryHandler(
        IDocumentRepository documentRepository, IDocumentVersionRepository documentVersionRepository,
        IWikiVisibilityChecker visibilityChecker, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _documentVersionRepository = documentVersionRepository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DocumentVersionDto>?> Handle(
        GetDocumentVersionsQuery request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return null;

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        if (!_visibilityChecker.IsVisibleTo(document.Visibility.ToString(), document.DepartmentName, viewerDepartment, viewerIsAdmin))
            return null;

        var versions = await _documentVersionRepository.GetByDocumentIdAsync(document.Id, cancellationToken);

        return versions
            .Select(v => new DocumentVersionDto(
                v.VersionNumber, v.OriginalFileName, v.ContentType, v.SizeBytes, v.CreatedByEmail, v.CreatedAtUtc))
            .ToList();
    }
}
