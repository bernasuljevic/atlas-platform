using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Shared.Testing;
using Atlas.Modules.Wiki.Application.WikiPages.Commands;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;

namespace Atlas.Modules.Wiki.Application.Tests;

public class RestoreWikiPageVersionCommandHandlerTests
{
    private static RestoreWikiPageVersionCommandHandler CreateHandler(
        FakeWikiPageRepository pageRepository,
        FakeWikiPageVersionRepository? versionRepository = null,
        FakeUnitOfWork? unitOfWork = null,
        Guid? viewerUserId = null,
        bool viewerIsAdmin = false)
        => new(
            pageRepository,
            versionRepository ?? new FakeWikiPageVersionRepository(),
            new FakeCurrentUserAccessor("IT", viewerIsAdmin, viewerUserId),
            unitOfWork ?? new FakeUnitOfWork());

    [Fact]
    public async Task Sahibi_EskiVersiyonaGeriDondurebilir_VeGeriDonulmedenOnceki_HalDeSnapshotlanir()
    {
        var ownerId = Guid.NewGuid();
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("v1 Başlık", "v1 içerik", "IT", WikiVisibility.Public, ownerId);
        pageRepository.AddedPages.Add(page);

        var versionRepository = new FakeWikiPageVersionRepository();
        var v1 = WikiPageVersion.CreateSnapshot(
            page.Id, 1, "v1 Başlık", "v1 içerik", "Public", null, ownerId, "owner@atlas.local");
        versionRepository.Versions.Add(v1);

        // Sayfa şu an "v2" hâlinde (CurrentVersionNumber=2 olacak şekilde bir
        // Update çağrısıyla ilerletiliyor) - restore'un bunu v3 olarak
        // arşivleyip v1'e dönmesi bekleniyor.
        page.Update("v2 Başlık", "v2 içerik", WikiVisibility.Public, null, null);

        var handler = CreateHandler(pageRepository, versionRepository, viewerUserId: ownerId);
        await handler.Handle(new RestoreWikiPageVersionCommand(page.Id, 1), CancellationToken.None);

        Assert.Equal("v1 Başlık", page.Title);
        Assert.Equal("v1 içerik", page.Content);
        Assert.Equal(3, page.CurrentVersionNumber);

        // Geri dönülmeden HEMEN ÖNCEki (v2) hâl de kayboluyor değil, yeni bir
        // snapshot olarak arşive eklenmiş olmalı.
        var preRestoreSnapshot = Assert.Single(versionRepository.Versions, v => v.VersionNumber == 2);
        Assert.Equal("v2 Başlık", preRestoreSnapshot.Title);
    }

    [Fact]
    public async Task BaskasininSayfasiniGeriDondurmeyeCalisan_NormalKullanici_YetkisizHatasiAlir()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        var handler = CreateHandler(pageRepository, viewerUserId: Guid.NewGuid());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new RestoreWikiPageVersionCommand(page.Id, 1), CancellationToken.None));
    }

    [Fact]
    public async Task Admin_BaskasininSayfasiniDaGeriDondurebilir()
    {
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);
        page.Update("Yeni Başlık", "Yeni içerik", WikiVisibility.Public, null, null);

        var versionRepository = new FakeWikiPageVersionRepository();
        versionRepository.Versions.Add(WikiPageVersion.CreateSnapshot(
            page.Id, 1, "Başlık", "İçerik", "Public", null, Guid.NewGuid(), "owner@atlas.local"));

        var handler = CreateHandler(pageRepository, versionRepository, viewerUserId: Guid.NewGuid(), viewerIsAdmin: true);
        await handler.Handle(new RestoreWikiPageVersionCommand(page.Id, 1), CancellationToken.None);

        Assert.Equal("Başlık", page.Title);
    }

    [Fact]
    public async Task OlmayanSayfayiGeriDondurmeyeCalisan_ArgumentExceptionAlir()
    {
        var handler = CreateHandler(new FakeWikiPageRepository(), viewerUserId: Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new RestoreWikiPageVersionCommand(Guid.NewGuid(), 1), CancellationToken.None));
    }

    [Fact]
    public async Task OlmayanVersiyonaGeriDondurmeyeCalisan_ArgumentExceptionAlir()
    {
        var ownerId = Guid.NewGuid();
        var pageRepository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, ownerId);
        pageRepository.AddedPages.Add(page);

        // Hiç düzenlenmemiş bir sayfanın versiyon geçmişi BOŞTUR (henüz hiç
        // snapshot alınmadı) - var olmayan bir versiyon numarası istenince.
        var handler = CreateHandler(pageRepository, viewerUserId: ownerId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new RestoreWikiPageVersionCommand(page.Id, 99), CancellationToken.None));
    }
}
