using Atlas.Modules.Vault.Application.Abstractions;

namespace Atlas.Modules.Vault.Application.Tests.Fakes;

// Gerçek DataProtectionPasswordEncryptor'ın (Infrastructure) yerine - basit,
// tersinir bir sabit önek deseni. Amaç GERÇEK bir şifreleme algoritması test
// etmek DEĞİL (o zaten Infrastructure'ın işi), sadece Handler'ın
// Encrypt/Decrypt'i doğru sırada/doğru argümanla çağırdığını doğrulamak.
public class FakePasswordEncryptor : IPasswordEncryptor
{
    private const string Prefix = "enc:";

    public string Encrypt(string plainPassword) => Prefix + plainPassword;

    public string Decrypt(string encryptedPassword) =>
        encryptedPassword.StartsWith(Prefix, StringComparison.Ordinal)
            ? encryptedPassword[Prefix.Length..]
            : throw new InvalidOperationException("Beklenmeyen şifreli metin formatı.");
}
