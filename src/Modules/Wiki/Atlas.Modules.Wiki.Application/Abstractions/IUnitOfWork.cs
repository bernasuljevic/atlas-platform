namespace Atlas.Modules.Wiki.Application.Abstractions;

/// <summary>
/// Outbox Pattern'in atomiklik parçası (Gün 2). ÖNCEDEN her repository metodu
/// (AddAsync/DeleteAsync) kendi SaveChangesAsync()'ini çağırıyordu - bu, bir
/// WikiPage yazmasıyla bir OutboxMessage yazmasının AYRI iki veritabanı
/// işlemi (iki round-trip) olacağı, dolayısıyla biri başarılı biri başarısız
/// olabileceği anlamına gelirdi (atomik DEĞİL). Artık repository metotları
/// sadece DbContext'in change tracker'ına EKLİYOR (henüz veritabanına
/// yazmıyor) - Handler, WikiPage'i eklemeyi + IOutboxWriter.Enqueue'yu
/// çağırdıktan SONRA bu arayüzü TEK BİR KEZ çağırarak ikisini AYNI
/// transaction'da, tek bir SaveChanges ile kalıcı hale getiriyor.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
