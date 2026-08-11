namespace Atlas.Modules.Documents.Application.Documents;

// Wiki.Application/WikiPages/PagedResult.cs ve Audit.Application/AuditLog/
// PagedResult.cs ile BİREBİR aynı şekil - her modül kendi kopyasını tutuyor
// (bkz. proje mimari kararı: modüller arası paylaşılan bir Shared.CQRS DTO'su
// YOK, küçük, durumsuz bir record'u paylaşmak modül izolasyonunu bozmaya
// değmez).
public record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
