using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class ResendVerificationCodeCommandHandler : IRequestHandler<ResendVerificationCodeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationCodeRepository _codeRepository;
    private readonly IEmailSender _emailSender;

    public ResendVerificationCodeCommandHandler(
        IUserRepository userRepository, IEmailVerificationCodeRepository codeRepository, IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _codeRepository = codeRepository;
        _emailSender = emailSender;
    }

    public async Task Handle(ResendVerificationCodeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // GÜVENLİK: Kullanıcı bulunamasa ya da zaten doğrulanmış olsa BİLE
        // sessizce (hatasız) dönüyoruz - aksi halde bu endpoint, hangi
        // e-postaların sistemde kayıtlı olduğunu (ya da zaten doğrulanmış
        // olduğunu) dışarıdan sınayabilen bir "email enumeration" aracına
        // dönüşürdü. İstemci her durumda aynı "kod gönderildiyse gelen
        // kutunu kontrol et" mesajını görür.
        if (user is null || user.EmailVerified)
            return;

        // "Eski kodların geçersiz olması" kuralı - yeni kod üretilmeden ÖNCE
        // önceki aktif kod(lar) geçersiz kılınıyor.
        await _codeRepository.InvalidateActiveCodesForUserAsync(user.Id, cancellationToken);

        var code = EmailVerificationCode.Create(user.Id);
        await _codeRepository.AddAsync(code, cancellationToken);
        await _codeRepository.SaveChangesAsync(cancellationToken);

        await _emailSender.SendAsync(
            user.Email,
            "Atlas Wiki - Yeni Doğrulama Kodu",
            $"Merhaba {user.FullName},\n\nYeni doğrulama kodun: {code.Code}\n" +
            "Bu kod 10 dakika içinde geçerliliğini yitirecek.",
            cancellationToken);
    }
}
