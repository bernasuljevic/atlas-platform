using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;

    public RegisterUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // NOT: Gerçek bir şifre hash'leme (BCrypt, Argon2 vs.) henüz eklemedik -
        // bu bilerek bırakılmış bir eksik, ileride JWT/login konusuna geçince ele alacağız.
        // Şimdilik amaç sadece Command → Handler → Domain → Infrastructure akışını görmek.
        var placeholderHash = $"hashed-{request.Password}";

        var user = User.Create(request.Email, request.FullName, placeholderHash);

        await _userRepository.AddAsync(user, cancellationToken);

        return user.Id;
    }
}
