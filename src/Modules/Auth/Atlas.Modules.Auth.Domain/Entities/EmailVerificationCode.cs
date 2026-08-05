using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Auth.Domain.Entities;

/// <summary>
/// Kayıt sırasında (ve "yeniden gönder" ile) üretilen 6 haneli doğrulama kodu.
/// Bir kullanıcının aynı anda sadece TEK bir aktif kodu olmalı - yeni kod
/// üretilmeden önce öncekiler Application katmanında (bkz.
/// IEmailVerificationCodeRepository.InvalidateActiveCodesForUserAsync)
/// geçersiz kılınıyor; bu entity'nin kendisi bunu bilmiyor, sadece TEK bir
/// kaydın geçerliliğine (süresi/kullanılmış mı) bakıyor.
/// </summary>
public class EmailVerificationCode : Entity<Guid>
{
    private const int ExpiryMinutes = 10;

    public Guid UserId { get; private set; }
    public string Code { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private EmailVerificationCode() { }

    private EmailVerificationCode(
        Guid id, Guid userId, string code, DateTime expiresAtUtc, DateTime createdAtUtc) : base(id)
    {
        UserId = userId;
        Code = code;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    // Kod Random.Shared (thread-safe, .NET 6+) ile burada üretiliyor - bu,
    // IPasswordHasher/IEmbeddingService gibi ileride SAĞLAYICISI değişecek bir
    // soyutlama DEĞİL, basit bir rastgele sayı üretimi - dışarıdan enjekte
    // edilen ayrı bir servise gerek yok.
    public static EmailVerificationCode Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId boş olamaz.", nameof(userId));

        var code = Random.Shared.Next(0, 1_000_000).ToString("D6");
        var now = DateTime.UtcNow;

        return new EmailVerificationCode(Guid.NewGuid(), userId, code, now.AddMinutes(ExpiryMinutes), now);
    }

    public bool IsValid(string code) =>
        UsedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc && Code == code;

    public void MarkUsed()
    {
        UsedAtUtc = DateTime.UtcNow;
    }
}
