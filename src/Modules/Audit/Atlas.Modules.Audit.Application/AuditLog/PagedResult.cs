namespace Atlas.Modules.Audit.Application.AuditLog;

// Wiki.Application'daki PagedResult<T> ile aynı şekil - ama Audit, Wiki'nin
// Application katmanına referans veremez (modüller arası izolasyon), o yüzden
// küçük bir kopya burada. Bu tür bir zarfın (Items/PageNumber/PageSize/
// TotalCount) ÜÇÜNCÜ modülde de tekrarlanması gerekirse Shared'a taşınmalı
// (bkz. CLAUDE.md "İzlenecek teknik borç" - "üç kural" eşiği).
public record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
