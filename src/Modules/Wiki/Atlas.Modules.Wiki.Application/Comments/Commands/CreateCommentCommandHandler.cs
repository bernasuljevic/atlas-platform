using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Comments.Commands;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Guid>
{
    private readonly IWikiCommentRepository _commentRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCommentCommandHandler(
        IWikiCommentRepository commentRepository,
        ISender sender,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _sender = sender;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Yorum yapmak için giriş yapmış olmalısınız.");

        if (request.PageId is not null)
        {
            await EnsurePageIsVisibleAsync(request.PageId.Value, cancellationToken);
        }

        var comment = Comment.Create(request.PageId, request.Content, _currentUser.UserId.Value, _currentUser.Email);

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }

    // GÜVENLİK: bir sayfaya yorum bırakabilmek, o sayfayı GÖREBİLMEYİ
    // gerektiriyor - aksi halde bir kullanıcı hiç erişemediği ("Sadece
    // Departman") bir sayfanın ID'sini tahmin ederek oraya yorum bırakabilir,
    // bu da o sayfanın VAR OLDUĞUNU (ve başlığını, GetAllWikiPagesRawQuery zaten
    // cache'lenmiş olduğu için ek bir sorgu maliyeti de olmadan) sızdırırdı -
    // GetWikiPageByIdQueryHandler'daki AYNI görünürlük kontrolü burada da uygulanıyor.
    private async Task EnsurePageIsVisibleAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var allPages = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);
        var page = allPages.FirstOrDefault(p => p.Id == pageId);

        var isVisible = page is not null && WikiVisibilityRules.IsVisibleTo(
            Enum.Parse<WikiVisibility>(page.Visibility), page.DepartmentName, _currentUser.Department, _currentUser.IsAdmin);

        if (!isVisible)
            throw new ArgumentException("Sayfa bulunamadı.", nameof(pageId));
    }
}
