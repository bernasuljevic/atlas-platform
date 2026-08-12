using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

// GetDocumentDownloadInfoQuery'nin (mevcut, GÜNCEL dosya için) belirli bir
// ESKİ versiyon için karşılığı - AYNI DocumentDownloadInfoDto'yu (StorageKey
// istemciye hiç dönmüyor) yeniden kullanıyor, yeni bir DTO icat etmeye gerek
// yok.
public record GetDocumentVersionDownloadInfoQuery(Guid DocumentId, int VersionNumber) : IRequest<DocumentDownloadInfoDto?>;
