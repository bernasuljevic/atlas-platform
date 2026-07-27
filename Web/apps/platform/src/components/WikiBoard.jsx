import { useState, useEffect } from "react";
import { getWikiPages, createWikiPage } from "../api";
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
  const [pages, setPages] = useState([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [department, setDepartment] = useState("IT");
  const [visibility, setVisibility] = useState("Public");
  const [error, setError] = useState(null);
  const [isLoadingPages, setIsLoadingPages] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [selectedPage, setSelectedPage] = useState(null);

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
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="wiki-department">Departman</Label>
                  <Input
                    id="wiki-department"
                    value={department}
                    onChange={(e) => setDepartment(e.target.value)}
                    disabled={isCreating}
                  />
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
                    disabled={isCreating}
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
