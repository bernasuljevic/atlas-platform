using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public record RemovePasswordEntryShareCommand(Guid PasswordEntryId, Guid SharedWithUserId)
    : IRequest, IAuditableCommand
{
    public string AuditAction => "PasswordEntry.ShareRevoked";
    public string? AuditResourceId => PasswordEntryId.ToString();
    public string? AuditDetails { get; set; }
}
