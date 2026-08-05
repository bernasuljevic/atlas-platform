using Atlas.Modules.Wiki.Application.Tests.Fakes;
using Atlas.Shared.Testing;
using Atlas.Modules.Wiki.Application.WikiFolders.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;

namespace Atlas.Modules.Wiki.Application.Tests;

public class GetWikiFolderTreeQueryHandlerTests
{
    private static GetWikiFolderTreeQueryHandler CreateHandler(
        FakeWikiFolderRepository folderRepository,
        FakeWikiPageRepository pageRepository,
        string? viewerDepartment,
        bool viewerIsAdmin = false)
        => new(folderRepository, pageRepository, new FakeCurrentUserAccessor(viewerDepartment, viewerIsAdmin));

    [Fact]
    public async Task KendiDepartmaniniGezenKullanici_TumKlasorVeSayfalariGorur()
    {
        var folderRepository = new FakeWikiFolderRepository();
        var pageRepository = new FakeWikiPageRepository();

        var react = WikiFolder.Create("React", "IT", null, Guid.NewGuid());
        var ui = WikiFolder.Create("UI", "IT", react.Id, Guid.NewGuid());
        folderRepository.AddedFolders.AddRange([react, ui]);

        var publicPage = WikiPage.Create("Herkese Açık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid(), ui.Id);
        var privatePage = WikiPage.Create("Sadece IT", "İçerik", "IT", WikiVisibility.DepartmentOnly, Guid.NewGuid(), ui.Id);
        pageRepository.AddedPages.AddRange([publicPage, privatePage]);

        var handler = CreateHandler(folderRepository, pageRepository, viewerDepartment: "IT");
        var result = await handler.Handle(new GetWikiFolderTreeQuery("IT"), CancellationToken.None);

        var reactNode = Assert.Single(result.Folders);
        Assert.Equal("React", reactNode.Name);
        var uiNode = Assert.Single(reactNode.Children);
        Assert.Equal("UI", uiNode.Name);
        Assert.Equal(2, uiNode.Pages.Count);
    }

    [Fact]
    public async Task BaskaDepartmaniGezenKullanici_SadeceHerkeseAcikSayfalariVeOnlarinYoluGorur()
    {
        var folderRepository = new FakeWikiFolderRepository();
        var pageRepository = new FakeWikiPageRepository();

        var react = WikiFolder.Create("React", "IT", null, Guid.NewGuid());
        var ui = WikiFolder.Create("UI", "IT", react.Id, Guid.NewGuid());
        // Sadece boş bir klasör - hiç sayfası yok, ne herkese açık ne de değil.
        var bosKlasor = WikiFolder.Create("Boş", "IT", null, Guid.NewGuid());
        folderRepository.AddedFolders.AddRange([react, ui, bosKlasor]);

        var publicPage = WikiPage.Create("Herkese Açık", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid(), ui.Id);
        var privatePage = WikiPage.Create("Sadece IT", "İçerik", "IT", WikiVisibility.DepartmentOnly, Guid.NewGuid(), ui.Id);
        pageRepository.AddedPages.AddRange([publicPage, privatePage]);

        var handler = CreateHandler(folderRepository, pageRepository, viewerDepartment: "IK");
        var result = await handler.Handle(new GetWikiFolderTreeQuery("IT"), CancellationToken.None);

        // "Boş" klasör hiç görünmüyor - içinde görünür (Public) hiçbir sayfaya
        // ulaşmıyor. React > UI zinciri sadece Public sayfaya ulaşmak GEREKTİĞİ
        // için görünüyor.
        var reactNode = Assert.Single(result.Folders);
        Assert.Equal("React", reactNode.Name);
        var uiNode = Assert.Single(reactNode.Children);
        var visiblePage = Assert.Single(uiNode.Pages);
        Assert.Equal("Herkese Açık", visiblePage.Title);
    }

    [Fact]
    public async Task Admin_BaskaDepartmaninTamAgacinGorur()
    {
        var folderRepository = new FakeWikiFolderRepository();
        var pageRepository = new FakeWikiPageRepository();

        var bosKlasor = WikiFolder.Create("Boş", "IT", null, Guid.NewGuid());
        folderRepository.AddedFolders.Add(bosKlasor);

        var handler = CreateHandler(folderRepository, pageRepository, viewerDepartment: "IK", viewerIsAdmin: true);
        var result = await handler.Handle(new GetWikiFolderTreeQuery("IT"), CancellationToken.None);

        // Admin bypass - normalde budanacak boş klasör bile görünüyor.
        Assert.Single(result.Folders);
    }

    [Fact]
    public async Task KlasorsuzSayfalar_UnfiledPagesAltindaGorunur()
    {
        var folderRepository = new FakeWikiFolderRepository();
        var pageRepository = new FakeWikiPageRepository();

        var page = WikiPage.Create("Klasörsüz", "İçerik", "IT", WikiVisibility.Public, Guid.NewGuid());
        pageRepository.AddedPages.Add(page);

        var handler = CreateHandler(folderRepository, pageRepository, viewerDepartment: "IT");
        var result = await handler.Handle(new GetWikiFolderTreeQuery("IT"), CancellationToken.None);

        Assert.Empty(result.Folders);
        var unfiled = Assert.Single(result.UnfiledPages);
        Assert.Equal("Klasörsüz", unfiled.Title);
    }
}
