using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Abstractions;

public interface IEmailVerificationCodeRepository
{
    // En son üretilen, henüz kullanılmamış kod - doğrulama ve "yeniden gönder"
    // aynı sorguyu paylaşıyor.
    Task<EmailVerificationCode?> GetLatestActiveForUserAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(EmailVerificationCode code, CancellationToken ct = default);

    // Yeni bir kod üretilmeden ÖNCE çağrılıyor - "eski kodların geçersiz
    // olması" kuralı burada uygulanıyor (kullanıcı "yeniden gönder"e bassa
    // bile önceki kod artık işe yaramamalı).
    Task InvalidateActiveCodesForUserAsync(Guid userId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
