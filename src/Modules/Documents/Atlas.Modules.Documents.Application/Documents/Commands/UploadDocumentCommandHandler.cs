using System.Security.Cryptography;
using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Guid>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserAccessor _currentUser;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepository, IFileStorageService fileStorageService, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Belge yüklemek için giriş yapmış olmalısınız.");

        // CreateWikiPageCommandHandler'daki AYNI kural: normal kullanıcı SADECE
        // kendi departmanına yükleyebilir, Admin istediği departmanı seçebilir.
        var departmentName = _currentUser.IsAdmin ? request.DepartmentName : _currentUser.Department;
        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException("Belge yüklemek için bir departmana ait olmalısınız.", nameof(departmentName));

        var visibility = Enum.Parse<DocumentVisibility>(request.Visibility);

        // İçerik TEK SEFER belleğe alınıyor - hem hash hesaplaması hem disk
        // yazımı AYNI byte dizisinden yapılıyor, IFormFile'ın alttaki stream'inin
        // birden fazla kez okunabilir/seekable olduğuna güvenmek yerine (bazı
        // implementasyonlarda tek yönlü). MaxFileSizeBytes zaten Validator'da
        // kontrol edildiği için burada makul boyutta bir dosya bekleniyor.
        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var contentHash = Convert.ToHexString(SHA256.HashData(bytes));

        var fileExtension = Path.GetExtension(request.OriginalFileName).TrimStart('.');

        using var saveStream = new MemoryStream(bytes);
        var storageKey = await _fileStorageService.SaveAsync(saveStream, fileExtension, cancellationToken);

        var document = Document.Create(
            request.Title, request.OriginalFileName, storageKey, request.ContentType, fileExtension,
            request.SizeBytes, departmentName, visibility, _currentUser.UserId.Value, _currentUser.Email,
            request.Description, request.Tags, contentHash);

        request.AuditDetails = document.Title;

        await _documentRepository.AddAsync(document, cancellationToken);

        return document.Id;
    }
}
