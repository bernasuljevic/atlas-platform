using Atlas.Shared.Kernel.Entities;

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

    // EF Core gibi ORM'ler nesneyi veritabanından okurken parametresiz constructor'a
    // ihtiyaç duyar, ama dışarıdan "new User()" ile boş kullanıcı oluşturulmasını
    // istemediğimiz için private yapıyoruz.
    private User() { }

    private User(Guid id, string email, string fullName, string passwordHash) : base(id)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
    }

    /// <summary>
    /// "new User(...)" yerine bilerek statik factory method (Create) kullanıyoruz.
    /// Neden? Çünkü bu sayede geçersiz bir kural içeren User nesnesi HİÇBİR ZAMAN
    /// var olamaz - kurallar tek bir kapıdan (buradan) geçmek zorunda.
    /// </summary>
    public static User Create(string email, string fullName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email boş olamaz.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Şifre boş olamaz.", nameof(passwordHash));

        return new User(Guid.NewGuid(), email.Trim().ToLowerInvariant(), fullName.Trim(), passwordHash);
    }
}
