using Atlas.Modules.Auth.Application.Tests.Fakes;
using Atlas.Modules.Auth.Application.Users.Commands;
using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Tests;

public class ResendVerificationCodeCommandHandlerTests
{
    private static (ResendVerificationCodeCommandHandler handler, FakeUserRepository users, FakeEmailVerificationCodeRepository codes, FakeEmailSender emails) CreateHandler()
    {
        var users = new FakeUserRepository();
        var codes = new FakeEmailVerificationCodeRepository();
        var emails = new FakeEmailSender();
        return (new ResendVerificationCodeCommandHandler(users, codes, emails), users, codes, emails);
    }

    [Fact]
    public async Task DogrulanmamisKullaniciIcin_YeniKodUretilirVeEskisiGecersizKilinir()
    {
        var (handler, users, codes, emails) = CreateHandler();
        var user = User.Create("test@atlas.local", "Test", "hash");
        users.Users.Add(user);
        var oldCode = EmailVerificationCode.Create(user.Id);
        codes.Codes.Add(oldCode);

        await handler.Handle(new ResendVerificationCodeCommand("test@atlas.local"), CancellationToken.None);

        Assert.NotNull(oldCode.UsedAtUtc);
        Assert.Equal(2, codes.Codes.Count);
        Assert.Single(emails.SentEmails);
    }

    [Fact]
    public async Task OlmayanEmailIcin_SessizceHicbirSeyYapmaz()
    {
        // GÜVENLİK: email enumeration'a karşı - hata fırlatmıyor, e-posta da
        // göndermiyor, sadece sessizce dönüyor.
        var (handler, _, codes, emails) = CreateHandler();

        await handler.Handle(new ResendVerificationCodeCommand("yok@atlas.local"), CancellationToken.None);

        Assert.Empty(codes.Codes);
        Assert.Empty(emails.SentEmails);
    }

    [Fact]
    public async Task ZatenDogrulanmisKullaniciIcin_SessizceHicbirSeyYapmaz()
    {
        var (handler, users, codes, emails) = CreateHandler();
        var user = User.Create("test@atlas.local", "Test", "hash", emailVerified: true);
        users.Users.Add(user);

        await handler.Handle(new ResendVerificationCodeCommand("test@atlas.local"), CancellationToken.None);

        Assert.Empty(codes.Codes);
        Assert.Empty(emails.SentEmails);
    }
}
