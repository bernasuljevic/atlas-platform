using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationCodeRepository _codeRepository;
    private readonly IEmailSender _emailSender;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailVerificationCodeRepository codeRepository,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _codeRepository = codeRepository;
        _emailSender = emailSender;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var placeholderHash = _passwordHasher.Hash(request.Password);

        // emailVerified BİLEREK belirtilmiyor - varsayılan false (bkz. User.cs'teki
        // not), LoginCommandHandler doğrulama koduyla doğrulanana kadar girişi
        // reddediyor.
        var user = User.Create(request.Email, request.FullName, placeholderHash, department: request.Department);

        // EfUserRepository.AddAsync kendi SaveChangesAsync'ini çağırıyor (Wiki'nin
        // Outbox deseninin aksine, Auth burada atomiklik gerektiren bir olay
        // yayınlamıyor) - bu yüzden user.Id burada zaten kalıcı.
        await _userRepository.AddAsync(user, cancellationToken);

        var code = EmailVerificationCode.Create(user.Id);
        await _codeRepository.AddAsync(code, cancellationToken);
        await _codeRepository.SaveChangesAsync(cancellationToken);

        // LoggingEmailSender gerçek bir SMTP sağlayıcısı bağlanana kadarki yer
        // tutucu - şimdilik kodu sadece logluyor (bkz. LoggingEmailSender'daki not).
        await _emailSender.SendAsync(
            user.Email,
            "Atlas Wiki - E-posta Doğrulama Kodu",
            $"Merhaba {user.FullName},\n\nAtlas Wiki hesabını doğrulamak için kodun: {code.Code}\n" +
            "Bu kod 10 dakika içinde geçerliliğini yitirecek.",
            cancellationToken);

        return user.Id;
    }
}
