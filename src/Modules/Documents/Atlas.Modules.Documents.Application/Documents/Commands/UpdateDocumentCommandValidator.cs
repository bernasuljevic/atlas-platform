using Atlas.Modules.Documents.Domain.Enums;
using FluentValidation;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class UpdateDocumentCommandValidator : AbstractValidator<UpdateDocumentCommand>
{
    public UpdateDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Başlık boş olamaz.").MaximumLength(200);
        RuleFor(x => x.Visibility)
            .Must(v => Enum.TryParse<DocumentVisibility>(v, out _))
            .WithMessage("Görünürlük 'Public' ya da 'DepartmentOnly' olmalı.");
    }
}
