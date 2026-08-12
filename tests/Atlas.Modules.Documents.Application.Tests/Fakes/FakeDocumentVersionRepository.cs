using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;

namespace Atlas.Modules.Documents.Application.Tests.Fakes;

public class FakeDocumentVersionRepository : IDocumentVersionRepository
{
    public List<DocumentVersion> Versions { get; } = new();

    public Task AddAsync(DocumentVersion version, CancellationToken ct = default)
    {
        Versions.Add(version);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentVersion>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DocumentVersion>>(
            Versions.Where(v => v.DocumentId == documentId).OrderByDescending(v => v.VersionNumber).ToList());

    public Task<DocumentVersion?> GetByDocumentIdAndVersionNumberAsync(
        Guid documentId, int versionNumber, CancellationToken ct = default) =>
        Task.FromResult(Versions.FirstOrDefault(v => v.DocumentId == documentId && v.VersionNumber == versionNumber));

    public Task DeleteAllForDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        Versions.RemoveAll(v => v.DocumentId == documentId);
        return Task.CompletedTask;
    }
}
