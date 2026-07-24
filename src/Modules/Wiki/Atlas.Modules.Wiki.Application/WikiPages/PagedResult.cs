namespace Atlas.Modules.Wiki.Application.WikiPages;

// Genel amaçlı bir sayfalama zarfı - şimdilik sadece GetWikiPagesQuery kullanıyor
// ama isim WikiPage'e özel değil, ileride başka bir liste endpoint'i de kullanabilir.
public record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
