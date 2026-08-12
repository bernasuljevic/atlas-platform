using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Application.Tests.Fakes;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Contracts;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Documents.Application.Tests;

public class ReprocessDocumentCommandHandlerTests
{
    private static Document CreateDocument(Guid ownerId, DocumentStatus status = DocumentStatus.Failed)
    {
        var document = Document.Create(
            "Yeniden İşlenecek Belge", "belge.txt", "storage-key.txt", "text/plain", "txt", 100, "IT",
            DocumentVisibility.Public, ownerId, "owner@atlas.local", null, null, "hash");

        if (status == DocumentStatus.Extracting) document.MarkExtracting();

        return document;
    }

    private static ReprocessDocumentCommandHandler CreateHandler(
        FakeDocumentRepository documentRepository, FakeOutboxWriter outboxWriter, FakeUnitOfWork unitOfWork,
        FakeCurrentUserAccessor currentUser)
        => new(documentRepository, outboxWriter, unitOfWork, currentUser);

    [Fact]
    public async Task Handle_BelgeBulunamazsa_HataFirlatir()
    {
        var handler = CreateHandler(
            new FakeDocumentRepository(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new ReprocessDocumentCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SahibiOlmayanKullanici_YetkisizHatasiFirlatir()
    {
        var document = CreateDocument(Guid.NewGuid());
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var handler = CreateHandler(
            documentRepository, new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT", userId: Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new ReprocessDocumentCommand(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HalaIslenmekteOlanBelgeye_HataFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var document = CreateDocument(ownerId, DocumentStatus.Extracting);
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var handler = CreateHandler(
            documentRepository, new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT", userId: ownerId));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new ReprocessDocumentCommand(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BasariliIstek_VarOlanDosyayiKullanarakDocumentUploadedEventYenidenYazar()
    {
        var ownerId = Guid.NewGuid();
        var document = CreateDocument(ownerId);
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);
        var outboxWriter = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(
            documentRepository, outboxWriter, unitOfWork,
            new FakeCurrentUserAccessor(department: "IT", userId: ownerId));

        await handler.Handle(new ReprocessDocumentCommand(document.Id), CancellationToken.None);

        var enqueued = Assert.Single(outboxWriter.Enqueued);
        var uploadedEvent = Assert.IsType<DocumentUploadedEvent>(enqueued);
        // Yeni bir extraction akışı İCAT EDİLMEDİ - var olan StorageKey aynen
        // kullanılıyor (bkz. Command'daki "aynı fikir" notu).
        Assert.Equal("storage-key.txt", uploadedEvent.StorageKey);
        Assert.Equal(document.Id, uploadedEvent.DocumentId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
