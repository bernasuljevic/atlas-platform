using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Xunit;

namespace Atlas.Modules.Wiki.Domain.Tests;

public class WikiPageTests
{
    [Fact]
    public void PublicSayfa_DepartmanBelirtilmeden_GorunurOlmali()
    {
        var page = WikiPage.Create("Baslik", "Icerik", "IT", WikiVisibility.Public, Guid.NewGuid());

        var result = page.IsVisibleTo(null);

        Assert.True(result);
    }

    [Fact]
    public void DepartmentOnlySayfa_DogruDepartmandan_GorunurOlmali()
    {
        var page = WikiPage.Create("Baslik", "Icerik", "Engineering", WikiVisibility.DepartmentOnly, Guid.NewGuid());

        var result = page.IsVisibleTo("Engineering");

        Assert.True(result);
    }

    [Fact]
    public void DepartmentOnlySayfa_YanlisDepartmandan_GorunmemeliDir()
    {
        var page = WikiPage.Create("Baslik", "Icerik", "Engineering", WikiVisibility.DepartmentOnly, Guid.NewGuid());

        var result = page.IsVisibleTo("IT");

        Assert.False(result);
    }

    [Fact]
    public void DepartmentOnlySayfa_DepartmanBelirtilmeden_GorunmemeliDir()
    {
        var page = WikiPage.Create("Baslik", "Icerik", "Engineering", WikiVisibility.DepartmentOnly, Guid.NewGuid());

        var result = page.IsVisibleTo(null);

        Assert.False(result);
    }

    [Theory]
    [InlineData("engineering")]
    [InlineData("ENGINEERING")]
    [InlineData("EnGiNeErInG")]
    public void DepartmentOnlySayfa_BuyukKucukHarfDuyarsiz_GorunurOlmali(string viewerDepartment)
    {
        var page = WikiPage.Create("Baslik", "Icerik", "Engineering", WikiVisibility.DepartmentOnly, Guid.NewGuid());

        var result = page.IsVisibleTo(viewerDepartment);

        Assert.True(result);
    }
}