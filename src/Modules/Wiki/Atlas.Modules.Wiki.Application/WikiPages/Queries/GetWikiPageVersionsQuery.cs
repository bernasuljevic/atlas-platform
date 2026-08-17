using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

// GetWikiPageByIdQuery ile AYNI "varlığı gizle" deseni - sayfa görünür değilse
// (ya da hiç yoksa) null döner, endpoint 404'e çevirir. Görünür ama hiç
// düzenlenmemiş bir sayfa için BOŞ liste döner (null DEĞİL) - "sayfayı
// göremiyorsun" ile "henüz versiyon geçmişi yok" iki farklı durum.
// Content BİLEREK YOK - liste, Documents'ın GetDocumentVersionsQuery'sindeki
// AYNI gerekçeyle sadece METADATA taşıyor (payload'ı küçük tutmak için); tam
// içerik GetWikiPageVersionByNumberQuery'nin işi.
public record GetWikiPageVersionsQuery(Guid PageId) : IRequest<IReadOnlyList<WikiPageVersionSummaryDto>?>;

public record WikiPageVersionSummaryDto(int VersionNumber, string Title, string? EditedByEmail, DateTime EditedAtUtc);
