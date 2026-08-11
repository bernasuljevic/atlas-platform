using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public record DeletePasswordEntryCommand(Guid Id) : IRequest, IAuditableCommand
{
    public string AuditAction => "PasswordEntry.Deleted";
    public string? AuditResourceId => Id.ToString();
    public string? AuditDetails { get; set; }
}
