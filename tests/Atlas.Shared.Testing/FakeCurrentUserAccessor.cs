using Atlas.Shared.Contracts;

namespace Atlas.Shared.Testing;

// Wiki.Application.Tests ve AI.Application.Tests'te neredeyse birebir aynı
// (tek fark opsiyonel userId parametresiydi) iki ayrı kopya olarak yaşıyordu
// - "üç kural" eşiği aşıldığında (bkz. CLAUDE.md "İzlenecek teknik borç")
// buraya, paylaşılan bir test projesine taşındı. Superset (userId parametreli)
// versiyon korundu - AI tarafındaki testler bu parametreyi kullanmıyor,
// varsayılan (yeni bir Guid) yeterli.
public class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public FakeCurrentUserAccessor(string? department, bool isAdmin = false, Guid? userId = null)
    {
        Department = department;
        IsAdmin = isAdmin;
        UserId = userId ?? Guid.NewGuid();
    }

    public Guid? UserId { get; }
    public string? Email { get; } = "test@atlas.local";
    public bool IsAuthenticated { get; } = true;
    public string? Department { get; }
    public bool IsAdmin { get; }
}
