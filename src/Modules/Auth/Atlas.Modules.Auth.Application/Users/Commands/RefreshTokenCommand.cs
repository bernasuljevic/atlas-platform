using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokensDto?>;
