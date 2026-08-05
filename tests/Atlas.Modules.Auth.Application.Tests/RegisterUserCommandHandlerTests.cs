using Atlas.Modules.Auth.Application.Tests.Fakes;
using Atlas.Modules.Auth.Application.Users.Commands;

namespace Atlas.Modules.Auth.Application.Tests;

public class RegisterUserCommandHandlerTests
{
    private static RegisterUserCommandHandler CreateHandler(
        out FakeUserRepository userRepository,
        out FakeEmailVerificationCodeRepository codeRepository,
        out FakeEmailSender emailSender)
    {
        userRepository = new FakeUserRepository();
        codeRepository = new FakeEmailVerificationCodeRepository();
        emailSender = new FakeEmailSender();
        return new RegisterUserCommandHandler(userRepository, new FakePasswordHasher(), codeRepository, emailSender);
    }

    [Fact]
    public async Task KayitOlunca_KullaniciEmailDogrulanmamisOlarakOlusur()
    {
        var handler = CreateHandler(out var userRepository, out _, out _);
        var command = new RegisterUserCommand("test@atlas.local", "Test Kullanici", "Sifre123!");

        await handler.Handle(command, CancellationToken.None);

        Assert.False(userRepository.Users[0].EmailVerified);
    }

    [Fact]
    public async Task KayitOlunca_DogrulamaKoduUretilirVeKaydedilir()
    {
        var handler = CreateHandler(out var userRepository, out var codeRepository, out _);
        var command = new RegisterUserCommand("test@atlas.local", "Test Kullanici", "Sifre123!");

        var userId = await handler.Handle(command, CancellationToken.None);

        var code = Assert.Single(codeRepository.Codes);
        Assert.Equal(userId, code.UserId);
        Assert.Equal(6, code.Code.Length);
    }

    [Fact]
    public async Task KayitOlunca_DogrulamaKoduEPostaIleGonderilir()
    {
        var handler = CreateHandler(out _, out var codeRepository, out var emailSender);
        var command = new RegisterUserCommand("test@atlas.local", "Test Kullanici", "Sifre123!");

        await handler.Handle(command, CancellationToken.None);

        var sentEmail = Assert.Single(emailSender.SentEmails);
        Assert.Equal("test@atlas.local", sentEmail.ToEmail);
        // Gönderilen e-posta gövdesinde ÜRETİLEN kodun kendisi geçmeli - Handler'ın
        // rastgele başka bir değer değil, DB'ye kaydettiğiyle AYNI kodu gönderdiğini
        // doğruluyor.
        Assert.Contains(codeRepository.Codes[0].Code, sentEmail.Body);
    }
}
