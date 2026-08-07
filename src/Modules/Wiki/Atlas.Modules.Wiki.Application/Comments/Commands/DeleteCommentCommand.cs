using MediatR;

namespace Atlas.Modules.Wiki.Application.Comments.Commands;

public record DeleteCommentCommand(Guid CommentId) : IRequest;
