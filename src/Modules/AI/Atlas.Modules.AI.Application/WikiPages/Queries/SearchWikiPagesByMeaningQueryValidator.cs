using FluentValidation;

namespace Atlas.Modules.AI.Application.WikiPages.Queries;

public class SearchWikiPagesByMeaningQueryValidator : AbstractValidator<SearchWikiPagesByMeaningQuery>
{
    public SearchWikiPagesByMeaningQueryValidator()
    {
        RuleFor(x => x.QueryText)
            .NotEmpty().WithMessage("Arama sorgusu boş olamaz.")
            // Çok uzun bir sorgu (örn. yanlışlıkla yapıştırılmış tüm bir wiki
            // sayfası) embedding servisine gereksiz yere büyük bir istek olarak
            // gider - makul bir üst sınırla erken kesiyoruz.
            .MaximumLength(500).WithMessage("Arama sorgusu 500 karakterden uzun olamaz.");

        RuleFor(x => x.TopN)
            .InclusiveBetween(1, 50).WithMessage("TopN 1 ile 50 arasında olmalı.");
    }
}
