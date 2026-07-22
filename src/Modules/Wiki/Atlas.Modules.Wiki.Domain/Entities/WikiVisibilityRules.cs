using Atlas.Modules.Wiki.Domain.Enums;

namespace Atlas.Modules.Wiki.Domain.Entities;

/// <summary>
/// Yetkilendirme kuralının kalbi - "bu sayfayı şu departmandaki biri görebilir mi?"
/// sorusunun cevabı burada, TEK YERDE yaşıyor. Hem WikiPage (Domain nesnesi) hem de
/// WikiPageDto (Application katmanında, cache'lenmiş veri üzerinde) bu kuralı çağırır -
/// böylece aynı mantık iki yerde ayrı ayrı yazılıp birbirinden sapma riski taşımaz.
/// </summary>
public static class WikiVisibilityRules
{
    public static bool IsVisibleTo(WikiVisibility visibility, string departmentName, string? viewerDepartmentName)
    {
        if (visibility == WikiVisibility.Public)
            return true;

        return !string.IsNullOrWhiteSpace(viewerDepartmentName)
            && string.Equals(departmentName, viewerDepartmentName, StringComparison.OrdinalIgnoreCase);
    }
}