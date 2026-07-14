using Atlas.Modules.Auth.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Queries;

/// <summary>
/// MediatR, "IRequestHandler&lt;GetAllUsersQuery, IReadOnlyList&lt;UserDto&gt;&gt;" imzasını görünce
/// bu sınıfı otomatik bulur ve eşler - biz hiçbir yerde elle "hangi query hangi handler'a
/// gider" diye kayıt yapmıyoruz, MediatR bunu tip eşleşmesiyle kendisi çözüyor.
/// </summary>
public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(u => new UserDto(u.Id, u.Email, u.FullName)).ToList();
    }
}
