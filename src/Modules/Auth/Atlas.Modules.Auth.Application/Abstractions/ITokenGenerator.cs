using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Abstractions;

public interface ITokenGenerator
{
    string GenerateAccessToken(User user);

    // Refresh token'ın kendisi JWT değil - sadece rastgele, tahmin edilemez bir
    // string. Kimlik bilgisi taşımıyor, DB'de kime ait olduğu (UserId) ayrıca tutuluyor.
    string GenerateRefreshTokenValue();
}