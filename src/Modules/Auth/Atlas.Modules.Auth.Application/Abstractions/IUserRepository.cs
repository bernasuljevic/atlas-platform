using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Abstractions;

/// <summary>
/// Bu interface'in EN ÖNEMLİ özelliği: burada EF Core yok, SQL yok, "nasıl" hiç yok.
/// Application katmanı sadece "kullanıcı ekleyebilmeliyim, getirebilmeliyim" diyor.
/// Gerçek implementasyonu (bellek mi, PostgreSQL mi, MongoDB mi) Infrastructure katmanı yazacak.
/// Bu ayrım sayesinde ileride veritabanı değiştirsek Application'a hiç dokunmayız.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
