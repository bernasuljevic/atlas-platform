using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

// DocumentDto'nun AKSİNE StorageKey İÇERİYOR - ama bu DTO istemciye HİÇ
// dönmüyor, sadece DocumentsEndpoints.cs'in indirme endpoint'i içinde
// IFileStorageService.OpenReadAsync'e geçirmek için, sunucu içinde kalıyor.
public record DocumentDownloadInfoDto(string StorageKey, string ContentType, string OriginalFileName);

// GetDocumentByIdQuery ile AYNI "varlığı gizle" deseni - görünür değilse
// null döner, endpoint 404'e çevirir.
public record GetDocumentDownloadInfoQuery(Guid Id) : IRequest<DocumentDownloadInfoDto?>;
