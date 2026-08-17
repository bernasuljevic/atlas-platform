import { useEffect, useState } from "react";
import { toast } from "sonner";
import { History, RotateCcw } from "lucide-react";
import { getWikiPageVersionByNumber, getWikiPageVersions, restoreWikiPageVersion } from "../api";
import { formatUtcTimestamp } from "../dateUtils";
import { renderWikiMarkdown } from "../markdown";
import { Button } from "@atlas/ui/button";

// DiscussionPanel'le AYNI desen - WikiArticlePage'in "Geçmiş" sekmesine
// kendi kendine yeten (self-contained) bir bileşen olarak takılıyor.
// DocumentDetailPage'in versiyon geçmişi listesindeki (P6) fikirle AYNI:
// SADECE eski (arşivlenmiş) versiyonlar listeleniyor - güncel hâl zaten
// makalenin kendisinde gösteriliyor.
//
// Documents'ın "İndir" düğmesinin AKSİNE burada "Önizle" (inline genişlet,
// renderWikiMarkdown ile salt-okunur render) + "Bu sürüme geri dön" var -
// bir Dialog AÇILMADI (bu projede içerik görüntüleme artık Dialog'dan tam
// sayfaya kaydı, bkz. WikiPageTable'ın eski detay dialogunun kaldırılma
// gerekçesi) - bunun yerine satır İÇİNDE genişleyen bir önizleme, ayrı bir
// route/Dialog açmadan aynı "tam sayfa" felsefesiyle tutarlı kalıyor.
function WikiVersionHistoryPanel({ token, pageId, canRestore, onRestored }) {
  const [versions, setVersions] = useState(null);
  const [error, setError] = useState(null);
  const [expandedVersion, setExpandedVersion] = useState(null);
  const [previewContent, setPreviewContent] = useState(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [restoringVersion, setRestoringVersion] = useState(null);

  async function loadVersions() {
    try {
      const result = await getWikiPageVersions(token, pageId);
      setVersions(result);
    } catch (err) {
      setError(err.message);
    }
  }

  useEffect(() => {
    setVersions(null);
    setError(null);
    setExpandedVersion(null);
    loadVersions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, pageId]);

  async function handleToggleExpand(versionNumber) {
    if (expandedVersion === versionNumber) {
      setExpandedVersion(null);
      return;
    }

    setExpandedVersion(versionNumber);
    setPreviewContent(null);
    setIsPreviewLoading(true);
    try {
      const detail = await getWikiPageVersionByNumber(token, pageId, versionNumber);
      setPreviewContent(detail);
    } catch (err) {
      toast("Versiyon önizlemesi yüklenemedi", { description: err.message });
      setExpandedVersion(null);
    } finally {
      setIsPreviewLoading(false);
    }
  }

  async function handleRestore(versionNumber) {
    if (!window.confirm(`Sayfayı versiyon ${versionNumber} hâline geri döndürmek istediğine emin misin? Şu anki hâl de otomatik olarak arşive eklenecek.`)) {
      return;
    }

    setRestoringVersion(versionNumber);
    try {
      await restoreWikiPageVersion(token, pageId, versionNumber);
      toast(`Sayfa versiyon ${versionNumber} hâline geri döndürüldü`);
      setExpandedVersion(null);
      await loadVersions();
      onRestored?.();
    } catch (err) {
      toast("Geri dönülemedi", { description: err.message });
    } finally {
      setRestoringVersion(null);
    }
  }

  if (error) {
    return (
      <p className="text-xs" style={{ color: "red" }}>
        {error}
      </p>
    );
  }

  if (versions === null) {
    return (
      <p className="text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
        Yükleniyor...
      </p>
    );
  }

  if (versions.length === 0) {
    return (
      <p className="text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
        Bu sayfa henüz hiç düzenlenmedi - versiyon geçmişi boş.
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-2">
      <p className="mb-1 flex items-center gap-1.5 text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
        <History size={13} />
        Sadece eski sürümler listelenir - güncel hâl "Madde" sekmesinde.
      </p>
      {versions.map((v) => {
        const isExpanded = expandedVersion === v.versionNumber;
        return (
          <div key={v.versionNumber} className="rounded-lg border" style={{ borderColor: "var(--border)" }}>
            <button
              type="button"
              onClick={() => handleToggleExpand(v.versionNumber)}
              className="flex w-full flex-wrap items-center justify-between gap-2 px-3 py-2 text-left text-sm"
            >
              <div className="min-w-0">
                <span className="font-medium" style={{ color: "var(--text-h)" }}>
                  Versiyon {v.versionNumber}
                </span>
                <span className="ml-2 truncate" style={{ color: "var(--text)", opacity: 0.7 }}>
                  {v.title}
                </span>
                <p className="text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
                  {v.editedByEmail ?? "Bilinmiyor"} tarafından {formatUtcTimestamp(v.editedAtUtc)} tarihinde değiştirildi
                </p>
              </div>
            </button>

            {isExpanded && (
              <div className="border-t px-3 py-3" style={{ borderColor: "var(--border)" }}>
                {isPreviewLoading ? (
                  <p className="text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
                    Yükleniyor...
                  </p>
                ) : (
                  previewContent && (
                    <>
                      <div className="mb-3 max-h-64 overflow-y-auto text-sm" style={{ color: "var(--text)" }}>
                        {renderWikiMarkdown(previewContent.content).nodes}
                      </div>
                      {canRestore && (
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={restoringVersion === v.versionNumber}
                          onClick={() => handleRestore(v.versionNumber)}
                        >
                          <RotateCcw size={13} className="mr-1" />
                          {restoringVersion === v.versionNumber ? "Geri dönülüyor..." : "Bu sürüme geri dön"}
                        </Button>
                      )}
                    </>
                  )
                )}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

export default WikiVersionHistoryPanel;
