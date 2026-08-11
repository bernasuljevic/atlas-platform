using Atlas.Modules.Documents.Application.Documents;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

public record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto?>;
