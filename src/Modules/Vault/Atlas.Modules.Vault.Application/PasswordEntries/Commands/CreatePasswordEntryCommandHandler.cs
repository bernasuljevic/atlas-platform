using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public class CreatePasswordEntryCommandHandler : IRequestHandler<CreatePasswordEntryCommand, Guid>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEncryptor _encryptor;
    private readonly ICurrentUserAccessor _currentUser;

    public CreatePasswordEntryCommandHandler(
        IPasswordEntryRepository repository, IPasswordEncryptor encryptor, ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _encryptor = encryptor;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePasswordEntryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Şifre eklemek için giriş yapmış olmalısınız.");

        // Düz metin parola SADECE burada, bir an için, RAM'de var - hiçbir yere
        // (log, exception mesajı, cache) yazılmadan hemen şifreleniyor.
        var encryptedPassword = _encryptor.Encrypt(request.Password);

        var entry = PasswordEntry.Create(
            request.Title, request.Username, encryptedPassword, request.Url,
            request.Description, request.Category, request.Notes, _currentUser.UserId.Value, _currentUser.Email);

        // AuditBehavior, Handler bitince (next() sonrası) bunu okuyacak - bkz.
        // IAuditableCommand.AuditDetails'teki not. DİKKAT: burada şifrenin
        // KENDİSİ değil, sadece kaydın Title'ı yazılıyor - audit log asla
        // parola içeriğine dokunmuyor.
        request.AuditDetails = entry.Title;

        await _repository.AddAsync(entry, cancellationToken);

        return entry.Id;
    }
}
