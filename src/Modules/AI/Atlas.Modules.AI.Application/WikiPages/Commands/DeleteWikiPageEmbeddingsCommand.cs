using MediatR;

namespace Atlas.Modules.AI.Application.WikiPages.Commands;

/// <summary>
/// GenerateWikiPageEmbeddingsCommand'ın silme tarafındaki karşılığı - istemci
/// bunu hiç bilmiyor, sadece WikiPageDeletedEvent'e abone olan
/// WikiPageDeletedEventHandler (AI.Infrastructure) tarafından tetikleniyor.
/// </summary>
public record DeleteWikiPageEmbeddingsCommand(Guid WikiPageId) : IRequest;
