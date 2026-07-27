using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

/// <summary>
/// Admin aracı: var olan TÜM wiki sayfaları için WikiPageCreatedEvent'i yeniden
/// yayınlıyor - AI'ın embedding'lerini (örn. bir bakım hatası ya da embedding
/// sağlayıcısı değişikliği sonrası) baştan üretmesini tetikliyor. Wiki, AI'ın
/// var olduğundan yine habersiz - sadece kendi event'ini kendi verisi için
/// tekrar yayınlıyor (modül izolasyon kuralını bozmuyor).
/// </summary>
public record ReindexWikiPagesCommand : IRequest<int>;
