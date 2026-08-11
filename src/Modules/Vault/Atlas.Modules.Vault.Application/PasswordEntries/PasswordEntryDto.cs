using Atlas.Modules.Vault.Domain.Entities;

namespace Atlas.Modules.Vault.Application.PasswordEntries;

// DİKKAT: Parolanın kendisi (EncryptedPassword/plaintext) BURADA YOK - bu DTO
// liste ve tekil görüntüleme (GetById) sorgularının döndürdüğü tek şekil,
// ikisi de SADECE metadata gösteriyor. Düz metin parola YALNIZCA
// RevealPasswordEntryCommand'ın dönüş değeri (çıplak bir string, DTO bile
// değil) - başka HİÇBİR yerden sızmıyor.
public record PasswordEntryDto(
    Guid Id,
    string Title,
    string? Username,
    string? Url,
    string? Description,
    string? Category,
    string? Notes,
    Guid CreatedByUserId,
    string? CreatedByEmail,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastAccessedAtUtc);

public static class PasswordEntryMappingExtensions
{
    public static PasswordEntryDto ToDto(this PasswordEntry entry) => new(
        entry.Id, entry.Title, entry.Username, entry.Url, entry.Description,
        entry.Category, entry.Notes, entry.CreatedByUserId, entry.CreatedByEmail,
        entry.CreatedAtUtc, entry.UpdatedAtUtc, entry.LastAccessedAtUtc);
}
