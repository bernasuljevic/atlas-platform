using Atlas.Modules.Auth.Application.Abstractions;

namespace Atlas.Modules.Auth.Application.Tests.Fakes;

// Gerçek PBKDF2 yerine - testlerde hız/basitlik için "hash" sadece "plain:" öneki
// ekliyor, "doğrulama" bu öneki çıkarıp karşılaştırıyor. Güvenlik burada önemli
// değil, sadece Handler'ların IPasswordHasher'ı doğru çağırdığını doğruluyoruz.
public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"plain:{password}";

    public bool Verify(string password, string hash) => hash == $"plain:{password}";
}
