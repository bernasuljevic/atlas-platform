using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

// DepartmentName BİLEREK YOK - Document.UpdateMetadata'daki AYNI kural
// (WikiPage.Update'in departmanı değiştirememesiyle aynı gerekçe): bir
// belgenin departmanı "başka bir yere taşıma" sayılır, ayrı bir işlem olurdu.
public record UpdateDocumentCommand(
    Guid Id, string Title, string? Description, string Visibility, string? Tags) : IRequest, IAuditableCommand
{
    public string AuditAction => "Document.Updated";
    public string? AuditResourceId => Id.ToString();
    public string? AuditDetails { get; set; }
}
