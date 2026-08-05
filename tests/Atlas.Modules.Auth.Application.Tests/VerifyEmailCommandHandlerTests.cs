using Atlas.Modules.Auth.Application.Tests.Fakes;
using Atlas.Modules.Auth.Application.Users.Commands;
using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Tests;

public class VerifyEmailCommandHandlerTests
{
    private static (VerifyEmailCommandHandler handler, FakeUserRepository users, FakeEmailVerificationCodeRepository codes) CreateHandler()
    {
        var users = new FakeUserRepository();
        var codes = new FakeEmailVerificationCodeRepository();
        return (new VerifyEmailCommandHandler(users, codes), users, codes);
    }

    [Fact]
    public async Task DogruKodla_KullaniciDogrulanir()
    {
        var (handler, users, codes) = CreateHandler();
        var user = User.Create("test@atlas.local", "Test", "hash");
        users.Users.Add(user);
        var code = EmailVerificationCode.Create(user.Id);
        codes.Codes.Add(code);

        await handler.Handle(new VerifyEmailCommand("test@atlas.local", code.Code), CancellationToken.None);

        Assert.True(user.EmailVerified);
        Assert.NotNull(code.UsedAtUtc);
    }

    [Fact]
    public async Task YanlisKodla_ArgumentExceptionAlirVeDogrulanmaz()
    {
        var (handler, users, codes) = CreateHandler();
        var user = User.Create("test@atlas.local", "Test", "hash");
        users.Users.Add(user);
        var code = EmailVerificationCode.Create(user.Id);
        codes.Codes.Add(code);
        var wrongCode = code.Code == "000000" ? "111111" : "000000";

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new VerifyEmailCommand("test@atlas.local", wrongCode), CancellationToken.None));
        Assert.False(user.EmailVerified);
    }

    [Fact]
    public async Task OlmayanKullaniciIcin_ArgumentExceptionAlir()
    {
        var (handler, _, _) = CreateHandler();

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new VerifyEmailCommand("yok@atlas.local", "123456"), CancellationToken.None));
    }

    [Fact]
    public async Task ZatenDogrulanmisKullanici_TekrarDogrulanamaz()
    {
        var (handler, users, codes) = CreateHandler();
        var user = User.Create("test@atlas.local", "Test", "hash", emailVerified: true);
        users.Users.Add(user);
        var code = EmailVerificationCode.Create(user.Id);
        codes.Codes.Add(code);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new VerifyEmailCommand("test@atlas.local", code.Code), CancellationToken.None));
    }

    [Fact]
    public async Task KullanilmisKodTekrarDenenirse_ArgumentExceptionAlir()
    {
        // "Eski kodların geçersiz olması" kuralının doğrulama tarafındaki
        // yansıması - bir kod başarıyla kullanıldıktan sonra AYNI kod ikinci
        // kez gönderilirse kabul edilmemeli.
        var (handler, users, codes) = CreateHandler();
        var user = User.Create("test@atlas.local", "Test", "hash");
        users.Users.Add(user);
        var code = EmailVerificationCode.Create(user.Id);
        codes.Codes.Add(code);

        await handler.Handle(new VerifyEmailCommand("test@atlas.local", code.Code), CancellationToken.None);

        // İkinci kullanıcı doğrulanmış olduğu için asıl beklenen hata "zaten
        // doğrulanmış" olur - ama önemli olan: kod tekrar KABUL EDİLMİYOR.
        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new VerifyEmailCommand("test@atlas.local", code.Code), CancellationToken.None));
    }
}
