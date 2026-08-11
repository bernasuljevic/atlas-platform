using FluentValidation;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Commands;

public class UpdatePasswordEntryCommandValidator : AbstractValidator<UpdatePasswordEntryCommand>
{
    public UpdatePasswordEntryCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.")
            .MaximumLength(200).WithMessage("Başlık 200 karakteri geçemez.");

        // Password BİLEREK NotEmpty DEĞİL - boş bırakmak "mevcut parolayı
        // koru" anlamına geliyor (bkz. Handler).
        RuleFor(x => x.Username).MaximumLength(200).WithMessage("Kullanıcı adı 200 karakteri geçemez.");
        RuleFor(x => x.Url).MaximumLength(500).WithMessage("URL 500 karakteri geçemez.");
        RuleFor(x => x.Category).MaximumLength(100).WithMessage("Kategori 100 karakteri geçemez.");
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Açıklama 1000 karakteri geçemez.");
        RuleFor(x => x.Notes).MaximumLength(2000).WithMessage("Notlar 2000 karakteri geçemez.");
    }
}
