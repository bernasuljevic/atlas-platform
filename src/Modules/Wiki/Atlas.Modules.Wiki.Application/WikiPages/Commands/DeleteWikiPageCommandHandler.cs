using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class DeleteWikiPageCommandHandler : IRequestHandler<DeleteWikiPageCommand>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWikiPageCommandHandler(
        IWikiPageRepository wikiPageRepository,
        ICurrentUserAccessor currentUser,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork)
    {
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
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

        // Outbox Pattern (Gün 2): CreateWikiPageCommandHandler'daki AYNI desen -
        // DeleteAsync artık kalıcı yazmıyor (stage), event doğrudan yayınlanmıyor,
        // AYNI SaveChanges'e bir OutboxMessage ekleniyor (atomiklik).
        await _wikiPageRepository.DeleteAsync(page, cancellationToken);

        _outboxWriter.Enqueue(new WikiPageDeletedEvent(page.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
