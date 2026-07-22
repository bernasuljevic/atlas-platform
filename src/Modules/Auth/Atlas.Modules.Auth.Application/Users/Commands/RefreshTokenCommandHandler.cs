using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokensDto?>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthTokensDto?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (existingToken is null || !existingToken.IsActive(DateTime.UtcNow))
            return null;

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);

        if (user is null)
            return null;

        // Rotation: gelen refresh token'ı kullanılmış say (iptal et), yenisini üret.
        // Aynı token ikinci kez gelirse artık IsActive() false döner - reddedilir.
        existingToken.Revoke();

        var newAccessToken = _tokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenValue = _tokenGenerator.GenerateRefreshTokenValue();
        var newRefreshToken = RefreshToken.Create(newRefreshTokenValue, user.Id, DateTime.UtcNow.AddDays(7));

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(newAccessToken, newRefreshTokenValue);
    }
}
