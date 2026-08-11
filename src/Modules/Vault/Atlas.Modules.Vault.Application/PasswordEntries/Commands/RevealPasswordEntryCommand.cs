using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

// BİR QUERY DEĞİL, BİLEREK bir Command - GetPasswordEntryByIdQuery'nin AKSİNE
// yan etkisi var: MarkAccessed() (LastAccessedAtUtc) VE audit kaydı
// ("PasswordEntry.Revealed" - kullanıcının açık isteği: "kopyalama işlemi
// audit log'a yazılmalı" - reveal, kopyalamanın ön koşulu, o yüzden burada
// denetleniyor). Bu yüzden GetById'nin "bulunamadı/yetkisiz -> null -> 404"
// deseni YERİNE Update/Delete'teki throw tabanlı desen kullanılıyor -
// AuditBehavior sadece next() BAŞARIYLA dönerse yazıyor (bkz. AuditBehavior),
// reddedilen bir reveal denemesi (throw) hiç audit'e düşmüyor - bu, projenin
// genelindeki AuditBehavior'ın (best-effort, sadece başarılı işlemleri
// denetleyen) mevcut, Vault'a özel olmayan bir sınırlaması.
public record RevealPasswordEntryCommand(Guid Id) : IRequest<string>, IAuditableCommand
{
    public string AuditAction => "PasswordEntry.Revealed";
    public string? AuditResourceId => Id.ToString();
    public string? AuditDetails { get; set; }
}
