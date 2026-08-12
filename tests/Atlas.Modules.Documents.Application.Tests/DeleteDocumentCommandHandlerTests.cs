using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Application.Tests.Fakes;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Documents.Application.Tests;

public class DeleteDocumentCommandHandlerTests
{
    private static Document CreateDocument(Guid ownerId, string storageKey = "current.txt") =>
        Document.Create(
            "Silinecek Belge", "current.txt", storageKey, "text/plain", "txt", 100, "IT",
            DocumentVisibility.Public, ownerId, "owner@atlas.local", null, null, "hash");

    private static DeleteDocumentCommandHandler CreateHandler(
        FakeDocumentRepository documentRepository, FakeDocumentVersionRepository versionRepository,
        FakeFileStorageService fileStorageService, FakeOutboxWriter outboxWriter, FakeUnitOfWork unitOfWork,
        FakeCurrentUserAccessor currentUser)
        => new(documentRepository, versionRepository, fileStorageService, outboxWriter, unitOfWork, currentUser);

    [Fact]
    public async Task Handle_BelgeBulunamazsa_HataFirlatir()
    {
        var handler = CreateHandler(
            new FakeDocumentRepository(), new FakeDocumentVersionRepository(), new FakeFileStorageService(),
            new FakeOutboxWriter(), new FakeUnitOfWork(), new FakeCurrentUserAccessor(department: "IT"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new DeleteDocumentCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SahibiOlmayanKullanici_YetkisizHatasiFirlatir()
    {
        var document = CreateDocument(Guid.NewGuid());
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var handler = CreateHandler(
            documentRepository, new FakeDocumentVersionRepository(), new FakeFileStorageService(),
            new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT", userId: Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new DeleteDocumentCommand(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BasariliSilme_MevcutDosyaVeTumVersiyonDosyalariSilinir()
    {
        var ownerId = Guid.NewGuid();
        var document = CreateDocument(ownerId, storageKey: "current.txt");
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var versionRepository = new FakeDocumentVersionRepository();
        versionRepository.Versions.Add(DocumentVersion.CreateSnapshot(
            document.Id, 1, "v1.txt", "v1-key.txt", "text/plain", "txt", 50, "v1-hash", ownerId, "owner@atlas.local"));
        versionRepository.Versions.Add(DocumentVersion.CreateSnapshot(
            document.Id, 2, "v2.txt", "v2-key.txt", "text/plain", "txt", 60, "v2-hash", ownerId, "owner@atlas.local"));

        var fileStorageService = new FakeFileStorageService();
        var outboxWriter = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(
            documentRepository, versionRepository, fileStorageService, outboxWriter, unitOfWork,
            new FakeCurrentUserAccessor(department: "IT", userId: ownerId));

        await handler.Handle(new DeleteDocumentCommand(document.Id), CancellationToken.None);

        // Güncel dosya + İKİ eski versiyonun dosyası, HEPSİ diskten silindi -
        // "yetim" dosya kalmadı (bkz. Handler'daki not).
        Assert.Contains("current.txt", fileStorageService.DeletedKeys);
        Assert.Contains("v1-key.txt", fileStorageService.DeletedKeys);
        Assert.Contains("v2-key.txt", fileStorageService.DeletedKeys);

        // Versiyon SATIRLARI da temizlendi.
        Assert.Empty(versionRepository.Versions);

        // Document silindi, DocumentDeletedEvent kuyruklandı.
        Assert.Empty(documentRepository.Documents);
        Assert.Single(outboxWriter.Enqueued);
        Assert.IsType<Atlas.Shared.Contracts.DocumentDeletedEvent>(outboxWriter.Enqueued[0]);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_AdminBaskasininBelgesiniSilebilir()
    {
        var document = CreateDocument(Guid.NewGuid());
        var documentRepository = new FakeDocumentRepository();
        documentRepository.Documents.Add(document);

        var handler = CreateHandler(
            documentRepository, new FakeDocumentVersionRepository(), new FakeFileStorageService(),
            new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IK", isAdmin: true, userId: Guid.NewGuid()));

        await handler.Handle(new DeleteDocumentCommand(document.Id), CancellationToken.None);

        Assert.Empty(documentRepository.Documents);
    }
}
