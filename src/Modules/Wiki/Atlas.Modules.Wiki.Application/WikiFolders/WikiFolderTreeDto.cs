namespace Atlas.Modules.Wiki.Application.WikiFolders;

/// <summary>
/// Bir departmanın klasör ağacı. Folders = kök seviyesindeki klasörler (her biri
/// kendi Children/Pages'ini iç içe taşır). UnfiledPages = hiçbir klasöre
/// dosyalanmamış (FolderId=null), departmanın kök seviyesindeki sayfalar -
/// klasörleme özelliğinden ÖNCE oluşturulmuş tüm sayfalar burada yaşar.
/// </summary>
public record WikiFolderTreeDto(
    IReadOnlyList<WikiFolderNodeDto> Folders,
    IReadOnlyList<WikiPageSummaryDto> UnfiledPages);

public record WikiFolderNodeDto(
    Guid Id,
    string Name,
    IReadOnlyList<WikiFolderNodeDto> Children,
    IReadOnlyList<WikiPageSummaryDto> Pages);

// Ağaçta göstermek için sayfanın tam İçerik'i (Content) BİLEREK yok - tıklanınca
// GET /api/wiki/pages/{id} zaten tam sayfayı getiriyor (bkz. WikiEndpoints).
public record WikiPageSummaryDto(
    Guid Id,
    string Title,
    string Visibility,
    DateTime CreatedAtUtc);
