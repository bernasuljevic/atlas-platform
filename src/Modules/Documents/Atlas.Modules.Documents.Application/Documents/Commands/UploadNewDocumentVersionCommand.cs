using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

// UploadDocumentCommand'ın "yeni belge" hâlinden FARKLI - VAR OLAN bir
// Document'ın dosyasını değiştiriyor, Title/DepartmentName/Visibility/Tags
// GİBİ metadata alanlarını hiç almıyor (bunlar için zaten UpdateDocumentCommand
// var - ikisi farklı sorumluluklar). Owner-or-Admin yetkisi (Delete/Update/
// Reprocess ile AYNI desen) Handler'da kontrol ediliyor.
public record UploadNewDocumentVersionCommand(
    Guid DocumentId,
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long SizeBytes) : IRequest, IAuditableCommand
{
    public string AuditAction => "Document.VersionUploaded";
    public string? AuditResourceId => DocumentId.ToString();
    public string? AuditDetails { get; set; }
}
