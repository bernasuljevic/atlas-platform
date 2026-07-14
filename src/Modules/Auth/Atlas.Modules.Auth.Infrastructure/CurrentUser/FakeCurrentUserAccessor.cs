using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Shared.Contracts;

namespace Atlas.Modules.Auth.Infrastructure.CurrentUser;

/// <summary>
/// GEÇİCİ İMPLEMENTASYON - henüz JWT/login yok, o yüzden "giriş yapmış kullanıcı"
/// diye InMemoryUserRepository'deki ilk kullanıcıyı (admin) sabit olarak kullanıyoruz.
///
/// İleride JWT konusuna geçince bu sınıfı, HttpContext'teki token'dan gerçek kullanıcıyı
/// okuyan bir implementasyonla DEĞİŞTİRECEĞİZ. Wiki modülü (ve ileride başka modüller)
/// bu değişiklikten HİÇ etkilenmeyecek - çünkü onlar sadece ICurrentUserAccessor
/// interface'ini tanıyor, bu sınıfı değil.
/// </summary>
public class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public Guid? UserId { get; }
    public string? Email { get; }
    public bool IsAuthenticated { get; }

    public FakeCurrentUserAccessor(IUserRepository userRepository)
    {
        // NOT: Constructor içinde .GetAwaiter().GetResult() ile senkron bekletmek
        // normalde kaçınılması gereken bir pratiktir - burada sadece geçici bir
        // stub olduğu, kalıcı kod olmadığı için bilerek göz yumuyoruz.
        var users = userRepository.GetAllAsync().GetAwaiter().GetResult();
        var fakeLoggedInUser = users.FirstOrDefault();

        if (fakeLoggedInUser is not null)
        {
            UserId = fakeLoggedInUser.Id;
            Email = fakeLoggedInUser.Email;
            IsAuthenticated = true;
        }
    }
}
