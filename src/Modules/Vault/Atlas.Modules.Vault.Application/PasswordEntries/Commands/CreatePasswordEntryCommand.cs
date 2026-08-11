using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

// CreateWikiPageCommand ile AYNI IAuditableCommand deseni - AuditResourceId
// BİLEREK null (yeni kaydın ID'si Handler çalışana kadar bilinmiyor,
// AuditBehavior TResponse'tan (Guid) otomatik türetiyor).
public record CreatePasswordEntryCommand(
    string Title,
    string? Username,
    string Password,
    string? Url,
    string? Description,
    string? Category,
    string? Notes) : IRequest<Guid>, IAuditableCommand
{
    public string AuditAction => "PasswordEntry.Created";
    public string? AuditResourceId => null;
    public string? AuditDetails { get; set; }
}
