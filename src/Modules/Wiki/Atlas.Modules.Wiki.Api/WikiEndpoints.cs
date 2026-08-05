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
        // GetWikiDashboardQueryHandler).
        group.MapGet("/dashboard", async (IMediator mediator) =>
        {
            var dashboard = await mediator.Send(new GetWikiDashboardQuery());
            return Results.Ok(dashboard);
        })
        .WithName("GetWikiDashboard");

        // Üst bardaki arama kutusunun harf harf çağırdığı hafif öneri
        // endpoint'i - AI'ın anlam bazlı aramasından (SearchWikiPagesByMeaningQuery)
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
