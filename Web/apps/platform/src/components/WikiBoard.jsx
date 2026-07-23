import { useState, useEffect } from "react";
import { getWikiPages, createWikiPage } from "../api";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

function WikiBoard({ token, onLogout }) {
  const [pages, setPages] = useState([]);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [department, setDepartment] = useState("IT");
  const [visibility, setVisibility] = useState("Public");
  const [error, setError] = useState(null);
  const [isLoadingPages, setIsLoadingPages] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  useEffect(() => {
    loadPages();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadPages() {
    setIsLoadingPages(true);
    try {
      // Departman filtresi artık burada seçilebilir bir şey değil - backend,
      // token'daki kullanıcının GERÇEK departmanına göre otomatik filtreliyor
      // (bkz. GetWikiPagesQueryHandler). Önceden serbest metin olarak
      // gönderilebiliyordu, bu da başka departmanların içeriğini tahmin ederek
      // görebilmeyi mümkün kılan bir güvenlik açığıydı.
      const data = await getWikiPages(token);
      setPages(data);
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
      await loadPages();
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
                  <TableRow key={p.id}>
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
                    <TableCell className="max-w-xs whitespace-normal">{p.content}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default WikiBoard;
