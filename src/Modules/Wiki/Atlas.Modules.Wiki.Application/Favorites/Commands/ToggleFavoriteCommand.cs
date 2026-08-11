using MediatR;

namespace Atlas.Modules.Wiki.Application.Favorites.Commands;

// IAuditableCommand BİLEREK implemente EDİLMİYOR - favoriye ekleme/çıkarma
// güvenlik açısından önemsiz bir eylem, audit log'u (WikiPage.Created/Deleted
// gibi gerçekten denetlenmesi gereken olaylarla dolu) gereksiz gürültüye
// boğardı. Dönüş değeri (bool) işlemden SONRA favori mi değil mi olduğu -
// istemci bunu tekrar bir GET atmadan optimistic UI güncellemesi için kullanabilir.
public record ToggleFavoriteCommand(Guid WikiPageId) : IRequest<bool>;
