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
        group.MapGet("/pages", async (IMediator mediator) =>
        {
            var pages = await mediator.Send(new GetWikiPagesQuery());
            return Results.Ok(pages);
        })
        .WithName("GetWikiPages");

        group.MapPost("/pages", async (CreateWikiPageCommand command, IMediator mediator) =>
        {
            var newPageId = await mediator.Send(command);
            return Results.Ok(new { id = newPageId });
        })
        .WithName("CreateWikiPage")
        .RequireAuthorization();

        return app;
    }
}
