using FluentValidation;

namespace Atlas.Modules.Wiki.Application.WikiFolders.Commands;

public class CreateWikiFolderCommandValidator : AbstractValidator<CreateWikiFolderCommand>
{
    public CreateWikiFolderCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Klasör adı boş olamaz.")
            .MaximumLength(100).WithMessage("Klasör adı en fazla 100 karakter olabilir.");

        // DepartmentName için CreateWikiPageCommandValidator'daki AYNI gerekçeyle
        // kural YOK - normal bir kullanıcı için bu alan zaten Handler tarafından
        // tamamen yok sayılıyor (departman her zaman JWT'den geliyor).
    }
}
