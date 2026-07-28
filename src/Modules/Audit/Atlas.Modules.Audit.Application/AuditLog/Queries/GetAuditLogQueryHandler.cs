using Atlas.Modules.Audit.Application.Abstractions;
using MediatR;

namespace Atlas.Modules.Audit.Application.AuditLog.Queries;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    private readonly IAuditLogRepository _repository;

    public GetAuditLogQueryHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return _repository.GetPagedAsync(
            request.Action, request.FromUtc, request.ToUtc, pageNumber, pageSize, cancellationToken);
    }
}
