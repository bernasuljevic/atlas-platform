using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public class RevealPasswordEntryCommandHandler : IRequestHandler<RevealPasswordEntryCommand, string>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEncryptor _encryptor;
    private readonly ICurrentUserAccessor _currentUser;

    public RevealPasswordEntryCommandHandler(
        IPasswordEntryRepository repository, IPasswordEncryptor encryptor, ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _encryptor = encryptor;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(RevealPasswordEntryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Parolayı görüntülemek için giriş yapmış olmalısınız.");

        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (entry is null)
            throw new ArgumentException("Kayıt bulunamadı.", nameof(request.Id));

        var isOwner = entry.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu kaydın parolasını görüntüleme yetkiniz yok.");

        request.AuditDetails = entry.Title;

        entry.MarkAccessed();
        await _repository.UpdateAsync(entry, cancellationToken);

        // Düz metin parola SADECE bu satırda üretiliyor, doğrudan çağırana
        // dönüyor - hiçbir yere (log, DTO, cache) yazılmıyor.
        return _encryptor.Decrypt(entry.EncryptedPassword);
    }
}
