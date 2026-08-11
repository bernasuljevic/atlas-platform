import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router";
import {
  Code2,
  Heading2,
  Image as ImageIcon,
  Info,
  ListChecks,
  Minus,
  Table2,
  Video,
} from "lucide-react";
import {
  createWikiFolder,
  createWikiPage,
  getWikiFolderTree,
  getWikiPageById,
  getWikiSearchSuggestions,
  updateWikiPage,
} from "../api";
import { getUserInfoFromToken } from "../jwt";
import { DEPARTMENTS } from "../departments";
import { Button } from "@atlas/ui/button";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { Textarea } from "@atlas/ui/textarea";
import { RadioGroup, RadioGroupItem } from "@atlas/ui/radio-group";
import SlashCommandMenu from "./SlashCommandMenu";

const CONTENT_TEXTAREA_ID = "wiki-editor-content";
const LINK_SEARCH_DEBOUNCE_MS = 250;

// Tablo düğmesi seçili metni SARMALAMIYOR (bir tabloyu seçili metnin içine
// "sarmak" anlamsız olurdu) - sabit bir şablon ekliyor, kullanıcı hücreleri
// elle dolduruyor.
const TABLE_TEMPLATE =
  "\n| Sütun 1 | Sütun 2 |\n| --- | --- |\n| Değer 1 | Değer 2 |\n";

// Faz 1 (2026-08-08, "Atlas İçerik Sistemi" spec'i - "3.3 Callout blokları") -
// dört tip, markdown.jsx'teki CALLOUT_CONFIG ile BİREBİR aynı anahtar/sıra
// (`info`/`warning`/`error`/`success`) - buradaki değer, üretilen ":::<value>"
// açılış satırına doğrudan yazılıyor, ikisi senkron kalmalı.
const CALLOUT_TYPE_OPTIONS = [
  { value: "info", label: "💡 Bilgi" },
  { value: "warning", label: "⚠️ Uyarı" },
  { value: "error", label: "❌ Hata" },
  { value: "success", label: "✅ Başarılı" },
];

// Faz 2 (2026-08-11, "Kapsamlı Geliştirme Paketi" - editör blok genişletmesi
// v2) - "/" slash-command menüsünün listesi. Her öğe toolbar düğmelerinin
// ZATEN kullandığı before/after/placeholder üçlüsüyle applyToolbarInsert'i
// tetikliyor (bkz. handleSlashSelect) - YENİ bir ekleme mekanizması icat
// edilmedi, sadece toolbar'ın dışında ikinci bir tetikleyici eklendi.
// Dosya-referans (`/file`) BİLEREK YOK - Documents modülü henüz kurulmadı,
// referans verilecek gerçek bir belge/GUID yok (bkz. proje notu).
const SLASH_ITEMS = [
  { key: "heading", label: "Başlık", icon: Heading2, before: "## ", after: "", placeholder: "Başlık" },
  { key: "code", label: "Kod Bloğu", icon: Code2, before: "```\n", after: "\n```", placeholder: "kod" },
  { key: "table", label: "Tablo", icon: Table2, before: TABLE_TEMPLATE, after: "", placeholder: "" },
  { key: "callout", label: "Callout", icon: Info, before: ":::info\n", after: "\n:::", placeholder: "Metin" },
  { key: "checklist", label: "Checklist", icon: ListChecks, before: "- [ ] ", after: "", placeholder: "Yapılacak" },
  { key: "divider", label: "Ayraç", icon: Minus, before: "\n---\n", after: "", placeholder: "" },
  {
    key: "video",
    label: "Video",
    icon: Video,
    before: ":::video\n",
    after: "\n:::",
    placeholder: "https://www.youtube.com/watch?v=... veya video dosyası URL'si",
  },
  {
    key: "image",
    label: "Resim (Sola Hizalı)",
    icon: ImageIcon,
    before: ":::image-left\n![",
    after: "](https://...)\n:::",
    placeholder: "Açıklama",
  },
];

// Textarea'ya React ref yerine DOM id'siyle erişiyoruz - @atlas/ui/textarea
// forwardRef kullanmıyor, ref buraya kadar güvenilir şekilde ulaşmayabilirdi.
// document.getElementById basit, garanti çalışan bir alternatif.
function insertAtCursor(before, after, placeholder) {
  const el = document.getElementById(CONTENT_TEXTAREA_ID);
  if (!el) return null;

  const start = el.selectionStart;
  const end = el.selectionEnd;
  const value = el.value;
  const selected = value.slice(start, end) || placeholder;
  const newValue = value.slice(0, start) + before + selected + after + value.slice(end);
  const cursorPos = start + before.length + selected.length + after.length;

  return { newValue, cursorPos };
}

function restoreFocusAndCursor(cursorPos) {
  const el = document.getElementById(CONTENT_TEXTAREA_ID);
  if (!el) return;
  el.focus();
  el.setSelectionRange(cursorPos, cursorPos);
}

function flattenFolders(folders, depth = 0, acc = []) {
  for (const folder of folders) {
    acc.push({ id: folder.id, name: folder.name, depth });
    flattenFolders(folder.children, depth + 1, acc);
  }
  return acc;
}

// Hem "Yeni Sayfa" (/wiki/new) hem "Düzenle" (/wiki/:id/edit) TEK bileşen -
// aradaki fark sadece: edit modunda var olan sayfa önce çekilip alanlar
// dolduruluyor, departman hiç değiştirilemiyor (bkz. UpdateWikiPageCommand'daki
// not) ve kaydedince PUT'a gidiyor. Araç çubuğu Wikipedia'nın editör
// araç çubuğuyla aynı fikir (bkz. kullanıcının referans ekran görüntüsü) ama
// WYSIWYG değil - üretilen sözdizimi WikiArticlePage'in render ettiği hafif
// markdown alt kümesiyle (Gün E) birebir eşleşiyor.
function WikiEditorPage({ token }) {
  const { id: pageId } = useParams();
  const isEditMode = Boolean(pageId);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { isAdmin, department: ownDepartment } = useMemo(() => getUserInfoFromToken(token), [token]);

  // Kırmızı linke tıklanınca (bkz. markdown.jsx'teki "wiki-new:" hedefi)
  // buraya "?title=..." ile geliniyor - başlık alanı önceden dolduruluyor,
  // tıpkı Wikipedia'da olduğu gibi. Sadece oluşturma modunda anlamlı (edit
  // modunda aşağıdaki fetch zaten title'ı gerçek sayfa başlığıyla eziyor).
  const [title, setTitle] = useState(() => searchParams.get("title") ?? "");
  const [content, setContent] = useState("");
  const [visibility, setVisibility] = useState("Public");
  const [department, setDepartment] = useState(isAdmin ? DEPARTMENTS[0].value : "");
  const [folderId, setFolderId] = useState(null);
  const [newFolderName, setNewFolderName] = useState("");
  const [tags, setTags] = useState("");

  const [tree, setTree] = useState(null);
  const [isLoadingExisting, setIsLoadingExisting] = useState(isEditMode);
  const [isSaving, setIsSaving] = useState(false);
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);
  const [error, setError] = useState(null);
  const [isLinkPopoverOpen, setIsLinkPopoverOpen] = useState(false);
  const [isImagePopoverOpen, setIsImagePopoverOpen] = useState(false);
  const [headingLevel, setHeadingLevel] = useState(2);
  const [calloutType, setCalloutType] = useState("info");
  const [imageAlign, setImageAlign] = useState("left");
  // Slash-command menüsü - triggerPos bir state DEĞİL bir ref, çünkü
  // handleSlashSelect içinde SENKRON olarak (bir sonraki render'ı beklemeden)
  // okunması gerekiyor - "/" karakterinin textarea'daki TAM konumu.
  const [isSlashMenuOpen, setIsSlashMenuOpen] = useState(false);
  const slashTriggerPosRef = useRef(null);
  const [linkText, setLinkText] = useState("");
  const [linkTarget, setLinkTarget] = useState("");
  const [imageUrl, setImageUrl] = useState("");
  const [imageAlt, setImageAlt] = useState("");
  const [linkSearchQuery, setLinkSearchQuery] = useState("");
  const [linkSearchResults, setLinkSearchResults] = useState([]);
  const [isSearchingLink, setIsSearchingLink] = useState(false);
  const linkSearchDebounceRef = useRef(null);

  const [existingDepartment, setExistingDepartment] = useState(null);

  // Düzenleme modunda departman sabit (var olan sayfanın departmanı) - Admin
  // dahil kimse değiştiremiyor. Oluşturma modunda: Admin seçebiliyor, normal
  // kullanıcı için kendi departmanı.
  const effectiveDepartment = isEditMode ? existingDepartment : (isAdmin ? department : ownDepartment);

  useEffect(() => {
    if (!isEditMode) return;
    let cancelled = false;

    getWikiPageById(token, pageId)
      .then((page) => {
        if (cancelled) return;
        setTitle(page.title);
        setContent(page.content);
        setVisibility(page.visibility);
        setFolderId(page.folderId);
        setExistingDepartment(page.departmentName);
        setTags(page.tags ?? "");
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setIsLoadingExisting(false);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEditMode, pageId, token]);

  async function reloadTree(dept) {
    if (!dept) return;
    try {
      const result = await getWikiFolderTree(token, dept);
      setTree(result);
    } catch {
      // Klasör ağacı yüklenemese bile sayfa yine de klasörsüz oluşturulabilsin -
      // bu, sayfa oluşturmayı engelleyecek kritik bir hata değil.
      setTree({ folders: [], unfiledPages: [] });
    }
  }

  useEffect(() => {
    reloadTree(effectiveDepartment);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [effectiveDepartment]);

  useEffect(() => {
    return () => clearTimeout(linkSearchDebounceRef.current);
  }, []);

  function applyToolbarInsert(before, after, placeholder) {
    const result = insertAtCursor(before, after, placeholder);
    if (!result) return;
    setContent(result.newValue);
    // Textarea kontrollü olduğu için değer state güncellenince yeniden render
    // oluyor - imleç konumunu geri koymak için bir sonraki tick'i bekliyoruz.
    setTimeout(() => restoreFocusAndCursor(result.cursorPos), 0);
  }

  // İmleç bir satırın EN BAŞINDA "/" yazınca slash menüsünü açıyor - piksel
  // konumu takip ETMİYORUZ (bkz. SlashCommandMenu.jsx'teki mimari not), sadece
  // "kullanıcı yeni bir satırın başında / yazdı mı" kontrolü yeterli.
  // Kullanıcı başka bir şey yazmaya devam ederse (örn. "/x") menü kapanıyor -
  // yeniden "/" yazması gerekir.
  function handleContentChange(e) {
    const value = e.target.value;
    const cursorPos = e.target.selectionStart;
    setContent(value);

    const charBeforeCursor = value[cursorPos - 1];
    const charBeforeThat = value[cursorPos - 2];
    const isAtLineStart = charBeforeThat === undefined || charBeforeThat === "\n";

    if (charBeforeCursor === "/" && isAtLineStart) {
      slashTriggerPosRef.current = cursorPos - 1;
      setIsSlashMenuOpen(true);
      setIsLinkPopoverOpen(false);
      setIsImagePopoverOpen(false);
    } else if (isSlashMenuOpen) {
      setIsSlashMenuOpen(false);
    }
  }

  function handleSlashSelect(item) {
    const triggerPos = slashTriggerPosRef.current;
    const el = document.getElementById(CONTENT_TEXTAREA_ID);
    setIsSlashMenuOpen(false);
    if (!el || triggerPos === null) return;

    // Önce tetikleyici "/" karakterini metinden çıkarıyoruz - kullanıcı bir
    // blok seçtiğinde geride "/video" gibi bir kalıntı kalmasın.
    const withoutSlash = el.value.slice(0, triggerPos) + el.value.slice(triggerPos + 1);
    setContent(withoutSlash);

    // insertAtCursor, textarea'nın GÜNCEL DOM value'sunu okuyor - controlled
    // bileşenin yeni değeri (withoutSlash) render'a yansıyana kadar bir tık
    // bekliyoruz (applyToolbarInsert'teki AYNI setTimeout(...,0) deseni).
    setTimeout(() => {
      const el2 = document.getElementById(CONTENT_TEXTAREA_ID);
      if (!el2) return;
      el2.focus();
      el2.setSelectionRange(triggerPos, triggerPos);
      const result = insertAtCursor(item.before, item.after, item.placeholder);
      if (result) {
        setContent(result.newValue);
        setTimeout(() => restoreFocusAndCursor(result.cursorPos), 0);
      }
    }, 0);
  }

  function handleInsertLink() {
    if (!linkTarget.trim()) return;
    const text = linkText.trim() || linkTarget.trim();
    const markdown = `[${text}](${linkTarget.trim()})`;
    setContent((prev) => prev + markdown);
    setIsLinkPopoverOpen(false);
    setLinkText("");
    setLinkTarget("");
    setLinkSearchQuery("");
    setLinkSearchResults([]);
  }

  function handleInsertImage() {
    if (!imageUrl.trim()) return;
    const inserted = insertAtCursor(`![${imageAlt.trim()}](${imageUrl.trim()})`, "", "");
    if (inserted) {
      setContent(inserted.newValue);
      setIsImagePopoverOpen(false);
      setImageUrl("");
      setImageAlt("");
      setTimeout(() => restoreFocusAndCursor(inserted.cursorPos), 0);
    }
  }

  // Link penceresindeki arama - üst bardaki arama kutusuyla (bkz. WikiLayout)
  // AYNI hafif öneri endpoint'i, aynı debounce deseni. Eskiden burada mevcut
  // klasör ağacından çıkarılan SABİT bir sayfa listesi vardı - departman
  // büyüdükçe kullanışsız hale geliyordu, artık gerçek zamanlı arama var.
  function handleLinkSearchChange(e) {
    const value = e.target.value;
    setLinkSearchQuery(value);
    clearTimeout(linkSearchDebounceRef.current);

    if (!value.trim()) {
      setLinkSearchResults([]);
      setIsSearchingLink(false);
      return;
    }

    linkSearchDebounceRef.current = setTimeout(async () => {
      setIsSearchingLink(true);
      try {
        const results = await getWikiSearchSuggestions(token, value);
        setLinkSearchResults(results);
      } catch {
        setLinkSearchResults([]);
      } finally {
        setIsSearchingLink(false);
      }
    }, LINK_SEARCH_DEBOUNCE_MS);
  }

  function handleSelectExistingPage(page) {
    setLinkText(linkText || page.title);
    setLinkTarget(`wiki:${page.id}`);
  }

  // Kırmızı link (bkz. markdown.jsx) - aranan başlıkla eşleşen bir sayfa
  // YOKSA, Wikipedia'daki gibi "bu sayfa henüz yok ama oluşturabilirsin"
  // bağlantısı eklenebiliyor. Gerçek sayfa ID'si henüz olmadığı için hedef
  // GUID değil, doğrudan (URL-encoded) başlık taşıyor.
  function handleInsertRedLink() {
    const wantedTitle = linkSearchQuery.trim();
    if (!wantedTitle) return;
    setLinkText(linkText || wantedTitle);
    setLinkTarget(`wiki-new:${encodeURIComponent(wantedTitle)}`);
  }

  async function handleCreateFolder() {
    if (!newFolderName.trim() || !effectiveDepartment) return;
    setIsCreatingFolder(true);
    setError(null);
    try {
      const created = await createWikiFolder(token, {
        name: newFolderName.trim(),
        departmentName: effectiveDepartment,
        parentFolderId: folderId,
      });
      setNewFolderName("");
      await reloadTree(effectiveDepartment);
      setFolderId(created.id);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsCreatingFolder(false);
    }
  }

  async function handleSave(e) {
    e.preventDefault();
    setError(null);
    setIsSaving(true);

    try {
      if (isEditMode) {
        await updateWikiPage(token, pageId, { title, content, visibility, folderId, tags });
        navigate(`/wiki/${pageId}`);
      } else {
        const created = await createWikiPage(token, {
          title,
          content,
          departmentName: department,
          visibility,
          folderId,
          tags,
        });
        navigate(`/wiki/${created.id}`);
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoadingExisting) {
    return <p style={{ color: "var(--text)" }}>Yükleniyor...</p>;
  }

  const flatFolders = flattenFolders(tree?.folders ?? []);

  return (
    <div className="mx-auto max-w-3xl text-left">
      <h1 className="mb-6 text-2xl font-medium" style={{ color: "var(--text-h)" }}>
        {isEditMode ? "Sayfayı Düzenle" : "Yeni Sayfa"}
      </h1>

      <form onSubmit={handleSave} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="editor-title">Başlık</Label>
          <Input id="editor-title" value={title} onChange={(e) => setTitle(e.target.value)} disabled={isSaving} />
        </div>

        {/* Araç çubuğu - seçili metni sarmalıyor, seçim yoksa yer tutucu metinle
            ekliyor. Ürettiği sözdizimi WikiArticlePage'in render katmanıyla
            (Gün E) birebir eşleşiyor. */}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor={CONTENT_TEXTAREA_ID}>İçerik</Label>
          <div className="flex flex-wrap items-center gap-1 rounded-t-lg border border-b-0 border-[var(--border)] bg-[var(--code-bg)] p-1.5">
            {/* H1-H6 arası bir seviye seçip "Ekle"ye basınca o seviyede bir
                başlık ekleniyor - tek bir "Başlık" düğmesi (eskiden sabit
                H2) yerine, WikiArticlePage/markdown.jsx artık 6 seviyeyi de
                render edebiliyor (bkz. o dosyadaki HEADING_TAGS). */}
            <select
              value={headingLevel}
              onChange={(e) => setHeadingLevel(Number(e.target.value))}
              className="rounded border px-1.5 py-1 text-sm"
              style={{ borderColor: "var(--border)", background: "var(--bg)", color: "var(--text)" }}
            >
              {[1, 2, 3, 4, 5, 6].map((level) => (
                <option key={level} value={level}>
                  H{level}
                </option>
              ))}
            </select>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => applyToolbarInsert("#".repeat(headingLevel) + " ", "", "Başlık")}
            >
              Başlık Ekle
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert("**", "**", "kalın metin")}>
              <strong>K</strong>
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert("*", "*", "italik metin")}>
              <em>İ</em>
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert("- ", "", "liste öğesi")}>
              Liste
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert("> ", "", "alıntı")}>
              Alıntı
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => applyToolbarInsert("```\n", "\n```", "kod")}
            >
              Kod Bloğu
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert(TABLE_TEMPLATE, "", "")}>
              Tablo
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert("`", "`", "kod")}>
              Satır İçi Kod
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert("- [ ] ", "", "Yapılacak")}>
              ☑ Checklist
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => applyToolbarInsert("\n---\n", "", "")}>
              Ayraç
            </Button>
            {/* Callout - önce tipi seç (heading seviyesi seçiciyle AYNI desen),
                sonra ":::<tip>" ... ":::" sarmalayıcısını ekle (bkz.
                markdown.jsx'teki CalloutBlock). */}
            <select
              value={calloutType}
              onChange={(e) => setCalloutType(e.target.value)}
              className="rounded border px-1.5 py-1 text-sm"
              style={{ borderColor: "var(--border)", background: "var(--bg)", color: "var(--text)" }}
            >
              {CALLOUT_TYPE_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => applyToolbarInsert(`:::${calloutType}\n`, "\n:::", "Metin")}
            >
              Callout Ekle
            </Button>
            {/* Video - tek bir şablon insert (Callout/Checklist/Ayraç ile AYNI
                desen, popover YOK) - kullanıcı yer tutucu URL'yi elle
                değiştiriyor. YouTube/mp4/webm/ogg/mov otomatik algılanıyor
                (bkz. markdown.jsx'teki VideoBlock), tanınmayan bir URL sade
                bir "Videoyu Aç" linkine düşüyor. */}
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() =>
                applyToolbarInsert(":::video\n", "\n:::", "https://www.youtube.com/watch?v=... veya video dosyası URL'si")
              }
            >
              🎬 Video
            </Button>
            {/* Hizalı resim - metnin ImageBlock'un aksine (her zaman ortada,
                sabit boyutta) metnin YANINA konup metnin etrafından dolanması
                gereken durumlar için (bkz. markdown.jsx'teki AlignedImageBlock). */}
            <select
              value={imageAlign}
              onChange={(e) => setImageAlign(e.target.value)}
              className="rounded border px-1.5 py-1 text-sm"
              style={{ borderColor: "var(--border)", background: "var(--bg)", color: "var(--text)" }}
            >
              <option value="left">Sol</option>
              <option value="center">Orta</option>
              <option value="right">Sağ</option>
            </select>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => applyToolbarInsert(`:::image-${imageAlign}\n![`, "](https://...)\n:::", "Açıklama")}
            >
              🖼️ Hizalı Resim
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => { setIsLinkPopoverOpen((o) => !o); setIsImagePopoverOpen(false); setIsSlashMenuOpen(false); }}
            >
              🔗 Link
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => { setIsImagePopoverOpen((o) => !o); setIsLinkPopoverOpen(false); setIsSlashMenuOpen(false); }}
            >
              🖼️ Resim
            </Button>
          </div>

          {isImagePopoverOpen && (
            <div
              className="flex flex-col gap-2 border border-b-0 border-[var(--border)] p-3"
              style={{ background: "var(--bg)" }}
            >
              <div className="flex gap-2">
                <Input
                  placeholder="Görsel açıklaması / Alt metin (ör. Sistem Mimarisi)"
                  value={imageAlt}
                  onChange={(e) => setImageAlt(e.target.value)}
                  className="flex-1 text-xs"
                />
                <Input
                  placeholder="Görsel URL bağlantısı (https://...)"
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                  className="flex-1 text-xs"
                />
                <Button type="button" size="sm" onClick={handleInsertImage} disabled={!imageUrl.trim()}>
                  Resim Ekle
                </Button>
              </div>
            </div>
          )}

          {isLinkPopoverOpen && (
            <div
              className="flex flex-col gap-2 border border-b-0 border-[var(--border)] p-3"
              style={{ background: "var(--bg)" }}
            >
              <div className="flex gap-2">
                <Input
                  placeholder="Bağlantı metni (ör. React dokümanı)"
                  value={linkText}
                  onChange={(e) => setLinkText(e.target.value)}
                  className="flex-1"
                />
                <Input
                  placeholder="https://... ya da aşağıdan bir sayfa ara/seç"
                  value={linkTarget}
                  onChange={(e) => setLinkTarget(e.target.value)}
                  className="flex-1"
                />
                <Button type="button" size="sm" onClick={handleInsertLink} disabled={!linkTarget.trim()}>
                  Ekle
                </Button>
              </div>
              <Input
                placeholder="Atlas içi bağlantı için sayfa ara..."
                value={linkSearchQuery}
                onChange={handleLinkSearchChange}
                className="text-sm"
              />

              {linkSearchQuery.trim() && (
                <div className="max-h-32 overflow-y-auto rounded border" style={{ borderColor: "var(--border)" }}>
                  {isSearchingLink ? (
                    <p className="px-2 py-1.5 text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
                      Aranıyor...
                    </p>
                  ) : linkSearchResults.length > 0 ? (
                    linkSearchResults.map((p) => (
                      <button
                        key={p.id}
                        type="button"
                        onClick={() => handleSelectExistingPage(p)}
                        className="block w-full truncate px-2 py-1 text-left text-xs hover:bg-[var(--brand-accent)]/10"
                      >
                        {p.title} <span style={{ opacity: 0.6 }}>· {p.departmentName}</span>
                      </button>
                    ))
                  ) : (
                    // Kırmızı link teklifi - Wikipedia'daki gibi, aranan
                    // başlıkla eşleşen bir sayfa yoksa "henüz yok ama
                    // oluşturulabilir" bağlantısı ekleniyor (bkz.
                    // handleInsertRedLink / markdown.jsx'teki "wiki-new:").
                    <button
                      type="button"
                      onClick={handleInsertRedLink}
                      className="block w-full px-2 py-1.5 text-left text-xs"
                      style={{ color: "red" }}
                    >
                      "{linkSearchQuery.trim()}" adında bir sayfa yok - kırmızı bağlantı olarak ekle
                      (tıklanınca sayfa oluşturma ekranı açılır)
                    </button>
                  )}
                </div>
              )}
            </div>
          )}

          {isSlashMenuOpen && <SlashCommandMenu items={SLASH_ITEMS} onSelect={handleSlashSelect} />}

          <Textarea
            id={CONTENT_TEXTAREA_ID}
            value={content}
            onChange={handleContentChange}
            disabled={isSaving}
            className="min-h-64 rounded-t-none"
          />
        </div>

        {/* Departman düzenleme modunda hiç gösterilmiyor - değiştirilemiyor
            (bkz. UpdateWikiPageCommand'daki not). */}
        {!isEditMode && (
          <div className="flex flex-col gap-1.5">
            <Label>Departman</Label>
            {isAdmin ? (
              <RadioGroup value={department} onValueChange={setDepartment} className="flex flex-row gap-4">
                {DEPARTMENTS.map((d) => (
                  <div key={d.value} className="flex items-center gap-2">
                    <RadioGroupItem value={d.value} id={`editor-department-${d.value}`} disabled={isSaving} />
                    <Label htmlFor={`editor-department-${d.value}`}>{d.label}</Label>
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
        )}

        <div className="flex flex-col gap-1.5">
          <Label>Görünürlük</Label>
          <RadioGroup value={visibility} onValueChange={setVisibility} className="flex flex-row gap-4">
            <div className="flex items-center gap-2">
              <RadioGroupItem value="Public" id="editor-visibility-public" disabled={isSaving} />
              <Label htmlFor="editor-visibility-public">Herkese Açık</Label>
            </div>
            <div className="flex items-center gap-2">
              <RadioGroupItem value="DepartmentOnly" id="editor-visibility-department" disabled={isSaving} />
              <Label htmlFor="editor-visibility-department">Sadece Departman</Label>
            </div>
          </RadioGroup>
        </div>

        {/* Etiketler - ayrı bir Tag entity'si/yönetim ekranı YOK (bkz.
            WikiPage.cs'teki not), sadece virgülle ayrılmış tek bir metin
            alanı. Normalizasyon (trim/küçük harf/tekrarsız) Domain'de oluyor,
            burada kullanıcı ne yazarsa göründüğü gibi tutuluyor - kaydedince
            gerçek (normalize edilmiş) hali geri gelip alanı günceller. */}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="editor-tags">Etiketler</Label>
          <Input
            id="editor-tags"
            placeholder="virgülle ayırarak yazın, ör. react, frontend, ui"
            value={tags}
            onChange={(e) => setTags(e.target.value)}
            disabled={isSaving}
          />
        </div>

        {/* Klasör seçici - mevcut ağaç düz bir listeye açılıyor (girinti derinliği
            gösteriyor). "+ Ekle" seçili klasörün ALTINA yeni bir alt klasör
            oluşturup otomatik seçiyor - IT'nin "React > UI" gibi iç içe bir
            yapı kurması bu şekilde mümkün oluyor. */}
        <div className="flex flex-col gap-1.5">
          <Label>Klasör</Label>
          <div className="max-h-40 overflow-y-auto rounded border" style={{ borderColor: "var(--border)" }}>
            <button
              type="button"
              onClick={() => setFolderId(null)}
              className="block w-full px-2 py-1.5 text-left text-sm hover:bg-[var(--brand-accent)]/10"
              style={{ fontWeight: folderId === null ? 600 : 400, color: "var(--text-h)" }}
            >
              (Kök - klasörsüz)
            </button>
            {flatFolders.map((f) => (
              <button
                key={f.id}
                type="button"
                onClick={() => setFolderId(f.id)}
                className="block w-full truncate py-1.5 text-left text-sm hover:bg-[var(--brand-accent)]/10"
                style={{ paddingLeft: 8 + f.depth * 16, fontWeight: folderId === f.id ? 600 : 400, color: "var(--text-h)" }}
              >
                {f.name}
              </button>
            ))}
          </div>
          <div className="flex gap-2">
            <Input
              placeholder="Yeni klasör adı (seçili klasörün altına eklenir)"
              value={newFolderName}
              onChange={(e) => setNewFolderName(e.target.value)}
              disabled={isCreatingFolder}
              className="flex-1"
            />
            <Button type="button" variant="outline" onClick={handleCreateFolder} disabled={isCreatingFolder || !newFolderName.trim()}>
              {isCreatingFolder ? "Ekleniyor..." : "+ Ekle"}
            </Button>
          </div>
        </div>

        {error && <p style={{ color: "red" }} className="text-sm">{error}</p>}

        <div className="flex gap-2">
          <Button
            type="submit"
            disabled={isSaving || (!isEditMode && !isAdmin && !ownDepartment)}
            className="text-white hover:opacity-90"
            style={{ background: "var(--brand-accent)" }}
          >
            {isSaving ? "Kaydediliyor..." : isEditMode ? "Kaydet" : "Yayınla"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate(isEditMode ? `/wiki/${pageId}` : "/wiki")}
          >
            Vazgeç
          </Button>
        </div>
      </form>
    </div>
  );
}

export default WikiEditorPage;
