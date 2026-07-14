using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

public class WikiPage : Entity<Guid>
{
    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public string DepartmentName { get; private set; } = default!;
    public WikiVisibility Visibility { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private WikiPage() { }

    private WikiPage(
        Guid id, string title, string content, string departmentName,
        WikiVisibility visibility, Guid createdByUserId, DateTime createdAtUtc) : base(id)
    {
        Title = title;
        Content = content;
        DepartmentName = departmentName;
        Visibility = visibility;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public static WikiPage Create(string title, string content, string departmentName,
        WikiVisibility visibility, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Başlık boş olamaz.", nameof(title));

        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException("Departman adı boş olamaz.", nameof(departmentName));

        return new WikiPage(
            Guid.NewGuid(), title.Trim(), content, departmentName.Trim(),
            visibility, createdByUserId, DateTime.UtcNow);
    }

    /// <summary>
    /// Yetkilendirme kuralının kalbi - "bu sayfayı şu departmandaki biri görebilir mi?"
    /// sorusunun cevabı burada, TEK YERDE yaşıyor. Yarın kural değişirse (örn. "yöneticiler
    /// her departmanı görebilir") sadece bu metodu değiştiririz, her yerde arama yapmayız.
    /// </summary>
    public bool IsVisibleTo(string? viewerDepartmentName)
    {
        if (Visibility == WikiVisibility.Public)
            return true;

        return !string.IsNullOrWhiteSpace(viewerDepartmentName)
            && string.Equals(DepartmentName, viewerDepartmentName, StringComparison.OrdinalIgnoreCase);
    }
}
