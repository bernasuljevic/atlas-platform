using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public record GetWikiDashboardQuery(int ItemsPerSection = 5) : IRequest<WikiDashboardDto>;
