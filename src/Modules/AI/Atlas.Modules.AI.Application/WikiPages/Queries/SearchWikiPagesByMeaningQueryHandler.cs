using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.AI.Application.WikiPages.Queries;

public class SearchWikiPagesByMeaningQueryHandler
    : IRequestHandler<SearchWikiPagesByMeaningQuery, IReadOnlyList<WikiSearchResultDto>>
{
    // Postgres'ten TopN'den fazla aday çekiyoruz - hem aynı sayfadan birden
    // fazla chunk gelebiliyor (sayfa başına TEK sonuç istiyoruz) hem de
    // görünürlük filtresi bazı adayları eleyebiliyor. Filtre SONRASI elimizde
    // TopN'den az sonuç kalmasın diye geniş bir aday havuzuyla başlıyoruz -
    // bu tam bir garanti değil (çok fazla eleme olursa yine az sonuç dönebilir),
    // ama DB'de her şeyi taramaktan çok daha ucuz. Kesin çözüm (DB seviyesinde
    // görünürlük filtresi) Wiki'nin kuralını AI'ın sorgusuna sızdırmayı
    // gerektirirdi - modül izolasyonunu bozardı.
    private const int CandidatePoolMultiplier = 4;
    private const int MinimumCandidatePool = 20;

    private readonly IEmbeddingService _embeddingService;
    private readonly IWikiPageEmbeddingRepository _repository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public SearchWikiPagesByMeaningQueryHandler(
        IEmbeddingService embeddingService,
        IWikiPageEmbeddingRepository repository,
        IWikiVisibilityChecker visibilityChecker,
        ICurrentUserAccessor currentUser)
    {
        _embeddingService = embeddingService;
        _repository = repository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WikiSearchResultDto>> Handle(
        SearchWikiPagesByMeaningQuery request, CancellationToken cancellationToken)
    {
        var queryVectors = await _embeddingService.EmbedAsync([request.QueryText], cancellationToken);
        var queryVector = queryVectors[0];

        var candidatePoolSize = Math.Max(MinimumCandidatePool, request.TopN * CandidatePoolMultiplier);
        var candidates = await _repository.FindNearestAsync(
            queryVector, candidatePoolSize, request.FromUtc, request.ToUtc, cancellationToken);

        // Aynı departman güvenlik kuralı burada da geçerli (bkz. CLAUDE.md
        // "Öğrenilen dersler #10") - GetWikiPagesQueryHandler'ın Wiki tarafında
        // yaptığının AYNISI, sadece kural WikiVisibilityChecker üzerinden
        // Wiki.Domain'den ödünç alınıyor, AI kendi kopyasını yazmıyor.
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;
        var visibleCandidates = candidates.Where(hit =>
            _visibilityChecker.IsVisibleTo(
                hit.Embedding.Visibility, hit.Embedding.DepartmentName, _currentUser.Department, viewerIsAdmin));

        // Bir sayfanın birden fazla chunk'ı aday havuzuna girmiş olabilir -
        // sayfa başına sadece EN YAKIN (en düşük mesafeli) chunk'ı tutuyoruz,
        // aksi halde aynı sayfa sonuç listesinde birden fazla kez görünürdü.
        return visibleCandidates
            .GroupBy(hit => hit.Embedding.WikiPageId)
            .Select(group => group.MinBy(hit => hit.Distance)!)
            .OrderBy(hit => hit.Distance)
            .Take(request.TopN)
            .Select(hit => new WikiSearchResultDto(
                hit.Embedding.WikiPageId,
                hit.Embedding.Title,
                hit.Embedding.DepartmentName,
                hit.Embedding.ChunkText,
                Score: 1 - hit.Distance,
                hit.Embedding.CreatedAtUtc))
            .ToList();
    }
}
