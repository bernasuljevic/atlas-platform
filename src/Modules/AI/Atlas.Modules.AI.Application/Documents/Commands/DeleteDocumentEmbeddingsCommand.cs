using MediatR;

namespace Atlas.Modules.AI.Application.Documents.Commands;

/// <summary>
/// DeleteWikiPageEmbeddingsCommand'ın Documents tarafındaki karşılığı -
/// istemci bunu hiç bilmiyor, sadece DocumentDeletedEvent'e abone olan
/// DocumentDeletedEventHandler (AI.Infrastructure) tarafından tetikleniyor.
/// </summary>
public record DeleteDocumentEmbeddingsCommand(Guid DocumentId) : IRequest;
