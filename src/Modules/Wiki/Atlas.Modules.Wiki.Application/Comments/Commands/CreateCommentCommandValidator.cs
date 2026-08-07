using FluentValidation;

namespace Atlas.Modules.Wiki.Application.Comments.Commands;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Yorum boş olamaz.")
            .MaximumLength(2000).WithMessage("Yorum en fazla 2000 karakter olabilir.");
    }
}
