using FluentValidation;

namespace Atlas.Modules.Wiki.Application.WikiFolders.Queries;

public class GetWikiFolderTreeQueryValidator : AbstractValidator<GetWikiFolderTreeQuery>
{
    public GetWikiFolderTreeQueryValidator()
    {
        RuleFor(x => x.DepartmentName)
            .NotEmpty().WithMessage("Departman adı boş olamaz.");
    }
}
