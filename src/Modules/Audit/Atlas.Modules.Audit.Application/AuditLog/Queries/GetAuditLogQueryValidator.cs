using FluentValidation;

namespace Atlas.Modules.Audit.Application.AuditLog.Queries;

public class GetAuditLogQueryValidator : AbstractValidator<GetAuditLogQuery>
{
    public GetAuditLogQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize 1 ile 100 arasında olmalı.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber 1'den küçük olamaz.");

        When(x => x.FromUtc is not null && x.ToUtc is not null, () =>
        {
            RuleFor(x => x)
                .Must(x => x.FromUtc <= x.ToUtc)
                .WithMessage("FromUtc, ToUtc'den sonra olamaz.");
        });
    }
}
