namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// ICacheInvalidatingCommand ile aynı desen: bir Command bunu implemente
/// ederse, MediatR pipeline'ındaki AuditBehavior Handler BAŞARIYLA
/// tamamlandıktan SONRA bir audit satırı yazdırır (Handler hata fırlatırsa
/// hiç yazılmaz - sadece gerçekleşen işlemler denetleniyor).
///
/// AuditResourceId BİLEREK nullable - Create komutlarında (ör.
/// CreateWikiPageCommand) kaynağın ID'si Handler çalışana kadar bilinmiyor,
/// bu durumda null bırakılıp AuditBehavior'ın TResponse'tan (Guid dönüyorsa)
/// türetmesine izin veriliyor. Delete gibi ID'si zaten Command'ın kendisinde
/// olan komutlar bunu doğrudan doldurabilir.
/// </summary>
public interface IAuditableCommand
{
    string AuditAction { get; }
    string? AuditResourceId { get; }

    // BİLEREK settable - AuditResourceId'nin aksine, insan-okunur özet
    // (ör. WikiPage.Title) çoğu zaman Command oluşturulduğu anda değil,
    // Handler içinde (ör. silmeden önce ilgili kaydı çektikten sonra) ortaya
    // çıkıyor. Handler bu alanı Handle() içinde doldurur, AuditBehavior
    // Handler bitince (next() sonrası) okur - aynı Command nesnesi pipeline
    // boyunca taşındığı için bu mutasyon güvenle görülebiliyor.
    string? AuditDetails { get; set; }
}
