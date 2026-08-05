using Atlas.Modules.Auth.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationCodeRepository _codeRepository;

    public VerifyEmailCommandHandler(IUserRepository userRepository, IEmailVerificationCodeRepository codeRepository)
    {
        _userRepository = userRepository;
        _codeRepository = codeRepository;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
            throw new ArgumentException("Kullanıcı bulunamadı.", nameof(request.Email));

        if (user.EmailVerified)
            throw new ArgumentException("Bu hesap zaten doğrulanmış.", nameof(request.Email));

        var activeCode = await _codeRepository.GetLatestActiveForUserAsync(user.Id, cancellationToken);

        // Kod bulunamadıysa, süresi dolduysa, zaten kullanıldıysa ya da yanlışsa
        // hepsi AYNI genel mesaja düşüyor - "kod hangi sebeple geçersiz"
        // bilgisini istemciye sızdırmak (ör. "böyle bir kod hiç yok" vs "süresi
        // dolmuş") saldırgana gereksiz bir ipucu verirdi.
        if (activeCode is null || !activeCode.IsValid(request.Code))
            throw new ArgumentException("Kod geçersiz ya da süresi dolmuş.", nameof(request.Code));

        activeCode.MarkUsed();
        user.MarkEmailVerified();

        // İkisi de AYNI DbContext'i (AuthDbContext, Scoped) paylaştığı için tek
        // bir SaveChanges her iki değişikliği de yazardı, ama her repository
        // KENDİ mutasyonunu KENDİSİ kaydetsin diye açıkça ikisini de çağırıyoruz -
        // repository'ler arasında paylaşılan context varsayımına örtük şekilde
        // güvenmemek için.
        await _codeRepository.SaveChangesAsync(cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
