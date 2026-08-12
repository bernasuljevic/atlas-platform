using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class ReprocessDocumentCommandHandler : IRequestHandler<ReprocessDocumentCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserAccessor _currentUser;

    public ReprocessDocumentCommandHandler(
        IDocumentRepository documentRepository, IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(ReprocessDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Belgeyi yeniden işlemek için giriş yapmış olmalısınız.");

        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (document is null)
            throw new ArgumentException("Belge bulunamadı.", nameof(request.Id));

        // Delete/Update ile AYNI owner-or-admin deseni.
        var isOwner = document.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu belgeyi yeniden işleme yetkiniz yok.");

        // DocumentUploadedEventHandler zaten Extracting'e geçtiğinde bu durumu
        // görecek bir sonraki OutboxProcessor turuna kadar - iki eşzamanlı
        // "reprocess" tıklaması aynı belgeyi iki kez kuyruğa sokmasın diye
        // erken bir 400 ile engelliyoruz (kilit/transaction değil, ama pratikte
        // "çift tıklama" senaryosunu yakalamaya yetiyor).
        if (document.Status == DocumentStatus.Extracting)
            throw new ArgumentException("Bu belge şu anda zaten işleniyor.", nameof(request.Id));

        request.AuditDetails = document.Title;

        // ReindexWikiPagesCommand'daki AYNI fikir: YENİ bir extraction akışı
        // yazmak yerine, ilk yüklemedeki event'i (var olan StorageKey/ContentType/
        // FileExtension ile) yeniden Outbox'a yazıyoruz - DocumentUploadedEventHandler
        // (Documents.Infrastructure) bunu ilk yüklemedekiyle BİREBİR AYNI şekilde
        // işleyip Extracting -> Ready/Failed geçişini kendisi yapacak.
        _outboxWriter.Enqueue(new DocumentUploadedEvent(
            document.Id, document.StorageKey, document.ContentType, document.FileExtension,
            document.Title, document.DepartmentName, document.Visibility.ToString()));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
