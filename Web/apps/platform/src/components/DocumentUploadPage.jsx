import { useMemo, useState } from "react";
import { useNavigate } from "react-router";
import { UploadCloud } from "lucide-react";
import { uploadDocument } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { DEPARTMENTS } from "../departments";
import { Button } from "@atlas/ui/button";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { RadioGroup, RadioGroupItem } from "@atlas/ui/radio-group";

// WikiEditorPage/VaultEntryFormPage ile AYNI "tam sayfa, dialog değil"
// deseni - bu proje geçmişte oluşturma akışlarını Dialog'dan tam sayfaya
// taşıdı (bkz. WikiBoard.jsx'teki "CreateWikiPageDialog kaldırıldı" notu),
// burada da aynı, kurulu desene uyuluyor.
function DocumentUploadPage({ token }) {
  const navigate = useNavigate();
  const { isAdmin, department: ownDepartment } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [file, setFile] = useState(null);
  const [title, setTitle] = useState("");
  const [visibility, setVisibility] = useState("Public");
  const [department, setDepartment] = useState(isAdmin ? DEPARTMENTS[0].value : "");
  const [description, setDescription] = useState("");
  const [tags, setTags] = useState("");
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState(null);

  function handleFileSelect(selectedFile) {
    if (!selectedFile) return;
    setFile(selectedFile);
    // Kullanıcı başlığı elle yazmadıysa dosya adından öneriyoruz - WikiEditorPage'in
    // kırmızı linkten gelen başlığı önceden doldurmasıyla AYNI kolaylık fikri.
    if (!title.trim()) {
      setTitle(selectedFile.name.replace(/\.[^/.]+$/, ""));
    }
  }

  function handleDrop(e) {
    e.preventDefault();
    setIsDragging(false);
    handleFileSelect(e.dataTransfer.files?.[0]);
  }

  async function handleSubmit(e) {
    e.preventDefault();
    if (!file) {
      setError("Bir dosya seçmelisin.");
      return;
    }

    setError(null);
    setIsUploading(true);
    try {
      const result = await uploadDocument(token, {
        file,
        title,
        visibility,
        departmentName: isAdmin ? department : undefined,
        description,
        tags,
      });
      navigate(`/documents/${result.id}`);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsUploading(false);
    }
  }

  return (
    <div className="mx-auto max-w-xl text-left">
      <h1 className="mb-6 text-2xl font-medium" style={{ color: "var(--text-h)" }}>
        Belge Yükle
      </h1>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div
          onDragOver={(e) => {
            e.preventDefault();
            setIsDragging(true);
          }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={handleDrop}
          className="flex flex-col items-center gap-2 rounded-lg border-2 border-dashed p-8 text-center"
          style={{
            borderColor: isDragging ? "var(--brand-accent)" : "var(--border)",
            background: isDragging ? "color-mix(in srgb, var(--brand-accent) 10%, transparent)" : "transparent",
          }}
        >
          <UploadCloud size={28} style={{ color: "var(--text)", opacity: 0.5 }} />
          {file ? (
            <p className="text-sm font-medium" style={{ color: "var(--text-h)" }}>
              {file.name}
            </p>
          ) : (
            <p className="text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
              Dosyayı buraya sürükle ya da aşağıdan seç
            </p>
          )}
          <Input type="file" onChange={(e) => handleFileSelect(e.target.files?.[0])} className="mt-1" />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="upload-title">Başlık</Label>
          <Input id="upload-title" value={title} onChange={(e) => setTitle(e.target.value)} disabled={isUploading} required />
        </div>

        {isAdmin ? (
          <div className="flex flex-col gap-1.5">
            <Label>Departman</Label>
            <RadioGroup value={department} onValueChange={setDepartment} className="flex flex-row gap-4">
              {DEPARTMENTS.map((d) => (
                <div key={d.value} className="flex items-center gap-2">
                  <RadioGroupItem value={d.value} id={`upload-department-${d.value}`} disabled={isUploading} />
                  <Label htmlFor={`upload-department-${d.value}`}>{d.label}</Label>
                </div>
              ))}
            </RadioGroup>
          </div>
        ) : (
          <p className="text-sm" style={{ color: "var(--text)" }}>
            {ownDepartment
              ? `Bu belge senin departmanına (${ownDepartment}) eklenecek.`
              : "Departmanın olmadığı için belge yükleyemezsin."}
          </p>
        )}

        <div className="flex flex-col gap-1.5">
          <Label>Görünürlük</Label>
          <RadioGroup value={visibility} onValueChange={setVisibility} className="flex flex-row gap-4">
            <div className="flex items-center gap-2">
              <RadioGroupItem value="Public" id="upload-visibility-public" disabled={isUploading} />
              <Label htmlFor="upload-visibility-public">Herkese Açık</Label>
            </div>
            <div className="flex items-center gap-2">
              <RadioGroupItem value="DepartmentOnly" id="upload-visibility-department" disabled={isUploading} />
              <Label htmlFor="upload-visibility-department">Sadece Departman</Label>
            </div>
          </RadioGroup>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="upload-description">Açıklama</Label>
          <Input id="upload-description" value={description} onChange={(e) => setDescription(e.target.value)} disabled={isUploading} />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="upload-tags">Etiketler</Label>
          <Input
            id="upload-tags"
            placeholder="virgülle ayırarak yazın, ör. sozlesme, hukuk"
            value={tags}
            onChange={(e) => setTags(e.target.value)}
            disabled={isUploading}
          />
        </div>

        {error && <p style={{ color: "red" }} className="text-sm">{error}</p>}

        <div className="flex gap-2">
          <Button
            type="submit"
            disabled={isUploading || (!isAdmin && !ownDepartment)}
            className="text-white hover:opacity-90"
            style={{ background: "var(--brand-accent)" }}
          >
            {isUploading ? "Yükleniyor..." : "Yükle"}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate("/documents")}>
            Vazgeç
          </Button>
        </div>
      </form>
    </div>
  );
}

export default DocumentUploadPage;
