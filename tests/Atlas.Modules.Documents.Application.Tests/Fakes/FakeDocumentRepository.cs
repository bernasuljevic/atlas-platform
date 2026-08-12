using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;

namespace Atlas.Modules.Documents.Application.Tests.Fakes;

// Gerçek bir DbContext yok - basit bir bellek-içi liste. Change tracker
// davranışını (Update/Delete "staging") simüle etmiyor, sadece Handler'ın
// hangi metotları hangi argümanlarla çağırdığını doğrulamaya yetecek kadar
// gerçekçi.
public class FakeDocumentRepository : IDocumentRepository
{
    public List<Document> Documents { get; } = new();
    public List<Document> Deleted { get; } = new();

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Documents.FirstOrDefault(d => d.Id == id));

    public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Document>>(Documents.ToList());

    public Task<IReadOnlyList<Document>> GetAllByContentHashAsync(string contentHash, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Document>>(Documents.Where(d => d.ContentHash == contentHash).ToList());

    public Task AddAsync(Document document, CancellationToken ct = default)
    {
        Documents.Add(document);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Document document, CancellationToken ct = default) => Task.CompletedTask;

    public Task DeleteAsync(Document document, CancellationToken ct = default)
    {
        Documents.RemoveAll(d => d.Id == document.Id);
        Deleted.Add(document);
        return Task.CompletedTask;
    }
}
