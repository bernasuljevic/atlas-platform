using System.Text;
using Atlas.Modules.Documents.Application.Abstractions;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;

namespace Atlas.Modules.Documents.Infrastructure.Processing;

/// <summary>
/// Kullanıcının orijinal spec'inin "6.1 PPTX" isteği: slide metni + speaker
/// notes çıkarılıyor, slide numarası korunuyor - arama sonucunda "Slide 12 –
/// 2026 Sales Targets" gibi bir eşleşme gösterebilmek (AI/RAG entegrasyonu,
/// P5) için chunk metninin İÇİNDE hangi slide'dan geldiği bilgisi duruyor.
/// </summary>
public class OpenXmlPresentationDocumentProcessor : IDocumentProcessor
{
    public bool CanProcess(string fileExtension) => string.Equals(fileExtension, "pptx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var presentationDocument = PresentationDocument.Open(content, isEditable: false);
        var slideParts = presentationDocument.PresentationPart?.SlideParts ?? [];

        var sb = new StringBuilder();
        var slideNumber = 1;

        foreach (var slidePart in slideParts)
        {
            var slideTexts = slidePart.Slide?.Descendants<A.Text>().Select(t => t.Text ?? string.Empty) ?? [];
            sb.AppendLine($"Slayt {slideNumber}: {string.Join(" ", slideTexts)}");

            var notesText = slidePart.NotesSlidePart?.NotesSlide?.Descendants<A.Text>().Select(t => t.Text ?? string.Empty);
            if (notesText is not null)
            {
                var notes = string.Join(" ", notesText);
                if (!string.IsNullOrWhiteSpace(notes))
                    sb.AppendLine($"Notlar: {notes}");
            }

            slideNumber++;
        }

        return Task.FromResult(sb.ToString());
    }
}
