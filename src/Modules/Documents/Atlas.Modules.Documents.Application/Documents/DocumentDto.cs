using Atlas.Modules.Documents.Domain.Entities;

namespace Atlas.Modules.Documents.Application.Documents;

// StorageKey BİLEREK YOK - Vault'un PasswordEntryDto'sunun şifreyi hiç
// içermemesiyle AYNI gerekçe: istemci diske giden gerçek yolu asla görmemeli,
// dosyaya erişimin TEK yolu authenticated download endpoint'i (Gün 4) olmalı.
public record DocumentDto(
    Guid Id,
    string Title,
    string OriginalFileName,
    string ContentType,
    string FileExtension,
    long SizeBytes,
    string DepartmentName,
    string Visibility,
    Guid CreatedByUserId,
    string? CreatedByEmail,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? Description,
    string? Tags,
    string Status,
    string? ProcessingError);

public static class DocumentMappingExtensions
{
    public static DocumentDto ToDto(this Document document) => new(
        document.Id, document.Title, document.OriginalFileName, document.ContentType, document.FileExtension,
        document.SizeBytes, document.DepartmentName, document.Visibility.ToString(), document.CreatedByUserId,
        document.CreatedByEmail, document.CreatedAtUtc, document.UpdatedAtUtc, document.Description, document.Tags,
        document.Status.ToString(), document.ProcessingError);
}
