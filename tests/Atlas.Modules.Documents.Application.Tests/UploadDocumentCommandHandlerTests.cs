using System.Text;
using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Application.Tests.Fakes;
using Atlas.Shared.Testing;

namespace Atlas.Modules.Documents.Application.Tests;

public class UploadDocumentCommandHandlerTests
{
    private static UploadDocumentCommandHandler CreateHandler(
        FakeDocumentRepository documentRepository, FakeFileStorageService fileStorageService,
        FakeMalwareScanner malwareScanner, FakeWikiVisibilityChecker visibilityChecker,
        FakeOutboxWriter outboxWriter, FakeUnitOfWork unitOfWork, FakeCurrentUserAccessor currentUser)
        => new(documentRepository, fileStorageService, malwareScanner, visibilityChecker, outboxWriter, unitOfWork, currentUser);

    private static UploadDocumentCommand CreateCommand(string content, string? departmentName = null, string visibility = "Public")
        => new(
            new MemoryStream(Encoding.UTF8.GetBytes(content)), "test.txt", "text/plain", content.Length,
            "Test Belgesi", departmentName, visibility, null, null);

    [Fact]
    public async Task Handle_KullaniciDepartmansizsa_HataFirlatir()
    {
        // Normal (Admin olmayan) bir kullanıcının departmanı yoksa - departman
        // her zaman JWT'den zorlanıyor, istemcinin gönderdiği DepartmentName
        // YOK SAYILIYOR (bkz. Handler'daki not).
        var handler = CreateHandler(
            new FakeDocumentRepository(), new FakeFileStorageService(), new FakeMalwareScanner(),
            new FakeWikiVisibilityChecker(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: null));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(CreateCommand("içerik"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DosyaGuvenlikTaramasindanGecemezse_BelgeHicOlusturulmaz()
    {
        var documentRepository = new FakeDocumentRepository();
        var fileStorageService = new FakeFileStorageService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            documentRepository, fileStorageService, new FakeMalwareScanner { IsClean = false, ThreatName = "TestVirus" },
            new FakeWikiVisibilityChecker(), new FakeOutboxWriter(), unitOfWork,
            new FakeCurrentUserAccessor(department: "IT"));

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(CreateCommand("içerik", "IT"), CancellationToken.None));

        Assert.Contains("TestVirus", ex.Message);
        // Diskte dosya YOK, Document HİÇ oluşturulmadı, SaveChanges hiç çağrılmadı -
        // tarama diske yazmadan/kaydetmeden ÖNCE yapılıyor.
        Assert.Empty(documentRepository.Documents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_BasariliYukleme_BelgeOlusturulurVeOutboxaDocumentUploadedEventYazilir()
    {
        var documentRepository = new FakeDocumentRepository();
        var outboxWriter = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            documentRepository, new FakeFileStorageService(), new FakeMalwareScanner(),
            new FakeWikiVisibilityChecker(), outboxWriter, unitOfWork,
            new FakeCurrentUserAccessor(department: "IT"));

        var result = await handler.Handle(CreateCommand("içerik", "IT"), CancellationToken.None);

        Assert.Single(documentRepository.Documents);
        Assert.Equal(documentRepository.Documents[0].Id, result.Id);
        Assert.Null(result.DuplicateOfDocumentId);
        Assert.Single(outboxWriter.Enqueued);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_AyniIcerikliGorunurBirBelgeVarsa_DuplicateBilgisiDoner()
    {
        var documentRepository = new FakeDocumentRepository();
        var currentUser = new FakeCurrentUserAccessor(department: "IT");
        var handler = CreateHandler(
            documentRepository, new FakeFileStorageService(), new FakeMalwareScanner(),
            new FakeWikiVisibilityChecker(), new FakeOutboxWriter(), new FakeUnitOfWork(), currentUser);

        var sharedContent = "tekrarlanan içerik";
        var first = await handler.Handle(CreateCommand(sharedContent, "IT"), CancellationToken.None);
        var second = await handler.Handle(CreateCommand(sharedContent, "IT"), CancellationToken.None);

        // Yükleme YİNE DE başarılı - engellenmedi, sadece bilgilendirdi.
        Assert.Equal(first.Id, second.DuplicateOfDocumentId);
        Assert.Equal("Test Belgesi", second.DuplicateOfTitle);
        Assert.Equal(2, documentRepository.Documents.Count);
    }

    [Fact]
    public async Task Handle_AyniIcerikFarkliDepartmandanGizliyse_DuplicateBilgisiDonmez()
    {
        var documentRepository = new FakeDocumentRepository();
        var handler = CreateHandler(
            documentRepository, new FakeFileStorageService(), new FakeMalwareScanner(),
            new FakeWikiVisibilityChecker(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IK"));

        var sharedContent = "gizli tekrar";
        // İK kullanıcısı DepartmentOnly bir belge yüklüyor.
        await handler.Handle(CreateCommand(sharedContent, "IK", "DepartmentOnly"), CancellationToken.None);

        // AYNI içeriği İT kullanıcısı yüklüyor - İK'nın DepartmentOnly belgesinin
        // VARLIĞINI BİLE görmemeli.
        var itHandler = CreateHandler(
            documentRepository, new FakeFileStorageService(), new FakeMalwareScanner(),
            new FakeWikiVisibilityChecker(), new FakeOutboxWriter(), new FakeUnitOfWork(),
            new FakeCurrentUserAccessor(department: "IT"));
        var result = await itHandler.Handle(CreateCommand(sharedContent, "IT"), CancellationToken.None);

        Assert.Null(result.DuplicateOfDocumentId);
        Assert.Null(result.DuplicateOfTitle);
    }
}
