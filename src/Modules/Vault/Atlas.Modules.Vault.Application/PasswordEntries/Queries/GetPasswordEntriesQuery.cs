using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Queries;

// ICacheableQuery BİLEREK implemente EDİLMEDİ - Wiki'nin GetAllWikiPagesRawQuery'si
// 30 saniyelik bir cache kullanıyor ama Vault için bu YANLIŞ bir tercih olurdu:
// (1) hacim küçük (kişisel bir kasa, organizasyon geneli içerik değil) - cache'in
// asıl faydası (sık istenen büyük bir listeyi tekrar tekrar DB'den çekmemek)
// burada anlamlı değil, (2) güvenlik hassasiyeti - bir kayıt silindikten sonra
// "cache süresi dolana kadar" listede görünmeye devam etmesi kabul edilebilir bir
// risk DEĞİL (Wiki sayfası için sorun değil, bir şifre kaydı için olabilir).
public record GetPasswordEntriesQuery(string? Category = null) : IRequest<IReadOnlyList<PasswordEntryDto>>;
