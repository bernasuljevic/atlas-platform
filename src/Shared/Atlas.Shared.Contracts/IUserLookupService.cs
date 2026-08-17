namespace Atlas.Shared.Contracts;

// Vault paylaşım modeli (D grubu, Gün 1, 2026-08-17) - Vault, "bu e-postayla
// bir kullanıcı var mı, ID'si ne" sorusuna cevap vermek için Auth'a ihtiyaç
// duyuyor ama Auth'un Domain/Application/Infrastructure'ına ASLA doğrudan
// referans veremez (modüller arası izolasyon kuralı). ICurrentUserAccessor/
// IWikiVisibilityChecker ile AYNI desen - Vault bu arayüzü TANIYOR, gerçek
// implementasyonunu (Auth.Infrastructure'daki AuthUserLookupService) hiç
// bilmiyor.
public interface IUserLookupService
{
    Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default);
}

// Sadece paylaşım akışının ihtiyaç duyduğu üç alan - User entity'sinin
// TAMAMI (PasswordHash, EmailVerified vb.) DEĞİL, modüller arası sızıntıyı
// en aza indirmek için bilinçli olarak dar bir DTO.
public record UserSummary(Guid Id, string Email, string? FullName);
