using Atlas.Modules.Documents.Domain.Entities;

namespace Atlas.Modules.Documents.Application.Abstractions;

// Wiki'nin GetAllWikiPagesRawQuery deseniyle AYNI felsefe - GetAllAsync
// FİLTRESİZ, TÜM belgeleri döndürüyor, departman/görünürlük/sayfalama filtresi
// Gün 4'te yazılacak Query Handler'da (bellekte) uygulanacak. Vault'un aksine
// (orada güvenlik hassasiyeti "sahibinin olmayan veriyi hiç belleğe çekme"
// gerekçesiyle SQL seviyesinde filtreye zorluyordu) Document Library daha çok
// Wiki'nin "gözat/keşfet" modeline benziyor - SQL seviyesi filtre şimdilik
// YAGNI, veri hacmi büyürse yeniden değerlendirilir.
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);

    // P4 Gün 3'te Wiki'nin IUnitOfWork deseni burada da devrede - SaveChangesAsync
    // BİLEREK yok, her metot sadece change tracker'a ekliyor. Kalıcı hale
    // getirmek çağıran Handler'ın IUnitOfWork.SaveChangesAsync'i (genelde bir
    // IOutboxWriter.Enqueue ile AYNI değişiklik kümesinde) çağırmasıyla olur -
    // P3'teki (Vault'un basit deseniyle aynı, her metot kendi SaveChanges'ini
    // çağıran) hâlinden BİLİNÇLİ bir geçiş, artık Outbox mesajıyla ATOMİK
    // yazmamız gerektiği için.
    Task AddAsync(Document document, CancellationToken ct = default);

    Task UpdateAsync(Document document, CancellationToken ct = default);

    Task DeleteAsync(Document document, CancellationToken ct = default);
}
