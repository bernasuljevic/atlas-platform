using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public class UpdatePasswordEntryCommandHandler : IRequestHandler<UpdatePasswordEntryCommand>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEncryptor _encryptor;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdatePasswordEntryCommandHandler(
        IPasswordEntryRepository repository, IPasswordEncryptor encryptor, ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _encryptor = encryptor;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdatePasswordEntryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Şifre düzenlemek için giriş yapmış olmalısınız.");

        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (entry is null)
            throw new ArgumentException("Kayıt bulunamadı.", nameof(request.Id));

        // Yetki kuralı: DeleteWikiPageCommandHandler'daki AYNI desen - sadece
        // oluşturan kullanıcı ya da Admin. Vault'ta departman bazlı bir
        // bypass/paylaşım YOK (bkz. PasswordEntry.cs'teki not) - bilinçli
        // olarak en dar, en güvenli varsayılan.
        var isOwner = entry.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu kaydı düzenleme yetkiniz yok.");

        // Kullanıcı yeni bir parola YAZMADIYSA (alan boş bırakıldıysa) mevcut
        // şifrelenmiş değeri aynen koru - "her düzenlemede parolayı yeniden
        // gir" kötü bir UX olurdu (bkz. Command'daki not).
        var encryptedPassword = string.IsNullOrWhiteSpace(request.Password)
            ? entry.EncryptedPassword
            : _encryptor.Encrypt(request.Password);

        entry.Update(
            request.Title, request.Username, encryptedPassword, request.Url,
            request.Description, request.Category, request.Notes);

        request.AuditDetails = entry.Title;

        await _repository.UpdateAsync(entry, cancellationToken);
    }
}
