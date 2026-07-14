using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public record LoginCommand(string Email, string Password) : IRequest<string?>;