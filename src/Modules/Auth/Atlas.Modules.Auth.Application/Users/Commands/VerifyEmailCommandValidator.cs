using FluentValidation;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir email adresi girin.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kod boş olamaz.")
            .Length(6).WithMessage("Kod 6 haneli olmalı.")
            .Must(c => c.All(char.IsDigit)).WithMessage("Kod sadece rakamlardan oluşmalı.");
    }
}
