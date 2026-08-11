using Atlas.Modules.Vault.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Atlas.Modules.Vault.Infrastructure.Encryption;

/// <summary>
/// ASP.NET Core'un yerleşik Data Protection API'sini sarmalıyor - kendi AES
/// anahtar yönetimimizi elle yazmak yerine (bu genellikle "kendi kriptonu
/// yazma" hatasına düşülen bir alan - anahtar rotasyonu, IV üretimi, güvenli
/// depolama hepsi kolayca yanlış yapılabilir), framework'ün zaten
/// denetlenmiş, üretime hazır mekanizması kullanılıyor.
///
/// "Purpose string" (`CreateProtector` argümanı) - Data Protection'ın kendi
/// izolasyon mekanizması: aynı anahtar zincirinden türetilen bir alt-anahtar
/// SADECE bu tam purpose string'iyle şifrelenmiş veriyi çözebilir. İleride
/// başka bir modül de Data Protection kullanırsa (farklı bir purpose
/// string'iyle), iki modülün şifreli verileri birbirini YANLIŞLIKLA
/// çözemez - versiyon numarası (".v1") ileride algoritma/anahtar stratejisi
/// değişirse eski verinin yeni koddan farklı ele alınabilmesi için.
/// </summary>
public class DataProtectionPasswordEncryptor : IPasswordEncryptor
{
    private readonly IDataProtector _protector;

    public DataProtectionPasswordEncryptor(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Atlas.Vault.PasswordEntry.v1");
    }

    public string Encrypt(string plainPassword) => _protector.Protect(plainPassword);

    public string Decrypt(string encryptedPassword) => _protector.Unprotect(encryptedPassword);
}
