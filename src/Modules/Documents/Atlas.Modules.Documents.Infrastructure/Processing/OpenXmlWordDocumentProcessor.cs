using Atlas.Modules.Documents.Application.Abstractions;
using DocumentFormat.OpenXml.Packaging;

namespace Atlas.Modules.Documents.Infrastructure.Processing;

/// <summary>
/// DocumentFormat.OpenXml - Microsoft'un kendi resmi OOXML SDK'sı, MIT
/// lisanslı. SADECE .docx (OOXML/XML tabanlı format) - eski ikili .doc
/// formatını AÇAMIYOR, bu kütüphanenin yapısal bir sınırı (OpenXml SDK hiçbir
/// zaman eski ikili Office formatlarını desteklemedi). Upload'ın izin
/// verdiği ama HENÜZ bir işleyicisi olmayan formatlar (.doc/.odt/.rtf) bu
/// yüzden P4 Gün 3'teki "işleyici bulunamadı" yoluyla düzgün şekilde
/// Failed'a düşecek - kapsam dışı bırakıldığı AÇIKÇA belirtiliyor, sessizce
/// unutulmuyor.
/// </summary>
public class OpenXmlWordDocumentProcessor : IDocumentProcessor
{
    public bool CanProcess(string fileExtension) => string.Equals(fileExtension, "docx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var wordDocument = WordprocessingDocument.Open(content, isEditable: false);
        var text = wordDocument.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        return Task.FromResult(text);
    }
}
