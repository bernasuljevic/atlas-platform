using MediatR;

namespace Atlas.Modules.Wiki.Application.Comments.Queries;

public record GetCommentsQuery(Guid? PageId) : IRequest<IReadOnlyList<CommentDto>>;

public record CommentDto(Guid Id, string Content, string? AuthorEmail, Guid AuthorUserId, DateTime CreatedAtUtc);
