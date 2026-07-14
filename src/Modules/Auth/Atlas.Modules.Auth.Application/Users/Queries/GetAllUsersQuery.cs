using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Queries;

/// <summary>
/// DİKKAT: Bu sınıfın içinde HİÇ mantık yok. Sadece "ben tüm kullanıcıları istiyorum,
/// cevap olarak IReadOnlyList&lt;UserDto&gt; bekliyorum" diyen bir zarf (envelope).
/// "Nasıl" sorusu tamamen GetAllUsersQueryHandler'ın işi.
/// </summary>
public record GetAllUsersQuery : IRequest<IReadOnlyList<UserDto>>;
