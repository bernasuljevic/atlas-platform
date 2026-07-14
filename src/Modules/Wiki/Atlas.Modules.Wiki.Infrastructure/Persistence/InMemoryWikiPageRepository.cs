using System.Collections.Concurrent;
using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

/// <summary>
/// GEÇEN DERSTEKİ HATAYI HATIRLA: bu sınıf, bağlantı sarmalamıyor, kendisi depo.
/// Bu yüzden AddWikiModule() içinde bunu SINGLETON kaydedeceğiz - Scoped yaparsak
/// her istekte veriler sıfırlanır (Auth modülünde canlı olarak yaşadığımız bug).
/// EF Core'a geçtiğimizde bu sınıf tamamen değişecek ve o zaman Scoped olacak.
/// </summary>
public class InMemoryWikiPageRepository : IWikiPageRepository
{
    private readonly ConcurrentBag<WikiPage> _pages = new();

    public Task<IReadOnlyList<WikiPage>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<WikiPage>)_pages.ToList());

    public Task AddAsync(WikiPage page, CancellationToken ct = default)
    {
        _pages.Add(page);
        return Task.CompletedTask;
    }
}
