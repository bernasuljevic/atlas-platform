using Atlas.Modules.Auth.Application.Abstractions;
using FluentValidation;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    // Önceden bu kontrol hiç yoktu - Handler doğrudan DB'ye yazmayı deniyordu,
    // Email üzerindeki unique index'e çarpıp ham bir DbUpdateException
    // fırlatıyordu. GlobalExceptionHandler bunu ArgumentException saymadığı
    // için 500'e (Detail gizli) düşüyordu - kullanıcı "email zaten kayıtlı"
    // yerine anlamsız bir "beklenmeyen hata" görüyordu. Kontrolü buraya
    // (async FluentValidation kuralı) taşımak, var olan alan-bazlı hata
    // formatını (errors: {Email: [...]})  React tarafının zaten okuduğu
    // şekilde kullanmamızı sağlıyor.
    public RegisterUserCommandValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir email adresi girilmeli.")
            .MustAsync(async (email, cancellationToken) =>
                await userRepository.GetByEmailAsync(email, cancellationToken) is null)
            .WithMessage("Bu email adresi zaten kayıtlı.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad boş olamaz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.");
    }
}
