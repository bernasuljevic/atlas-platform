using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.AI.Application.Search.Queries;

public class SearchByMeaningQueryHandler : IRequestHandler<SearchByMeaningQuery, IReadOnlyList<SemanticSearchResultDto>>
{
    // SearchWikiPagesByMeaningQueryHandler'daki AYNI gerekçe - şimdi İKİ ayrı
    // aday havuzu için de geçerli (her kaynak kendi candidatePoolSize'ıyla
    // sorgulanıyor, aksi halde bir kaynak diğerini "aç" bırakabilirdi).
    private const int CandidatePoolMultiplier = 4;
    private const int MinimumCandidatePool = 20;

    private readonly IEmbeddingService _embeddingService;
    private readonly IWikiPageEmbeddingRepository _wikiPageEmbeddingRepository;
    private readonly IDocumentEmbeddingRepository _documentEmbeddingRepository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public SearchByMeaningQueryHandler(
        IEmbeddingService embeddingService,
        IWikiPageEmbeddingRepository wikiPageEmbeddingRepository,
        IDocumentEmbeddingRepository documentEmbeddingRepository,
        IWikiVisibilityChecker visibilityChecker,
        ICurrentUserAccessor currentUser)
    {
        _embeddingService = embeddingService;
        _wikiPageEmbeddingRepository = wikiPageEmbeddingRepository;
        _documentEmbeddingRepository = documentEmbeddingRepository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SemanticSearchResultDto>> Handle(
        SearchByMeaningQuery request, CancellationToken cancellationToken)
    {
        var queryVectors = await _embeddingService.EmbedAsync([request.QueryText], cancellationToken);
        var queryVector = queryVectors[0];

        var candidatePoolSize = Math.Max(MinimumCandidatePool, request.TopN * CandidatePoolMultiplier);

        // BULUNAN GERÇEK BUG (canlı integration testte yakalandı): burada önce
        // Task.WhenAll ile İKİ repository çağrısı "aynı anda" başlatılmıştı -
        // ama IWikiPageEmbeddingRepository VE IDocumentEmbeddingRepository
        // AYNI DI scope'undaki AYNI AiDbContext instance'ını (Scoped) sarmalıyor.
        // EF Core'un DbContext'i thread-safe DEĞİL / aynı anda birden fazla
        // sorguyu desteklemiyor - "A second operation was started on this
        // context instance before a previous operation completed" hatasıyla
        // HER istekte patlıyordu. Çözüm: iki sorguyu SIRAYLA (art arda) await
        // etmek - iki Postgres round-trip'i toplam gecikmeyi biraz artırıyor
        // ama tabloların ikisi de LIMIT'li küçük sorgular olduğu için önemsiz.
        var wikiCandidates = await _wikiPageEmbeddingRepository.FindNearestAsync(
            queryVector, candidatePoolSize, request.FromUtc, request.ToUtc, cancellationToken);
        var documentCandidates = await _documentEmbeddingRepository.FindNearestAsync(
            queryVector, candidatePoolSize, request.FromUtc, request.ToUtc, cancellationToken);

        // Aynı departman güvenlik kuralı İKİ kaynak için de geçerli - Documents
        // zaten aynı Visibility/DepartmentName semantiğini WikiPage'den ödünç
        // aldığı için (bkz. Document entity'deki not) tek bir IWikiVisibilityChecker
        // çağrısı ikisine de uyuyor, ayrı bir kural İCAT EDİLMEDİ.
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        var wikiResults = wikiCandidates
            .Where(hit => _visibilityChecker.IsVisibleTo(
                hit.Embedding.Visibility, hit.Embedding.DepartmentName, _currentUser.Department, viewerIsAdmin))
            // Bir sayfanın birden fazla chunk'ı aday havuzuna girmiş olabilir -
            // kaynak başına sadece EN YAKIN chunk'ı tutuyoruz.
            .GroupBy(hit => hit.Embedding.WikiPageId)
            .Select(group => group.MinBy(hit => hit.Distance)!)
            .Select(hit => new SemanticSearchResultDto(
                SearchResultSourceTypes.WikiPage, hit.Embedding.WikiPageId, hit.Embedding.Title,
                hit.Embedding.DepartmentName, hit.Embedding.ChunkText, 1 - hit.Distance, hit.Embedding.CreatedAtUtc));

        var documentResults = documentCandidates
            .Where(hit => _visibilityChecker.IsVisibleTo(
                hit.Embedding.Visibility, hit.Embedding.DepartmentName, _currentUser.Department, viewerIsAdmin))
            .GroupBy(hit => hit.Embedding.DocumentId)
            .Select(group => group.MinBy(hit => hit.Distance)!)
            .Select(hit => new SemanticSearchResultDto(
                SearchResultSourceTypes.Document, hit.Embedding.DocumentId, hit.Embedding.Title,
                hit.Embedding.DepartmentName, hit.Embedding.ChunkText, 1 - hit.Distance, hit.Embedding.CreatedAtUtc));

        // İki kaynaktan gelen sonuçlar BURADA (uygulama seviyesinde) birleşiyor -
        // asıl "birleşik arama" bu satır. Skor İKİ kaynak için de AYNI formülle
        // (1 - cosine distance) hesaplandığı için doğrudan karşılaştırılabilir.
        return wikiResults.Concat(documentResults)
            .OrderByDescending(r => r.Score)
            .Take(request.TopN)
            .ToList();
    }
}
