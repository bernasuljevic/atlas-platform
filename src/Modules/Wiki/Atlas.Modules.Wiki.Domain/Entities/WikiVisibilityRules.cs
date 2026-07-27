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
    // viewerIsAdmin: bilinçli bir istisna - Admin rolü departman sınırını
    // görmezden gelip TÜM DepartmentOnly sayfaları görebilir (kurumsal bir
    // sistemde IT desteği/üst yönetim için makul bir beklenti). Varsayılan
    // "false" sayesinde var olan tüm çağrılar (Admin kavramını bilmeyen eski
    // kod) davranış değiştirmeden derlenmeye devam eder.
    public static bool IsVisibleTo(
        WikiVisibility visibility, string departmentName, string? viewerDepartmentName, bool viewerIsAdmin = false)
    {
        if (visibility == WikiVisibility.Public)
            return true;

        if (viewerIsAdmin)
            return true;

        return !string.IsNullOrWhiteSpace(viewerDepartmentName)
            && string.Equals(departmentName, viewerDepartmentName, StringComparison.OrdinalIgnoreCase);
    }
}