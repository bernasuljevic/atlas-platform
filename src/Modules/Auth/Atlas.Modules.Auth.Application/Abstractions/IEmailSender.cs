namespace Atlas.Modules.Auth.Application.Abstractions;

/// <summary>
/// IPasswordHasher/ITokenGenerator ile AYNI desen - gerçek bir SMTP/e-posta
/// sağlayıcısı (API key'ler) henüz yok, sağlayıcı gelince tek yapılacak şey bu
/// interface'in DI kaydını değiştirmek olacak (AI modülünün IEmbeddingService'i
/// ile birebir aynı bilinçli tasarım kararı).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
