using System.Security.Cryptography;
using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Modules.Documents.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, UploadDocumentResult>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserAccessor _currentUser;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepository, IFileStorageService fileStorageService,
        IWikiVisibilityChecker visibilityChecker, IOutboxWriter outboxWriter, IUnitOfWork unitOfWork,
        ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _visibilityChecker = visibilityChecker;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UploadDocumentResult> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
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

        // P6 Gün 3 (duplicate-detection) - YÜKLEMEYİ ENGELLEMİYOR, sadece
        // istemciye "aynı içerikli bir belge zaten var" uyarısı taşıyor.
        // Görünürlük filtresi BURADA da uygulanıyor - başka departmanın
        // DepartmentOnly bir belgesiyle eşleştiğini SÖYLEMEK bile o belgenin
        // VARLIĞINI sızdırırdı (aynı sınıf hata: CLAUDE.md "Öğrenilen
        // dersler #10"). Birden fazla görünür eşleşme varsa EN YENİSİ
        // gösteriliyor - kullanıcı için en alakalı referans muhtemelen budur.
        var candidatesWithSameHash = await _documentRepository.GetAllByContentHashAsync(contentHash, cancellationToken);
        var visibleDuplicate = candidatesWithSameHash
            .Where(d => _visibilityChecker.IsVisibleTo(
                d.Visibility.ToString(), d.DepartmentName, _currentUser.Department, _currentUser.IsAdmin))
            .OrderByDescending(d => d.CreatedAtUtc)
            .FirstOrDefault();

        var fileExtension = Path.GetExtension(request.OriginalFileName).TrimStart('.');

        using var saveStream = new MemoryStream(bytes);
        var storageKey = await _fileStorageService.SaveAsync(saveStream, fileExtension, cancellationToken);

        var document = Document.Create(
            request.Title, request.OriginalFileName, storageKey, request.ContentType, fileExtension,
            request.SizeBytes, departmentName, visibility, _currentUser.UserId.Value, _currentUser.Email,
            request.Description, request.Tags, contentHash);

        request.AuditResourceId = document.Id.ToString();
        request.AuditDetails = document.Title;

        await _documentRepository.AddAsync(document, cancellationToken);

        // Belge yazmasıyla AYNI transaction'da (atomik) - Document satırı
        // yazılmadan bu event asla kalıcı olmaz, event yazılmadan Document da
        // (aynı SaveChanges'in içinde olduğu için) kalıcı olmaz. Wiki'nin
        // CreateWikiPageCommandHandler'ındaki AYNI desen.
        _outboxWriter.Enqueue(new DocumentUploadedEvent(
            document.Id, document.StorageKey, document.ContentType, document.FileExtension,
            document.Title, document.DepartmentName, document.Visibility.ToString()));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadDocumentResult(document.Id, visibleDuplicate?.Id, visibleDuplicate?.Title);
    }
}
