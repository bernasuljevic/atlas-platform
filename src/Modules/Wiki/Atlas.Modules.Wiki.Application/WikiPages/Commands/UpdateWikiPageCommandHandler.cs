using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class UpdateWikiPageCommandHandler : IRequestHandler<UpdateWikiPageCommand>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly IWikiPageVersionRepository _wikiPageVersionRepository;
    private readonly IWikiFolderRepository _wikiFolderRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWikiPageCommandHandler(
        IWikiPageRepository wikiPageRepository,
        IWikiPageVersionRepository wikiPageVersionRepository,
        IWikiFolderRepository wikiFolderRepository,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _wikiPageRepository = wikiPageRepository;
        _wikiPageVersionRepository = wikiPageVersionRepository;
        _wikiFolderRepository = wikiFolderRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateWikiPageCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Sayfa düzenlemek için giriş yapmış olmalısınız.");

        var page = await _wikiPageRepository.GetByIdAsync(request.PageId, cancellationToken);

        if (page is null)
            throw new ArgumentException("Sayfa bulunamadı.", nameof(request.PageId));

        // DeleteWikiPageCommandHandler'daki AYNI yetki kuralı: Admin HER sayfayı,
        // normal bir kullanıcı SADECE kendi oluşturduğu sayfayı düzenleyebilir.
        var isOwner = page.CreatedByUserId == _currentUser.UserId.Value;
        if (!_currentUser.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Bu sayfayı düzenleme yetkiniz yok.");

        var visibility = Enum.Parse<WikiVisibility>(request.Visibility, ignoreCase: true);

        // CreateWikiPageCommandHandler'daki AYNI kural - sayfa sadece KENDİ
        // departmanının klasörüne dosyalanabilir (departman düzenlemede
        // değişmiyor, bkz. UpdateWikiPageCommand'daki not).
        Guid? folderId = null;
        if (request.FolderId is not null)
        {
            var folder = await _wikiFolderRepository.GetByIdAsync(request.FolderId.Value, cancellationToken);

            if (folder is null)
                throw new ArgumentException("Klasör bulunamadı.", nameof(request.FolderId));

            if (!string.Equals(folder.DepartmentName, page.DepartmentName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Klasör başka bir departmana ait.", nameof(request.FolderId));

            folderId = folder.Id;
        }

        // Version History (2026-08-17) - ÖNCE mevcut (şu ana kadar güncel
        // olan) hâli bir WikiPageVersion'a snapshot'la, page.Update() çağrılır
        // çağrılmaz bu bilgi page'in kendisinden kaybolacak (Content/Title/
        // Visibility/Tags ÜZERİNE yazılacak) - Documents'ın "önce snapshot'la,
        // SONRA ReplaceFile çağır" sırasıyla AYNI ilke.
        var snapshot = WikiPageVersion.CreateSnapshot(
            page.Id, page.CurrentVersionNumber, page.Title, page.Content, page.Visibility.ToString(), page.Tags,
            _currentUser.UserId.Value, _currentUser.Email);
        await _wikiPageVersionRepository.AddAsync(snapshot, cancellationToken);

        page.Update(request.Title, request.Content, visibility, folderId, request.Tags);

        // AuditBehavior, Handler bitince (next() sonrası) bunu okuyacak.
        request.AuditDetails = page.Title;

        // GetByIdAsync EF Core'un change tracker'ından geliyor - page üzerindeki
        // mutasyon zaten izleniyor, SaveChangesAsync bunu otomatik algılayıp
        // UPDATE olarak yazıyor (repository'de ayrı bir UpdateAsync'e gerek yok).
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
