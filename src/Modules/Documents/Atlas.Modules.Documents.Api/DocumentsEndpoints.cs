using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Application.Documents.Commands;
using Atlas.Modules.Documents.Application.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Atlas.Modules.Documents.Api;

public static class DocumentsEndpoints
{
    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        // GetWikiPages ile AYNI desen - açık endpoint, görünürlük filtresi
        // ICurrentUserAccessor'dan (varsa) otomatik uygulanıyor. Anonim bir
        // istek sadece Public belgeleri görür.
        group.MapGet("/", async (
            IMediator mediator, string? departmentName, string? status, int pageNumber = 1, int pageSize = 10) =>
        {
            var result = await mediator.Send(new GetDocumentsQuery(departmentName, status, pageNumber, pageSize));
            return Results.Ok(result);
        })
        .WithName("GetDocuments");

        // GetWikiPageById ile AYNI "varlığı gizle" deseni - Handler null
        // dönerse 404, başka departmanın DepartmentOnly belgesinin ID'si
        // tahmin edilse bile "yok" gibi davranılıyor.
        // SearchWikiPageSuggestions ile AYNI desen (auth GEREKMİYOR, görünürlük
        // filtresi ICurrentUserAccessor'dan otomatik) - WikiEditorPage'in link
        // penceresi (P5 Gün 4) bunu Wiki'ninkiyle BİRLİKTE çağırıp tek bir
        // öneri listesinde birleştiriyor.
        group.MapGet("/search-suggestions", async (IMediator mediator, string q = "") =>
        {
            var suggestions = await mediator.Send(new SearchDocumentSuggestionsQuery(q));
            return Results.Ok(suggestions);
        })
        .WithName("SearchDocumentSuggestions");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var document = await mediator.Send(new GetDocumentByIdQuery(id));
            return document is null ? Results.NotFound() : Results.Ok(document);
        })
        .WithName("GetDocumentById");

        // İndirme SADECE bu endpoint üzerinden - UseStaticFiles KULLANILMIYOR,
        // dosyanın diskteki gerçek yolu (StorageKey) hiçbir zaman istemciye
        // sızmıyor (bkz. GetDocumentDownloadInfoQuery'deki not). Authenticated
        // olması BİLİNÇLİ bir ek katman - görünürlük kontrolü zaten Handler'da
        // uygulanıyor ama indirme (liste/detay'ın aksine) somut bir dosya
        // transferi, giriş şartı Vault'un reveal'ıyla aynı temkinli yaklaşım.
        group.MapGet("/{id:guid}/download", async (Guid id, IMediator mediator, IFileStorageService fileStorageService) =>
        {
            var downloadInfo = await mediator.Send(new GetDocumentDownloadInfoQuery(id));
            if (downloadInfo is null)
                return Results.NotFound();

            var stream = await fileStorageService.OpenReadAsync(downloadInfo.StorageKey);
            return Results.File(stream, downloadInfo.ContentType, downloadInfo.OriginalFileName);
        })
        .WithName("DownloadDocument")
        .RequireAuthorization();

        // multipart/form-data - UploadDocumentCommand'daki notta açıklandığı
        // gibi minimal API'de bir record'a doğrudan bind edilemiyor. IFormFile
        // otomatik tanınan bir tip (ek bir attribute gerekmiyor), diğer basit
        // form alanları [FromForm] ile işaretlenmeli - aksi halde ASP.NET Core
        // bunları query string'den okumaya çalışırdı.
        group.MapPost("/upload", async (
            IFormFile file,
            [FromForm] string title,
            [FromForm] string visibility,
            [FromForm] string? departmentName,
            [FromForm] string? description,
            [FromForm] string? tags,
            IMediator mediator) =>
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadDocumentCommand(
                stream, file.FileName, file.ContentType, file.Length,
                title, departmentName, visibility, description, tags);

            var newDocumentId = await mediator.Send(command);
            return Results.Ok(new { id = newDocumentId });
        })
        .WithName("UploadDocument")
        .RequireAuthorization()
        .DisableAntiforgery();

        // DeleteWikiPage/DeletePasswordEntry ile AYNI desen - yetki kuralı
        // (Admin ya da belgenin sahibi) istemciden gelmiyor, Handler kendisi
        // karar veriyor.
        group.MapPut("/{id:guid}", async (Guid id, UpdateDocumentRequest request, IMediator mediator) =>
        {
            await mediator.Send(new UpdateDocumentCommand(id, request.Title, request.Description, request.Visibility, request.Tags));
            return Results.NoContent();
        })
        .WithName("UpdateDocument")
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteDocumentCommand(id));
            return Results.NoContent();
        })
        .WithName("DeleteDocument")
        .RequireAuthorization();

        // POST /api/wiki/reindex'in Documents'taki karşılığı - AMA tek bir
        // belgeyi hedefliyor, Admin-only bulk DEĞİL. owner-or-Admin yetkisi
        // (Delete/Update ile AYNI desen) Handler'da uygulanıyor - istemciden
        // gelen bir alana güvenilmiyor. Failed'a düşmüş bir belgeyi (ör. o an
        // desteklenmeyen bir uzantıydı, sonradan bir processor eklendi) sahibi/
        // Admin elle yeniden tetikleyebilsin diye.
        group.MapPost("/{id:guid}/reprocess", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new ReprocessDocumentCommand(id));
            return Results.Accepted();
        })
        .WithName("ReprocessDocument")
        .RequireAuthorization();

        return app;
    }
}

// PUT gövdesi - id route'tan geldiği için Command'ın kendisine karışmıyor
// (UpdateWikiPageRequest ile AYNI ayrım).
public record UpdateDocumentRequest(string Title, string? Description, string Visibility, string? Tags);
