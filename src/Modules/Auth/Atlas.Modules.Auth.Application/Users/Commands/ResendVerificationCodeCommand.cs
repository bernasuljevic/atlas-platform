using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public record ResendVerificationCodeCommand(string Email) : IRequest;
