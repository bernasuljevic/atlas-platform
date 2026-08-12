using System.Text;
using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Application.Tests.Fakes;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Documents.Application.Tests;

public class UploadNewDocumentVersionCommandHandlerTests
{
    private static Document CreateDocument(Guid ownerId, string storageKey = "old-key.txt", DocumentStatus status = DocumentStatus.Ready)
    {
        var document = Document.Create(
            "Test Belgesi", "eski.txt", storageKey, "text/plain", "txt", 100, "IT",
            DocumentVisibility.Public, ownerId, "owner@atlas.local", null, null, "old-hash");

        // Testin ihtiyacına göre durumu sabitlemek için - MarkExtracting/MarkReady
        // gerçek Domain metotları, doğrudan çağırmak new bir Command icat etmekten
        // daha temiz.
        if (status == DocumentStatus.Extracting) document.MarkExtracting();

        return document;
    }

    private static UploadNewDocumentVersionCommandHandler CreateHandler(
        FakeDocumentRepository documentRepository, FakeDocumentVersionRepository versionRepository,
        FakeFileStorageService fileStorageService, FakeMalwareScanner malwareScanner,
        FakeOutboxWriter outboxWriter, FakeUnitOfWork unitOfWork, FakeCurrentUserAccessor currentUser)
        => new(documentRepository, versionRepository, fileStorageService, malwareScanner, outboxWriter, unitOfWork, currentUser);

    private static UploadNewDocumentVersionCommand CreateCommand(Guid documentId, string content = "yeni içerik")
        => new(documentId, new MemoryStream(Encoding.UTF8.GetBytes(content)), "yeni.txt", "text/plain", content.Length);

    [Fact]
    public async Task Handle_BelgeBulunamazsa_HataFirlatir()
    {
        var handler = CreateHandler(
            new FakeDocumentRepository(), new FakeDocumentVersionRepository(), new FakeFileStorageService(),
            new FakeMalwareScanner(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SahibiOlmayanKullanici_YetkisizHatasiFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var document = CreateDocument(ownerId);
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var handler = CreateHandler(
            documentRepository, new FakeDocumentVersionRepository(), new FakeFileStorageService(),
            new FakeMalwareScanner(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT", userId: Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(CreateCommand(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HalaIslenmekteOlanBelgeye_HataFirlatir()
    {
        var ownerId = Guid.NewGuid();
        var document = CreateDocument(ownerId, status: DocumentStatus.Extracting);
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var handler = CreateHandler(
            documentRepository, new FakeDocumentVersionRepository(), new FakeFileStorageService(),
            new FakeMalwareScanner(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT", userId: ownerId));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(CreateCommand(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BasariliYukleme_EskiDosyaSnapshotlanirVeDocumentYeniDosyayaGuncellenir()
    {
        var ownerId = Guid.NewGuid();
        var document = CreateDocument(ownerId, storageKey: "old-key.txt");
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);
        var versionRepository = new FakeDocumentVersionRepository();
        var outboxWriter = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(
            documentRepository, versionRepository, new FakeFileStorageService(), new FakeMalwareScanner(),
            outboxWriter, unitOfWork, new FakeCurrentUserAccessor(department: "IT", userId: ownerId));

        await handler.Handle(CreateCommand(document.Id, "yeni içerik"), CancellationToken.None);

        // Eski dosya (version 1) arşive taşındı - orijinal StorageKey/başlık
        // KORUNMUŞ olmalı.
        var snapshot = Assert.Single(versionRepository.Versions);
        Assert.Equal(1, snapshot.VersionNumber);
        Assert.Equal("old-key.txt", snapshot.StorageKey);
        Assert.Equal("eski.txt", snapshot.OriginalFileName);

        // Document artık versiyon 2'yi işaret ediyor, Status Uploaded'a döndü
        // (yeniden işlenecek).
        Assert.Equal(2, document.CurrentVersionNumber);
        Assert.Equal(DocumentStatus.Uploaded, document.Status);
        Assert.Equal("yeni.txt", document.OriginalFileName);
        Assert.NotEqual("old-key.txt", document.StorageKey);

        Assert.Single(outboxWriter.Enqueued);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_AdminSahibiOlmayanBelgeyeDeVersiyonYukleyebilir()
    {
        var ownerId = Guid.NewGuid();
        var document = CreateDocument(ownerId);
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var handler = CreateHandler(
            documentRepository, new FakeDocumentVersionRepository(), new FakeFileStorageService(),
            new FakeMalwareScanner(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IK", isAdmin: true, userId: Guid.NewGuid()));

        await handler.Handle(CreateCommand(document.Id), CancellationToken.None);

        Assert.Equal(2, document.CurrentVersionNumber);
    }
}
