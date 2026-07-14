using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class CreateWikiPageCommandHandler : IRequestHandler<CreateWikiPageCommand, Guid>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;

    // ICurrentUserAccessor, Wiki.Application'ın Shared.Contracts'tan tanıdığı bir
    // interface. Gerçek implementasyonu KİM sağlıyor? Wiki'nin haberi yok, umurunda değil.
    // (Bugün Auth.Infrastructure sağlayacak - ama bu handler bunu asla bilmeyecek.)
    public CreateWikiPageCommandHandler(IWikiPageRepository wikiPageRepository, ICurrentUserAccessor currentUser)
    {
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateWikiPageCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Wiki sayfası oluşturmak için giriş yapmış olmalısınız.");

        var visibility = Enum.Parse<WikiVisibility>(request.Visibility, ignoreCase: true);

        var page = WikiPage.Create(
            request.Title, request.Content, request.DepartmentName,
            visibility, _currentUser.UserId.Value);

        await _wikiPageRepository.AddAsync(page, cancellationToken);

        return page.Id;
    }
}
