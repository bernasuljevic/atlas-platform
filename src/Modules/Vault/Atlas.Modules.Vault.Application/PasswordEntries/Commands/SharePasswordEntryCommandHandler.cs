using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Modules.Vault.Domain.Entities;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public class SharePasswordEntryCommandHandler : IRequestHandler<SharePasswordEntryCommand>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEntryShareRepository _shareRepository;
    private readonly IUserLookupService _userLookup;
    private readonly ICurrentUserAccessor _currentUser;

    public SharePasswordEntryCommandHandler(
        IPasswordEntryRepository repository, IPasswordEntryShareRepository shareRepository,
        IUserLookupService userLookup, ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _shareRepository = shareRepository;
        _userLookup = userLookup;
        _currentUser = currentUser;
    }

    public async Task Handle(SharePasswordEntryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Bir kaydı paylaşmak için giriş yapmış olmalısınız.");

        var entry = await _repository.GetByIdAsync(request.PasswordEntryId, cancellationToken);
        if (entry is null)
            throw new ArgumentException("Kayıt bulunamadı.", nameof(request.PasswordEntryId));

        // UpdatePasswordEntryCommandHandler'daki AYNI owner-or-Admin kuralı -
        // paylaşımı yönetmek de bir düzenleme eylemi sayılıyor (paylaşılan
        // kullanıcının KENDİSİ başkasına yeniden paylaşamıyor).
        var isOwner = entry.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu kaydı paylaşma yetkiniz yok.");

        var targetUser = await _userLookup.FindByEmailAsync(request.SharedWithEmail, cancellationToken);
        if (targetUser is null)
            throw new ArgumentException("Bu e-posta ile bir kullanıcı bulunamadı.", nameof(request.SharedWithEmail));

        if (targetUser.Id == entry.CreatedByUserId)
            throw new ArgumentException("Bir kayıt kendi sahibiyle paylaşılamaz.", nameof(request.SharedWithEmail));

        var existingShare = await _shareRepository.GetAsync(entry.Id, targetUser.Id, cancellationToken);
        if (existingShare is not null)
            throw new ArgumentException("Bu kayıt zaten bu kullanıcıyla paylaşılmış.", nameof(request.SharedWithEmail));

        var share = PasswordEntryShare.Create(entry.Id, targetUser.Id, targetUser.Email, _currentUser.UserId.Value);
        await _shareRepository.AddAsync(share, cancellationToken);

        request.AuditDetails = $"{entry.Title} -> {targetUser.Email}";
    }
}
