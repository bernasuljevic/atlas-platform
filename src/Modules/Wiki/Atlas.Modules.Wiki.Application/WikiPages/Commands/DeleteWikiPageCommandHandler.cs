using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class DeleteWikiPageCommandHandler : IRequestHandler<DeleteWikiPageCommand>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPublisher _publisher;

    public DeleteWikiPageCommandHandler(
        IWikiPageRepository wikiPageRepository, ICurrentUserAccessor currentUser, IPublisher publisher)
    {
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
        _publisher = publisher;
    }

    public async Task Handle(DeleteWikiPageCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Sayfa silmek için giriş yapmış olmalısınız.");

        var page = await _wikiPageRepository.GetByIdAsync(request.PageId, cancellationToken);

        if (page is null)
            throw new ArgumentException("Sayfa bulunamadı.", nameof(request.PageId));

        // Yetki kuralı: Admin HER sayfayı silebilir, normal bir kullanıcı
        // SADECE kendi oluşturduğu sayfayı silebilir - başkasının sayfasını
        // silme denemesi 403 (Forbidden) ile reddediliyor.
        var isOwner = page.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu sayfayı silme yetkiniz yok.");

        await _wikiPageRepository.DeleteAsync(page, cancellationToken);

        // WikiPageCreatedEvent'teki AYNI desen - Wiki, AI'ın var olduğundan
        // habersiz, sadece "bu sayfa silindi" diye duyuruyor. Bunsuz, AI'ın
        // Postgres'teki embedding'leri sonsuza kadar yetim kalırdı (bkz. bu
        // event'in kendi XML yorumu).
        await _publisher.Publish(new WikiPageDeletedEvent(page.Id), cancellationToken);
    }
}
