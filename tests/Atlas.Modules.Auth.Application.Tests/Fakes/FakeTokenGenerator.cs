using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Tests.Fakes;

public class FakeTokenGenerator : ITokenGenerator
{
    public string GenerateAccessToken(User user) => $"fake-access-token-{user.Id}";

    public string GenerateRefreshTokenValue() => $"fake-refresh-token-{Guid.NewGuid()}";
}
