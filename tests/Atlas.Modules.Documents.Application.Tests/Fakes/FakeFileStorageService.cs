using Atlas.Modules.Documents.Application.Abstractions;

namespace Atlas.Modules.Documents.Application.Tests.Fakes;

// LocalDiskFileStorageService'in bellek-içi eşdeğeri - StorageKey'i GERÇEK
// implementasyonla AYNI gerekçeyle (kullanıcı girdisinden türetilmiyor)
// kendisi üretiyor, çağırana bir key ÖNERMİYOR (bkz. IFileStorageService'teki
// güvenlik notu).
public class FakeFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _files = new();

    public List<string> DeletedKeys { get; } = new();

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var key = $"{Guid.NewGuid():N}.{fileExtension}";
        _files[key] = buffer.ToArray();
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new MemoryStream(_files[storageKey]));

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        _files.Remove(storageKey);
        DeletedKeys.Add(storageKey);
        return Task.CompletedTask;
    }
}
