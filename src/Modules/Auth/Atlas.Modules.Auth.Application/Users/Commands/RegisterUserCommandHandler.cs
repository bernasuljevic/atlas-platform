using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var placeholderHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(request.Email, request.FullName, placeholderHash, department: request.Department);

        await _userRepository.AddAsync(user, cancellationToken);

        return user.Id;
    }
}
