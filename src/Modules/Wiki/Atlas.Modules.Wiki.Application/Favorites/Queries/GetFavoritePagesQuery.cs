using Atlas.Modules.Wiki.Application.WikiPages;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Favorites.Queries;

public record GetFavoritePagesQuery : IRequest<IReadOnlyList<WikiPageDto>>;
