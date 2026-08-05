using Atlas.Modules.Wiki.Domain.Enums;
using FluentValidation;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class UpdateWikiPageCommandValidator : AbstractValidator<UpdateWikiPageCommand>
{
    public UpdateWikiPageCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik boş olamaz.");

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Görünürlük boş olamaz.")
            .Must(v => Enum.TryParse<WikiVisibility>(v, ignoreCase: true, out _))
            .WithMessage("Görünürlük 'Public' ya da 'DepartmentOnly' olmalı.");

        RuleFor(x => x.Tags)
            .MaximumLength(300).WithMessage("Etiketler 300 karakteri geçemez.");
    }
}
