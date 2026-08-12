using System.Security.Cryptography;
using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class UploadNewDocumentVersionCommandHandler : IRequestHandler<UploadNewDocumentVersionCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _documentVersionRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserAccessor _currentUser;

    public UploadNewDocumentVersionCommandHandler(
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

    public async Task Handle(UploadNewDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Yeni versiyon yüklemek için giriş yapmış olmalısınız.");

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            throw new ArgumentException("Belge bulunamadı.", nameof(request.DocumentId));

        // Delete/Update/Reprocess ile AYNI owner-or-admin deseni.
        var isOwner = document.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu belgeye yeni versiyon yükleme yetkiniz yok.");

        // ReprocessDocumentCommandHandler'daki AYNI "çift tıklama" savunması -
        // belge hâlâ (önceki versiyonun) işlenmesi devam ederken üzerine
        // yazmak, DocumentUploadedEventHandler'ın hangi versiyonu işlediğini
        // belirsizleştirirdi.
        if (document.Status == DocumentStatus.Extracting)
            throw new ArgumentException("Bu belge şu anda işleniyor, yeni versiyon şimdi yüklenemez.", nameof(request.DocumentId));

        // UploadDocumentCommandHandler'daki AYNI "tek sefer belleğe al" gerekçesi.
        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var newContentHash = Convert.ToHexString(SHA256.HashData(bytes));

        var newFileExtension = Path.GetExtension(request.OriginalFileName).TrimStart('.');

        using var saveStream = new MemoryStream(bytes);
        var newStorageKey = await _fileStorageService.SaveAsync(saveStream, newFileExtension, cancellationToken);

        // ÖNCE mevcut (şu ana kadar güncel olan) dosyayı bir DocumentVersion'a
        // snapshot'la - document.ReplaceFile çağrılır çağrılmaz bu bilgi
        // Document'ın kendisinden kaybolacak, bu yüzden sıralama ÖNEMLİ.
        var snapshot = DocumentVersion.CreateSnapshot(
            document.Id, document.CurrentVersionNumber, document.OriginalFileName, document.StorageKey,
            document.ContentType, document.FileExtension, document.SizeBytes, document.ContentHash,
            _currentUser.UserId.Value, _currentUser.Email);

        document.ReplaceFile(
            request.OriginalFileName, newStorageKey, request.ContentType, newFileExtension,
            request.SizeBytes, newContentHash);

        request.AuditDetails = document.Title;

        await _documentVersionRepository.AddAsync(snapshot, cancellationToken);
        await _documentRepository.UpdateAsync(document, cancellationToken);

        // UploadDocumentCommandHandler'daki AYNI event - DocumentUploadedEventHandler
        // (Documents.Infrastructure) bunu ilk yüklemedekiyle BİREBİR aynı şekilde
        // işleyip Extracting -> Ready/Failed geçişini YENİ içerik üzerinden yapacak.
        // Eski embedding'ler AI tarafında GenerateDocumentEmbeddingsCommandHandler'ın
        // idempotent silme adımıyla (Ders/P5 Gün 2) otomatik temizlenip yenileriyle
        // değişecek.
        _outboxWriter.Enqueue(new DocumentUploadedEvent(
            document.Id, document.StorageKey, document.ContentType, document.FileExtension,
            document.Title, document.DepartmentName, document.Visibility.ToString()));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
