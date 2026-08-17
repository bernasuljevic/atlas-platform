using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Shared.Contracts;

namespace Atlas.Modules.Auth.Infrastructure.UserLookup;

/// <summary>
/// Shared.Contracts'taki IUserLookupService'in gerçek implementasyonu -
/// HttpCurrentUserAccessor'ın "Auth burada, ama dışarıya sadece dar bir
/// arayüz gösteriyor" desenindeki AYNI mantık. Var olan IUserRepository'yi
/// (Auth.Application, login/register akışında zaten kullanılıyor) SARMALIYOR -
/// ikinci bir veri erişim yolu İCAT EDİLMEDİ.
/// </summary>
public class AuthUserLookupService : IUserLookupService
{
    private readonly IUserRepository _userRepository;

    public AuthUserLookupService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, ct);
        return user is null ? null : new UserSummary(user.Id, user.Email, user.FullName);
    }
}
