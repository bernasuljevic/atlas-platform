using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

// Password BİLEREK nullable - kullanıcı sadece Title/Category gibi
// metadata'yı düzenlemek isteyebilir, her seferinde parolayı YENİDEN
// yazmaya zorlamak kötü bir kullanıcı deneyimi olurdu. null/boş gelirse
// Handler mevcut EncryptedPassword'ü aynen korur (bkz. Handler'daki not).
public record UpdatePasswordEntryCommand(
    Guid Id,
    string Title,
    string? Username,
    string? Password,
    string? Url,
    string? Description,
    string? Category,
    string? Notes) : IRequest, IAuditableCommand
{
    public string AuditAction => "PasswordEntry.Updated";
    public string? AuditResourceId => Id.ToString();
    public string? AuditDetails { get; set; }
}
