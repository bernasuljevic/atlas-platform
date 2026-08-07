import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import { ChevronRight, FileText, Folder, Lock } from "lucide-react";
import { getWikiFolderTree } from "../api";
import { DEPARTMENTS } from "../departments";

// WikiFolderTree.jsx'teki AYNI kontrol (bkz. o dosyadaki not) - burada da
// aynı küçük, dosyaya özel yardımcı olarak tutuluyor, ortak bir util dosyası
// açmaya değecek kadar büyük bir mantık değil.
function hasPrivateContent(folder) {
  if (folder.pages.some((p) => p.visibility !== "Public")) return true;
  return folder.children.some(hasPrivateContent);
}

// WikiArticlePage'in findFolderPath'inden FARKLI olarak (sadece isim listesi
// döndürüyordu) burada hem hedef klasörün KENDİSİ hem üstündeki ata klasörlerin
// {id, name} çiftleri lazım - breadcrumb'taki her segmentin kendi linkine
// ihtiyacı var.
function findFolderNode(folders, targetId, path = []) {
  for (const folder of folders) {
    if (folder.id === targetId) return { folder, path };
    const found = findFolderNode(folder.children, targetId, [...path, { id: folder.id, name: folder.name }]);
    if (found) return found;
  }
  return null;
}

// WikiArticlePage'deki breadcrumb'ın "Departman > Klasör > ..." segmentleri
// eskiden sadece konum bilgisi veriyordu, tıklanamıyordu (kullanıcı geri
// bildirimi, 2026-08-05: "buradaki dosyalama yaptığımız şeylere tıklayabilelim
// istiyorum"). Bu sayfa o tıklamanın gittiği yer - aynı klasör ağacı verisini
// (WikiFolderTree'nin sidebar'da zaten kullandığı GetWikiFolderTreeQuery)
// yeniden çekip, hedef klasörün (ya da departman kökünün) doğrudan içeriğini
// (alt klasörler + sayfalar) listeliyor. Ayrı bir backend endpoint'i
// GEREKMEDİ - var olan ağaç sorgusu zaten tüm veriyi taşıyor, burada sadece
// istenen düğüme kadar client-side gezip o düğümü gösteriyoruz.
function WikiFolderBrowsePage({ token }) {
  const { departmentName, folderId } = useParams();
  const [tree, setTree] = useState(null);
  const [error, setError] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(null);

    getWikiFolderTree(token, departmentName)
      .then((result) => {
        if (!cancelled) setTree(result);
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
  }, [token, departmentName]);

  if (isLoading) {
    return <p style={{ color: "var(--text)" }}>Yükleniyor...</p>;
  }

  if (error || !tree) {
    return (
      <div>
        <p style={{ color: "red" }} className="mb-3 text-sm">
          {error ?? "Bu departman yüklenemedi."}
        </p>
        <Link to="/wiki" className="underline">
          Ana sayfaya dön
        </Link>
      </div>
    );
  }

  const deptLabel = DEPARTMENTS.find((d) => d.value === departmentName)?.label ?? departmentName;

  // folderId YOKSA (departman kökü) - tree'nin en üst seviyesi doğrudan
  // gösteriliyor. VARSA - hedef düğüm bulunup onun children/pages'i gösteriliyor.
  let currentFolder = null;
  let ancestorPath = [];
  let subfolders = tree.folders;
  let pages = tree.unfiledPages;

  if (folderId) {
    const found = findFolderNode(tree.folders, folderId);
    if (!found) {
      return (
        <div>
          <p style={{ color: "red" }} className="mb-3 text-sm">
            Bu klasör bulunamadı ya da görme yetkin yok.
          </p>
          <Link to={`/wiki/browse/${departmentName}`} className="underline">
            {deptLabel} Departmanına dön
          </Link>
        </div>
      );
    }
    currentFolder = found.folder;
    ancestorPath = found.path;
    subfolders = currentFolder.children;
    pages = currentFolder.pages;
  }

  const breadcrumbParts = [
    { label: `${deptLabel} Departmanı`, to: `/wiki/browse/${departmentName}` },
    ...ancestorPath.map((f) => ({ label: f.name, to: `/wiki/browse/${departmentName}/${f.id}` })),
    ...(currentFolder ? [{ label: currentFolder.name, to: null }] : []),
  ];

  return (
    <div className="mx-auto max-w-4xl text-left">
      <nav className="mb-2 flex flex-wrap items-center gap-1 text-xs" style={{ color: "var(--text)", opacity: 0.85 }}>
        {breadcrumbParts.map((part, idx) => (
          <span key={idx} className="flex items-center gap-1">
            {idx > 0 && <ChevronRight size={12} className="opacity-60" />}
            {part.to ? (
              <Link to={part.to} className="hover:underline" style={{ color: "var(--brand-accent)" }}>
                {part.label}
              </Link>
            ) : (
              <span className="font-semibold">{part.label}</span>
            )}
          </span>
        ))}
      </nav>

      <h1 className="mt-1 mb-4 text-2xl font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
        {currentFolder ? currentFolder.name : `${deptLabel} Departmanı`}
      </h1>

      {subfolders.length > 0 && (
        <section className="mb-6">
          <h2 className="mb-2 text-xs font-semibold tracking-wider uppercase" style={{ color: "var(--text-h)", opacity: 0.7 }}>
            Klasörler
          </h2>
          <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
            {subfolders.map((f) => (
              <Link
                key={f.id}
                to={`/wiki/browse/${departmentName}/${f.id}`}
                className="flex items-center gap-2 rounded-lg border p-3 text-sm hover:bg-[var(--brand-accent)]/5"
                style={{ borderColor: "var(--border)", color: "var(--text-h)" }}
              >
                <Folder size={16} className="shrink-0 opacity-70" />
                <span className="truncate">{f.name}</span>
                {hasPrivateContent(f) && <Lock size={13} className="ml-auto shrink-0 opacity-60" />}
                <ChevronRight size={14} className="shrink-0 opacity-40" style={{ marginLeft: hasPrivateContent(f) ? 0 : "auto" }} />
              </Link>
            ))}
          </div>
        </section>
      )}

      {pages.length > 0 && (
        <section>
          <h2 className="mb-2 text-xs font-semibold tracking-wider uppercase" style={{ color: "var(--text-h)", opacity: 0.7 }}>
            Sayfalar
          </h2>
          <div className="flex flex-col divide-y overflow-hidden rounded-lg border" style={{ borderColor: "var(--border)" }}>
            {pages.map((p) => (
              <Link
                key={p.id}
                to={`/wiki/${p.id}`}
                className="flex items-center gap-2 px-3 py-2.5 text-sm hover:bg-[var(--brand-accent)]/5"
                style={{ borderColor: "var(--border)", color: "var(--text-h)" }}
              >
                <FileText size={15} className="shrink-0 opacity-60" />
                <span className="truncate">{p.title}</span>
                {p.visibility !== "Public" && <Lock size={13} className="ml-auto shrink-0 opacity-60" />}
              </Link>
            ))}
          </div>
        </section>
      )}

      {subfolders.length === 0 && pages.length === 0 && (
        <p className="text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
          Burada henüz görebileceğin bir içerik yok.
        </p>
      )}
    </div>
  );
}

export default WikiFolderBrowsePage;
