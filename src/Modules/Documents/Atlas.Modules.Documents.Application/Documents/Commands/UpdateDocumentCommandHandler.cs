using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateDocumentCommandHandler(IDocumentRepository documentRepository, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Belge güncellemek için giriş yapmış olmalısınız.");

        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (document is null)
            throw new ArgumentException("Belge bulunamadı.", nameof(request.Id));

        // Vault'un Update/Delete'indeki AYNI owner-or-admin deseni.
        var isOwner = document.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu belgeyi düzenleme yetkiniz yok.");

        var visibility = Enum.Parse<DocumentVisibility>(request.Visibility);
        document.UpdateMetadata(request.Title, request.Description, visibility, request.Tags);

        request.AuditDetails = document.Title;

        await _documentRepository.UpdateAsync(document, cancellationToken);
    }
}
