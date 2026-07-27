namespace Atlas.Shared.Contracts;

/// <summary>
/// ICurrentUserAccessor ile AYNI desen: gerçek görünürlük kuralı (WikiVisibilityRules)
/// Wiki.Domain'de yaşıyor ve orada KALIYOR - bu arayüz sadece Wiki'nin DIŞINDAKİ
/// modüllerin (AI gibi) o kuralı KENDİ KOPYASINI YAZMADAN kullanabilmesini sağlıyor.
///
/// NEDEN ÖNEMLİ: AI modülü, arama sonuçlarını Wiki'nin departman görünürlük kuralına
/// göre filtrelemek ZORUNDA - aksi halde DepartmentOnly bir sayfanın içeriği, o
/// departmanla hiç ilgisi olmayan bir kullanıcının arama sonucunda görünebilir
/// (CLAUDE.md "Öğrenilen dersler #10"daki güvenlik açığıyla AYNI sınıf hata).
/// Kuralı AI.Domain'e kopyalamak yerine bu arayüzü kullanıyoruz - aksi halde Wiki'de
/// kural değişse (örn. yeni bir görünürlük seviyesi eklense) AI'daki kopya sessizce
/// eskimiş kalırdı.
/// </summary>
public interface IWikiVisibilityChecker
{
    bool IsVisibleTo(string visibility, string departmentName, string? viewerDepartmentName, bool viewerIsAdmin = false);
}
