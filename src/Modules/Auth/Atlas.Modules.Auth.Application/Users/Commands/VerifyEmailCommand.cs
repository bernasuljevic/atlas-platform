using MediatR;

namespace Atlas.Modules.Auth.Application.Users.Commands;

public record VerifyEmailCommand(string Email, string Code) : IRequest;
