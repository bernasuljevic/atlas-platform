namespace Atlas.Modules.Documents.Application.Documents;

// UploadDocumentCommandValidator'daki listenin TEK doğruluk kaynağına
// çıkarılmış hali (P6 Gün 2) - UploadNewDocumentVersionCommandValidator da
// AYNI listeye ihtiyaç duyunca (bir versiyonun uzantısı orijinal belgeninkiyle
// aynı OLMAK ZORUNDA değil - ör. .docx'i .pdf'e "export edip" yeni versiyon
// olarak yüklemek makul bir senaryo) iki ayrı kopya birbirinden habersiz
// eskimesin diye buraya taşındı.
public static class AllowedDocumentExtensions
{
    public static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        "pdf", "doc", "docx", "odt", "rtf", "txt", "md",
        // Presentations
        "ppt", "pptx", "odp",
        // Spreadsheets
        "xls", "xlsx", "csv", "ods",
        // Data / Technical
        "json", "xml", "yaml", "yml", "sql", "log",
        // Images
        "png", "jpg", "jpeg", "webp", "svg",
        // Video
        "mp4", "webm", "mov",
        // Audio
        "mp3", "wav",
        // Archive
        "zip",
    };
}
