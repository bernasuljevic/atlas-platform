using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiFolders.Commands;

public class CreateWikiFolderCommandHandler : IRequestHandler<CreateWikiFolderCommand, Guid>
{
    private readonly IWikiFolderRepository _wikiFolderRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWikiFolderCommandHandler(
        IWikiFolderRepository wikiFolderRepository,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _wikiFolderRepository = wikiFolderRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateWikiFolderCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Klasör oluşturmak için giriş yapmış olmalısınız.");

        // GÜVENLİK: CreateWikiPageCommandHandler'daki AYNI kural - normal bir
        // kullanıcı SADECE kendi departmanında klasör açabilir, Admin istediği
        // departmanı seçebilir.
        var departmentName = _currentUser.IsAdmin ? request.DepartmentName : _currentUser.Department;

        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException(
                "Klasör oluşturmak için bir departmana ait olmalısınız.", nameof(departmentName));

        Guid? parentFolderId = null;
        if (request.ParentFolderId is not null)
        {
            var parentFolder = await _wikiFolderRepository.GetByIdAsync(request.ParentFolderId.Value, cancellationToken);

            if (parentFolder is null)
                throw new ArgumentException("Üst klasör bulunamadı.", nameof(request.ParentFolderId));

            // Bir departmanın klasör ağacı başka bir departmanın klasörünün
            // altına dallanamaz - aksi halde ör. IT, HR'ın kök klasörünün
            // altına kendi klasörünü ekleyip HR'ın ağacını "ele geçirebilirdi".
            if (!string.Equals(parentFolder.DepartmentName, departmentName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Üst klasör başka bir departmana ait.", nameof(request.ParentFolderId));

            parentFolderId = parentFolder.Id;
        }

        var folder = WikiFolder.Create(request.Name, departmentName, parentFolderId, _currentUser.UserId.Value);

        // AuditBehavior, Handler bitince (next() sonrası) bunu okuyacak -
        // bkz. IAuditableCommand.AuditDetails'teki not.
        request.AuditDetails = folder.Name;

        await _wikiFolderRepository.AddAsync(folder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return folder.Id;
    }
}
