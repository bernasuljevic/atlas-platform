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

        // POST /api/wiki/reindex ile AYNI gerekçe: embedding sağlayıcısı
        // değişince (Fake -> gerçek) var olan TÜM belgeler yeniden işlensin.
        // ReprocessDocument'ın (owner-or-Admin, tekil) AKSİNE Admin-only VE
        // bulk - farklı bir senaryoya hizmet ediyor, bkz. ReindexDocumentsCommand.
        group.MapPost("/reindex", async (IMediator mediator) =>
        {
            var count = await mediator.Send(new ReindexDocumentsCommand());
            return Results.Ok(new { reindexedCount = count });
        })
        .WithName("ReindexDocuments")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

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

            // UploadDocumentResult (P6 Gün 3) - id'nin YANINDA artık isteğe
            // bağlı duplicateOfDocumentId/duplicateOfTitle de taşıyor
            // (ikisi de null ise eşleşme yok demektir).
            var result = await mediator.Send(command);
            return Results.Ok(result);
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

        // P6 (versiyonlama) - Upload endpoint'iyle AYNI multipart deseni, ama
        // Title/Visibility/DepartmentName/Description/Tags YOK (bunlar için
        // PUT /{id} zaten var - dosya değiştirmek AYRI bir sorumluluk).
        // owner-or-Admin yetkisi Handler'da (Delete/Update/Reprocess ile AYNI).
        group.MapPost("/{id:guid}/versions", async (Guid id, IFormFile file, IMediator mediator) =>
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadNewDocumentVersionCommand(id, stream, file.FileName, file.ContentType, file.Length);
            await mediator.Send(command);
            return Results.Accepted();
        })
        .WithName("UploadNewDocumentVersion")
        .RequireAuthorization()
        .DisableAntiforgery();

        // GetDocumentById ile AYNI "varlığı gizle" deseni (görünmüyorsa 404) -
        // ama Handler burada null'u "belge yok/görünmüyor" İLE "belge var ama
        // hiç eski versiyonu yok" arasında AYIRT ediyor (bkz. Query'deki not),
        // bu yüzden boş liste 200 ile dönüyor, null 404'e çevriliyor.
        group.MapGet("/{id:guid}/versions", async (Guid id, IMediator mediator) =>
        {
            var versions = await mediator.Send(new GetDocumentVersionsQuery(id));
            return versions is null ? Results.NotFound() : Results.Ok(versions);
        })
        .WithName("GetDocumentVersions");

        // DownloadDocument ile AYNI desen - token gerektirir, StorageKey
        // istemciye hiç sızmıyor.
        group.MapGet("/{id:guid}/versions/{versionNumber:int}/download", async (
            Guid id, int versionNumber, IMediator mediator, IFileStorageService fileStorageService) =>
        {
            var downloadInfo = await mediator.Send(new GetDocumentVersionDownloadInfoQuery(id, versionNumber));
            if (downloadInfo is null)
                return Results.NotFound();

            var stream = await fileStorageService.OpenReadAsync(downloadInfo.StorageKey);
            return Results.File(stream, downloadInfo.ContentType, downloadInfo.OriginalFileName);
        })
        .WithName("DownloadDocumentVersion")
        .RequireAuthorization();

        return app;
    }
}

// PUT gövdesi - id route'tan geldiği için Command'ın kendisine karışmıyor
// (UpdateWikiPageRequest ile AYNI ayrım).
public record UpdateDocumentRequest(string Title, string? Description, string Visibility, string? Tags);
