using Atlas.Modules.Wiki.Application.WikiPages;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Pins.Queries;

public record GetPinnedPagesQuery : IRequest<IReadOnlyList<WikiPageDto>>;
