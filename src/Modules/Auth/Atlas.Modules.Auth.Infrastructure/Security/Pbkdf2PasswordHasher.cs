using System.Security.Cryptography;
using Atlas.Modules.Auth.Application.Abstractions;

namespace Atlas.Modules.Auth.Infrastructure.Security;

/// <summary>
/// PBKDF2 algoritmasi: sifreyi rastgele bir "salt" ile birlestirip binlerce kez
/// hash'liyor. Salt sayesinde ayni sifreye sahip iki kullanicinin hash'i FARKLI
/// cikar - bu, "rainbow table" saldirilarina karsi temel korumadir.
/// Saklama formati: "{iterasyon}.{salt-base64}.{hash-base64}" - tek string'de
/// dogrulama icin gereken her seyi tasiyoruz.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 3) return false;

        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}