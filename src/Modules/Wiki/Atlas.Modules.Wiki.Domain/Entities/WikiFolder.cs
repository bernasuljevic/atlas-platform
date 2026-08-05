using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.Wiki.Domain.Entities;

/// <summary>
/// Departman içi hiyerarşik klasörleme (örn. IT > React > UI). ParentFolderId
/// null ise departmanın kök klasörüdür. Görünürlük için AYRI bir alan BİLEREK
/// YOK - klasör sadece organizasyonel bir kap, bir sayfayı kimin görebileceği
/// hâlâ TEK doğruluk kaynağından (WikiPage.Visibility, bkz. WikiVisibilityRules)
/// geliyor. Böylece var olan, test edilmiş görünürlük kuralı hiç değişmeden
/// klasörlü yapıda da aynen çalışmaya devam ediyor.
/// </summary>
public class WikiFolder : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public string DepartmentName { get; private set; } = default!;
    public Guid? ParentFolderId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private WikiFolder() { }

    private WikiFolder(
        Guid id, string name, string departmentName, Guid? parentFolderId,
        Guid createdByUserId, DateTime createdAtUtc) : base(id)
    {
        Name = name;
        DepartmentName = departmentName;
        ParentFolderId = parentFolderId;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public static WikiFolder Create(
        string name, string departmentName, Guid? parentFolderId, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Klasör adı boş olamaz.", nameof(name));

        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException("Departman adı boş olamaz.", nameof(departmentName));

        return new WikiFolder(
            Guid.NewGuid(), name.Trim(), departmentName.Trim(), parentFolderId,
            createdByUserId, DateTime.UtcNow);
    }
}
