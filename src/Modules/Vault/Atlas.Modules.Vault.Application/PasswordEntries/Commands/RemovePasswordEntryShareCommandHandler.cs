using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public class RemovePasswordEntryShareCommandHandler : IRequestHandler<RemovePasswordEntryShareCommand>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEntryShareRepository _shareRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public RemovePasswordEntryShareCommandHandler(
        IPasswordEntryRepository repository, IPasswordEntryShareRepository shareRepository,
        ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _shareRepository = shareRepository;
        _currentUser = currentUser;
    }

    public async Task Handle(RemovePasswordEntryShareCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Bir paylaşımı kaldırmak için giriş yapmış olmalısınız.");

        var entry = await _repository.GetByIdAsync(request.PasswordEntryId, cancellationToken);
        if (entry is null)
            throw new ArgumentException("Kayıt bulunamadı.", nameof(request.PasswordEntryId));

        var isOwner = entry.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu kaydın paylaşımlarını yönetme yetkiniz yok.");

        var share = await _shareRepository.GetAsync(entry.Id, request.SharedWithUserId, cancellationToken);
        if (share is null)
            throw new ArgumentException("Paylaşım bulunamadı.", nameof(request.SharedWithUserId));

        request.AuditDetails = $"{entry.Title} -> {share.SharedWithEmail}";

        await _shareRepository.RemoveAsync(share, cancellationToken);
    }
}
