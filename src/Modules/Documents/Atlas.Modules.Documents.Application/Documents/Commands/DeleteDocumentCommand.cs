using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public record DeleteDocumentCommand(Guid Id) : IRequest, IAuditableCommand
{
    public string AuditAction => "Document.Deleted";
    public string? AuditResourceId => Id.ToString();
    public string? AuditDetails { get; set; }
}
