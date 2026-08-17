using Atlas.Modules.Vault.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Queries;

public class GetPasswordEntrySharesQueryHandler
    : IRequestHandler<GetPasswordEntrySharesQuery, IReadOnlyList<PasswordEntryShareDto>?>
{
    private readonly IPasswordEntryRepository _repository;
    private readonly IPasswordEntryShareRepository _shareRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public GetPasswordEntrySharesQueryHandler(
        IPasswordEntryRepository repository, IPasswordEntryShareRepository shareRepository,
        ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _shareRepository = shareRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PasswordEntryShareDto>?> Handle(
        GetPasswordEntrySharesQuery request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.PasswordEntryId, cancellationToken);
        if (entry is null)
            return null;

        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;
        var isOwner = _currentUser.IsAuthenticated && _currentUser.UserId == entry.CreatedByUserId;

        if (!viewerIsAdmin && !isOwner)
            return null;

        var shares = await _shareRepository.GetSharesForEntryAsync(entry.Id, cancellationToken);
        return shares.Select(s => new PasswordEntryShareDto(s.SharedWithUserId, s.SharedWithEmail, s.SharedAtUtc)).ToList();
    }
}
