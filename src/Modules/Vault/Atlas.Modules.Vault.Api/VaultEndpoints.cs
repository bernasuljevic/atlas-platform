using Atlas.Modules.Vault.Application.PasswordEntries.Commands;
using Atlas.Modules.Vault.Application.PasswordEntries.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Atlas.Modules.Vault.Api;

// PUT'un gövdesi - id route'tan geliyor (WikiEndpoints'teki UpdateWikiPageRequest
// ile AYNI desen), geri kalan alanlar buradan.
public record UpdatePasswordEntryRequest(
    string Title,
    string? Username,
    string? Password,
    string? Url,
    string? Description,
    string? Category,
    string? Notes);

public static class VaultEndpoints
{
    public static IEndpointRouteBuilder MapVaultEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vault/entries").WithTags("Vault");

        // Gün 2: liste + tekil görüntüleme. DİKKAT: İKİSİ DE parolanın
        // KENDİSİNİ döndürmüyor (bkz. PasswordEntryDto'daki not) - Reveal
        // AYRI, kendi audit izini bırakan bir uç nokta (aşağıda).
        // category BİLEREK opsiyonel query string - "Tümü" filtresi hiç
        // gönderilmeyerek ifade ediliyor.
        group.MapGet("/", async (string? category, IMediator mediator) =>
        {
            var entries = await mediator.Send(new GetPasswordEntriesQuery(category));
            return Results.Ok(entries);
        })
        .WithName("GetPasswordEntries")
        .RequireAuthorization();

        // GetWikiPageById'deki AYNI desen - null dönerse 404 (bkz.
        // GetPasswordEntryByIdQueryHandler'daki "varlığını bile sızdırma" notu).
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var entry = await mediator.Send(new GetPasswordEntryByIdQuery(id));
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        })
        .WithName("GetPasswordEntryById")
        .RequireAuthorization();

        // Yetki kuralı istemciden gelmiyor - herhangi bir giriş yapmış kullanıcı
        // KENDİ adına bir kayıt oluşturabilir (WikiEndpoints'teki POST /pages ile
        // aynı desen) - "kimin görebileceği" sorusu GET tarafında (Gün 2) devreye giriyor.
        group.MapPost("/", async (CreatePasswordEntryCommand command, IMediator mediator) =>
        {
            var newEntryId = await mediator.Send(command);
            return Results.Ok(new { id = newEntryId });
        })
        .WithName("CreatePasswordEntry")
        .RequireAuthorization();

        // Yetki kuralı (Admin ya da kaydın sahibi) Handler'da - DeleteWikiPage'deki
        // AYNI desen, istemciden gelmiyor.
        group.MapPut("/{id:guid}", async (Guid id, UpdatePasswordEntryRequest request, IMediator mediator) =>
        {
            await mediator.Send(new UpdatePasswordEntryCommand(
                id, request.Title, request.Username, request.Password, request.Url,
                request.Description, request.Category, request.Notes));
            return Results.NoContent();
        })
        .WithName("UpdatePasswordEntry")
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeletePasswordEntryCommand(id));
            return Results.NoContent();
        })
        .WithName("DeletePasswordEntry")
        .RequireAuthorization();

        // AYRI bir uç nokta (GetById'ye "?reveal=true" gibi bir parametre
        // EKLENMEDİ) - bilinçli bir tasarım kararı: parolayı DÖNDÜREN tek
        // yol bu, ve SADECE bu yol audit'e "Revealed" yazıyor + LastAccessedAtUtc
        // günceliyor. GET'in kendisini asla yan etkili yapmıyoruz (bir GET
        // isteğinin veri değiştirmemesi/denetim izi bırakmaması REST'in temel
        // kuralı) - POST kullanılması da bu yüzden.
        // P7: rate-limit eklendi (bkz. Program.cs'teki "vault-reveal" notu) -
        // login/ai-search/email-verification zaten korunuyordu, bu gerçek
        // bir boşluktu.
        group.MapPost("/{id:guid}/reveal", async (Guid id, IMediator mediator) =>
        {
            var password = await mediator.Send(new RevealPasswordEntryCommand(id));
            return Results.Ok(new { password });
        })
        .WithName("RevealPasswordEntry")
        .RequireAuthorization()
        .RequireRateLimiting("vault-reveal");

        return app;
    }
}
