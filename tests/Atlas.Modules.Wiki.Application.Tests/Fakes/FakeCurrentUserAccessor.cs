using Atlas.Shared.Contracts;

namespace Atlas.Modules.Wiki.Application.Tests.Fakes;

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
