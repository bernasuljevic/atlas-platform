namespace Atlas.Modules.Wiki.Application.WikiPages;

public record WikiPageDto(
    Guid Id,
    string Title,
    string Content,
    string DepartmentName,
    string Visibility,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc);
