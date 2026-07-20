using Atlas.Modules.Auth.Domain.Entities;
using Atlas.Modules.Auth.Domain.Enums;
using Xunit;

namespace Atlas.Modules.Auth.Domain.Tests;

public class UserTests
{
    [Fact]
    public void GecerliBilgilerle_KullaniciBasariylaOlusur()
    {
        var user = User.Create("test@atlas.local", "Test Kullanici", "hashli-sifre");

        Assert.Equal("test@atlas.local", user.Email);
        Assert.Equal("Test Kullanici", user.FullName);
        Assert.Equal(UserRole.Member, user.Role);
    }

    [Fact]
    public void BosEmailIle_ArgumentExceptionFirlatilir()
    {
        // Assert.Throws, verilen kod bloğunu (lambda) çalıştırır ve belirtilen
        // exception türünün fırlatıldığını doğrular - fırlatılmazsa test kırmızı çıkar.
        Assert.Throws<ArgumentException>(() =>
            User.Create("", "Test Kullanici", "hashli-sifre"));
    }

    [Fact]
    public void BosSifreHashIle_ArgumentExceptionFirlatilir()
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create("test@atlas.local", "Test Kullanici", ""));
    }

    [Theory]
    [InlineData("  ADMIN@Test.com  ", "admin@test.com")]
    [InlineData("USER@ATLAS.LOCAL", "user@atlas.local")]
    public void Email_KucukHarfeCevrilirVeTrimlenir(string girdi, string beklenen)
    {
        var user = User.Create(girdi, "Test Kullanici", "hashli-sifre");

        Assert.Equal(beklenen, user.Email);
    }

    [Fact]
    public void RolBelirtilmezse_VarsayilanMemberOlur()
    {
        var user = User.Create("test@atlas.local", "Test Kullanici", "hashli-sifre");

        Assert.Equal(UserRole.Member, user.Role);
    }

    [Fact]
    public void AdminRolBelirtilirse_AdminOlur()
    {
        var user = User.Create("admin@atlas.local", "Admin", "hashli-sifre", UserRole.Admin);

        Assert.Equal(UserRole.Admin, user.Role);
    }
}