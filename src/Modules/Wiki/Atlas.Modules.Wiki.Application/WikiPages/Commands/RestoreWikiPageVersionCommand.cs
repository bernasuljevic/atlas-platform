using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

/// <summary>
/// Eski bir versiyona "geri dön" - UpdateWikiPageCommand'ın owner-or-Admin
/// yetki deseniyle AYNI (bkz. Handler). "Silme" değil "git revert" mantığı:
/// geri dönüş, versiyon geçmişindeki eski satırı SİLMEZ/taşımaz, sayfanın
/// GÜNCEL hâlini eski içeriğe eşitler ve bunu YENİ bir versiyon olarak
/// kaydeder - önceki (geri dönülmeden hemen önceki) hâl de kendi snapshot'ı
/// olarak arşive düşer, hiçbir şey kaybolmuyor.
/// </summary>
public record RestoreWikiPageVersionCommand(Guid PageId, int VersionNumber)
    : IRequest, ICacheInvalidatingCommand, IAuditableCommand
{
    public string CacheKeyToInvalidate => "wiki-pages:all";

    public string AuditAction => "WikiPage.VersionRestored";
    public string? AuditResourceId => PageId.ToString();
    public string? AuditDetails { get; set; }
}
