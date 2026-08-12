namespace Atlas.Modules.Documents.Application.Abstractions;

/// <summary>
/// Format-bazlı içerik çıkarımı için genişletilebilir soyutlama -
/// IEmbeddingService'in Fake->gerçek DI-swap felsefesiyle AYNI: bugün tek
/// implementasyonu PlainTextDocumentProcessor (txt/md/csv/json/xml/yaml/sql/
/// log), Gün 4'te Pdf/OpenXml processor'lar EKLENECEK - hiçbiri bu arayüzü ya
/// da mevcut bir implementasyonu DEĞİŞTİRMEYECEK, sadece DI'a yeni bir
/// IDocumentProcessor kaydı eklenecek (open-closed prensibi).
///
/// DocumentUploadedEventHandler, DI'dan `IEnumerable&lt;IDocumentProcessor&gt;`
/// alıp CanProcess'i true dönen İLK processor'ı kullanıyor - birden fazla
/// processor AYNI uzantıyı desteklerse hangisinin kazanacağı DI kayıt sırasına
/// bağlı (bugün için sorun değil, tek processor var).
/// </summary>
public interface IDocumentProcessor
{
    bool CanProcess(string fileExtension);

    Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}
