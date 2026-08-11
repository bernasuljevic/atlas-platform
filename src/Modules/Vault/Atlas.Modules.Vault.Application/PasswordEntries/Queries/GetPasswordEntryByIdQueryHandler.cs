using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Queries;

public class GetPasswordEntryByIdQueryHandler : IRequestHandler<GetPasswordEntryByIdQuery, PasswordEntryDto?>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetPasswordEntryByIdQueryHandler(IPasswordEntryRepository repository, ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PasswordEntryDto?> Handle(GetPasswordEntryByIdQuery request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
            return null;

        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;
        var isOwner = _currentUser.IsAuthenticated && _currentUser.UserId == entry.CreatedByUserId;

        // GetWikiPageByIdQueryHandler'daki AYNI desen - Id'yi bilmek görebilmek
        // anlamına gelmiyor. Sahibi/Admin değilse kayıt HİÇ VARMIŞ GİBİ
        // davranılıyor (null -> endpoint 404 döner) - 403 DEĞİL: bir şifre
        // kaydının VARLIĞINI bile başkasına sızdırmamak, Wiki sayfasından
        // daha kritik bir gizlilik gereksinimi.
        if (!viewerIsAdmin && !isOwner)
            return null;

        return entry.ToDto();
    }
}
