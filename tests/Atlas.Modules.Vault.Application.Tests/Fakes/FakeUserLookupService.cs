using Atlas.Shared.Contracts;

namespace Atlas.Modules.Vault.Application.Tests.Fakes;

// Auth.Infrastructure'daki gerçek AuthUserLookupService'in yerine - testler
// hangi e-postaların "var" sayılacağını Users listesine ekleyerek kontrol
// ediyor, listede olmayan bir e-posta FindByEmailAsync'in null dönmesiyle
// (gerçek serviste "kullanıcı yok" durumuna) eşleniyor.
public class FakeUserLookupService : IUserLookupService
{
    public List<UserSummary> Users { get; } = new();

    public Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));
}
