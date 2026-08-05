import { useState } from "react";
import { useNavigate } from "react-router";
import { deleteWikiPage } from "../api";
import { formatUtcTimestamp } from "../dateUtils";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Badge } from "@atlas/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@atlas/ui/table";

// WikiBoard.jsx'ten ayrıldı (bkz. CLAUDE.md "İzlenecek teknik borç") - liste
// ve silme burada, oluşturma/düzenleme ayrı bir editör sekmesinde (bkz.
// WikiEditorPage). Satıra tıklanınca artık küçük bir dialog DEĞİL, gerçek bir
// /wiki/:id sayfasına gidiliyor (bkz. WikiArticlePage) - Wikipedia'nın makale
// sayfası deneyimiyle aynı fikir.
function WikiPageTable({
  token,
  pages,
  isLoadingPages,
  pageNumber,
  totalPages,
  onPageChange,
  isAdmin,
  userId,
  onDeleted,
}) {
  const navigate = useNavigate();
  const [deletingPageId, setDeletingPageId] = useState(null);
  const [error, setError] = useState(null);

  async function handleDelete(pageId, e) {
    // Satırın kendi onClick'i (/wiki/:id'ye giden) tetiklenmesin.
    e.stopPropagation();

    if (!window.confirm("Bu sayfayı silmek istediğine emin misin?")) return;

    setDeletingPageId(pageId);
    try {
      await deleteWikiPage(token, pageId);
      await onDeleted();
    } catch (err) {
      setError(err.message);
    } finally {
      setDeletingPageId(null);
    }
  }

  return (
    <>
      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      <Card className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardContent>
          {isLoadingPages ? (
            <p>Yükleniyor...</p>
          ) : pages.length === 0 ? (
            <p>Görebileceğin bir sayfa yok.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Başlık</TableHead>
                  <TableHead>Departman</TableHead>
                  <TableHead>Görünürlük</TableHead>
                  <TableHead className="hidden lg:table-cell">Oluşturulma</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pages.map((p) => (
                  <TableRow
                    key={p.id}
                    onClick={() => navigate(`/wiki/${p.id}`)}
                    className="cursor-pointer hover:bg-[var(--brand-accent)]/10"
                  >
                    <TableCell className="max-w-[110px] truncate font-medium sm:max-w-[220px]" title={p.title}>
                      {p.title}
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{p.departmentName}</Badge>
                    </TableCell>
                    <TableCell>
                      {p.visibility === "Public" ? (
                        <Badge className="text-white" style={{ background: "var(--brand-accent)" }}>Herkese Açık</Badge>
                      ) : (
                        <Badge variant="outline">Sadece Departman</Badge>
                      )}
                    </TableCell>
                    <TableCell className="hidden whitespace-nowrap text-sm text-[var(--text)]/70 lg:table-cell">
                      {formatUtcTimestamp(p.createdAtUtc)}
                    </TableCell>
                    <TableCell>
                      {/* Silme yetkisi istemcide "gösterme" kararı - gerçek
                          kontrol DeleteWikiPageCommandHandler'da (Admin ya da
                          sayfanın gerçek sahibi olmayan biri butonu görmese
                          bile isteği elle atarsa 403 alır). */}
                      {(isAdmin || p.createdByUserId === userId) && (
                        <Button
                          variant="destructive"
                          size="sm"
                          disabled={deletingPageId === p.id}
                          onClick={(e) => handleDelete(p.id, e)}
                        >
                          {deletingPageId === p.id ? "Siliniyor..." : "Sil"}
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          {!isLoadingPages && pages.length > 0 && (
            <div className="mt-4 flex items-center justify-between">
              <Button
                variant="outline"
                disabled={pageNumber <= 1 || isLoadingPages}
                onClick={() => onPageChange(pageNumber - 1)}
              >
                ← Önceki
              </Button>
              <span className="text-sm" style={{ color: "var(--text)" }}>
                Sayfa {pageNumber} / {totalPages}
              </span>
              <Button
                variant="outline"
                disabled={pageNumber >= totalPages || isLoadingPages}
                onClick={() => onPageChange(pageNumber + 1)}
              >
                Sonraki →
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </>
  );
}

export default WikiPageTable;
