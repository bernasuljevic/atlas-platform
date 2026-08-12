using System.Text;
using Atlas.Modules.Documents.Application.Abstractions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Atlas.Modules.Documents.Infrastructure.Processing;

/// <summary>
/// Kullanıcının orijinal spec'inin "6.2 Excel/CSV" isteği: sheet adı korunuyor,
/// satır satır hücre değerleri çıkarılıyor ("Product | Sales | Stock" gibi
/// tablo yapısı, TextChunker'a düz metin olarak gidiyor ama satır/sheet
/// sınırları görünür kalıyor). ClosedXML gibi ek bir sarmalayıcı EKLENMEDİ -
/// DocumentFormat.OpenXml SDK'sının kendisi (SharedStringTable çözümlemesiyle
/// birlikte) yeterli, ekstra bağımlılık gerekmiyor.
/// </summary>
public class OpenXmlSpreadsheetDocumentProcessor : IDocumentProcessor
{
    public bool CanProcess(string fileExtension) => string.Equals(fileExtension, "xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var spreadsheetDocument = SpreadsheetDocument.Open(content, isEditable: false);
        var workbookPart = spreadsheetDocument.WorkbookPart;
        if (workbookPart is null)
            return Task.FromResult(string.Empty);

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sb = new StringBuilder();

        foreach (var sheet in workbookPart.Workbook?.Descendants<Sheet>() ?? [])
        {
            if (sheet.Id?.Value is null)
                continue;

            sb.AppendLine($"Sayfa: {sheet.Name}");

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
            var rows = worksheetPart.Worksheet?.Descendants<Row>() ?? [];

            foreach (var row in rows)
            {
                var cellTexts = row.Descendants<Cell>().Select(cell => GetCellText(cell, sharedStrings));
                sb.AppendLine(string.Join(" | ", cellTexts));
            }
        }

        return Task.FromResult(sb.ToString());
    }

    // Excel, metin hücrelerini genelde DOĞRUDAN saklamıyor - SharedStringTable'daki
    // bir INDEX'i saklıyor (aynı metin binlerce kez tekrarlanıyorsa dosya
    // boyutunu küçültmek için). DataType SharedString ise CellValue aslında
    // bir index, gerçek metni tabloda arıyoruz.
    private static string GetCellText(Cell cell, SharedStringTable? sharedStrings)
    {
        var rawValue = cell.CellValue?.Text ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString && sharedStrings is not null && int.TryParse(rawValue, out var index))
            return sharedStrings.ElementAtOrDefault(index)?.InnerText ?? string.Empty;

        return rawValue;
    }
}
