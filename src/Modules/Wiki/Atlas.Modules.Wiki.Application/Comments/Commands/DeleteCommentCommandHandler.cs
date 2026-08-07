using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Comments.Commands;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly IWikiCommentRepository _commentRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommentCommandHandler(
        IWikiCommentRepository commentRepository,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Yorum silmek için giriş yapmış olmalısınız.");

        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);

        if (comment is null)
            throw new ArgumentException("Yorum bulunamadı.", nameof(request.CommentId));

        // DeleteWikiPageCommandHandler'daki AYNI yetki kuralı: Admin HER
        // yorumu, normal bir kullanıcı SADECE kendi yorumunu silebilir.
        var isOwner = comment.AuthorUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu yorumu silme yetkiniz yok.");

        await _commentRepository.DeleteAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
