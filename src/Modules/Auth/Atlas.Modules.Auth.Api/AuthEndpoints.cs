using Atlas.Modules.Auth.Application.Users.Commands;
using Atlas.Modules.Auth.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Atlas.Modules.Auth.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // Artık UserQueries'i doğrudan tanımıyoruz - sadece IMediator'a "bu Query'yi
        // gönder" diyoruz, hangi Handler'ın cevap vereceğini bilmemize gerek yok.
        group.MapGet("/users", async (IMediator mediator) =>
        {
            var users = await mediator.Send(new GetAllUsersQuery());
            return Results.Ok(users);
        })
        .WithName("GetAllUsers")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // YENİ: Gerçek bir Command örneği. RegisterUserCommand, Minimal API'nin
        // "request body'den otomatik doldur" özelliğiyle JSON gövdesinden okunuyor.
        group.MapPost("/register", async (RegisterUserCommand command, IMediator mediator) =>
        {
            var newUserId = await mediator.Send(command);
            return Results.Ok(new { id = newUserId });
        })
        .WithName("RegisterUser");

        group.MapPost("/login", async (LoginCommand command, IMediator mediator) =>
        {
            var tokens = await mediator.Send(command);
            return tokens is null
                ? Results.Unauthorized()
                : Results.Ok(tokens);
        })
        .WithName("Login");

        // Access token süresi dolunca (15dk) React tarafı buraya refresh token'ı
        // gönderiyor, karşılığında yeni bir access+refresh token çifti alıyor -
        // kullanıcı yeniden email/şifre girmek zorunda kalmıyor.
        group.MapPost("/refresh", async (RefreshTokenCommand command, IMediator mediator) =>
        {
            var tokens = await mediator.Send(command);
            return tokens is null
                ? Results.Unauthorized()
                : Results.Ok(tokens);
        })
        .WithName("RefreshToken");

        return app;
    }
}