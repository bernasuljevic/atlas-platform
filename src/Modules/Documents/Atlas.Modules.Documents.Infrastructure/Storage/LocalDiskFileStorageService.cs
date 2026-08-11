using Atlas.Modules.Documents.Application.Abstractions;

namespace Atlas.Modules.Documents.Infrastructure.Storage;

/// <summary>
/// <c>wwwroot</c> DIŞINDA, hiçbir zaman <c>UseStaticFiles</c> ile servis
/// edilmeyen bir dizine yazıyor - Vault'un DataProtection anahtarlarını
/// <c>%LOCALAPPDATA%</c>'a yazma gerekçesiyle AYNI (proje klasörü OneDrive ile
/// senkronize, oraya kritik/büyük dosyalar yazmak riskli) VE ayrıca güvenlik
/// gereksinimi: dosyaya erişimin TEK yolu authenticated bir download endpoint'i
/// olmalı (Gün 3), diskteki dosyanın kendisi asla doğrudan bir URL ile
/// açılabilir olmamalı.
///
/// Diskteki dosya adı HER ZAMAN <c>"{GUID:N}.{uzantı}"</c> - orijinal dosya adı
/// hiçbir zaman path'e karışmıyor (bkz. IFileStorageService'teki güvenlik
/// notu - bu, path traversal'ı bir "sanitize et" kontrolüyle değil, YAPISAL
/// olarak imkânsız kılan asıl mekanizma).
/// </summary>
public class LocalDiskFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalDiskFileStorageService(FileStorageOptions options)
    {
        _rootPath = options.RootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct = default)
    {
        var normalizedExtension = fileExtension.TrimStart('.').ToLowerInvariant();
        var storageKey = $"{Guid.NewGuid():N}.{normalizedExtension}";
        var fullPath = Path.Combine(_rootPath, storageKey);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        Stream stream = File.OpenRead(ResolvePath(storageKey));
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    // Savunma amaçlı İKİNCİ katman: storageKey normalde HER ZAMAN DB'den
    // (Document.StorageKey, hep bizim SaveAsync'imizin ürettiği bir değer)
    // geliyor - yine de Path.GetFileName ile "../" gibi klasör-atlama
    // karakterlerini burada da budayıp SADECE _rootPath'in İÇİNDE bir yol
    // üretebiliyoruz (bir gün bu metot yanlışlıkla kullanıcı girdisiyle
    // çağrılırsa bile path traversal mümkün olmasın diye).
    private string ResolvePath(string storageKey)
    {
        var safeFileName = Path.GetFileName(storageKey);
        return Path.Combine(_rootPath, safeFileName);
    }
}
