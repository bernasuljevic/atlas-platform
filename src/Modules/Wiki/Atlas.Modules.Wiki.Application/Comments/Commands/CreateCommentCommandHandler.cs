using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Comments.Commands;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Guid>
{
    // Platform-geneli (PageId null) bir yorum için gerçek bir DepartmentName
    // yok - WikiCommentCreatedEvent'in Visibility="Public" ile dolacağı için
    // (bkz. o dosyadaki not) bu değer görünürlük kontrolünde hiç kullanılmıyor,
    // sadece NotificationEntry'nin NOT NULL şemasını doldurmak için bir yer tutucu.
    private const string GeneralDiscussionDepartmentPlaceholder = "Genel";

    private readonly IWikiCommentRepository _commentRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCommentCommandHandler(
        IWikiCommentRepository commentRepository,
        ISender sender,
        ICurrentUserAccessor currentUser,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _sender = sender;
        _currentUser = currentUser;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Yorum yapmak için giriş yapmış olmalısınız.");

        WikiPageDto? page = null;
        if (request.PageId is not null)
        {
            page = await GetVisiblePageAsync(request.PageId.Value, cancellationToken);
        }

        // Bildirim alıcıları (2026-08-17, "tartışmaya cevap geldiğinde bildirim
        // oluşsun" isteği) - YENİ yorum eklenmeden ÖNCE hesaplanıyor: bu sayfaya
        // (ya da platform-geneli tartışmaya) daha önce yorum yapmış HERKES +
        // (sayfa bazlıysa) sayfanın SAHİBİ, kendi yazdığı yorum HARİÇ. Thread'siz
        // (düz) yorum modelinde "cevap" kavramının en doğal karşılığı bu -
        // Comment.cs'teki "iç içe yanıt YOK" kararıyla TUTARLI, ayrı bir
        // ParentCommentId icat edilmedi.
        var priorComments = await _commentRepository.GetByPageIdAsync(request.PageId, cancellationToken);
        var recipientIds = priorComments.Select(c => c.AuthorUserId).ToHashSet();
        if (page is not null)
        {
            recipientIds.Add(page.CreatedByUserId);
        }
        recipientIds.Remove(_currentUser.UserId.Value);

        var comment = Comment.Create(request.PageId, request.Content, _currentUser.UserId.Value, _currentUser.Email);

        await _commentRepository.AddAsync(comment, cancellationToken);

        if (recipientIds.Count > 0)
        {
            _outboxWriter.Enqueue(new WikiCommentCreatedEvent(
                comment.Id,
                request.PageId,
                page?.Title,
                comment.Content,
                _currentUser.UserId.Value,
                _currentUser.Email,
                page?.DepartmentName ?? GeneralDiscussionDepartmentPlaceholder,
                page?.Visibility ?? WikiVisibility.Public.ToString(),
                recipientIds.ToList(),
                comment.CreatedAtUtc));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }

    // GÜVENLİK: bir sayfaya yorum bırakabilmek, o sayfayı GÖREBİLMEYİ
    // gerektiriyor - aksi halde bir kullanıcı hiç erişemediği ("Sadece
    // Departman") bir sayfanın ID'sini tahmin ederek oraya yorum bırakabilir,
    // bu da o sayfanın VAR OLDUĞUNU (ve başlığını, GetAllWikiPagesRawQuery zaten
    // cache'lenmiş olduğu için ek bir sorgu maliyeti de olmadan) sızdırırdı -
    // GetWikiPageByIdQueryHandler'daki AYNI görünürlük kontrolü burada da uygulanıyor.
    //
    // Artık bulduğu sayfayı (Title/DepartmentName/Visibility/CreatedByUserId
    // dahil) DÖNDÜRÜYOR - WikiCommentCreatedEvent'in ihtiyaç duyduğu veri
    // burada zaten elde ediliyor, ikinci bir sorgu GEREKMEDİ.
    private async Task<WikiPageDto> GetVisiblePageAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var allPages = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);
        var page = allPages.FirstOrDefault(p => p.Id == pageId);

        var isVisible = page is not null && WikiVisibilityRules.IsVisibleTo(
            Enum.Parse<WikiVisibility>(page.Visibility), page.DepartmentName, _currentUser.Department, _currentUser.IsAdmin);

        if (!isVisible)
            throw new ArgumentException("Sayfa bulunamadı.", nameof(pageId));

        return page!;
    }
}
