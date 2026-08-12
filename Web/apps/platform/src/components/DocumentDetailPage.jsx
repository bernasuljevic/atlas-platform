import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { toast } from "sonner";
import { History } from "lucide-react";
import {
  deleteDocument,
  downloadDocument,
  downloadDocumentVersion,
  getDocumentById,
  getDocumentVersions,
  updateDocument,
  uploadNewDocumentVersion,
} from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { formatFileSize, getDocumentIcon, getDocumentTypeLabel } from "../documentIcons";
import { Button } from "@atlas/ui/button";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { Badge } from "@atlas/ui/badge";
import { RadioGroup, RadioGroupItem } from "@atlas/ui/radio-group";

const STATUS_LABELS = { Uploaded: "Yüklendi", Extracting: "İşleniyor", Ready: "Hazır", Failed: "Başarısız" };

// Wiki'nin "Düzenle" ayrı bir SAYFAYA gitmesinin AKSİNE - burada düzenleme
// AYNI sayfada, inline bir form olarak açılıyor. Gerekçe: Document'ın
// düzenlenebilir alanları (Başlık/Açıklama/Görünürlük/Etiket) sadece 4 tane,
// Wiki'nin tam markdown editörüyle KIYASLANAMAYACAK kadar küçük - ayrı bir
// route/component açmak bu boyuttaki bir form için gereksiz bir katman olurdu.
function DocumentDetailPage({ token }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const { userId, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [doc, setDoc] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const [editTitle, setEditTitle] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [editVisibility, setEditVisibility] = useState("Public");
  const [editTags, setEditTags] = useState("");

  // P6 (versiyonlama) - versiyon geçmişi (SADECE eski versiyonlar, bkz.
  // GetDocumentVersionsQuery'deki not) belgeden AYRI, kendi state'inde.
  const [versions, setVersions] = useState([]);
  const [isNewVersionFormOpen, setIsNewVersionFormOpen] = useState(false);
  const [newVersionFile, setNewVersionFile] = useState(null);
  const [isUploadingVersion, setIsUploadingVersion] = useState(false);

  useEffect(() => {
    let cancelled = false;
    getDocumentById(token, id)
      .then((result) => {
        if (cancelled) return;
        setDoc(result);
        setEditTitle(result.title);
        setEditDescription(result.description ?? "");
        setEditVisibility(result.visibility);
        setEditTags(result.tags ?? "");
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

  // Ayrı bir effect - belge yüklenemese bile (404/403) versiyon isteği hiç
  // atılmasın diye "doc" state'ine bağlı, "id" yerine.
  useEffect(() => {
    if (!doc) return;
    let cancelled = false;
    getDocumentVersions(token, id)
      .then((result) => {
        if (!cancelled) setVersions(result);
      })
      .catch(() => {
        // Sessizce boş bırak - versiyon geçmişi görüntülenemezse ana belge
        // görünümünü hiç engellememeli, sadece o bölüm boş kalır.
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [doc, token, id]);

  async function handleDownloadVersion(versionNumber) {
    try {
      await downloadDocumentVersion(token, id, versionNumber);
    } catch (err) {
      toast("Versiyon indirilemedi", { description: err.message });
    }
  }

  async function handleUploadNewVersion(e) {
    e.preventDefault();
    if (!newVersionFile) return;

    setIsUploadingVersion(true);
    try {
      await uploadNewDocumentVersion(token, id, newVersionFile);
      toast("Yeni versiyon yüklendi", {
        description: "Belge yeniden işleniyor, birkaç saniye içinde arama sonuçlarına yansıyacak.",
      });
      setIsNewVersionFormOpen(false);
      setNewVersionFile(null);
      // Belge (Status/CurrentVersionNumber değişti) ve versiyon geçmişi
      // (bir satır daha eklendi) YENİDEN çekiliyor - iyimser (optimistic)
      // bir state güncellemesi YAPMIYORUZ, sunucudan dönen GERÇEK hali
      // gösteriyoruz (Status'un Uploaded'a döndüğünü doğru yansıtsın diye).
      const [refreshedDoc, refreshedVersions] = await Promise.all([
        getDocumentById(token, id),
        getDocumentVersions(token, id),
      ]);
      setDoc(refreshedDoc);
      setVersions(refreshedVersions);
    } catch (err) {
      toast("Yeni versiyon yüklenemedi", { description: err.message });
    } finally {
      setIsUploadingVersion(false);
    }
  }

  async function handleDownload() {
    try {
      await downloadDocument(token, id);
    } catch (err) {
      toast("Belge indirilemedi", { description: err.message });
    }
  }

  async function handleSave(e) {
    e.preventDefault();
    setIsSaving(true);
    try {
      await updateDocument(token, id, {
        title: editTitle,
        description: editDescription,
        visibility: editVisibility,
        tags: editTags,
      });
      setDoc((prev) => ({
        ...prev,
        title: editTitle,
        description: editDescription,
        visibility: editVisibility,
        tags: editTags,
        updatedAtUtc: new Date().toISOString(),
      }));
      setIsEditing(false);
    } catch (err) {
      toast("Belge güncellenemedi", { description: err.message });
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete() {
    if (!window.confirm(`"${doc.title}" belgesini silmek istediğine emin misin?`)) return;
    setIsDeleting(true);
    try {
      await deleteDocument(token, id);
      navigate("/documents");
    } catch (err) {
      toast("Belge silinemedi", { description: err.message });
      setIsDeleting(false);
    }
  }

  if (isLoading) {
    return <p style={{ color: "var(--text)" }}>Yükleniyor...</p>;
  }

  if (error || !doc) {
    return (
      <div>
        <p style={{ color: "red" }} className="mb-3 text-sm">
          {error ?? "Bu belge artık mevcut değil ya da görme yetkin yok."}
        </p>
        <Link to="/documents" className="underline">
          Belgelere dön
        </Link>
      </div>
    );
  }

  const canEdit = isAdmin || doc.createdByUserId === userId;
  const Icon = getDocumentIcon(doc.fileExtension);

  return (
    <div className="mx-auto max-w-2xl text-left">
      <Link to="/documents" className="mb-4 inline-block text-sm hover:underline" style={{ color: "var(--brand-accent)" }}>
        ← Belgelere dön
      </Link>

      <div className="mb-4 flex items-start gap-3">
        <Icon size={32} className="shrink-0" style={{ color: "var(--brand-accent)" }} />
        <div className="min-w-0">
          <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
            {doc.title}
          </h1>
          <p className="text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
            {getDocumentTypeLabel(doc.fileExtension)} · {formatFileSize(doc.sizeBytes)}
          </p>
        </div>
      </div>

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Badge variant="outline">{doc.departmentName}</Badge>
        <Badge variant="outline">{doc.visibility === "Public" ? "Herkese Açık" : "Sadece Departman"}</Badge>
        <Badge variant="outline">{STATUS_LABELS[doc.status] ?? doc.status}</Badge>
        <Badge variant="outline">Versiyon {doc.currentVersionNumber}</Badge>
      </div>

      {!isEditing && doc.description && (
        <p className="mb-4 text-sm" style={{ color: "var(--text)" }}>
          {doc.description}
        </p>
      )}
      {!isEditing && doc.tags && (
        <div className="mb-4 flex flex-wrap gap-1.5">
          {doc.tags.split(",").map((tag) => (
            <Badge key={tag} variant="outline" className="text-[11px] font-normal">
              {tag}
            </Badge>
          ))}
        </div>
      )}

      <p className="mb-6 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
        {doc.createdByEmail ?? "Bilinmiyor"} tarafından {formatUtcTimestamp(doc.createdAtUtc)} tarihinde yüklendi
        {doc.updatedAtUtc && ` · Güncellendi ${formatUtcTimestamp(doc.updatedAtUtc)}`}
      </p>

      {isEditing ? (
        <form onSubmit={handleSave} className="flex flex-col gap-4 rounded-lg border p-4" style={{ borderColor: "var(--border)" }}>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-title">Başlık</Label>
            <Input id="edit-title" value={editTitle} onChange={(e) => setEditTitle(e.target.value)} disabled={isSaving} required />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-description">Açıklama</Label>
            <Input id="edit-description" value={editDescription} onChange={(e) => setEditDescription(e.target.value)} disabled={isSaving} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>Görünürlük</Label>
            <RadioGroup value={editVisibility} onValueChange={setEditVisibility} className="flex flex-row gap-4">
              <div className="flex items-center gap-2">
                <RadioGroupItem value="Public" id="edit-visibility-public" disabled={isSaving} />
                <Label htmlFor="edit-visibility-public">Herkese Açık</Label>
              </div>
              <div className="flex items-center gap-2">
                <RadioGroupItem value="DepartmentOnly" id="edit-visibility-department" disabled={isSaving} />
                <Label htmlFor="edit-visibility-department">Sadece Departman</Label>
              </div>
            </RadioGroup>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-tags">Etiketler</Label>
            <Input id="edit-tags" value={editTags} onChange={(e) => setEditTags(e.target.value)} disabled={isSaving} />
          </div>
          <div className="flex gap-2">
            <Button type="submit" disabled={isSaving} className="text-white hover:opacity-90" style={{ background: "var(--brand-accent)" }}>
              {isSaving ? "Kaydediliyor..." : "Kaydet"}
            </Button>
            <Button type="button" variant="outline" onClick={() => setIsEditing(false)} disabled={isSaving}>
              Vazgeç
            </Button>
          </div>
        </form>
      ) : (
        <div className="flex gap-2">
          <Button onClick={handleDownload} className="text-white hover:opacity-90" style={{ background: "var(--brand-accent)" }}>
            İndir
          </Button>
          {canEdit && (
            <>
              <Button variant="outline" onClick={() => setIsEditing(true)}>
                Düzenle
              </Button>
              <Button variant="outline" onClick={() => setIsNewVersionFormOpen((prev) => !prev)}>
                Yeni Versiyon Yükle
              </Button>
              <Button variant="destructive" disabled={isDeleting} onClick={handleDelete}>
                {isDeleting ? "Siliniyor..." : "Sil"}
              </Button>
            </>
          )}
        </div>
      )}

      {isNewVersionFormOpen && (
        <form
          onSubmit={handleUploadNewVersion}
          className="mt-4 flex flex-col gap-3 rounded-lg border p-4"
          style={{ borderColor: "var(--border)" }}
        >
          <p className="text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
            Yeni dosya, mevcut "{doc.title}" belgesinin yerine geçecek - eski dosya versiyon geçmişinde
            ({`Versiyon ${doc.currentVersionNumber}`}) kalıcı olarak saklanmaya devam edecek.
          </p>
          <Input
            type="file"
            onChange={(e) => setNewVersionFile(e.target.files?.[0] ?? null)}
            disabled={isUploadingVersion}
          />
          <div className="flex gap-2">
            <Button
              type="submit"
              disabled={isUploadingVersion || !newVersionFile}
              className="text-white hover:opacity-90"
              style={{ background: "var(--brand-accent)" }}
            >
              {isUploadingVersion ? "Yükleniyor..." : "Yükle"}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setIsNewVersionFormOpen(false);
                setNewVersionFile(null);
              }}
              disabled={isUploadingVersion}
            >
              Vazgeç
            </Button>
          </div>
        </form>
      )}

      {versions.length > 0 && (
        <div className="mt-6">
          <h2 className="mb-2 flex items-center gap-1.5 text-sm font-medium" style={{ color: "var(--text-h)" }}>
            <History size={16} />
            Versiyon Geçmişi
          </h2>
          <ul className="flex flex-col gap-1.5">
            {versions.map((v) => (
              <li
                key={v.versionNumber}
                className="flex flex-wrap items-center justify-between gap-2 rounded-lg border px-3 py-2 text-sm"
                style={{ borderColor: "var(--border)" }}
              >
                <div className="min-w-0">
                  <span className="font-medium" style={{ color: "var(--text-h)" }}>
                    Versiyon {v.versionNumber}
                  </span>
                  <span className="ml-2 truncate" style={{ color: "var(--text)", opacity: 0.7 }}>
                    {v.originalFileName} · {formatFileSize(v.sizeBytes)}
                  </span>
                  <p className="text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
                    {v.createdByEmail ?? "Bilinmiyor"} tarafından {formatUtcTimestamp(v.createdAtUtc)} tarihinde
                    değiştirildi
                  </p>
                </div>
                <Button variant="outline" size="sm" onClick={() => handleDownloadVersion(v.versionNumber)}>
                  İndir
                </Button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

export default DocumentDetailPage;
