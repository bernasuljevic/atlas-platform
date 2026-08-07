import { useEffect, useState } from "react";
import { Trash2 } from "lucide-react";
import { createComment, deleteComment, getComments } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { Button } from "@atlas/ui/button";
import { Textarea } from "@atlas/ui/textarea";

// Wikipedia'nın "Tartışma" sayfası fikrinin gerçek karşılığı - hem
// WikiArticlePage'in hem HomePage'in "Tartışma" sekmesinde AYNI bileşen
// kullanılıyor (pageId farkı yönetiyor: dolu = o sayfaya özel, undefined/null =
// ana sayfadaki genel platform tartışması, bkz. backend'deki GetCommentsQuery
// notu). Thread'li (yanıtın-yanıtı) yapı BİLEREK yok - düz, kronolojik bir
// liste yeterli (bkz. Comment.cs'teki YAGNI notu).
function DiscussionPanel({ token, pageId }) {
  const { userId, isAdmin } = getUserInfoFromToken(token);
  const [comments, setComments] = useState(null);
  const [content, setContent] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  async function loadComments() {
    try {
      const result = await getComments(token, pageId);
      setComments(result);
    } catch (err) {
      setError(err.message);
    }
  }

  useEffect(() => {
    setComments(null);
    setError(null);
    loadComments();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, pageId]);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!content.trim()) return;

    setIsSubmitting(true);
    setError(null);
    try {
      await createComment(token, { pageId, content: content.trim() });
      setContent("");
      await loadComments();
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleDelete(commentId) {
    if (!window.confirm("Bu yorumu silmek istediğine emin misin?")) return;

    try {
      await deleteComment(token, commentId);
      await loadComments();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <form onSubmit={handleSubmit} className="flex flex-col gap-2">
        <Textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          placeholder="Bir yorum bırak..."
          disabled={isSubmitting}
          className="min-h-20"
        />
        <div className="flex justify-end">
          <Button type="submit" size="sm" disabled={isSubmitting || !content.trim()}>
            {isSubmitting ? "Gönderiliyor..." : "Gönder"}
          </Button>
        </div>
      </form>

      {error && <p style={{ color: "red" }} className="text-xs">{error}</p>}

      {comments === null ? (
        <p className="text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
          Yükleniyor...
        </p>
      ) : comments.length === 0 ? (
        <p className="text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
          Henüz yorum yok - ilk yorumu sen bırak.
        </p>
      ) : (
        <div className="flex flex-col divide-y" style={{ borderColor: "var(--border)" }}>
          {comments.map((c) => (
            <div key={c.id} className="flex items-start justify-between gap-2 py-3">
              <div className="min-w-0">
                <p className="text-xs font-semibold" style={{ color: "var(--text-h)" }}>
                  {c.authorEmail ?? "Bilinmiyor"}{" "}
                  <span className="font-normal opacity-60">· {formatUtcTimestamp(c.createdAtUtc)}</span>
                </p>
                <p className="mt-1 text-sm whitespace-pre-wrap" style={{ color: "var(--text)" }}>
                  {c.content}
                </p>
              </div>
              {(isAdmin || c.authorUserId === userId) && (
                <button
                  type="button"
                  onClick={() => handleDelete(c.id)}
                  className="shrink-0 rounded p-1 hover:bg-[var(--brand-accent)]/10"
                  title="Yorumu sil"
                  aria-label="Yorumu sil"
                >
                  <Trash2 size={13} style={{ color: "var(--text)", opacity: 0.6 }} />
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default DiscussionPanel;
