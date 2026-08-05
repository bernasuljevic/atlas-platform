using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class CreateWikiPageCommandHandler : IRequestHandler<CreateWikiPageCommand, Guid>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly IWikiFolderRepository _wikiFolderRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    // ICurrentUserAccessor, Wiki.Application'ın Shared.Contracts'tan tanıdığı bir
    // interface. Gerçek implementasyonu KİM sağlıyor? Wiki'nin haberi yok, umurunda değil.
    // (Bugün Auth.Infrastructure sağlayacak - ama bu handler bunu asla bilmeyecek.)
    public CreateWikiPageCommandHandler(
        IWikiPageRepository wikiPageRepository,
        IWikiFolderRepository wikiFolderRepository,
        ICurrentUserAccessor currentUser,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork)
    {
        _wikiPageRepository = wikiPageRepository;
        _wikiFolderRepository = wikiFolderRepository;
        _currentUser = currentUser;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateWikiPageCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("Wiki sayfası oluşturmak için giriş yapmış olmalısınız.");

        // GÜVENLİK: Normal (Member) bir kullanıcı SADECE kendi departmanına sayfa
        // ekleyebilir - istemcinin gönderdiği DepartmentName'e güvenmiyoruz, aksi
        // halde IK'daki biri "departmentName: IT" göndererek IT'nin alanına sayfa
        // ekleyebilirdi (Ders #10'daki okuma tarafı açığının yazma tarafındaki
        // karşılığı). Admin bu sınırın DIŞINDA - okuma tarafındaki bypass'la
        // simetrik olarak istediği departmana yazabiliyor (bkz. WikiVisibilityRules).
        var departmentName = _currentUser.IsAdmin ? request.DepartmentName : _currentUser.Department;

        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException(
                "Sayfa oluşturmak için bir departmana ait olmalısınız.", nameof(departmentName));

        var visibility = Enum.Parse<WikiVisibility>(request.Visibility, ignoreCase: true);

        // Klasörleme (Gün 2): CreateWikiFolderCommandHandler'daki AYNI kural -
        // bir sayfa sadece KENDİ departmanının klasörüne dosyalanabilir, aksi
        // halde IT'deki biri HR'ın klasör Id'sini tahmin edip sayfasını oraya
        // "sızdırabilirdi" (klasör görünürlük taşımasa da, organizasyon şeması
        // departmana ait bir kaynak).
        Guid? folderId = null;
        if (request.FolderId is not null)
        {
            var folder = await _wikiFolderRepository.GetByIdAsync(request.FolderId.Value, cancellationToken);

            if (folder is null)
                throw new ArgumentException("Klasör bulunamadı.", nameof(request.FolderId));

            if (!string.Equals(folder.DepartmentName, departmentName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Klasör başka bir departmana ait.", nameof(request.FolderId));

            folderId = folder.Id;
        }

        var page = WikiPage.Create(
            request.Title, request.Content, departmentName,
            visibility, _currentUser.UserId.Value, folderId, _currentUser.Email, request.Tags);

        // AuditBehavior, Handler bitince (next() sonrası) bunu okuyacak -
        // bkz. IAuditableCommand.AuditDetails'teki not.
        request.AuditDetails = page.Title;

        // Outbox Pattern (Gün 2): AddAsync artık kalıcı yazmıyor, sadece
        // ekliyor (stage). Event'i de doğrudan yayınlamıyoruz (IPublisher.Publish
        // KALDIRILDI) - onun yerine AYNI değişiklik kümesine bir OutboxMessage
        // ekliyoruz. Sonraki SaveChangesAsync çağrısı İKİSİNİ DE (WikiPage +
        // OutboxMessage) TEK bir transaction'da, atomik olarak yazıyor - biri
        // yazılıp diğeri yazılmadan process çökemez. Gerçek yayın (Notifications/
        // AI'ın haberdar olması) Gün 3'teki arka plan işleyicinin işi.
        await _wikiPageRepository.AddAsync(page, cancellationToken);

        _outboxWriter.Enqueue(
            new WikiPageCreatedEvent(page.Id, page.Title, page.DepartmentName, page.Content, page.Visibility.ToString()));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return page.Id;
    }
}