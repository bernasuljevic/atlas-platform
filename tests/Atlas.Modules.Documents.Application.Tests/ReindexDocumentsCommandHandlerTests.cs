using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Application.Tests.Fakes;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Contracts;

namespace Atlas.Modules.Documents.Application.Tests;

public class ReindexDocumentsCommandHandlerTests
{
    private static Document CreateDocument(string title, string storageKey, DocumentStatus status = DocumentStatus.Ready)
    {
        var document = Document.Create(
            title, "belge.txt", storageKey, "text/plain", "txt", 100, "IT",
            DocumentVisibility.Public, Guid.NewGuid(), "owner@atlas.local", null, null, storageKey);

        if (status == DocumentStatus.Failed)
        {
            document.MarkExtracting();
            document.MarkFailed("test hatası");
        }

        return document;
    }

    [Fact]
    public async Task Handle_HicBelgeYoksa_SifirDonerVeHicbirSeyKuyruklamaz()
    {
        var outboxWriter = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ReindexDocumentsCommandHandler(new FakeDocumentRepository(), outboxWriter, unitOfWork);

        var count = await handler.Handle(new ReindexDocumentsCommand(), CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Empty(outboxWriter.Enqueued);
        // Belge olmasa bile SaveChanges bir kez çağrılıyor - Handler'ın "sıfır
        // belge" durumunu ayrı bir dal olarak ele almadığını, tek bir akıştan
        // geçtiğini doğruluyor.
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_DurumFarkEtmeksizin_TumBelgelerIcinDocumentUploadedEventKuyruklar()
    {
        var readyDocument = CreateDocument("Hazır Belge", "storage-ready.txt");
        var failedDocument = CreateDocument("Başarısız Belge", "storage-failed.txt", DocumentStatus.Failed);
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(readyDocument);
        documentRepository.Documents.Add(failedDocument);
        var outboxWriter = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new ReindexDocumentsCommandHandler(documentRepository, outboxWriter, unitOfWork);
        var count = await handler.Handle(new ReindexDocumentsCommand(), CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal(2, outboxWriter.Enqueued.Count);

        var events = outboxWriter.Enqueued.Cast<DocumentUploadedEvent>().ToList();
        Assert.Contains(events, e => e.DocumentId == readyDocument.Id && e.StorageKey == "storage-ready.txt");
        Assert.Contains(events, e => e.DocumentId == failedDocument.Id && e.StorageKey == "storage-failed.txt");

        // Yüzlerce belge olsa bile TEK bir SaveChanges - atomiklik (bkz.
        // Handler'daki not).
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
