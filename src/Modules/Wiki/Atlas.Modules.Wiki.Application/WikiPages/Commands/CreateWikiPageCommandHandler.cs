using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Commands;

public class CreateWikiPageCommandHandler : IRequestHandler<CreateWikiPageCommand, Guid>
{
    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPublisher _publisher;

    // ICurrentUserAccessor, Wiki.Application'ın Shared.Contracts'tan tanıdığı bir
    // interface. Gerçek implementasyonu KİM sağlıyor? Wiki'nin haberi yok, umurunda değil.
    // (Bugün Auth.Infrastructure sağlayacak - ama bu handler bunu asla bilmeyecek.)
    public CreateWikiPageCommandHandler(
        IWikiPageRepository wikiPageRepository,
        ICurrentUserAccessor currentUser,
        IPublisher publisher)
    {
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
        _publisher = publisher;
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

        var page = WikiPage.Create(
            request.Title, request.Content, departmentName,
            visibility, _currentUser.UserId.Value);

        await _wikiPageRepository.AddAsync(page, cancellationToken);

        // Sayfa kaydedildikten SONRA event'i yayınlıyoruz - "olan bitmiş bir şeyi"
        // duyuruyoruz. Wiki, bunu kimin dinlediğini bilmiyor (şu an Notifications
        // dinleyecek, yarın AI modülü de dinleyebilir - Wiki'de hiçbir şey değişmez).
        await _publisher.Publish(
            new WikiPageCreatedEvent(page.Id, page.Title, page.DepartmentName, page.Content, page.Visibility.ToString()),
            cancellationToken);

        return page.Id;
    }
}