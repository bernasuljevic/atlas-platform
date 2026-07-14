using System.Collections.Concurrent;
using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Infrastructure.Persistence;

/// <summary>
/// BUGÜNLÜK gerçek bir veritabanı yok - bellekte (RAM'de) tutan geçici bir implementasyon.
/// Uygulama her yeniden başladığında veriler sıfırlanır, bu normal, henüz kalıcılık istemiyoruz.
///
/// EN ÖNEMLİ KISIM: İleride burayı "EfCoreUserRepository" ile değiştireceğiz.
/// Bunu yaptığımızda Domain'e ve Application'a TEK SATIR bile dokunmayacağız,
/// çünkü onlar sadece IUserRepository interface'ini tanıyor, bu sınıfı değil.
/// Bu, katmanlı mimarinin bize verdiği en somut faydalardan biri.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public InMemoryUserRepository()
    {
        var seedUser = User.Create("admin@atlas.local", "Atlas Admin", "hashed-password-placeholder");
        _users[seedUser.Id] = seedUser;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_users.GetValueOrDefault(id));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<User>)_users.Values.ToList());

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}
