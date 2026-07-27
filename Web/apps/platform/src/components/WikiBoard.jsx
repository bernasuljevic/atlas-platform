import { useState, useEffect, useMemo } from "react";
import { getWikiPages, createWikiPage, deleteWikiPage } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { DEPARTMENTS } from "../departments";
import WikiSearch from "./WikiSearch";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { Textarea } from "@atlas/ui/textarea";
import { Badge } from "@atlas/ui/badge";
import { RadioGroup, RadioGroupItem } from "@atlas/ui/radio-group";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@atlas/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@atlas/ui/table";

const PAGE_SIZE = 5;
const CONTENT_PREVIEW_LENGTH = 80;

// Tablo hücresinde tüm içeriği göstermek satırları dev boyutlara şişiriyordu -
// kısa bir önizleme yeterli, tam metin satıra tıklanınca açılan detay
// Dialog'unda gösteriliyor.
function truncateContent(text) {
  if (text.length <= CONTENT_PREVIEW_LENGTH) return text;
  return text.slice(0, CONTENT_PREVIEW_LENGTH).trimEnd() + "…";
}

function WikiBoard({ token, onLogout }) {
  // JWT'yi sadece UI kararları için okuyoruz (buton/alan göster-gizle) - gerçek
  // yetkilendirme her zaman backend'de (bkz. CreateWikiPageCommandHandler,
  // DeleteWikiPageCommandHandler). Token değişirse (refresh sonrası) yeniden hesaplanır.
  const { userId, department: ownDepartment, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [pages, setPages] = useState([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  // Admin herhangi bir departmanı seçebiliyor (varsayılan ilk departman);
  // normal kullanıcı için bu değerin önemi yok - backend zaten kendi
  // departmanını zorluyor, formda sadece bilgilendirme metni gösteriyoruz.
  const [department, setDepartment] = useState(isAdmin ? DEPARTMENTS[0].value : "");
  const [visibility, setVisibility] = useState("Public");
  const [error, setError] = useState(null);
  const [isLoadingPages, setIsLoadingPages] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [selectedPage, setSelectedPage] = useState(null);
  const [deletingPageId, setDeletingPageId] = useState(null);

  useEffect(() => {
    loadPages(pageNumber);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber]);

  async function loadPages(targetPageNumber) {
    setIsLoadingPages(true);
    try {
      // Departman filtresi artık burada seçilebilir bir şey değil - backend,
      // token'daki kullanıcının GERÇEK departmanına göre otomatik filtreliyor
      // (bkz. GetWikiPagesQueryHandler). Önceden serbest metin olarak
      // gönderilebiliyordu, bu da başka departmanların içeriğini tahmin ederek
      // görebilmeyi mümkün kılan bir güvenlik açığıydı.
      const result = await getWikiPages(token, targetPageNumber, PAGE_SIZE);
      setPages(result.items);
      setTotalPages(Math.max(1, result.totalPages));
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoadingPages(false);
    }
  }

  async function handleCreate(e) {
    e.preventDefault();
    setError(null);
    setIsCreating(true);

    try {
      await createWikiPage(token, {
        title,
        content,
        departmentName: department,
        visibility,
      });

      setTitle("");
      setContent("");
      setIsDialogOpen(false);

      // Yeni sayfa en yeni olarak 1. sayfada görünecek (backend CreatedAtUtc'ye
      // göre azalan sırada döndürüyor) - zaten 1. sayfadaysak yeniden yükle,
      // değilsek 1. sayfaya dön (bu da kendiliğinden yeniden yükletir, bkz. useEffect).
      if (pageNumber === 1) {
        await loadPages(1);
      } else {
        setPageNumber(1);
      }
    } catch (err) {
      setError(err.message.includes("giriş") ? err.message : "Sayfa oluşturulamadı: " + err.message);
    } finally {
      setIsCreating(false);
    }
  }

  async function handleDelete(pageId, e) {
    // Satırın kendi onClick'i (detay dialogunu açan) tetiklenmesin.
    e.stopPropagation();

    if (!window.confirm("Bu sayfayı silmek istediğine emin misin?")) return;

    setDeletingPageId(pageId);
    try {
      await deleteWikiPage(token, pageId);
      await loadPages(pageNumber);
    } catch (err) {
      setError(err.message);
    } finally {
      setDeletingPageId(null);
    }
  }

  return (
    <div style={{ maxWidth: 800, margin: "40px auto" }} className="px-4">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Atlas Wiki
        </h1>
        <div className="flex gap-2">
          {/* Eskiden form her zaman sayfada açık duruyordu - artık bir Dialog
              içinde, sadece "Yeni Sayfa" butonuna basılınca açılıyor. */}
          <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
            <DialogTrigger
              render={<Button className="bg-[var(--brand-accent)] text-[var(--text-h)] hover:opacity-90" />}
            >
              Yeni Sayfa
            </DialogTrigger>
            <DialogContent className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)] sm:max-w-md">
              <DialogHeader>
                <DialogTitle style={{ color: "var(--text-h)" }}>Yeni Sayfa</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleCreate} className="flex flex-col gap-4">
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="wiki-title">Başlık</Label>
                  <Input
                    id="wiki-title"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    disabled={isCreating}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="wiki-content">İçerik</Label>
                  <Textarea
                    id="wiki-content"
                    value={content}
                    onChange={(e) => setContent(e.target.value)}
                    disabled={isCreating}
                  />
                </div>
                {/* Departman artık serbest metin DEĞİL - backend, normal bir
                    kullanıcının sayfasını her zaman kendi (JWT'deki gerçek)
                    departmanına kaydediyor, istemcinin gönderdiği değeri yok
                    sayıyor (bkz. CreateWikiPageCommandHandler). Admin bu kuralın
                    dışında olduğu için sadece Admin bir seçim yapabiliyor. */}
                <div className="flex flex-col gap-1.5">
                  <Label>Departman</Label>
                  {isAdmin ? (
                    <RadioGroup value={department} onValueChange={setDepartment} className="flex flex-row gap-4">
                      {DEPARTMENTS.map((d) => (
                        <div key={d.value} className="flex items-center gap-2">
                          <RadioGroupItem
                            value={d.value}
                            id={`create-department-${d.value}`}
                            disabled={isCreating}
                          />
                          <Label htmlFor={`create-department-${d.value}`}>{d.label}</Label>
                        </div>
                      ))}
                    </RadioGroup>
                  ) : (
                    <p className="text-sm" style={{ color: "var(--text)" }}>
                      {ownDepartment
                        ? `Bu sayfa senin departmanına (${ownDepartment}) eklenecek.`
                        : "Departmanın olmadığı için sayfa oluşturamazsın."}
                    </p>
                  )}
                </div>

                {/* "Public"/"DepartmentOnly" string'leri backend'in beklediği
                    değerlerle BİREBİR aynı olmalı - Enum.Parse<WikiVisibility> bunu bekliyor. */}
                <div className="flex flex-col gap-1.5">
                  <Label>Görünürlük</Label>
                  <RadioGroup value={visibility} onValueChange={setVisibility} className="flex flex-row gap-4">
                    <div className="flex items-center gap-2">
                      <RadioGroupItem value="Public" id="visibility-public" disabled={isCreating} />
                      <Label htmlFor="visibility-public">Herkese Açık</Label>
                    </div>
                    <div className="flex items-center gap-2">
                      <RadioGroupItem value="DepartmentOnly" id="visibility-department" disabled={isCreating} />
                      <Label htmlFor="visibility-department">Sadece Departman</Label>
                    </div>
                  </RadioGroup>
                </div>

                {error && <p style={{ color: "red" }} className="text-sm">{error}</p>}

                <DialogFooter>
                  <Button
                    type="submit"
                    disabled={isCreating || (!isAdmin && !ownDepartment)}
                    className="bg-[var(--brand-accent)] text-[var(--text-h)] hover:opacity-90"
                  >
                    {isCreating ? "Ekleniyor..." : "Ekle"}
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
          <Button variant="outline" onClick={onLogout}>
            Çıkış Yap
          </Button>
        </div>
      </div>

      <WikiSearch token={token} />

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
                  <TableHead>İçerik</TableHead>
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
                    <TableCell className="font-medium">{p.title}</TableCell>
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
                    <TableCell className="max-w-xs whitespace-normal text-[var(--text)]/70">
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
                onClick={() => setPageNumber((p) => p - 1)}
              >
                ← Önceki
              </Button>
              <span className="text-sm" style={{ color: "var(--text)" }}>
                Sayfa {pageNumber} / {totalPages}
              </span>
              <Button
                variant="outline"
                disabled={pageNumber >= totalPages || isLoadingPages}
                onClick={() => setPageNumber((p) => p + 1)}
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
    </div>
  );
}

export default WikiBoard;
