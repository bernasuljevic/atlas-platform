using MediatR;

namespace Atlas.Modules.Audit.Application.AuditLog.Queries;

/// <summary>
/// Action/FromUtc/ToUtc HEPSİ opsiyonel - Admin ekranı (Gün 3) hiç filtresiz
/// "en yeni önce" bir liste de gösterebilmeli. Sıralama/filtreleme/sayfalama
/// DB seviyesinde (EfAuditLogRepository) yapılıyor - Wiki'nin "tüm veriyi çek,
/// bellekte filtrele" yaklaşımının aksine, çünkü audit log zamanla büyümesi
/// beklenen bir tablo (tüm satırları cache'lemek/belleğe çekmek mantıklı değil).
/// </summary>
public record GetAuditLogQuery(
    string? Action = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<AuditLogEntryDto>>;
