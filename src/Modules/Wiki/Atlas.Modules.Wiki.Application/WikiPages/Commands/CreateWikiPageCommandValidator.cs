using Atlas.Modules.Wiki.Domain.Enums;
using FluentValidation;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class CreateWikiPageCommandValidator : AbstractValidator<CreateWikiPageCommand>
{
    public CreateWikiPageCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik boş olamaz.");

        RuleFor(x => x.DepartmentName)
            .NotEmpty().WithMessage("Departman adı boş olamaz.");

        // Domain'deki Enum.Parse çağrısı geçersiz bir değerde exception fırlatıp
        // 400 yerine yanlışlıkla 500'e düşebilirdi - burada erken, alan bazlı bir
        // hata mesajıyla kesiyoruz.
        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Görünürlük boş olamaz.")
            .Must(v => Enum.TryParse<WikiVisibility>(v, ignoreCase: true, out _))
            .WithMessage("Görünürlük 'Public' ya da 'DepartmentOnly' olmalı.");
    }
}
