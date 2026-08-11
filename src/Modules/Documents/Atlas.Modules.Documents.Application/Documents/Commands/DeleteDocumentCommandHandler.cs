using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserAccessor _currentUser;

    public DeleteDocumentCommandHandler(
        IDocumentRepository documentRepository, IFileStorageService fileStorageService, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Belge silmek için giriş yapmış olmalısınız.");

        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (document is null)
            throw new ArgumentException("Belge bulunamadı.", nameof(request.Id));

        var isOwner = document.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu belgeyi silme yetkiniz yok.");

        request.AuditDetails = document.Title;

        // Disk'teki dosya da temizleniyor - aksi halde DB satırı silinse bile
        // diskte kalıcı bir "hayalet" dosya birikirdi (Wiki'nin sildiği bir
        // sayfanın embedding'lerini de temizlemesiyle AYNI gerekçe - bkz.
        // WikiPageDeletedEvent).
        await _fileStorageService.DeleteAsync(document.StorageKey, cancellationToken);
        await _documentRepository.DeleteAsync(document, cancellationToken);
    }
}
