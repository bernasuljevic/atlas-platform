using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public class DeletePasswordEntryCommandHandler : IRequestHandler<DeletePasswordEntryCommand>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEntryShareRepository _shareRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public DeletePasswordEntryCommandHandler(
        IPasswordEntryRepository repository, IPasswordEntryShareRepository shareRepository,
        ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _shareRepository = shareRepository;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePasswordEntryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Şifre silmek için giriş yapmış olmalısınız.");

        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (entry is null)
            throw new ArgumentException("Kayıt bulunamadı.", nameof(request.Id));

        var isOwner = entry.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu kaydı silme yetkiniz yok.");

        // Silinmeden ÖNCE yakalanması şart - DeleteWikiPageCommandHandler'daki
        // AYNI gerekçe (Ders #14/AuditLogEntry.Details notu): kayıt silindikten
        // sonra Title'a erişecek başka hiçbir yer kalmıyor.
        request.AuditDetails = entry.Title;

        // Vault paylaşım modeli (D grubu, Gün 1) - DeleteWikiPageCommandHandler'ın
        // versiyon geçmişini temizlemesiyle AYNI gerekçe: kayıt silinince
        // "yetim" paylaşım satırları kalmasın.
        await _shareRepository.DeleteAllForEntryAsync(entry.Id, cancellationToken);

        await _repository.DeleteAsync(entry, cancellationToken);
    }
}
