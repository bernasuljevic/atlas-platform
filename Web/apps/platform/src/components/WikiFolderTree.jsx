import { useState, useEffect } from "react";
import { Link, useParams } from "react-router";
import { ChevronDown, FileText, Folder, Lock } from "lucide-react";
import { getWikiFolderTree } from "../api";
import { DEPARTMENTS } from "../departments";

// Bir klasörün (ya da alt ağacının) içinde en az bir "Sadece Departman"
// sayfası var mı - referans mockup'taki kilit ikonunun hangi klasörlerde
// göründüğünü belirliyor. Zaten çekilmiş ağaç verisi üzerinde çalışıyor,
// ekstra bir API çağrısı gerekmiyor.
function hasPrivateContent(folder) {
  if (folder.pages.some((p) => p.visibility !== "Public")) return true;
  return folder.children.some(hasPrivateContent);
}

function PageLink({ page }) {
  const { id } = useParams();
  const isActive = id === page.id;

  return (
    <Link
      to={`/wiki/${page.id}`}
      className="flex items-center gap-1.5 truncate rounded px-2 py-1 text-sm hover:bg-[var(--brand-accent)]/10"
      style={{
        color: isActive ? "var(--brand-accent)" : "var(--text)",
        fontWeight: isActive ? 600 : 400,
        background: isActive ? "var(--brand-accent-bg)" : "transparent",
      }}
      title={page.title}
    >
      <FileText size={14} className="shrink-0 opacity-60" />
      <span className="truncate">{page.title}</span>
    </Link>
  );
}

// Bir klasör düğümü - kendi alt klasörlerini ve sayfalarını iç içe render eder.
// Kök seviyedeki klasörler (depth=0) varsayılan açık, daha derindekiler kapalı
// başlıyor - çok derin ağaçlarda ilk açılışta ekranı doldurmasın diye.
function FolderNode({ folder, depth }) {
  const [isOpen, setIsOpen] = useState(depth === 0);
  const hasContent = folder.children.length > 0 || folder.pages.length > 0;
  const isPrivate = hasPrivateContent(folder);

  return (
    <div>
      <button
        type="button"
        onClick={() => setIsOpen((o) => !o)}
        className="flex w-full items-center gap-1.5 truncate rounded px-2 py-1 text-left text-sm hover:bg-[var(--brand-accent)]/10"
        style={{ paddingLeft: 8 + depth * 14, color: "var(--text-h)" }}
      >
        <ChevronDown
          size={13}
          className="shrink-0 opacity-50 transition-transform"
          style={{ transform: hasContent && isOpen ? "rotate(0deg)" : "rotate(-90deg)", visibility: hasContent ? "visible" : "hidden" }}
        />
        <Folder size={14} className="shrink-0 opacity-70" />
        <span className="truncate">{folder.name}</span>
        {isPrivate && <Lock size={12} className="ml-auto shrink-0 opacity-60" />}
      </button>
      {isOpen && (
        <div style={{ paddingLeft: depth * 14 }}>
          {folder.pages.map((p) => (
            <div key={p.id} style={{ paddingLeft: 22 }}>
              <PageLink page={p} />
            </div>
          ))}
          {folder.children.map((child) => (
            <FolderNode key={child.id} folder={child} depth={depth + 1} />
          ))}
        </div>
      )}
    </div>
  );
}

// Wikipedia'nın sol "Ana menü"süyle aynı fikir: her zaman görünen bir gezinme
// ağacı. departmentName'i kendisi state olarak tutuyor - varsayılan olarak
// kullanıcının kendi departmanıyla açılıyor ama üstteki seçiciden başka bir
// departmana geçilebiliyor (o zaman backend otomatik olarak Public'e budanmış
// bir görünüm döndürüyor, bkz. GetWikiFolderTreeQueryHandler).
function WikiFolderTree({ token, ownDepartment }) {
  const [department, setDepartment] = useState(ownDepartment ?? DEPARTMENTS[0].value);
  const [isPickerOpen, setIsPickerOpen] = useState(false);
  const [tree, setTree] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    setTree(null);
    setError(null);

    getWikiFolderTree(token, department)
      .then((result) => {
        if (!cancelled) setTree(result);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [token, department]);

  const currentLabel = DEPARTMENTS.find((d) => d.value === department)?.label ?? department;

  return (
    <nav className="flex flex-col gap-2">
      {/* Referans mockup'taki "IT Departmanı ⌄" seçici - tıklanınca küçük bir
          liste açılıyor, sadece 2 departman olduğu için ayrı bir shadcn Select
          bileşenine gerek görülmedi (bkz. WikiEditorPage'in klasör seçicisinde
          de kullanılan AYNI "elle yazılmış küçük liste" deseni). */}
      {DEPARTMENTS.length > 1 && (
        <div className="relative mb-1">
          <button
            type="button"
            onClick={() => setIsPickerOpen((o) => !o)}
            className="flex w-full items-center gap-2 rounded-lg border px-2.5 py-2 text-sm font-medium"
            style={{ borderColor: "var(--border)", color: "var(--text-h)" }}
          >
            <span
              className="flex h-6 w-6 shrink-0 items-center justify-center rounded text-xs font-semibold"
              style={{ background: "var(--brand-accent-bg)", color: "var(--brand-accent)" }}
            >
              {department.slice(0, 2)}
            </span>
            <span className="truncate">{currentLabel}</span>
            <ChevronDown size={14} className="ml-auto shrink-0 opacity-60" />
          </button>

          {isPickerOpen && (
            <div
              className="absolute z-10 mt-1 w-full overflow-hidden rounded-lg border"
              style={{ borderColor: "var(--border)", background: "var(--bg)" }}
            >
              {DEPARTMENTS.map((d) => (
                <button
                  key={d.value}
                  type="button"
                  onClick={() => {
                    setDepartment(d.value);
                    setIsPickerOpen(false);
                  }}
                  className="block w-full truncate px-3 py-2 text-left text-sm hover:bg-[var(--brand-accent)]/10"
                  style={{ color: d.value === department ? "var(--brand-accent)" : "var(--text)" }}
                >
                  {d.label}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {error && <p className="px-2 text-xs" style={{ color: "red" }}>{error}</p>}

      {tree && (
        <div className="flex flex-col">
          {tree.folders.map((folder) => (
            <FolderNode key={folder.id} folder={folder} depth={0} />
          ))}
          {tree.unfiledPages.map((p) => (
            <div key={p.id} style={{ paddingLeft: 8 }}>
              <PageLink page={p} />
            </div>
          ))}
          {tree.folders.length === 0 && tree.unfiledPages.length === 0 && (
            <p className="px-2 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
              Bu departmanda henüz görebileceğin bir içerik yok.
            </p>
          )}
        </div>
      )}
    </nav>
  );
}

export default WikiFolderTree;
