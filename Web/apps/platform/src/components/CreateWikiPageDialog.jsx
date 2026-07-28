import { useState } from "react";
import { createWikiPage } from "../api";
import { DEPARTMENTS } from "../departments";
import { Button } from "@atlas/ui/button";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { Textarea } from "@atlas/ui/textarea";
import { RadioGroup, RadioGroupItem } from "@atlas/ui/radio-group";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@atlas/ui/dialog";

// WikiBoard.jsx'ten ayrıldı (bkz. CLAUDE.md "İzlenecek teknik borç") - "Yeni
// Sayfa" formunun kendi state'i (title/content/department/visibility/hata)
// ve API çağrısı burada, tamamen kendi kendine yetiyor. Parent'a sadece
// başarılı bir oluşturmadan sonra listeyi yenilemesi için onCreated ile haber veriyor.
function CreateWikiPageDialog({ token, isAdmin, ownDepartment, onCreated }) {
  const [isOpen, setIsOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  // Admin herhangi bir departmanı seçebiliyor (varsayılan ilk departman);
  // normal kullanıcı için bu değerin önemi yok - backend zaten kendi
  // departmanını zorluyor, formda sadece bilgilendirme metni gösteriyoruz.
  const [department, setDepartment] = useState(isAdmin ? DEPARTMENTS[0].value : "");
  const [visibility, setVisibility] = useState("Public");
  const [error, setError] = useState(null);
  const [isCreating, setIsCreating] = useState(false);

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
      setIsOpen(false);
      await onCreated();
    } catch (err) {
      setError(err.message.includes("giriş") ? err.message : "Sayfa oluşturulamadı: " + err.message);
    } finally {
      setIsCreating(false);
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
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
  );
}

export default CreateWikiPageDialog;
