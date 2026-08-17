using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Shared.Testing;
using Atlas.Modules.Wiki.Application.WikiPages.Commands;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;

namespace Atlas.Modules.Wiki.Application.Tests;

public class UpdateWikiPageCommandHandlerTests
{
    private static UpdateWikiPageCommandHandler CreateHandler(
        FakeWikiPageRepository pageRepository,
        FakeWikiFolderRepository? folderRepository = null,
        FakeUnitOfWork? unitOfWork = null,
        Guid? viewerUserId = null,
        bool viewerIsAdmin = false,
        FakeWikiPageVersionRepository? versionRepository = null)
        => new(
            pageRepository,
            versionRepository ?? new FakeWikiPageVersionRepository(),
            folderRepository ?? new FakeWikiFolderRepository(),
            new FakeCurrentUserAccessor("IT", viewerIsAdmin, viewerUserId),
            unitOfWork ?? new FakeUnitOfWork());

    [Fact]
    public async Task SayfayiOlusturanKullanici_KendiSayfasiniDuzenleyebilir()
    {
        var ownerId = Guid.NewGuid();
        var repository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Eski Başlık", "Eski içerik", "IT", WikiVisibility.Public, ownerId);
        repository.AddedPages.Add(page);

        var handler = CreateHandler(repository, viewerUserId: ownerId);
        var command = new UpdateWikiPageCommand(page.Id, "Yeni Başlık", "Yeni içerik", "DepartmentOnly", null);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Yeni Başlık", page.Title);
        Assert.Equal("Yeni içerik", page.Content);
        Assert.Equal(WikiVisibility.DepartmentOnly, page.Visibility);
        Assert.NotNull(page.UpdatedAtUtc);
    }

    [Fact]
    public async Task BaskasininSayfasiniDuzenlemeyeCalisan_NormalKullanici_YetkisizHatasiAlir()
    {
        var repository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        repository.AddedPages.Add(page);

        var handler = CreateHandler(repository, viewerUserId: Guid.NewGuid());
        var command = new UpdateWikiPageCommand(page.Id, "Yeni", "Yeni", "Public", null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Admin_BaskasininSayfasiniDaDuzenleyebilir()
    {
        var repository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        repository.AddedPages.Add(page);

        var handler = CreateHandler(repository, viewerUserId: Guid.NewGuid(), viewerIsAdmin: true);
        var command = new UpdateWikiPageCommand(page.Id, "Yeni", "Yeni", "Public", null);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Yeni", page.Title);
    }

    [Fact]
    public async Task OlmayanSayfayiDuzenlemeyeCalisan_ArgumentExceptionAlir()
    {
        var handler = CreateHandler(new FakeWikiPageRepository(), viewerUserId: Guid.NewGuid());
        var command = new UpdateWikiPageCommand(Guid.NewGuid(), "Yeni", "Yeni", "Public", null);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task BaskaDepartmaninKlasorune_SayfaTasinamaz()
    {
        var ownerId = Guid.NewGuid();
        var repository = new FakeWikiPageRepository();
        var page = WikiPage.Create("Başlık", "İçerik", "IT", WikiVisibility.Public, ownerId);
        repository.AddedPages.Add(page);

        var folderRepository = new FakeWikiFolderRepository();
        var otherDeptFolder = WikiFolder.Create("İK Klasörü", "IK", null, Guid.NewGuid());
        folderRepository.AddedFolders.Add(otherDeptFolder);

        var handler = CreateHandler(repository, folderRepository, viewerUserId: ownerId);
        var command = new UpdateWikiPageCommand(page.Id, "Başlık", "İçerik", "Public", otherDeptFolder.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
}
