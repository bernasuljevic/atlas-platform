namespace Atlas.Modules.Auth.Application.Abstractions;

/// <summary>
/// Application, şifrenin NASIL hash'lendiğini bilmiyor - sadece "hash'le" ve
/// "doğrula" diyebiliyor. Gerçek algoritma (PBKDF2) Infrastructure'da.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}