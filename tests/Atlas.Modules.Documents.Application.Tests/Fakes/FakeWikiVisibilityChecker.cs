using Atlas.Shared.Contracts;

namespace Atlas.Modules.Documents.Application.Tests.Fakes;

// AI.Application.Tests'teki FakeWikiVisibilityChecker'ın BİREBİR kopyası -
// gerçek WikiVisibilityRules'un kendisi DEĞİL (Documents, Wiki.Domain'e
// referans veremez), Handler'ın bu arayüzü doğru kullanıp kullanmadığını
// test etmeye yetecek basitleştirilmiş bir test double'ı.
public class FakeWikiVisibilityChecker : IWikiVisibilityChecker
{
    public bool IsVisibleTo(string visibility, string departmentName, string? viewerDepartmentName, bool viewerIsAdmin = false)
    {
        if (visibility == "Public") return true;
        if (viewerIsAdmin) return true;

        return departmentName == viewerDepartmentName;
    }
}
