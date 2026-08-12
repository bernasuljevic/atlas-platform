using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserAccessor _currentUser;

    public DeleteDocumentCommandHandler(
        IDocumentRepository documentRepository, IFileStorageService fileStorageService,
        IOutboxWriter outboxWriter, IUnitOfWork unitOfWork, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
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

        // Disk'teki dosya HEMEN (senkron) temizleniyor - Outbox'ı beklemiyor,
        // çünkü dosya silme kendi başına atomicity gerektirmeyen, tekrar
        // denenebilir (idempotent - StorageKey zaten yoksa no-op) bir işlem.
        // DocumentDeletedEvent ise AI'ın (Gün 5) embedding'leri temizlemesi
        // İÇİN - Wiki'nin WikiPageDeletedEvent'iyle AYNI gerekçe, henüz kimse
        // dinlemiyor ama Outbox'a yazılması Document'in KENDİSİYLE atomik olmalı.
        await _fileStorageService.DeleteAsync(document.StorageKey, cancellationToken);

        _outboxWriter.Enqueue(new DocumentDeletedEvent(document.Id));

        await _documentRepository.DeleteAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
