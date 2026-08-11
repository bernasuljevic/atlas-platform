using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Application.Documents;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public GetDocumentByIdQueryHandler(
        IDocumentRepository documentRepository, IWikiVisibilityChecker visibilityChecker, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (document is null)
            return null;

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        // GetWikiPageByIdQueryHandler'daki AYNI desen - Id'yi bilmek görebilmek
        // anlamına gelmiyor. Görünür değilse kayıt HİÇ VARMIŞ GİBİ davranılıyor
        // (null -> endpoint 404 döner), 403 DEĞİL.
        if (!_visibilityChecker.IsVisibleTo(document.Visibility.ToString(), document.DepartmentName, viewerDepartment, viewerIsAdmin))
            return null;

        return document.ToDto();
    }
}
