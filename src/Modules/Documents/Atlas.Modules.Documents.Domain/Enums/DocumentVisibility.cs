namespace Atlas.Modules.Documents.Domain.Enums;

// Wiki.Domain'deki WikiVisibility'nin AYNI iki değeri (Public/DepartmentOnly) -
// ama BİLEREK AYRI bir tanım. Modüller birbirinin Domain'ine referans veremez
// (bkz. modüler monolith kuralı), Documents'ın kendi görünürlük kavramına
// ihtiyacı var; semantiği Wiki'yle birebir aynı tutuyoruz ki
// IWikiVisibilityChecker (Shared.Contracts) - zaten genel amaçlı tasarlanmış,
// Wiki'ye özel hiçbir şey içermiyor - buraya da sorunsuz uygulanabilsin.
public enum DocumentVisibility
{
    Public = 0,
    DepartmentOnly = 1,
}
