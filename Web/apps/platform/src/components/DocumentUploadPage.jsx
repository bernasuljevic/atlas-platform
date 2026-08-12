import { useMemo, useState } from "react";
import { useNavigate } from "react-router";
import { toast } from "sonner";
import { CheckCircle2, UploadCloud, X, XCircle } from "lucide-react";
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
//
// P6 (çoklu dosya yükleme) - Visibility/Departman/Açıklama/Etiket TÜM
// dosyalara ORTAK uygulanıyor (aynı seferde yüklenen dosyalar genelde aynı
// bağlamdan gelir, ör. "şu toplantının tüm ekleri") - Title İSTİSNA: birden
// fazla dosya seçiliyken TEK bir başlık alanı anlamsız olurdu, bu yüzden
// o durumda Title alanı GİZLENİYOR ve her dosya kendi adından (uzantısız)
// bir başlık alıyor (handleFilesSelect'teki TEK dosya kısayolunun AYNISI).
// Dosyalar backend'e SIRAYLA (paralel değil) yükleniyor - basit tutuldu,
// "N dosya aynı anda" durumunun disk/DB üzerindeki etkisini şimdilik
// karmaşıklaştırmaya değmedi (YAGNI).
function DocumentUploadPage({ token }) {
  const navigate = useNavigate();
  const { isAdmin, department: ownDepartment } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [files, setFiles] = useState([]);
  const [title, setTitle] = useState("");
  const [visibility, setVisibility] = useState("Public");
  const [department, setDepartment] = useState(isAdmin ? DEPARTMENTS[0].value : "");
  const [description, setDescription] = useState("");
  const [tags, setTags] = useState("");
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState(null);
  // Her dosya için ayrı bir durum - "uploading" | "done" | "error". Submit
  // sonrası formun ALTINDA kalıcı gösteriliyor, kullanıcı hangi dosyaların
  // başarısız olduğunu görebilsin diye (tek dosyalık eski akışın aksine,
  // burada otomatik yönlendirme yok - bkz. handleSubmit).
  const [uploadResults, setUploadResults] = useState([]);

  function handleFilesSelect(selectedFiles) {
    const fileArray = Array.from(selectedFiles ?? []);
    if (fileArray.length === 0) return;
    setFiles(fileArray);
    if (fileArray.length === 1 && !title.trim()) {
      setTitle(fileArray[0].name.replace(/\.[^/.]+$/, ""));
    }
  }

  function handleDrop(e) {
    e.preventDefault();
    setIsDragging(false);
    handleFilesSelect(e.dataTransfer.files);
  }

  function removeFile(index) {
    setFiles((prev) => prev.filter((_, i) => i !== index));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    if (files.length === 0) {
      setError("En az bir dosya seçmelisin.");
      return;
    }

    setError(null);
    setIsUploading(true);
    setUploadResults(files.map((f) => ({ fileName: f.name, status: "uploading" })));

    const uploadedIds = [];
    for (let i = 0; i < files.length; i++) {
      const currentFile = files[i];
      const fileTitle = files.length === 1 ? title : currentFile.name.replace(/\.[^/.]+$/, "");
      try {
        const result = await uploadDocument(token, {
          file: currentFile,
          title: fileTitle,
          visibility,
          departmentName: isAdmin ? department : undefined,
          description,
          tags,
        });
        uploadedIds.push(result.id);

        // P6 Gün 3 (duplicate-detection) - yükleme YİNE DE başarılı oldu,
        // bu sadece bir bilgilendirme (bkz. UploadDocumentResult'taki not).
        if (result.duplicateOfDocumentId) {
          toast(`"${currentFile.name}" zaten yüklü olabilir`, {
            description: `Aynı içerikli bir belge var: "${result.duplicateOfTitle}". Yine de yüklendi.`,
          });
        }

        setUploadResults((prev) => prev.map((r, idx) => (idx === i ? { ...r, status: "done" } : r)));
      } catch (err) {
        setUploadResults((prev) =>
          prev.map((r, idx) => (idx === i ? { ...r, status: "error", message: err.message } : r))
        );
      }
    }

    setIsUploading(false);

    // Tek dosyalık eski davranış KORUNDU (doğrudan detay sayfasına git) -
    // birden fazla dosyada yönlendirme YOK, kullanıcı sonuç listesini
    // görüp kendi seçtiği belgeye tıklayabilsin diye burada kalıyoruz.
    if (files.length === 1 && uploadedIds.length === 1) {
      navigate(`/documents/${uploadedIds[0]}`);
    }
  }

  const isMultiple = files.length > 1;

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
          {files.length === 0 ? (
            <p className="text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
              Dosyaları buraya sürükle ya da aşağıdan seç (birden fazla seçilebilir)
            </p>
          ) : (
            <p className="text-sm font-medium" style={{ color: "var(--text-h)" }}>
              {files.length} dosya seçildi
            </p>
          )}
          <Input type="file" multiple onChange={(e) => handleFilesSelect(e.target.files)} className="mt-1" />
        </div>

        {files.length > 0 && (
          <ul className="flex flex-col gap-1 rounded-lg border p-2" style={{ borderColor: "var(--border)" }}>
            {files.map((f, i) => (
              <li key={`${f.name}-${i}`} className="flex items-center justify-between gap-2 px-1 py-0.5 text-sm">
                <span className="truncate" style={{ color: "var(--text)" }}>
                  {f.name}
                </span>
                <button
                  type="button"
                  onClick={() => removeFile(i)}
                  disabled={isUploading}
                  className="shrink-0"
                  style={{ color: "var(--text)", opacity: 0.6 }}
                  aria-label={`${f.name} dosyasını kaldır`}
                >
                  <X size={14} />
                </button>
              </li>
            ))}
          </ul>
        )}

        {!isMultiple && (
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="upload-title">Başlık</Label>
            <Input id="upload-title" value={title} onChange={(e) => setTitle(e.target.value)} disabled={isUploading} required />
          </div>
        )}
        {isMultiple && (
          <p className="text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
            Birden fazla dosya seçtiğin için her biri kendi dosya adından bir başlık alacak - yüklendikten sonra
            istediğin belgeyi ayrı ayrı düzenleyebilirsin.
          </p>
        )}

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
              ? `Bu belge(ler) senin departmanına (${ownDepartment}) eklenecek.`
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
            {isUploading ? "Yükleniyor..." : files.length > 1 ? `${files.length} Dosyayı Yükle` : "Yükle"}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate("/documents")}>
            {uploadResults.length > 0 ? "Belgelere dön" : "Vazgeç"}
          </Button>
        </div>

        {uploadResults.length > 0 && (
          <ul className="flex flex-col gap-1 rounded-lg border p-2" style={{ borderColor: "var(--border)" }}>
            {uploadResults.map((r, i) => (
              <li key={`${r.fileName}-${i}`} className="flex items-center gap-2 px-1 py-0.5 text-sm">
                {r.status === "uploading" && (
                  <span className="h-3.5 w-3.5 shrink-0 animate-pulse rounded-full" style={{ background: "var(--brand-accent)" }} />
                )}
                {r.status === "done" && <CheckCircle2 size={14} className="shrink-0" style={{ color: "green" }} />}
                {r.status === "error" && <XCircle size={14} className="shrink-0" style={{ color: "red" }} />}
                <span className="truncate" style={{ color: "var(--text)" }}>
                  {r.fileName}
                </span>
                {r.status === "error" && (
                  <span className="truncate text-xs" style={{ color: "red" }}>
                    · {r.message}
                  </span>
                )}
              </li>
            ))}
          </ul>
        )}
      </form>
    </div>
  );
}

export default DocumentUploadPage;
