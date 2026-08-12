using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _documentVersionRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserAccessor _currentUser;

    public DeleteDocumentCommandHandler(
        IDocumentRepository documentRepository, IDocumentVersionRepository documentVersionRepository,
        IFileStorageService fileStorageService, IOutboxWriter outboxWriter, IUnitOfWork unitOfWork,
        ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _documentVersionRepository = documentVersionRepository;
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
        // DocumentDeletedEvent ise AI'ın embedding'leri temizlemesi İÇİN -
        // Wiki'nin WikiPageDeletedEvent'iyle AYNI gerekçe, Outbox'a yazılması
        // Document'in KENDİSİYLE atomik olmalı.
        await _fileStorageService.DeleteAsync(document.StorageKey, cancellationToken);

        // P6 (versiyonlama): güncel dosyanın YANINDA, geçmişteki HER versiyonun
        // da kendi diskteki dosyası var (bkz. DocumentVersion) - onlar da
        // temizlenmezse "yetim" dosyalar olarak sonsuza kadar diskte kalırlardı.
        // Önce dosyaları sil (mevcut StorageKey temizliğiyle AYNI best-effort/
        // idempotent gerekçe), SONRA satırları toplu sil - AI'ın embedding
        // temizliğinden FARKLI olarak bu satır silme işlemi Outbox üzerinden
        // DEĞİL, burada doğrudan yapılıyor çünkü versiyon geçmişi tamamen
        // Documents modülünün kendi iç verisi (başka bir modül dinlemiyor).
        var versions = await _documentVersionRepository.GetByDocumentIdAsync(document.Id, cancellationToken);
        foreach (var version in versions)
        {
            await _fileStorageService.DeleteAsync(version.StorageKey, cancellationToken);
        }
        await _documentVersionRepository.DeleteAllForDocumentAsync(document.Id, cancellationToken);

        _outboxWriter.Enqueue(new DocumentDeletedEvent(document.Id));

        await _documentRepository.DeleteAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
