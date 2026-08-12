using System.Text;
using Atlas.Modules.Documents.Application.Abstractions;

namespace Atlas.Modules.Documents.Infrastructure.Processing;

/// <summary>
/// En basit processor - hiçbir kütüphaneye ihtiyacı yok, dosyayı olduğu gibi
/// UTF-8 metin olarak okuyor. UploadDocumentCommandValidator'ın "Data /
/// Technical" + düz metin kategorileriyle AYNI uzantı kümesi (json/xml/yaml/sql/
/// log İÇİN de "extraction" aslında sadece "dosyayı oku" - hiçbiri gerçek bir
/// binary format değil).
/// </summary>
public class PlainTextDocumentProcessor : IDocumentProcessor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "md", "csv", "json", "xml", "yaml", "yml", "sql", "log",
    };

    public bool CanProcess(string fileExtension) => SupportedExtensions.Contains(fileExtension);

    public async Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        // detectEncodingFromByteOrderMarks: BOM'lu bir dosya (Windows'ta Not
        // Defteri'nin varsayılan kaydettiği gibi) varsa doğru kodlamayı
        // otomatik seçiyor, yoksa UTF-8'e düşüyor.
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
