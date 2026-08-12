using Atlas.Modules.AI.Application.Search.Queries;
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

        // Token gerekli - SearchByMeaningQueryHandler, sonuçları
        // ICurrentUserAccessor.Department/IsAdmin'e göre filtreliyor (bkz.
        // IWikiVisibilityChecker); anonim bir istek sadece Public sonuçları
        // görürdü, bu da kafa karıştırıcı olurdu - net bir kural: giriş şart.
        // Route/isim BİLEREK değişmedi (zaten "SearchWikiPages" DIŞINDA
        // "/api/ai/search" adı hep kaynak-agnostikti) - P5'ten itibaren
        // sonuçlar hem wiki sayfalarını hem belgeleri kapsıyor (bkz.
        // SemanticSearchResultDto.SourceType).
        group.MapGet("/search", async (
            string q, IMediator mediator, int topN = 5, DateTime? fromUtc = null, DateTime? toUtc = null) =>
        {
            var results = await mediator.Send(new SearchByMeaningQuery(q, topN, fromUtc, toUtc));
            return Results.Ok(results);
        })
        .WithName("SearchByMeaning")
        .RequireAuthorization()
        // Embedding çağrısı + vector arama içeriyor, "ucuz" bir endpoint değil -
        // kullanıcı bazlı bir sınır (bkz. Program.cs).
        .RequireRateLimiting("ai-search");

        return app;
    }
}
