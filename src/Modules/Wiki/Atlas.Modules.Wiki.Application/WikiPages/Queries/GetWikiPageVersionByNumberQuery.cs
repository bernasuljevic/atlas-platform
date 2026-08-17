using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

// GetWikiPageVersionsQuery ile AYNI "varlığı gizle" deseni - hem sayfa hem
// istenen versiyon numarası bulunup GÖRÜNÜR olmalı, aksi halde null (404).
public record GetWikiPageVersionByNumberQuery(Guid PageId, int VersionNumber) : IRequest<WikiPageVersionDto?>;

public record WikiPageVersionDto(
    int VersionNumber, string Title, string Content, string Visibility, string? Tags,
    string? EditedByEmail, DateTime EditedAtUtc);
