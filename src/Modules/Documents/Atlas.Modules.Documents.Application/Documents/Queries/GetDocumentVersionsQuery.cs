using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

// GetDocumentByIdQuery ile AYNI "varlığı gizle" deseni - belge görünür
// değilse (ya da hiç yoksa) null döner, endpoint 404'e çevirir. Görünür ama
// hiç eski versiyonu olmayan bir belge için BOŞ liste döner (null DEĞİL) -
// "belgeyi göremiyorsun" ile "henüz versiyon geçmişi yok" iki farklı durum.
public record GetDocumentVersionsQuery(Guid DocumentId) : IRequest<IReadOnlyList<DocumentVersionDto>?>;

public record DocumentVersionDto(
    int VersionNumber, string OriginalFileName, string ContentType, long SizeBytes,
    string? CreatedByEmail, DateTime CreatedAtUtc);
