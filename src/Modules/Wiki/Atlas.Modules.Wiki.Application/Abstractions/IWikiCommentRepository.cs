using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Abstractions;

public interface IWikiCommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // pageId null ise "ana sayfadaki genel platform tartışması" demek -
    // WHERE c.PageId = @pageId, EF Core null karşılaştırmasını doğru üretiyor.
    Task<IReadOnlyList<Comment>> GetByPageIdAsync(Guid? pageId, CancellationToken ct = default);

    // SaveChangesAsync BİLEREK burada yok - diğer repository'lerdeki AYNI
    // gerekçe, kalıcı hale getirmek IUnitOfWork'ün sorumluluğu.
    Task AddAsync(Comment comment, CancellationToken ct = default);
    Task DeleteAsync(Comment comment, CancellationToken ct = default);
}
