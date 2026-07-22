using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Auth.Domain.Entities;

public class RefreshToken : Entity<Guid>
{
    public string Token { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() { }

    private RefreshToken(Guid id, string token, Guid userId, DateTime expiresAtUtc, DateTime createdAtUtc, bool isRevoked)
        : base(id)
    {
        Token = token;
        UserId = userId;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        IsRevoked = isRevoked;
    }

    public static RefreshToken Create(string token, Guid userId, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token boş olamaz.", nameof(token));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId boş olamaz.", nameof(userId));

        return new RefreshToken(Guid.NewGuid(), token, userId, expiresAtUtc, DateTime.UtcNow, isRevoked: false);
    }

    // "Rotation" deseni: her refresh'te eskisi iptal edilip yenisi üretiliyor.
    // Aynı refresh token iki kez kullanılamaz - bu, çalınmış bir token'ın
    // sessizce sonsuza kadar kullanılabilmesini engelliyor.
    public bool IsActive(DateTime nowUtc) => !IsRevoked && ExpiresAtUtc > nowUtc;

    public void Revoke()
    {
        IsRevoked = true;
    }
}
