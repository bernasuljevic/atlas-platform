using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

// RecentlyAddedCount BİLEREK ItemsPerSection'dan AYRI bir parametre (2026-08-17,
// "Son Eklenen Makaleler" carousel'ı) - ItemsPerSection üçü de (recentlyAdded/
// recentlyUpdated/departmentSpecific) etkiler, ama SADECE recentlyAdded'ın
// carousel'ın birden fazla "sayfası" için daha fazla veriye ihtiyacı var.
// ItemsPerSection'ı büyütmek recentlyUpdated/departmentSpecific'i de gereksiz
// yere şişirirdi (departmentSpecific'in listesi frontend'de hiç render
// edilmiyor bile, sadece Count'u kullanılıyor).
public record GetWikiDashboardQuery(int ItemsPerSection = 5, int RecentlyAddedCount = 5) : IRequest<WikiDashboardDto>;
