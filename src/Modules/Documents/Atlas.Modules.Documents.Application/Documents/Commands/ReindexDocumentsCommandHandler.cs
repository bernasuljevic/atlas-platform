using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class ReindexDocumentsCommandHandler : IRequestHandler<ReindexDocumentsCommand, int>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    public ReindexDocumentsCommandHandler(
        IDocumentRepository documentRepository, IOutboxWriter outboxWriter, IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ReindexDocumentsCommand request, CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        // Durumuna bakılmaksızın (Ready/Failed/Extracting fark etmez) HEPSİ
        // yeniden kuyruğa alınıyor - ReprocessDocumentCommand'daki "hâlâ
        // işleniyor" guard'ı BİLEREK burada YOK, çünkü bu tekil bir kullanıcı
        // tıklamasını değil, Admin'in bilinçli tek seferlik bir bakım eylemini
        // temsil ediyor (Wiki'nin reindex'i de aynı şekilde koşulsuz).
        foreach (var document in documents)
        {
            _outboxWriter.Enqueue(new DocumentUploadedEvent(
                document.Id, document.StorageKey, document.ContentType, document.FileExtension,
                document.Title, document.DepartmentName, document.Visibility.ToString()));
        }

        // TEK bir SaveChanges - yüzlerce belge olsa bile Outbox mesajlarının
        // TAMAMI ya birlikte yazılır ya hiç yazılmaz (yarım kalmış bir reindex
        // turu, hangi belgelerin zaten kuyruğa alındığını takip etmeyi
        // zorlaştırırdı).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return documents.Count;
    }
}
