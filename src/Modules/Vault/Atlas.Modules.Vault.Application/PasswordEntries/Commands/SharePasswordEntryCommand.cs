using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

// CreatePasswordEntryCommand'daki AYNI IAuditableCommand deseni - "kim bu
// kayda erişebilir" değişikliği RevealPasswordEntryCommand kadar (belki daha
// da fazla) denetlenmesi gereken bir olay.
public record SharePasswordEntryCommand(Guid PasswordEntryId, string SharedWithEmail)
    : IRequest, IAuditableCommand
{
    public string AuditAction => "PasswordEntry.Shared";
    public string? AuditResourceId => PasswordEntryId.ToString();
    public string? AuditDetails { get; set; }
}
