using MediatR;

namespace Atlas.Modules.Wiki.Application.Comments.Commands;

// PageId null ise ana sayfadaki genel platform tartışmasına yazılıyor demek.
// Audit'e BİLEREK bağlanmadı (IAuditableCommand implemente etmiyor) - yorumun
// kendisi zaten yazar+tarih+içerik taşıyor, ayrı bir audit satırı gereksiz
// bir tekrar olurdu (CreateWikiPageCommand'daki "gerçek bir kaynak oluşturma"
// eyleminden FARKLI - burası daha çok bir sohbet girdisi).
public record CreateCommentCommand(Guid? PageId, string Content) : IRequest<Guid>;
