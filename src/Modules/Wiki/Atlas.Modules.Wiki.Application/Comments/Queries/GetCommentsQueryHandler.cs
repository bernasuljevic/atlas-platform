using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Comments.Queries;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDto>>
{
    private readonly IWikiCommentRepository _commentRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;

    public GetCommentsQueryHandler(IWikiCommentRepository commentRepository, ISender sender, ICurrentUserAccessor currentUser)
    {
        _commentRepository = commentRepository;
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageId is not null && !await IsPageVisibleAsync(request.PageId.Value, cancellationToken))
        {
            // GetWikiPageByIdQueryHandler'daki AYNI kural - Id'yi bilmek
            // görebilmek anlamına gelmiyor, göremediğin bir sayfanın
            // yorumlarını da göremezsin. NotFound yerine boş liste - sayfanın
            // VAR OLUP OLMADIĞINI da sızdırmıyor (CreateCommentCommandHandler'ın
            // aksine burada bir hata fırlatmıyoruz, sadece görünür bir şey yok).
            return Array.Empty<CommentDto>();
        }

        var comments = await _commentRepository.GetByPageIdAsync(request.PageId, cancellationToken);

        return comments
            .Select(c => new CommentDto(c.Id, c.Content, c.AuthorEmail, c.AuthorUserId, c.CreatedAtUtc))
            .ToList();
    }

    private async Task<bool> IsPageVisibleAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var allPages = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);
        var page = allPages.FirstOrDefault(p => p.Id == pageId);

        var viewerDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        return page is not null && WikiVisibilityRules.IsVisibleTo(
            Enum.Parse<WikiVisibility>(page.Visibility), page.DepartmentName, viewerDepartment, viewerIsAdmin);
    }
}
