using Atlas.Modules.Auth.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.Auth.Infrastructure.Email;

/// <summary>
/// Gerçek bir SMTP sağlayıcısı bağlanana kadarki yer tutucu - e-postayı
/// GERÇEKTEN göndermiyor, sadece logluyor. Geliştirme ortamında doğrulama
/// kodunu görmenin yolu bu log satırı (gerçek bir gelen kutusu yok).
/// FakeEmbeddingService'teki (AI modülü) AYNI karar: iskelet uçtan uca
/// çalışsın, gerçek sağlayıcıya geçiş sadece bir DI kaydı değişikliği olsun.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV E-POSTA] Alıcı: {ToEmail} | Konu: {Subject} | İçerik: {Body}",
            toEmail, subject, body);

        return Task.CompletedTask;
    }
}
