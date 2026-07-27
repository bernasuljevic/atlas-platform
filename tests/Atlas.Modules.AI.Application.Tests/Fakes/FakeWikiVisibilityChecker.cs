using Atlas.Shared.Contracts;

namespace Atlas.Modules.AI.Application.Tests.Fakes;

/// <summary>
/// Wiki.Domain'deki gerçek WikiVisibilityRules'ın KENDİ KOPYASI DEĞİL - AI.Application
/// katmanı Wiki modülüne bağımlı olamayacağı için (modüller arası izolasyon kuralı,
/// bkz. CLAUDE.md), bu basitleştirilmiş bir test double'ı: Public her zaman görünür,
/// DepartmentOnly sadece departman eşleşirse görünür. Gerçek kuralın kendisi zaten
/// Wiki.Domain.Tests'te ayrıca test ediliyor - burada test edilen şey, Handler'ın bu
/// arayüzü DOĞRU KULLANIP KULLANMADIĞI (sonucu filtrelemek için çağırıp çağırmadığı).
/// </summary>
public class FakeWikiVisibilityChecker : IWikiVisibilityChecker
{
    public bool IsVisibleTo(string visibility, string departmentName, string? viewerDepartmentName, bool viewerIsAdmin = false)
    {
        if (visibility == "Public") return true;
        if (viewerIsAdmin) return true;

        return departmentName == viewerDepartmentName;
    }
}
