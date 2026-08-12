using System.Text;
using Atlas.Modules.Documents.Application.Abstractions;
using Docnet.Core;
using Docnet.Core.Models;

namespace Atlas.Modules.Documents.Infrastructure.Processing;

/// <summary>
/// Docnet.Core (PDFium sarmalayıcısı, MIT lisanslı) kullanıyor - ilk seçim
/// olan UglyToad.PdfPig'in NuGet'teki sürüm geçmişi GitHub'daki resmi release
/// listesiyle TUTARSIZDI (0.1.8...0.1.15 resmi sürümlerine karşılık,
/// NuGet'te farklı bir sahipten "1.7.0-custom-5"/"0.1.9-alpha001-patch1" gibi
/// hiçbir release tag'ine denk gelmeyen sürümler görüldü - tedarik zinciri
/// güvenliği açısından şüpheli, kullanıcıyla birlikte Docnet.Core'a
/// geçilmesine karar verildi).
///
/// Docnet, sayfa GÖRÜNTÜSÜ render etmek için tasarlanmış bir kütüphane -
/// PageDimensions parametresi bu yüzden var, ama biz görüntüyle değil sadece
/// GetText()'in döndürdüğü düz metinle ilgileniyoruz, verilen boyutun gerçek
/// sayfa boyutuyla hiçbir ilgisi yok.
/// </summary>
public class PdfDocumentProcessor : IDocumentProcessor
{
    public bool CanProcess(string fileExtension) => string.Equals(fileExtension, "pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        // Docnet.Core dosya yolu ya da byte[] alıyor, Stream almıyor - içerik
        // TEK SEFER belleğe alınıyor (UploadDocumentCommandHandler'daki AYNI
        // "hash+kaydet için tek seferde oku" gerekçesiyle tutarlı).
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        using var docReader = DocLib.Instance.GetDocReader(bytes, new PageDimensions(1080, 1920));

        var sb = new StringBuilder();
        var pageCount = docReader.GetPageCount();
        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            using var pageReader = docReader.GetPageReader(pageIndex);
            sb.AppendLine(pageReader.GetText());
        }

        return sb.ToString();
    }
}
