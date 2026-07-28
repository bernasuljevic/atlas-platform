import { useState } from "react";
import { deleteWikiPage } from "../api";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Badge } from "@atlas/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@atlas/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@atlas/ui/table";

const CONTENT_PREVIEW_LENGTH = 80;

// Tablo hücresinde tüm içeriği göstermek satırları dev boyutlara şişiriyordu -
// kısa bir önizleme yeterli, tam metin satıra tıklanınca açılan detay
// Dialog'unda gösteriliyor.
function truncateContent(text) {
  if (text.length <= CONTENT_PREVIEW_LENGTH) return text;
  return text.slice(0, CONTENT_PREVIEW_LENGTH).trimEnd() + "…";
}

// WikiBoard.jsx'ten ayrıldı (bkz. CLAUDE.md "İzlenecek teknik borç") - liste,
// silme ve satıra tıklayınca açılan detay dialogu burada; oluşturma formu
// ayrı CreateWikiPageDialog'da. Silme sonrası listeyi yenilemesi için
// parent'a onDeleted ile haber veriyor.
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
  const [selectedPage, setSelectedPage] = useState(null);
  const [deletingPageId, setDeletingPageId] = useState(null);
  const [error, setError] = useState(null);

  async function handleDelete(pageId, e) {
    // Satırın kendi onClick'i (detay dialogunu açan) tetiklenmesin.
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
                  <TableHead className="hidden md:table-cell">İçerik</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pages.map((p) => (
                  <TableRow
                    key={p.id}
                    onClick={() => setSelectedPage(p)}
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
                        <Badge className="bg-[var(--brand-accent)] text-[var(--text-h)]">Herkese Açık</Badge>
                      ) : (
                        <Badge variant="outline">Sadece Departman</Badge>
                      )}
                    </TableCell>
                    {/* Dar ekranlarda (bölünmüş pencere) tamamen gizleniyor -
                        tam içerik zaten satıra tıklayınca açılan detay
                        Dialog'unda var, bu sütun sadece bir önizleme. */}
                    <TableCell className="hidden max-w-[280px] truncate text-[var(--text)]/70 md:table-cell">
                      {truncateContent(p.content)}
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

      {/* Satıra tıklanınca açılan salt-okunur detay görünümü - tam içerik burada,
          liste tablosunda değil (bkz. truncateContent). Trigger'sız, "open" state'i
          doğrudan seçili sayfa var mı yok mu ona göre kontrol ediliyor. */}
      <Dialog open={selectedPage !== null} onOpenChange={(open) => !open && setSelectedPage(null)}>
        <DialogContent className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)] sm:max-w-lg">
          <DialogHeader>
            <DialogTitle style={{ color: "var(--text-h)" }}>{selectedPage?.title}</DialogTitle>
            <div className="flex gap-2">
              <Badge variant="outline">{selectedPage?.departmentName}</Badge>
              {selectedPage?.visibility === "Public" ? (
                <Badge className="bg-[var(--brand-accent)] text-[var(--text-h)]">Herkese Açık</Badge>
              ) : (
                <Badge variant="outline">Sadece Departman</Badge>
              )}
            </div>
          </DialogHeader>
          <p className="max-h-[60vh] overflow-y-auto whitespace-pre-wrap text-sm">
            {selectedPage?.content}
          </p>
        </DialogContent>
      </Dialog>
    </>
  );
}

export default WikiPageTable;
