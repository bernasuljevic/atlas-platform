using Atlas.Modules.Wiki.Application.Comments.Commands;
using Atlas.Modules.Wiki.Application.Comments.Queries;
using Atlas.Modules.Wiki.Application.Favorites.Commands;
using Atlas.Modules.Wiki.Application.Favorites.Queries;
using Atlas.Modules.Wiki.Application.Pins.Commands;
using Atlas.Modules.Wiki.Application.Pins.Queries;
using Atlas.Modules.Wiki.Application.WikiFolders.Commands;
using Atlas.Modules.Wiki.Application.WikiFolders.Queries;
using Atlas.Modules.Wiki.Application.WikiPages.Commands;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Atlas.Modules.Wiki.Api;

public static class WikiEndpoints
{
    public static IEndpointRouteBuilder MapWikiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wiki").WithTags("Wiki");

        // GÜVENLİK: Artık bir "?department=" query parametresi YOK - hangi departmanın
        // DepartmentOnly sayfalarının görüneceği, çağıranın JWT'sindeki imzalı
        // "department" claim'inden belirleniyor (bkz. GetWikiPagesQuery.cs'teki not).
        // ?pageNumber=1&pageSize=10 (ikisi de opsiyonel, varsayılanlar bunlar).
        group.MapGet("/pages", async (IMediator mediator, int pageNumber = 1, int pageSize = 10) =>
        {
            var result = await mediator.Send(new GetWikiPagesQuery(pageNumber, pageSize));
            return Results.Ok(result);
        })
        .WithName("GetWikiPages");

        // GetWikiPages ile AYNI görünürlük deseni - açık endpoint, giriş
        // yapmamış bir ziyaretçi sadece Public sayfaları görür (bkz.
        // GetWikiDashboardQueryHandler). ?recentlyAddedCount=... opsiyonel -
        // "Son Eklenen Makaleler" carousel'ının (2026-08-17) birden fazla
        // "sayfa"sını doldurmak için varsayılandan (5) fazlasını istiyor.
        group.MapGet("/dashboard", async (IMediator mediator, int recentlyAddedCount = 5) =>
        {
            var dashboard = await mediator.Send(new GetWikiDashboardQuery(RecentlyAddedCount: recentlyAddedCount));
            return Results.Ok(dashboard);
        })
        .WithName("GetWikiDashboard");

        // Üst bardaki arama kutusunun harf harf çağırdığı hafif öneri
        // endpoint'i - AI'ın anlam bazlı aramasından (SearchByMeaningQuery)
        // BİLEREK ayrı, bkz. SearchWikiPageSuggestionsQuery'deki not.
        group.MapGet("/search-suggestions", async (IMediator mediator, string q = "") =>
        {
            var suggestions = await mediator.Send(new SearchWikiPageSuggestionsQuery(q));
            return Results.Ok(suggestions);
        })
        .WithName("SearchWikiPageSuggestions");

        // Arama sonucundaki bir chunk'a tıklanınca tam sayfayı göstermek için -
        // aynı görünürlük kuralı burada da uygulanıyor (bkz. GetWikiPageByIdQueryHandler).
        group.MapGet("/pages/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var page = await mediator.Send(new GetWikiPageByIdQuery(id));
            return page is null ? Results.NotFound() : Results.Ok(page);
        })
        .WithName("GetWikiPageById");

        group.MapPost("/pages", async (CreateWikiPageCommand command, IMediator mediator) =>
        {
            var newPageId = await mediator.Send(command);
            return Results.Ok(new { id = newPageId });
        })
        .WithName("CreateWikiPage")
        .RequireAuthorization();

        // Yetki kuralı istemciden gelmiyor - Handler, Admin mi yoksa sayfanın
        // sahibi mi diye ICurrentUserAccessor üzerinden kendisi karar veriyor
        // (bkz. DeleteWikiPageCommandHandler).
        group.MapDelete("/pages/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteWikiPageCommand(id));
            return Results.NoContent();
        })
        .WithName("DeleteWikiPage")
        .RequireAuthorization();

        // DeleteWikiPage'deki AYNI desen - id route'tan, geri kalan gövdeden.
        // Yetki kuralı (Admin ya da sahip) yine Handler'da, istemciden gelmiyor.
        group.MapPut("/pages/{id:guid}", async (Guid id, UpdateWikiPageRequest request, IMediator mediator) =>
        {
            await mediator.Send(new UpdateWikiPageCommand(id, request.Title, request.Content, request.Visibility, request.FolderId, request.Tags));
            return Results.NoContent();
        })
        .WithName("UpdateWikiPage")
        .RequireAuthorization();

        // Version History (2026-08-17) - GetWikiPageById ile AYNI "varlığı
        // gizle" deseni (açık endpoint, görünürlük filtresi otomatik). SADECE
        // ESKİ versiyonları döndürür (güncel versiyon zaten GET /pages/{id}'in
        // currentVersionNumber alanında) - Documents'ın versiyon listesiyle
        // AYNI ayrım.
        group.MapGet("/pages/{id:guid}/versions", async (Guid id, IMediator mediator) =>
        {
            var versions = await mediator.Send(new GetWikiPageVersionsQuery(id));
            return versions is null ? Results.NotFound() : Results.Ok(versions);
        })
        .WithName("GetWikiPageVersions");

        group.MapGet("/pages/{id:guid}/versions/{versionNumber:int}", async (Guid id, int versionNumber, IMediator mediator) =>
        {
            var version = await mediator.Send(new GetWikiPageVersionByNumberQuery(id, versionNumber));
            return version is null ? Results.NotFound() : Results.Ok(version);
        })
        .WithName("GetWikiPageVersionByNumber");

        // Yetki kuralı istemciden gelmiyor - UpdateWikiPage ile AYNI owner-or-Admin
        // kararı Handler'da (bkz. RestoreWikiPageVersionCommandHandler).
        group.MapPost("/pages/{id:guid}/versions/{versionNumber:int}/restore", async (Guid id, int versionNumber, IMediator mediator) =>
        {
            await mediator.Send(new RestoreWikiPageVersionCommand(id, versionNumber));
            return Results.NoContent();
        })
        .WithName("RestoreWikiPageVersion")
        .RequireAuthorization();

        // GetWikiPages ile AYNI desen - açık endpoint, departmanı istemci seçer
        // ("hangi ağacı gösteriyorum") ama SONUÇTA neyin göründüğü Handler'ın
        // JWT'den okuduğu gerçek departmana/Admin rolüne göre belirlenir.
        group.MapGet("/folders", async (string departmentName, IMediator mediator) =>
        {
            var tree = await mediator.Send(new GetWikiFolderTreeQuery(departmentName));
            return Results.Ok(tree);
        })
        .WithName("GetWikiFolderTree");

        group.MapPost("/folders", async (CreateWikiFolderCommand command, IMediator mediator) =>
        {
            var newFolderId = await mediator.Send(command);
            return Results.Ok(new { id = newFolderId });
        })
        .WithName("CreateWikiFolder")
        .RequireAuthorization();

        // "Tartışma" sekmesi (bkz. HomePage/WikiArticlePage'deki DiscussionPanel).
        // GetWikiPages ile AYNI desen - açık endpoint (Public sayfalar/genel
        // tartışma giriş yapmamış bir ziyaretçiye de görünür olabilir), ama
        // pageId verilmiş VE o sayfa görünmüyorsa Handler boş liste döndürüyor
        // (bkz. GetCommentsQueryHandler'daki not).
        group.MapGet("/comments", async (IMediator mediator, Guid? pageId) =>
        {
            var comments = await mediator.Send(new GetCommentsQuery(pageId));
            return Results.Ok(comments);
        })
        .WithName("GetComments");

        group.MapPost("/comments", async (CreateCommentCommand command, IMediator mediator) =>
        {
            var newCommentId = await mediator.Send(command);
            return Results.Ok(new { id = newCommentId });
        })
        .WithName("CreateComment")
        .RequireAuthorization();

        // Yetki kuralı istemciden gelmiyor - DeleteWikiPage'deki AYNI desen,
        // Handler Admin mi yoksa yorumun sahibi mi diye kendisi karar veriyor.
        group.MapDelete("/comments/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteCommentCommand(id));
            return Results.NoContent();
        })
        .WithName("DeleteComment")
        .RequireAuthorization();

        // Faz: Favoriler/Pinler. Tek bir toggle endpoint'i (ayrı POST/DELETE
        // çifti değil) - istemci "şu an favori mi" durumunu zaten bildiği için
        // (sayfa/liste zaten yıldızı dolu/boş gösteriyor), tek bir tıklamanın
        // karşılığı tek bir istek. Yanıttaki bool, istemcinin ek bir GET
        // atmadan ikonu güncelleyebilmesi için.
        group.MapPost("/pages/{id:guid}/favorite", async (Guid id, IMediator mediator) =>
        {
            var isFavorited = await mediator.Send(new ToggleFavoriteCommand(id));
            return Results.Ok(new { isFavorited });
        })
        .WithName("ToggleFavorite")
        .RequireAuthorization();

        group.MapPost("/pages/{id:guid}/pin", async (Guid id, IMediator mediator) =>
        {
            var isPinned = await mediator.Send(new TogglePinCommand(id));
            return Results.Ok(new { isPinned });
        })
        .WithName("TogglePin")
        .RequireAuthorization();

        group.MapGet("/favorites", async (IMediator mediator) =>
        {
            var pages = await mediator.Send(new GetFavoritePagesQuery());
            return Results.Ok(pages);
        })
        .WithName("GetFavoritePages")
        .RequireAuthorization();

        group.MapGet("/pinned", async (IMediator mediator) =>
        {
            var pages = await mediator.Send(new GetPinnedPagesQuery());
            return Results.Ok(pages);
        })
        .WithName("GetPinnedPages")
        .RequireAuthorization();

        // Admin aracı: AI'ın embedding indeksini (örn. bir bakım hatası ya da
        // embedding sağlayıcısı değişikliği sonrası) baştan üretmek için var
        // olan TÜM sayfalar için WikiPageCreatedEvent'i yeniden yayınlıyor.
        group.MapPost("/reindex", async (IMediator mediator) =>
        {
            var count = await mediator.Send(new ReindexWikiPagesCommand());
            return Results.Ok(new { reindexedCount = count });
        })
        .WithName("ReindexWikiPages")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }
}

// PUT gövdesi - PageId route'tan geldiği için Command'ın kendisine karışmıyor
// (DeleteWikiPageCommand'ın id'yi route'tan alması, geri kalanı body'den almasıyla
// aynı ayrım).
public record UpdateWikiPageRequest(string Title, string Content, string Visibility, Guid? FolderId, string? Tags = null);
