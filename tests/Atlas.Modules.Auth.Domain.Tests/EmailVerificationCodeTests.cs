using Atlas.Modules.Auth.Domain.Entities;
using Xunit;

namespace Atlas.Modules.Auth.Domain.Tests;

public class EmailVerificationCodeTests
{
    [Fact]
    public void Create_AltiHaneliKodUretir()
    {
        var code = EmailVerificationCode.Create(Guid.NewGuid());

        Assert.Equal(6, code.Code.Length);
        Assert.True(int.TryParse(code.Code, out _));
    }

    [Fact]
    public void BosUserIdIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() => EmailVerificationCode.Create(Guid.Empty));
    }

    [Fact]
    public void OlusturulduktanHemenSonra_DogruKodlaGecerlidir()
    {
        var code = EmailVerificationCode.Create(Guid.NewGuid());

        Assert.True(code.IsValid(code.Code));
    }

    [Fact]
    public void YanlisKodIle_Gecersizdir()
    {
        var code = EmailVerificationCode.Create(Guid.NewGuid());

        // Gerçek kodla çakışma ihtimali pratikte sıfır (1/1.000.000) - deterministik
        // bir test için basitçe farklı bir 6 haneli string kullanıyoruz.
        var wrongCode = code.Code == "000000" ? "111111" : "000000";

        Assert.False(code.IsValid(wrongCode));
    }

    [Fact]
    public void KullanildiktanSonra_TekrarGecerliDegildir()
    {
        var code = EmailVerificationCode.Create(Guid.NewGuid());

        code.MarkUsed();

        Assert.False(code.IsValid(code.Code));
        Assert.NotNull(code.UsedAtUtc);
    }

    [Fact]
    public void SuresiOnDakikaSonraDolacakSekildeOlusur()
    {
        var beforeCreate = DateTime.UtcNow;
        var code = EmailVerificationCode.Create(Guid.NewGuid());
        var afterCreate = DateTime.UtcNow;

        Assert.InRange(code.ExpiresAtUtc, beforeCreate.AddMinutes(10), afterCreate.AddMinutes(10).AddSeconds(1));
    }
}
