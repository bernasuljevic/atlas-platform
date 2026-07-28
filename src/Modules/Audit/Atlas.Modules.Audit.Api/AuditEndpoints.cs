using Atlas.Modules.Audit.Application.AuditLog.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Atlas.Modules.Audit.Api;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit-log").WithTags("Audit");

        // Sadece Admin - audit log'un kendisi "kim ne yaptı" bilgisi taşıyor,
        // normal bir kullanıcının başka kullanıcıların işlemlerini görebilmesi
        // ayrı bir güvenlik/gizlilik sorunu olurdu (bkz. AuthEndpoints'teki
        // GET /users ile aynı desen).
        group.MapGet("/", async (
            IMediator mediator,
            string? action,
            DateTime? fromUtc,
            DateTime? toUtc,
            int pageNumber = 1,
            int pageSize = 20) =>
        {
            var result = await mediator.Send(new GetAuditLogQuery(action, fromUtc, toUtc, pageNumber, pageSize));
            return Results.Ok(result);
        })
        .WithName("GetAuditLog")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }
}
