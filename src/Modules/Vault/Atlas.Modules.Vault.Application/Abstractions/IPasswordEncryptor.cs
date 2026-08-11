namespace Atlas.Modules.Vault.Application.Abstractions;

// IPasswordHasher (Auth modülü) ile KASITLI OLARAK AYNI İSİM DEĞİL - "Hasher"
// TEK YÖNLÜ (login şifresi doğrulama: hash'i tekrar üretip karşılaştır, GERİ
// ÇÖZÜLEMEZ), "Encryptor" İKİ YÖNLÜ (Vault: kullanıcı daha sonra düz metni
// GÖRMEK istiyor - bu spec'in kendi açık talimatıydı: "Hashing ile encryption
// arasındaki farkı dikkate al"). Gerçek implementasyonu (Infrastructure'daki
// DataProtectionPasswordEncryptor) ASP.NET Core'un yerleşik Data Protection
// API'sini sarmalıyor - YENİ bir NuGet paketi/kendi AES anahtar yönetimini
// elle yazmak yerine, framework'ün zaten test edilmiş, anahtar rotasyonunu
// kendi hâlleden mekanizması kullanılıyor.
public interface IPasswordEncryptor
{
    string Encrypt(string plainPassword);
    string Decrypt(string encryptedPassword);
}
