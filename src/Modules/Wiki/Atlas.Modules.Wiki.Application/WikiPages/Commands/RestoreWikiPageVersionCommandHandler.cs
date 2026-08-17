using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class RestoreWikiPageVersionCommandHandler : IRequestHandler<RestoreWikiPageVersionCommand>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly IWikiPageVersionRepository _wikiPageVersionRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreWikiPageVersionCommandHandler(
        IWikiPageRepository wikiPageRepository, IWikiPageVersionRepository wikiPageVersionRepository,
        ICurrentUserAccessor currentUser, IUnitOfWork unitOfWork)
    {
        _wikiPageRepository = wikiPageRepository;
        _wikiPageVersionRepository = wikiPageVersionRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RestoreWikiPageVersionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Bir versiyona geri dönmek için giriş yapmış olmalısınız.");

        var page = await _wikiPageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            throw new ArgumentException("Sayfa bulunamadı.", nameof(request.PageId));

        // UpdateWikiPageCommandHandler'daki AYNI owner-or-Admin kuralı - geri
        // dönmek de bir düzenleme eylemi.
        var isOwner = page.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu sayfayı geri döndürme yetkiniz yok.");

        var targetVersion = await _wikiPageVersionRepository.GetByWikiPageIdAndVersionNumberAsync(
            page.Id, request.VersionNumber, cancellationToken);
        if (targetVersion is null)
            throw new ArgumentException("Belirtilen versiyon bulunamadı.", nameof(request.VersionNumber));

        // UpdateWikiPageCommandHandler'daki AYNI "önce snapshot'la, SONRA
        // üzerine yaz" sırası - geri dönülmeden HEMEN ÖNCEki hâl de kendi
        // versiyonu olarak arşive düşüyor, hiçbir şey sessizce kaybolmuyor.
        var preRestoreSnapshot = WikiPageVersion.CreateSnapshot(
            page.Id, page.CurrentVersionNumber, page.Title, page.Content, page.Visibility.ToString(), page.Tags,
            _currentUser.UserId.Value, _currentUser.Email);
        await _wikiPageVersionRepository.AddAsync(preRestoreSnapshot, cancellationToken);

        var visibility = Enum.Parse<WikiVisibility>(targetVersion.Visibility, ignoreCase: true);
        page.Update(targetVersion.Title, targetVersion.Content, visibility, page.FolderId, targetVersion.Tags);

        request.AuditDetails = $"{page.Title} (v{targetVersion.VersionNumber} sürümüne geri döndürüldü)";

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
