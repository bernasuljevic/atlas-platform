namespace Atlas.Modules.Documents.Application.Abstractions;

/// <summary>
/// IEmbeddingService'in (AI modülü) Fake->gerçek DI-swap felsefesiyle AYNI -
/// bugün (Gün 2'de) tek implementasyonu yerel disk olacak
/// (LocalDiskFileStorageService), ileride bulut depolamaya (S3/Azure Blob)
/// geçilirse sadece DI kaydı değişecek, çağıran kod (Command Handler'lar)
/// hiç değişmeyecek.
///
/// <para><b>GÜVENLİK - kritik tasarım kararı:</b> <c>SaveAsync</c> bir
/// storageKey PARAMETRESİ ALMIYOR, kendi GUID tabanlı anahtarını üretip
/// DÖNDÜRÜYOR. Böylece hiçbir çağıran kod (bugün ya da gelecekte) kullanıcı
/// girdisinden (orijinal dosya adı gibi) bir path parçası TÜRETEMEZ - path
/// traversal saldırısı bir "sanitize et" kontrolüyle değil, arayüzün kendi
/// şekliyle YAPISAL olarak imkânsız kılınıyor.</para>
/// </summary>
public interface IFileStorageService
{
    // fileExtension SADECE diskteki dosya adının uzantısı için (ör. "pdf") -
    // orijinal dosya adının KENDİSİ asla parametre olarak alınmıyor.
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
