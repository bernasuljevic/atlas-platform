using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokensDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthTokensDto?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        var accessToken = _tokenGenerator.GenerateAccessToken(user);

        // Refresh token 7 gün geçerli - access token'dan (15dk) çok daha uzun ömürlü,
        // kullanıcı sık sık yeniden giriş yapmak zorunda kalmasın diye.
        var refreshTokenValue = _tokenGenerator.GenerateRefreshTokenValue();
        var refreshToken = RefreshToken.Create(refreshTokenValue, user.Id, DateTime.UtcNow.AddDays(7));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(accessToken, refreshTokenValue);
    }
}