using MediatR;

namespace Atlas.Modules.AI.Application.WikiPages.Commands;

/// <summary>
/// İstemci bu Command'ı hiç bilmiyor/çağırmıyor - sadece WikiPageCreatedEvent'e
/// abone olan WikiPageCreatedEventHandler (AI.Infrastructure) tarafından
/// tetikleniyor. "IRequest" (generic olmayan) kullanıyoruz çünkü bu bir Command -
/// bir yan etki (embedding üretip kaydetmek) yapıyor, anlamlı bir dönüş değeri yok.
/// </summary>
public record GenerateWikiPageEmbeddingsCommand(Guid WikiPageId, string Content) : IRequest;
