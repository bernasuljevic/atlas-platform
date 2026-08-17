using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Queries;

// GetWikiPageVersionsQuery'deki AYNI "varlığı gizle" deseni - kayıt yoksa ya
// da görüntüleyen owner-or-Admin değilse null döner (endpoint 404 yapar).
// SADECE sahibi/Admin "kiminle paylaşıldığını" görebiliyor - paylaşılan
// kullanıcının KENDİSİ bu listeyi göremiyor (kimle BAŞKA paylaşıldığını
// bilmesine gerek yok).
public record GetPasswordEntrySharesQuery(Guid PasswordEntryId) : IRequest<IReadOnlyList<PasswordEntryShareDto>?>;

public record PasswordEntryShareDto(Guid SharedWithUserId, string? SharedWithEmail, DateTime SharedAtUtc);
