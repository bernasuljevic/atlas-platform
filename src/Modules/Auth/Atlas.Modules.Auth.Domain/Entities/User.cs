using Atlas.Shared.Kernel.Entities;
using Atlas.Modules.Auth.Domain.Enums;

namespace Atlas.Modules.Auth.Domain.Entities;

/// <summary>
/// DİKKAT: Bu sınıfta ASP.NET Core, Entity Framework, JWT... hiçbiri yok.
/// Sadece saf C#. Bir Domain sınıfı, altyapıdan tamamen habersiz olmalı.
/// Yarın veritabanını değiştirsek bile bu dosyaya dokunmayız.
/// </summary>
public class User : Entity<Guid>
{
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }

    // Kullanıcının GERÇEKTEN ait olduğu departman - Wiki modülündeki
    // "departman bazlı görünürlük" kuralının artık istemciden gelen bir query
    // string'e değil, buna güvenmesi gerekiyor (bkz. GetWikiPagesQueryHandler).
    // Nullable: departmansız kullanıcılar sadece Public sayfaları görür.
    public string? Department { get; private set; }

    private User() { }

    private User(Guid id, string email, string fullName, string passwordHash, UserRole role, string? department) : base(id)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        Role = role;
        Department = department;
    }

    /// <summary>
    /// "role" parametresine varsayılan değer verdik (Member) - böylece var olan
    /// tüm "User.Create(email, fullName, hash)" çağrıları (Register, seed vs.)
    /// hiçbir değişiklik yapmadan derlenmeye devam eder, otomatik olarak Member olur.
    /// Sadece admin seed'inde bilerek Admin geçeceğiz.
    /// </summary>
    public static User Create(string email, string fullName, string passwordHash, UserRole role = UserRole.Member, string? department = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email boş olamaz.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Şifre boş olamaz.", nameof(passwordHash));

        return new User(Guid.NewGuid(), email.Trim().ToLowerInvariant(), fullName.Trim(), passwordHash, role,
            string.IsNullOrWhiteSpace(department) ? null : department.Trim());
    }
}
