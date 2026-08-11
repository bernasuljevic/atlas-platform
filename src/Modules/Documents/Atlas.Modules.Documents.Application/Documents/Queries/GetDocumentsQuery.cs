using Atlas.Modules.Documents.Application.Documents;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

// GetWikiPagesQuery ile AYNI desen - DepartmentName/Status BİLEREK opsiyonel
// ek daraltma filtreleri (görünürlük kontrolünün YERİNE geçmiyor, ONA EK).
// Sayfalama filtrelemeden SONRA, bellekte uygulanıyor (Vault'un "kişisel
// liste küçük" kararının aksine, Document Library HERKESİN erişebildiği,
// büyüyebilecek bir alan - bu yüzden Wiki'nin sayfalama deseni tercih edildi).
public record GetDocumentsQuery(
    string? DepartmentName = null, string? Status = null, int PageNumber = 1, int PageSize = 10)
    : IRequest<PagedResult<DocumentDto>>;
