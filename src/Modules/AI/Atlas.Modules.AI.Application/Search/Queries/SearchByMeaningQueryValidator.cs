using FluentValidation;

namespace Atlas.Modules.AI.Application.Search.Queries;

// SearchWikiPagesByMeaningQueryValidator'ın BİREBİR kopyası, yeni isimle.
public class SearchByMeaningQueryValidator : AbstractValidator<SearchByMeaningQuery>
{
    public SearchByMeaningQueryValidator()
    {
        RuleFor(x => x.QueryText)
            .NotEmpty().WithMessage("Arama sorgusu boş olamaz.")
            .MaximumLength(500).WithMessage("Arama sorgusu 500 karakterden uzun olamaz.");

        RuleFor(x => x.TopN)
            .InclusiveBetween(1, 50).WithMessage("TopN 1 ile 50 arasında olmalı.");

        When(x => x.FromUtc is not null && x.ToUtc is not null, () =>
        {
            RuleFor(x => x)
                .Must(x => x.FromUtc <= x.ToUtc)
                .WithMessage("FromUtc, ToUtc'den sonra olamaz.");
        });
    }
}
