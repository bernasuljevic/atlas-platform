using FluentValidation;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir email adresi girilmeli.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad boş olamaz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.");
    }
}
