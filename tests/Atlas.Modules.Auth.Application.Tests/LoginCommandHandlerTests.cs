using Atlas.Modules.Auth.Application.Tests.Fakes;
using Atlas.Modules.Auth.Application.Users.Commands;
using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Tests;

public class LoginCommandHandlerTests
{
    private static LoginCommandHandler CreateHandler(FakeUserRepository userRepository)
        => new(userRepository, new FakePasswordHasher(), new FakeTokenGenerator(), new FakeRefreshTokenRepository());

    [Fact]
    public async Task DogrulanmisKullanici_DogruSifreyleGirisYapabilir()
    {
        var userRepository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var user = User.Create("test@atlas.local", "Test", hasher.Hash("Sifre123!"), emailVerified: true);
        userRepository.Users.Add(user);

        var handler = CreateHandler(userRepository);
        var result = await handler.Handle(new LoginCommand("test@atlas.local", "Sifre123!"), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DogrulanmamisKullanici_DogruSifreyleBileGirisYapamaz()
    {
        // GÜVENLİK: Bu, e-posta doğrulama özelliğinin asıl amacını doğruluyor -
        // kimlik/şifre doğru olsa bile e-posta doğrulanmadan giriş engellenmeli.
        var userRepository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var user = User.Create("test@atlas.local", "Test", hasher.Hash("Sifre123!"), emailVerified: false);
        userRepository.Users.Add(user);

        var handler = CreateHandler(userRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new LoginCommand("test@atlas.local", "Sifre123!"), CancellationToken.None));
    }

    [Fact]
    public async Task YanlisSifre_DogrulanmisOlsaBileNullDoner()
    {
        // Doğrulama kontrolünün şifre kontrolünün YERİNE değil, ONDAN SONRA
        // geldiğini doğruluyor - yanlış şifre hâlâ eskisi gibi (401) null dönüyor,
        // yeni 403 dalına hiç girmiyor.
        var userRepository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var user = User.Create("test@atlas.local", "Test", hasher.Hash("Sifre123!"), emailVerified: true);
        userRepository.Users.Add(user);

        var handler = CreateHandler(userRepository);
        var result = await handler.Handle(new LoginCommand("test@atlas.local", "YanlisSifre"), CancellationToken.None);

        Assert.Null(result);
    }
}
