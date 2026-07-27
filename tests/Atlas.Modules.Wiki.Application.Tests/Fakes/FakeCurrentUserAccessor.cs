using Atlas.Shared.Contracts;

namespace Atlas.Modules.Wiki.Application.Tests.Fakes;

public class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public FakeCurrentUserAccessor(string? department, bool isAdmin = false)
    {
        Department = department;
        IsAdmin = isAdmin;
    }

    public Guid? UserId { get; } = Guid.NewGuid();
    public string? Email { get; } = "test@atlas.local";
    public bool IsAuthenticated { get; } = true;
    public string? Department { get; }
    public bool IsAdmin { get; }
}
