namespace Atlas.Modules.Documents.Application.Abstractions;

// Gün 2'de Infrastructure/Storage altındaydı - Gün 3'te UploadDocumentCommandValidator'ın
// da MaxFileSizeBytes'a ihtiyacı olduğu ortaya çıkınca BURAYA taşındı (saf bir
// POCO, System.IO'ya bağımlı değil - Infrastructure'da durmasının hiçbir
// teknik gerekçesi yoktu, sadece "storage'ın seçenekleri" diye ilk düşünceyle
// oraya konmuştu). LocalDiskFileStorageService (Infrastructure) de BURADAN
// import ediyor artık.
public class FileStorageOptions
{
    public string RootPath { get; set; } = default!;
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50 MB varsayılan
}
