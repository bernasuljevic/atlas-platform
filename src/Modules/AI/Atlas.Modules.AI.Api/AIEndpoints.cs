using Atlas.Modules.AI.Application.WikiPages.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Atlas.Modules.AI.Api;

public static class AIEndpoints
{
    public static IEndpointRouteBuilder MapAIEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").WithTags("AI");

        // Token gerekli - SearchWikiPagesByMeaningQueryHandler, sonuçları
        // ICurrentUserAccessor.Department/IsAdmin'e göre filtreliyor (bkz.
        // IWikiVisibilityChecker); anonim bir istek sadece Public sonuçları
        // görürdü, bu da kafa karıştırıcı olurdu - net bir kural: giriş şart.
        group.MapGet("/search", async (string q, IMediator mediator, int topN = 5) =>
        {
            var results = await mediator.Send(new SearchWikiPagesByMeaningQuery(q, topN));
            return Results.Ok(results);
        })
        .WithName("SearchWikiPages")
        .RequireAuthorization();

        return app;
    }
}
