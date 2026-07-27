using Atlas.Modules.AI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.IntegrationTests;

/// <summary>
/// AtlasApiFactory, AI modülünün Postgres'ini BİLEREK InMemory'e çevirmiyor
/// (gerçek ingestion akışını uçtan uca test edebilmek için) - ama bunun
/// bir bedeli var: WikiDbContext InMemory olduğu için test bitince o sayfalar
/// yok oluyor, ama AI'ın Postgres'teki embedding'leri KALICI - her test
/// çalıştırması geride "yetim" embedding bırakıyor. Bu canlı olarak
/// doğrulanmış bir sorundu (arama, silinmiş/hiç var olmamış sayfaları hayalet
/// sonuç olarak döndürüyordu). TÜM tabloyu silmek GÜVENLİ DEĞİL - farklı test
/// sınıfları paralel çalışabiliyor, biri diğerinin hâlâ ihtiyaç duyduğu
/// veriyi silebilir. Bu yüzden her test sınıfı SADECE KENDİ oluşturduğu
/// sayfaların id'lerini takip edip, bitince (IAsyncLifetime.DisposeAsync)
/// sadece onları temizlemeli.
/// </summary>
public static class AiEmbeddingTestCleanup
{
    public static async Task DeleteEmbeddingsForPagesAsync(AtlasApiFactory factory, IReadOnlyCollection<Guid> pageIds)
    {
        if (pageIds.Count == 0) return;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();

        await db.WikiPageEmbeddings
            .Where(e => pageIds.Contains(e.WikiPageId))
            .ExecuteDeleteAsync();
    }
}
