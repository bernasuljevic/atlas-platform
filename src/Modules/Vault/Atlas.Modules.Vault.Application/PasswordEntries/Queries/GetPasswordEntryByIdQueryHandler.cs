using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Queries;

public class GetPasswordEntryByIdQueryHandler : IRequestHandler<GetPasswordEntryByIdQuery, PasswordEntryDto?>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEntryShareRepository _shareRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetPasswordEntryByIdQueryHandler(
        IPasswordEntryRepository repository, IPasswordEntryShareRepository shareRepository,
        ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _shareRepository = shareRepository;
        _currentUser = currentUser;
    }

    public async Task<PasswordEntryDto?> Handle(GetPasswordEntryByIdQuery request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
            return null;

        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;
        var isOwner = _currentUser.IsAuthenticated && _currentUser.UserId == entry.CreatedByUserId;

        // Vault paylaşım modeli (D grubu, Gün 1) - paylaşılan bir kayıt da
        // GÖRÜNTÜLENEBİLİR (owner-or-Admin'in AYNI "varlığı gizle" kuralına
        // ÜÇÜNCÜ bir istisna). Sadece isOwner/viewerIsAdmin false ise sorgulanıyor
        // - gereksiz bir DB round-trip'inden kaçınmak için.
        var isSharedWithViewer = !isOwner && !viewerIsAdmin && _currentUser.IsAuthenticated && _currentUser.UserId is not null
            && await _shareRepository.IsSharedWithAsync(entry.Id, _currentUser.UserId.Value, cancellationToken);

        // GetWikiPageByIdQueryHandler'daki AYNI desen - Id'yi bilmek görebilmek
        // anlamına gelmiyor. Sahibi/Admin/paylaşılan değilse kayıt HİÇ VARMIŞ
        // GİBİ davranılıyor (null -> endpoint 404 döner) - 403 DEĞİL: bir şifre
        // kaydının VARLIĞINI bile başkasına sızdırmamak, Wiki sayfasından
        // daha kritik bir gizlilik gereksinimi.
        if (!viewerIsAdmin && !isOwner && !isSharedWithViewer)
            return null;

        return entry.ToDto();
    }
}
