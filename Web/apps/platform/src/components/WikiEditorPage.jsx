import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router";
import {
  BookOpen,
  Code2,
  FileText,
  Heading2,
  Image as ImageIcon,
  Info,
  ListChecks,
  Minus,
  Plus,
  Table2,
  Video,
} from "lucide-react";
import {
  createWikiFolder,
  createWikiPage,
  getDocumentSearchSuggestions,
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
// Autosave/Draft (Eksik-özellik listesi B grubu, Gün 3, 2026-08-17) - BİLEREK
// backend'e YAZMIYOR (yeni bir Draft entity/endpoint İCAT EDİLMEDİ). Amacı
// SADECE tarayıcı-yerel bir kazayı (yanlışlıkla sekmeyi kapatmak, "Vazgeç"e
// basmak, tarayıcı çökmesi) telafi etmek - theme/Okuma Ayarları'yla AYNI
// localStorage deseni. WikiPageVersion'a (Gün 1-2, GERÇEK kaydedilmiş
// geçmiş) KARIŞTIRILMADI - "taslak karalama" ile "kaydedilmiş sürüm" farklı
// kavramlar, ikisini aynı tabloya koymak geçmişi anlamsız taslak satırlarıyla
// kirletirdi.
const AUTOSAVE_DEBOUNCE_MS = 1500;
function draftStorageKey(pageId) {
  return pageId ? `wiki-draft-edit-${pageId}` : "wiki-draft-new";
}

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
  // Eksik-özellik listesi Gün 2 (2026-08-17, "Resize") - varsayılan "medium",
  // markdown.jsx'teki AlignedImageBlock'un size prop'unun varsayılanıyla
  // AYNI (eski, boyut eki OLMADAN yazılmış içerikle tutarlı kalması için).
  const [imageSize, setImageSize] = useState("medium");
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

  // Autosave/Draft state - pendingDraft doluyken kullanıcı henüz "Geri
  // Yükle"/"Yok say" demedi, bu yüzden otomatik kaydetme BİLEREK duruyor
  // (draftCheckDoneRef, bkz. aşağıdaki iki effect) - aksi halde fetch'ten
  // gelen (ya da red-link'ten prefill edilen) İLK state değişikliği,
  // kullanıcının henüz GÖRMEDİĞİ bir taslağın üzerine sessizce yazardı.
  const [pendingDraft, setPendingDraft] = useState(null);
  const [draftSavedAt, setDraftSavedAt] = useState(null);
  const draftCheckDoneRef = useRef(false);
  const autosaveDebounceRef = useRef(null);

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

  // Taslak kontrolü - SADECE bir kez, gerçek (fetch edilmiş ya da red-link'ten
  // prefill edilmiş) başlangıç state'i oturduktan SONRA çalışıyor. Var olan
  // bir taslak, o anki state'ten HERHANGİ bir alanda farklıysa kullanıcıya
  // sorulmadan uygulanmıyor - "Geri Yükle"/"Yok say" banner'ı çıkıyor (bkz.
  // handleRestoreDraft/handleDiscardDraft). Aynıysa (ör. daha önce zaten
  // kaydedilmiş bir sayfanın taslağı, hiç değişmemiş) sessizce atlanıyor.
  useEffect(() => {
    if (isLoadingExisting) return;

    const key = draftStorageKey(isEditMode ? pageId : null);
    const raw = localStorage.getItem(key);

    if (!raw) {
      draftCheckDoneRef.current = true;
      return;
    }

    try {
      const parsed = JSON.parse(raw);
      const differs =
        parsed.title !== title ||
        parsed.content !== content ||
        parsed.tags !== tags ||
        parsed.visibility !== visibility ||
        parsed.folderId !== folderId;

      if (differs) {
        setPendingDraft(parsed);
      } else {
        draftCheckDoneRef.current = true;
      }
    } catch {
      // Bozuk/eski formatlı bir taslak - kurtarmaya çalışmak yerine temizle.
      localStorage.removeItem(key);
      draftCheckDoneRef.current = true;
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoadingExisting]);

  // Otomatik kaydetme - draftCheckDoneRef true OLMADAN (yani kullanıcı henüz
  // var olan bir taslağı görüp karar vermeden) hiçbir şey yazmıyor. Debounce
  // deseni linkSearchDebounceRef ile AYNI - her tuş vuruşunda zamanlayıcı
  // sıfırlanıyor, sadece AUTOSAVE_DEBOUNCE_MS'lik bir sessizlikten sonra
  // gerçekten localStorage'a yazıyor.
  useEffect(() => {
    if (!draftCheckDoneRef.current) return;

    clearTimeout(autosaveDebounceRef.current);
    autosaveDebounceRef.current = setTimeout(() => {
      const draft = {
        title,
        content,
        tags,
        visibility,
        folderId,
        department: !isEditMode ? department : undefined,
        savedAt: new Date().toISOString(),
      };
      localStorage.setItem(draftStorageKey(isEditMode ? pageId : null), JSON.stringify(draft));
      setDraftSavedAt(draft.savedAt);
    }, AUTOSAVE_DEBOUNCE_MS);

    return () => clearTimeout(autosaveDebounceRef.current);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [title, content, tags, visibility, folderId, department]);

  function handleRestoreDraft() {
    if (!pendingDraft) return;
    setTitle(pendingDraft.title ?? "");
    setContent(pendingDraft.content ?? "");
    setTags(pendingDraft.tags ?? "");
    setVisibility(pendingDraft.visibility ?? "Public");
    setFolderId(pendingDraft.folderId ?? null);
    if (!isEditMode && isAdmin && pendingDraft.department) setDepartment(pendingDraft.department);
    setPendingDraft(null);
    draftCheckDoneRef.current = true;
  }

  function handleDiscardDraft() {
    localStorage.removeItem(draftStorageKey(isEditMode ? pageId : null));
    setPendingDraft(null);
    draftCheckDoneRef.current = true;
  }

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

  // "/" yazarak AÇILDIYSA triggerPos dolu - önce o "/" karakterini kaldırıp
  // SONRA bloğu ekliyoruz. Medium-vari "+" düğmesiyle AÇILDIYSA (bkz.
  // aşağıdaki handlePlusButtonClick) triggerPos BİLEREK null bırakılıyor -
  // kaldırılacak bir tetikleyici karakter yok, doğrudan mevcut imleç
  // konumuna ekleniyor (applyToolbarInsert ile AYNI davranış). Aynı
  // SLASH_ITEMS/SlashCommandMenu'yü İKİ farklı tetikleyicinin paylaşması
  // için tek bir handler'da dallanıyor - iki ayrı ekleme mekanizması İCAT
  // EDİLMEDİ.
  function handleSlashSelect(item) {
    const triggerPos = slashTriggerPosRef.current;
    setIsSlashMenuOpen(false);

    if (triggerPos === null) {
      applyToolbarInsert(item.before, item.after, item.placeholder);
      return;
    }

    const el = document.getElementById(CONTENT_TEXTAREA_ID);
    if (!el) return;

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

  // Medium'daki "+" düğmesinin karşılığı - SlashCommandMenu'yü "/" yazmaya
  // gerek kalmadan, tek tıkla açan görünür bir tetikleyici (kullanıcı
  // isteği: "artı işareti ve yanında eklediğimiz özellikleri direkt
  // kullanma şansımız olsun"). YENİ bir menü/ekleme mantığı İCAT EDİLMEDİ -
  // "/" ile AYNI SLASH_ITEMS listesini, AYNI SlashCommandMenu'yü açıyor,
  // sadece triggerPos'u BİLEREK null'a sıfırlıyor (bkz. handleSlashSelect'teki
  // dallanma) çünkü kaldırılacak bir "/" karakteri yok.
  function handlePlusButtonClick() {
    slashTriggerPosRef.current = null;
    setIsSlashMenuOpen((open) => !open);
    setIsLinkPopoverOpen(false);
    setIsImagePopoverOpen(false);
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
  //
  // P5 Gün 4 (Documents→AI/RAG entegrasyonu, "document:GUID" içerik-referans
  // bloğunun P2'den ertelenen bağlanması) - Wiki VE Documents'ın öneri
  // endpoint'leri BİRLİKTE (Promise.all) çağrılıp TEK bir listede birleşiyor.
  // Biri başarısız olursa (ör. bir modül geçici olarak erişilemez) diğerinin
  // sonuçları YİNE DE gösterilsin diye Promise.allSettled kullanılıyor -
  // Promise.all olsaydı biri reddedince İKİSİ de kaybolurdu.
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
        const [wikiOutcome, documentOutcome] = await Promise.allSettled([
          getWikiSearchSuggestions(token, value),
          getDocumentSearchSuggestions(token, value),
        ]);

        const wikiResults = (wikiOutcome.status === "fulfilled" ? wikiOutcome.value : [])
          .map((p) => ({ ...p, kind: "wiki" }));
        const documentResults = (documentOutcome.status === "fulfilled" ? documentOutcome.value : [])
          .map((d) => ({ ...d, kind: "document" }));

        // Başlık eşleşmeleri en üstte kalsın diye iki listeyi ayrı ayrı
        // sıralamak yerine olduğu gibi art arda ekliyoruz - her iki
        // endpoint de kendi içinde ZATEN başlık>etiket önceliğiyle sıralı
        // döndürüyor (bkz. SearchWikiPageSuggestionsQueryHandler/
        // SearchDocumentSuggestionsQueryHandler).
        setLinkSearchResults([...wikiResults, ...documentResults]);
      } catch {
        setLinkSearchResults([]);
      } finally {
        setIsSearchingLink(false);
      }
    }, LINK_SEARCH_DEBOUNCE_MS);
  }

  function handleSelectExistingPage(item) {
    setLinkText(linkText || item.title);
    setLinkTarget(`${item.kind === "document" ? "document" : "wiki"}:${item.id}`);
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
        // Gerçek kayıt başarılı oldu - artık bu içeriğin "kaydedilmemiş"
        // hiçbir hâli yok, taslak anlamsız hale geldi.
        localStorage.removeItem(draftStorageKey(pageId));
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
        localStorage.removeItem(draftStorageKey(null));
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

      {pendingDraft && (
        <div
          className="mb-4 flex flex-wrap items-center justify-between gap-2 rounded-lg border px-3 py-2 text-sm"
          style={{ borderColor: "var(--brand-accent-border)", background: "var(--brand-accent-bg)" }}
        >
          <span style={{ color: "var(--text)" }}>
            Kaydedilmemiş bir taslak bulundu ({new Date(pendingDraft.savedAt).toLocaleString("tr-TR")} tarihli) - geri
            yüklemek ister misin?
          </span>
          <div className="flex gap-2">
            <Button type="button" size="sm" onClick={handleRestoreDraft}>
              Geri Yükle
            </Button>
            <Button type="button" size="sm" variant="outline" onClick={handleDiscardDraft}>
              Yok say
            </Button>
          </div>
        </div>
      )}

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
            {/* Medium-vari "+" düğmesi (kullanıcı isteği, 2026-08-12) -
                araç çubuğunun İLK öğesi, bilerek YUVARLAK ve diğer
                dikdörtgen Button'lardan görsel olarak AYRIŞIYOR ki "burada
                farklı, keşfedilebilir bir şey var" hissi versin - tıpkı
                Medium'daki gibi. Aşağıdaki uzun buton listesinin YERİNE
                geçmiyor (hiçbiri kaldırılmadı) - SADECE aynı blok menüsüne
                (SlashCommandMenu) "/" yazmayı bilmeye gerek kalmadan tek
                tıkla ulaşmanın ek bir yolu. */}
            <button
              type="button"
              onClick={handlePlusButtonClick}
              title="Blok ekle"
              aria-label="Blok ekle"
              className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full border hover:bg-[var(--brand-accent)]/10"
              style={{
                borderColor: "var(--brand-accent)",
                color: "var(--brand-accent)",
                background: isSlashMenuOpen ? "var(--brand-accent-bg)" : "transparent",
              }}
            >
              <Plus size={15} />
            </button>
            <div className="mx-0.5 h-5 w-px shrink-0" style={{ background: "var(--border)" }} />
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
                gereken durumlar için (bkz. markdown.jsx'teki AlignedImageBlock).
                İkinci select (boyut) - "Resize" (2026-08-17): serbest piksel
                sürükleme yerine üç sabit boyut, bkz. markdown.jsx'teki
                IMAGE_ALIGN_SIZE_CLASSES'daki gerekçe. */}
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
            <select
              value={imageSize}
              onChange={(e) => setImageSize(e.target.value)}
              className="rounded border px-1.5 py-1 text-sm"
              style={{ borderColor: "var(--border)", background: "var(--bg)", color: "var(--text)" }}
            >
              <option value="small">Küçük</option>
              <option value="medium">Orta</option>
              <option value="large">Büyük</option>
            </select>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() =>
                applyToolbarInsert(`:::image-${imageAlign}-${imageSize}\n![`, "](https://...)\n:::", "Açıklama")
              }
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
                placeholder="Atlas içi bağlantı için sayfa ya da belge ara..."
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
                    linkSearchResults.map((p) => {
                      const SourceIcon = p.kind === "document" ? FileText : BookOpen;
                      return (
                        <button
                          key={`${p.kind}-${p.id}`}
                          type="button"
                          onClick={() => handleSelectExistingPage(p)}
                          className="flex w-full items-center gap-1.5 truncate px-2 py-1 text-left text-xs hover:bg-[var(--brand-accent)]/10"
                        >
                          <SourceIcon size={12} className="shrink-0" style={{ opacity: 0.6 }} />
                          {p.title} <span style={{ opacity: 0.6 }}>· {p.departmentName}</span>
                        </button>
                      );
                    })
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
          {/* "Vazgeç"e basılsa bile taslak BİLEREK SİLİNMİYOR (bkz. handleSave'deki
              temizleme, SADECE gerçek kayıt sonrası) - yanlışlıkla "Vazgeç"e
              basan bir kullanıcı bir sonraki girişinde taslağını hâlâ bulabilsin. */}
          {draftSavedAt && (
            <span className="self-center text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
              Taslak kaydedildi · {new Date(draftSavedAt).toLocaleTimeString("tr-TR")}
            </span>
          )}
        </div>
      </form>
    </div>
  );
}

export default WikiEditorPage;
