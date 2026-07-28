using MediatR;

namespace Atlas.Modules.Audit.Application.AuditLog.Queries;

/// <summary>
/// Details/FromUtc/ToUtc HEPSİ opsiyonel - Admin ekranı (Gün 3) hiç filtresiz
/// "en yeni önce" bir liste de gösterebilmeli. Sıralama/filtreleme/sayfalama
/// DB seviyesinde (EfAuditLogRepository) yapılıyor - Wiki'nin "tüm veriyi çek,
/// bellekte filtrele" yaklaşımının aksine, çünkü audit log zamanla büyümesi
/// beklenen bir tablo (tüm satırları cache'lemek/belleğe çekmek mantıklı değil).
///
/// BİLEREK Action yerine Details ile filtreleniyor (2026-07-28, kullanıcı
/// isteğiyle değiştirildi) - Action sabit iki değerden (WikiPage.Created/
/// Deleted) biri olduğu için tam eşleşme aramak pratik değildi ("hangi sayfa"
/// sorusuna cevap vermiyordu). Details (sayfa başlığının anlık kopyası)
/// üzerinden KISMİ eşleşme aramak, "bu sayfayla ilgili tüm işlemleri göster"
/// gibi gerçek bir admin ihtiyacına karşılık geliyor.
/// </summary>
public record GetAuditLogQuery(
    string? Details = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<AuditLogEntryDto>>;
