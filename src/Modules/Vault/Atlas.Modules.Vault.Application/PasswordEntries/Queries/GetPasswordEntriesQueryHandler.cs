using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Queries;

public class GetPasswordEntriesQueryHandler : IRequestHandler<GetPasswordEntriesQuery, IReadOnlyList<PasswordEntryDto>>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetPasswordEntriesQueryHandler(IPasswordEntryRepository repository, ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PasswordEntryDto>> Handle(
        GetPasswordEntriesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Kayıtları görmek için giriş yapmış olmalısınız.");

        // Admin TÜM kayıtları görebiliyor (viewerFilter = null), normal kullanıcı
        // KENDİ OLUŞTURDUKLARI + KENDİSİYLE PAYLAŞILANLAR (D grubu, Gün 1 -
        // GetAllAsync'in içindeki sorgu artık ikisini birleştiriyor, bkz.
        // EfPasswordEntryRepository) - Wiki'nin departman bypass'ının Vault'taki
        // karşılığı (bkz. IPasswordEntryRepository'deki not).
        Guid? viewerFilter = _currentUser.IsAdmin ? null : _currentUser.UserId.Value;

        var entries = await _repository.GetAllAsync(viewerFilter, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            entries = entries
                .Where(e => string.Equals(e.Category, request.Category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return entries.Select(e => e.ToDto()).ToList();
    }
}
