using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

// Content bir Stream - multipart/form-data'nın minimal API'de bir record'a
// doğrudan bind edilememesi yüzünden endpoint (DocumentsEndpoints.cs) bu
// Command'ı IFormFile'dan elle kurup IMediator.Send ediyor (bilinçli bir
// sapma, CreateWikiPageCommand gibi doğrudan bind edilen komutlardan farklı -
// dosya yükleme mecburiyeti).
//
// IAuditableCommand implemente ediyor - Wiki'nin Create/Delete'i, Vault'un
// Create/Update/Delete/Reveal'ı ile AYNI gerekçe: bir belgenin oluşturulması
// denetlenmesi gereken bir eylem.
public record UploadDocumentCommand(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Title,
    string? DepartmentName,
    string Visibility,
    string? Description,
    string? Tags) : IRequest<Guid>, IAuditableCommand
{
    public string AuditAction => "Document.Uploaded";
    // AuditResourceId BİLEREK null - CreateWikiPageCommand'daki AYNI gerekçe,
    // yeni belgenin ID'si Handler çalışana kadar bilinmiyor. AuditBehavior
    // bunu TResponse'un (Guid) kendisinden türetiyor.
    public string? AuditResourceId => null;
    public string? AuditDetails { get; set; }
}
