using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;

namespace Atlas.Modules.Wiki.Application.WikiPages;

/// <summary>
/// IWikiVisibilityChecker'ın (Shared.Contracts) gerçek implementasyonu - ince bir
/// sarmalayıcı, tüm gerçek iş kuralı hâlâ WikiVisibilityRules'ta (Wiki.Domain)
/// yaşıyor. AI modülü bu sınıfı hiç görmüyor, sadece arayüzü biliyor.
/// </summary>
public class WikiVisibilityChecker : IWikiVisibilityChecker
{
    public bool IsVisibleTo(string visibility, string departmentName, string? viewerDepartmentName, bool viewerIsAdmin = false)
    {
        var parsedVisibility = Enum.Parse<WikiVisibility>(visibility);
        return WikiVisibilityRules.IsVisibleTo(parsedVisibility, departmentName, viewerDepartmentName, viewerIsAdmin);
    }
}
