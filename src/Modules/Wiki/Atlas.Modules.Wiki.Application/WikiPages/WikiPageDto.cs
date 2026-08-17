namespace Atlas.Modules.Wiki.Application.WikiPages;

public record WikiPageDto(
    Guid Id,
    string Title,
    string Content,
    string DepartmentName,
    string Visibility,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    Guid? FolderId,
    DateTime? UpdatedAtUtc,
    string? CreatedByEmail,
    string? Tags,
    int CurrentVersionNumber);
