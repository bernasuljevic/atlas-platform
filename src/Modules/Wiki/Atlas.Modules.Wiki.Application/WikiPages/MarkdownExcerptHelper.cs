using System.Text.RegularExpressions;

namespace Atlas.Modules.Wiki.Application.WikiPages;

// Sayfa önizlemeleri (dashboard kartları, arama önerileri) ham WikiPage.Content'i
// KISALTARAK üretiliyor - ama içerik markdown, ham hâliyle kısaltmak "[Başlık]
// (wiki:GUID)" gibi çirkin, yarım kalmış sözdizimini önizlemeye sızdırıyordu
// (canlı doğrulandı, 2026-08-05 - "Kod İnceleme En İyi Uygulamaları" sayfasının
// önizlemesinde bir bağlantının açılış parantezi görünüyordu). Bu yüzden
// kısaltmadan ÖNCE markdown işaretlerini (link/görsel/başlık/kalın-italik/
// liste-alıntı/kod bloğu) temizleyip DÜZ METNE indiriyoruz - gerçek render
// katmanı (markdown.jsx) BURADA kullanılmıyor çünkü bu, React'a değil sunucuya
// ait bir metin dönüşümü; amaç HTML/JSX üretmek değil, sade bir önizleme cümlesi.
public static class MarkdownExcerptHelper
{
    private static readonly Regex ImageSyntax = new(@"!\[[^\]]*\]\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex LinkSyntax = new(@"\[([^\]]+)\]\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex HeadingSyntax = new(@"^#{1,6}\s+", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex BoldItalicSyntax = new(@"\*{1,2}([^*]+)\*{1,2}", RegexOptions.Compiled);
    private static readonly Regex ListQuoteSyntax = new(@"^[-*>]\s+", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex CodeFenceSyntax = new(@"```[a-zA-Z]*", RegexOptions.Compiled);
    private static readonly Regex WhitespaceSyntax = new(@"\s+", RegexOptions.Compiled);

    // Görsel söz dizimi TAMAMEN kaldırılıyor (bir düz metin cümlesinde
    // "Görsel" yazısı anlamlı bir önizleme sayılmaz) - link söz dizimi ise
    // hedefi atıp SADECE bağlantı metnini bırakıyor, tıpkı markdown.jsx'in
    // gerçek render katmanında yaptığı gibi.
    public static string ToPlainText(string content)
    {
        var text = ImageSyntax.Replace(content, "");
        text = LinkSyntax.Replace(text, "$1");
        text = HeadingSyntax.Replace(text, "");
        text = BoldItalicSyntax.Replace(text, "$1");
        text = ListQuoteSyntax.Replace(text, "");
        text = CodeFenceSyntax.Replace(text, "");
        return WhitespaceSyntax.Replace(text, " ").Trim();
    }

    public static string Truncate(string content, int maxLength)
    {
        var plain = ToPlainText(content);
        return plain.Length <= maxLength ? plain : plain[..maxLength].TrimEnd() + "…";
    }
}
