using Atlas.Modules.Auth.Domain.Entities;

namespace Atlas.Modules.Auth.Application.Abstractions;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}