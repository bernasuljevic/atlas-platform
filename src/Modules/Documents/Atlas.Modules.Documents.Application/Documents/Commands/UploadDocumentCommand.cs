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
    string? Tags) : IRequest<UploadDocumentResult>, IAuditableCommand
{
    public string AuditAction => "Document.Uploaded";
    // P6 Gün 3'ten ÖNCE burası "=> null" idi (TResponse Guid'in kendisiydi,
    // AuditBehavior onu (response as Guid?) ile çeviriyordu - bkz.
    // AuditBehavior'daki yorum). Yanıt artık Guid DEĞİL, UploadDocumentResult
    // (duplicate-detection bilgisini de taşımak için) - o cast artık işe
    // yaramayacağından AuditResourceId SETTABLE'a çevrildi, Handler
    // Document oluşturulur oluşmaz kendisi dolduruyor (AuditDetails'le AYNI
    // "Handler doldurur, AuditBehavior next() sonrası okur" deseni).
    public string? AuditResourceId { get; set; }
    public string? AuditDetails { get; set; }
}

// DuplicateOfDocumentId/DuplicateOfTitle - P6 Gün 3 (duplicate-detection).
// Her ikisi de null ise eşleşen bir belge yok demektir. DOLU olması yükleme
// BAŞARISIZ oldu anlamına GELMEZ - belge zaten oluşturuldu, bu sadece
// istemciye "farkında ol" bilgisi (spec: "engellemiyor, sadece uyarıyor").
public record UploadDocumentResult(Guid Id, Guid? DuplicateOfDocumentId, string? DuplicateOfTitle);
