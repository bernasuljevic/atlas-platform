using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Atlas.Modules.AI.Infrastructure.Persistence;

public class EfWikiPageEmbeddingRepository : IWikiPageEmbeddingRepository
{
    private readonly AiDbContext _context;

    public EfWikiPageEmbeddingRepository(AiDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<WikiPageEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        _context.WikiPageEmbeddings.AddRange(embeddings);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WikiPageEmbeddingSearchHit>> FindNearestAsync(
        float[] queryEmbedding, int limit, DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var vector = new Vector(queryEmbedding);

        var query = _context.WikiPageEmbeddings.AsQueryable();

        // BULUNAN GERÇEK BUG (2026-07-28): ASP.NET Core'un query string
        // binder'ı "2026-07-29" gibi bir DateTime'ı Kind=Unspecified olarak
        // üretiyor. Npgsql, "timestamp with time zone" (WikiPageEmbedding.
        // CreatedAtUtc) sütunuyla karşılaştırırken Kind=Unspecified bir
        // DateTime'ı KABUL ETMİYOR - "only UTC is supported" hatasıyla 400
        // dönüyordu (canlı doğrulandı). SQL Server'daki Audit log'un AYNI
        // deseni sorun ÇIKARMADI çünkü SQL Server'ın datetime2 tipi Kind'ı
        // hiç umursamıyor - bu, Postgres/Npgsql'e özgü bir katılık. Çözüm:
        // Kind'ı burada AÇIKÇA Utc olarak işaretlemek (değeri DEĞİŞTİRMİYOR,
        // sadece .NET'e "bu zaten UTC" diyor - ki öyle, JWT/istemciden başka
        // bir zaman dilimi hiç gelmiyor).
        if (fromUtc is not null)
        {
            var from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAtUtc >= from);
        }

        if (toUtc is not null)
        {
            // Audit log'daki AYNI düzeltme (bkz. EfAuditLogRepository) - tarih
            // seçici saatsiz bir değer gönderiyor, "Bitiş" o günün TAMAMINI
            // kapsamalı, sadece gece yarısını değil.
            var exclusiveUpperBound = DateTime.SpecifyKind(toUtc.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAtUtc < exclusiveUpperBound);
        }

        // CosineDistance, Pgvector.EntityFrameworkCore'un LINQ'ü Postgres'in
        // "<=>" operatörüne çeviren extension metodu - sıralama VE mesafe
        // hesaplaması Postgres tarafında yapılıyor, .NET tarafına sadece
        // zaten sıralı "limit" kadar satır (mesafesiyle birlikte) geliyor.
        // Tarih filtresi BURADA, mesafe sıralamasından ÖNCE uygulanıyor -
        // "bu tarih aralığındaki sayfalar arasında en alakalı olanı bul".
        var rows = await query
            .Select(e => new { Embedding = e, Distance = e.Embedding.CosineDistance(vector) })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Savunma amaçlı: sıfır-vektörlü (bkz. GenerateWikiPageEmbeddingsCommandHandler'daki
        // not) ya da başka bir sebeple bozuk bir satır cosine distance'ı NaN/Infinity
        // üretebilir - System.Text.Json bunu yazamayıp TÜM isteği 500'e düşürürdü
        // (canlı doğrulandı). Kaynağında engellemiş olsak da (yeni chunk'lar için),
        // bu satır eski/olası bozuk veriye karşı ikinci bir güvenlik ağı.
        return rows
            .Where(x => !double.IsNaN(x.Distance) && !double.IsInfinity(x.Distance))
            .Select(x => new WikiPageEmbeddingSearchHit(x.Embedding, x.Distance))
            .ToList();
    }

    public async Task DeleteByWikiPageIdAsync(Guid wikiPageId, CancellationToken cancellationToken = default)
    {
        // ExecuteDeleteAsync - satırları önce belleğe çekmeden tek bir DELETE
        // sorgusuyla siliyor (EF Core'un "bulk delete" özelliği). Bir sayfa
        // birden fazla chunk'a bölünmüş olabileceği için burada WikiPageId
        // eşleşen TÜM satırlar siliniyor, tek bir Id değil.
        await _context.WikiPageEmbeddings
            .Where(e => e.WikiPageId == wikiPageId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
