namespace Atlas.Modules.Documents.Application.Abstractions;

/// <summary>
/// Wiki'nin IUnitOfWork'üyle AYNI sözleşme. P3'te IDocumentRepository'nin her
/// metodu (Add/Update/Delete) KENDİ SaveChangesAsync'ini çağırıyordu - bu,
/// bir Document yazmasıyla bir OutboxMessage yazmasının AYRI iki veritabanı
/// işlemi (iki round-trip) olacağı, dolayısıyla biri başarılı biri başarısız
/// olabileceği anlamına gelirdi. Bu arayüz P4 Gün 3'te (gerçek event'ler
/// tanımlanınca) repository metotlarının SaveChanges çağrısını buraya
/// devretmesiyle devreye girecek - bugün (Gün 2) sadece altyapı hazırlanıyor.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
