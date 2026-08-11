using MediatR;

namespace Atlas.Modules.Wiki.Application.Pins.Commands;

// ToggleFavoriteCommand'daki AYNI gerekçe - audit'lenmiyor.
public record TogglePinCommand(Guid WikiPageId) : IRequest<bool>;
