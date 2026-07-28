using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Shared.CQRS.Behaviors;

/// <summary>
/// CacheInvalidationBehavior ile aynı yapı - sadece IAuditableCommand'ı
/// implemente eden Command'lar için MediatR bu behavior'ı pipeline'a sokuyor.
/// IAuditLogWriter'ın GERÇEK implementasyonunu (Audit.Infrastructure'daki
/// EfAuditLogWriter) kim sağlıyor bilmiyor/umursamıyor - tıpkı diğer
/// behavior'ların Shared.Contracts üzerinden çalışması gibi.
/// </summary>
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IAuditableCommand
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(IAuditLogWriter auditLogWriter, ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditLogWriter = auditLogWriter;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Önce Handler çalışsın - hata fırlatırsa (örn. yetkisiz silme denemesi)
        // audit satırı hiç yazılmaz, sadece GERÇEKLEŞEN işlemler denetlenir.
        var response = await next();

        try
        {
            // Create komutlarında AuditResourceId henüz bilinmiyor (null) - bu
            // durumda TResponse'un kendisi (Guid dönüyorsa, ör. yeni sayfanın
            // Id'si) kaynak ID'si olarak kullanılıyor.
            var resourceId = request.AuditResourceId ?? (response as Guid?)?.ToString();

            await _auditLogWriter.LogAsync(request.AuditAction, resourceId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit yazımı BEST-EFFORT bir yan etki - başarısız olması asıl
            // işlemi (Handler zaten başarıyla tamamlandı) geriye almamalı ya
            // da istemciye 500 döndürmemeli. Aynı gerekçe: AI'ın
            // WikiPageCreatedEventHandler'ının kendi hatasını yutması.
            _logger.LogError(ex, "Audit kaydı yazılamadı: {Action}", request.AuditAction);
        }

        return response;
    }
}
