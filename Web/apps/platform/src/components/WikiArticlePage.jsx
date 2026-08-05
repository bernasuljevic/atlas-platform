import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { ChevronRight, Pencil, ThumbsDown, ThumbsUp, Trash2 } from "lucide-react";
import { deleteWikiPage, getWikiFolderTree, getWikiPageById } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { renderWikiMarkdown } from "../markdown";
import { Button } from "@atlas/ui/button";
import { Badge } from "@atlas/ui/badge";

// Bir sayfanın FolderId'sinden kök klasöre kadar olan ad zincirini (breadcrumb
// için) bulmak - klasör ağacı zaten sidebar için çekiliyor (bkz.
// WikiFolderTree), burada AYNI veri sayfa özelinde tekrar çekilip (departman
// başına küçük bir liste, maliyeti düşük) yürünüyor.
function findFolderPath(folders, targetFolderId, path = []) {
  for (const folder of folders) {
    if (folder.id === targetFolderId) return [...path, folder.name];
    const found = findFolderPath(folder.children, targetFolderId, [...path, folder.name]);
    if (found) return found;
  }
  return null;
}

// Wikipedia'nın makale sayfasıyla aynı fikir: küçük bir dialog yerine,
// kendi URL'i olan (paylaşılabilir, geri/ileri tuşlarıyla gezilebilir) tam
// bir sayfa. WikiPageTable ve WikiSearch'teki eski salt-okunur dialog'ların
// yerini bu aldı - ikisi de artık buraya link veriyor.
function WikiArticlePage({ token }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const { userId, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [page, setPage] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [folderPath, setFolderPath] = useState([]);
  // "Faydalı oldu mu" BİLEREK hiçbir yere kaydedilmiyor - sadece yerel bir
  // teşekkür mesajı gösteriyor. Gerçek bir geri bildirim/oylama sistemi
  // (backend'e yazan) ayrı, kendi başına bir özellik olurdu.
  const [feedbackGiven, setFeedbackGiven] = useState(null);
  // Wikipedia sekmeleri ("Madde"/"Tartışma") + İçindekiler kutusunun aç/kapa
  // durumu - Hooks kuralı gereği BURADA, erken return'lerin (isLoading/error
  // dallarının, aşağıda) ÖNCESİNDE tanımlanmalı. Bir önceki denemede bu ikisi
  // yanlışlıkla return'lerden SONRAYA konmuştu - "sayfa henüz yüklenmedi"
  // render'ında hiç çağrılmayıp "sayfa yüklendi" render'ında çağrılıyorlardı,
  // bu da React'ın "Hooks kuralları ihlal edildi" hatasına yol açıyordu (bkz.
  // WikiArticlePage'in daha önceki bir düzeltmesindeki AYNI ders, useMemo için).
  const [activeTab, setActiveTab] = useState("article");
  const [showToc, setShowToc] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(null);

    getWikiPageById(token, id)
      .then((result) => {
        if (!cancelled) setPage(result);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [token, id]);

  useEffect(() => {
    if (!page) return;
    let cancelled = false;

    if (!page.folderId) {
      setFolderPath([]);
      return;
    }

    getWikiFolderTree(token, page.departmentName)
      .then((tree) => {
        if (!cancelled) setFolderPath(findFolderPath(tree.folders, page.folderId) ?? []);
      })
      .catch(() => {
        if (!cancelled) setFolderPath([]);
      });

    return () => {
      cancelled = true;
    };
  }, [token, page?.departmentName, page?.folderId]);

  async function handleDelete() {
    if (!window.confirm("Bu sayfayı silmek istediğine emin misin?")) return;

    setIsDeleting(true);
    try {
      await deleteWikiPage(token, id);
      navigate("/wiki");
    } catch (err) {
      setError(err.message);
      setIsDeleting(false);
    }
  }

  // React'ın Hooks kuralı: bir Hook, erken return'lerin (isLoading/error
  // dallarının) ÖNCESİNDE, HER render'da aynı sırayla çağrılmalı - bu yüzden
  // useMemo burada, "page henüz yok" ihtimalini kendi içinde (page?.content
  // ile) ele alarak duruyor, "page var mı" kontrolünden SONRAYA taşınmıyor
  // (taşınsaydı bazı render'larda hiç çağrılmamış olurdu - canlı test
  // sırasında yakalanan gerçek bir hataydı, bkz. konsoldaki "change in the
  // order of Hooks" uyarısı).
  const { nodes, headings } = useMemo(
    () => renderWikiMarkdown(page?.content ?? ""),
    [page?.content]
  );

  if (isLoading) {
    return <p style={{ color: "var(--text)" }}>Yükleniyor...</p>;
  }

  if (error || !page) {
    return (
      <div>
        <p style={{ color: "red" }} className="mb-3 text-sm">
          {error ?? "Bu sayfa artık mevcut değil ya da görme yetkin yok."}
        </p>
        <Link to="/wiki" className="underline">
          Sayfa listesine dön
        </Link>
      </div>
    );
  }

  const canEdit = isAdmin || page.createdByUserId === userId;
  const breadcrumbParts = [`${page.departmentName} Departmanı`, ...folderPath, page.title];
  // Backend'de virgülle ayrılmış TEK bir string olarak saklanıyor (bkz.
  // WikiPage.cs'teki not) - görüntüleme için burada listeye ayrılıyor.
  const tagList = page.tags ? page.tags.split(",").filter(Boolean) : [];

  return (
    <article className="mx-auto max-w-5xl text-left text-[15px]">
      {/* Breadcrumb */}
      <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
        <nav className="flex flex-wrap items-center gap-1 text-xs" style={{ color: "var(--text)", opacity: 0.85 }}>
          {breadcrumbParts.map((part, idx) => (
            <span key={idx} className="flex items-center gap-1">
              {idx > 0 && <ChevronRight size={12} className="opacity-60" />}
              <span className={idx === breadcrumbParts.length - 1 ? "font-semibold" : ""}>{part}</span>
            </span>
          ))}
        </nav>
      </div>

      {/* Wikipedia Article Title */}
      <h1 className="mt-1 mb-1 text-2xl font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
        {page.title}
      </h1>

      {/* Wikipedia Subtabs Bar */}
      <div className="mb-3 flex flex-wrap items-center justify-between border-b" style={{ borderColor: "var(--border)" }}>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setActiveTab("article")}
            className={`wiki-tab ${activeTab === "article" ? "active" : ""}`}
          >
            Madde
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("talk")}
            className={`wiki-tab ${activeTab === "talk" ? "active" : ""}`}
          >
            Tartışma
          </button>
        </div>

        <div className="flex items-center gap-1">
          <button
            type="button"
            className="wiki-tab active"
          >
            Oku
          </button>
          {canEdit && (
            <Link to={`/wiki/${page.id}/edit`} className="wiki-tab">
              <Pencil size={13} /> Düzenle
            </Link>
          )}
          {canEdit && (
            <button type="button" onClick={handleDelete} disabled={isDeleting} className="wiki-tab text-red-500 hover:text-red-600">
              <Trash2 size={13} /> {isDeleting ? "Siliniyor..." : "Sil"}
            </button>
          )}
        </div>
      </div>

      <p className="mb-4 text-xs italic" style={{ color: "var(--text)", opacity: 0.75 }}>
        Atlas Wiki, şirket bilgi platformu ansiklopedisi
      </p>

      {activeTab === "talk" ? (
        <div className="rounded-lg border p-6 text-center" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
          <h2 className="text-base font-semibold" style={{ color: "var(--text-h)" }}>
            "{page.title}" Tartışma Sayfası
          </h2>
          <p className="mt-2 text-xs" style={{ color: "var(--text)" }}>
            Bu makale hakkındaki tartışma ve öneriler henüz başlatılmadı.
          </p>
        </div>
      ) : (
        /* Main Article Layout: Left TOC + Content + Right Infobox */
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start">
          {/* Left Table of Contents */}
          {headings.length > 1 && (
            <nav
              className="h-fit shrink-0 rounded-lg border p-3.5 lg:sticky lg:top-4 lg:w-52"
              style={{ borderColor: "var(--border)", background: "var(--code-bg)" }}
            >
              <div className="mb-2 flex items-center justify-between border-b pb-1.5" style={{ borderColor: "var(--border)" }}>
                <span className="text-xs font-bold uppercase tracking-wider" style={{ color: "var(--text-h)" }}>
                  İçindekiler
                </span>
                <button
                  type="button"
                  onClick={() => setShowToc((s) => !s)}
                  className="text-[11px] font-medium underline"
                  style={{ color: "var(--brand-accent)" }}
                >
                  [{showToc ? "gizle" : "göster"}]
                </button>
              </div>

              {showToc && (
                <ol className="flex flex-col gap-1 text-xs">
                  {headings.map((h) => (
                    <li key={h.id}>
                      <a
                        href={`#${h.id}`}
                        className="block rounded px-1.5 py-1 hover:bg-[var(--brand-accent)]/10"
                        style={{ paddingLeft: 6 + (h.level - 1) * 10, color: "var(--brand-accent)", fontWeight: 500 }}
                      >
                        {h.text}
                      </a>
                    </li>
                  ))}
                </ol>
              )}
            </nav>
          )}

          {/* Center Article Content */}
          <div className="min-w-0 flex-1">
            <div className="leading-relaxed font-normal" style={{ color: "var(--text)" }}>
              {nodes}
            </div>

            <div
              className="mt-8 flex flex-wrap items-center justify-between gap-3 border-t pt-4 text-xs"
              style={{ borderColor: "var(--border)", color: "var(--text)" }}
            >
              {feedbackGiven === null ? (
                <div className="flex items-center gap-2">
                  <span>Bu sayfa faydalı oldu mu?</span>
                  <Button variant="outline" size="sm" onClick={() => setFeedbackGiven("yes")}>
                    <ThumbsUp size={13} className="mr-1" /> Evet
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => setFeedbackGiven("no")}>
                    <ThumbsDown size={13} className="mr-1" /> Hayır
                  </Button>
                </div>
              ) : (
                <span style={{ opacity: 0.8 }}>Geri bildirimin için teşekkürler!</span>
              )}

              <span style={{ opacity: 0.7 }}>
                Son güncelleme: {formatUtcTimestamp(page.updatedAtUtc ?? page.createdAtUtc)}
              </span>
            </div>
          </div>

          {/* Right Wikipedia Infobox Card */}
          <aside
            className="w-full shrink-0 rounded-lg border lg:w-64"
            style={{ borderColor: "var(--border)", background: "var(--bg)" }}
          >
            <div className="border-b px-3.5 py-2 text-center" style={{ borderColor: "var(--border)", background: "var(--code-bg)" }}>
              <h3 className="text-xs font-bold text-[var(--text-h)]">{page.title}</h3>
              <p className="text-[11px] text-[var(--text)] opacity-70">Bilgi Kutusu</p>
            </div>
            <div className="flex flex-col divide-y text-xs" style={{ borderColor: "var(--border)" }}>
              <div className="flex justify-between px-3 py-2">
                <span className="font-semibold text-[var(--text-h)]">Departman</span>
                <span className="text-[var(--text)]">{page.departmentName}</span>
              </div>
              <div className="flex justify-between px-3 py-2">
                <span className="font-semibold text-[var(--text-h)]">Erişim</span>
                <span className="font-medium text-[var(--brand-accent)]">
                  {page.visibility === "Public" ? "Herkese Açık" : "Departmana Özel"}
                </span>
              </div>
              <div className="flex justify-between px-3 py-2">
                <span className="font-semibold text-[var(--text-h)]">Oluşturan</span>
                <span className="truncate max-w-[120px] text-[var(--text)]">{page.createdByEmail ?? "Bilinmiyor"}</span>
              </div>
              <div className="flex justify-between px-3 py-2">
                <span className="font-semibold text-[var(--text-h)]">Tarih</span>
                <span className="text-[var(--text)]">{formatUtcTimestamp(page.createdAtUtc)}</span>
              </div>
              {tagList.length > 0 && (
                <div className="p-3">
                  <span className="block mb-1 font-semibold text-[var(--text-h)]">Etiketler</span>
                  <div className="flex flex-wrap gap-1">
                    {tagList.map((tag) => (
                      <Badge key={tag} variant="outline" className="text-[10px] py-0 px-1.5 font-normal">
                        {tag}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </aside>
        </div>
      )}
    </article>
  );
}

export default WikiArticlePage;
