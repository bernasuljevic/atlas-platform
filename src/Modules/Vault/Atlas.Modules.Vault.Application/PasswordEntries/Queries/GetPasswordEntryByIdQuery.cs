using MediatR;

namespace Atlas.Modules.Vault.Application.PasswordEntries.Queries;

public record GetPasswordEntryByIdQuery(Guid Id) : IRequest<PasswordEntryDto?>;
