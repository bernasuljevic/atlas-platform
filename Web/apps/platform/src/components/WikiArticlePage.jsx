import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router";
import {
  Building2,
  ChevronRight,
  Folder,
  List,
  PanelRightClose,
  PanelRightOpen,
  Pencil,
  Pin,
  Settings2,
  Share2,
  Star,
  ThumbsDown,
  ThumbsUp,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  deleteWikiPage,
  getFavoritePages,
  getPinnedPages,
  getWikiFolderTree,
  getWikiPageById,
  toggleFavorite as apiToggleFavorite,
  togglePin as apiTogglePin,
} from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { estimateReadingMinutes } from "../readingTime";
import { renderWikiMarkdown } from "../markdown";
import DiscussionPanel from "./DiscussionPanel";
import ReadingSettingsPanel, { FONT_SIZE_PX, LINE_HEIGHT_VALUE, LINE_WIDTH_VALUE } from "./ReadingSettingsPanel";
import { Button } from "@atlas/ui/button";
import { Badge } from "@atlas/ui/badge";

// Bir sayfanın FolderId'sinden kök klasöre kadar olan ad zincirini (breadcrumb
// için) bulmak - klasör ağacı zaten sidebar için çekiliyor (bkz.
// WikiFolderTree), burada AYNI veri sayfa özelinde tekrar çekilip (departman
// başına küçük bir liste, maliyeti düşük) yürünüyor.
// {id, name} çiftleri döndürüyor (SADECE isim değil) - kullanıcı geri
// bildirimiyle (2026-08-05: "buradaki dosyalama yaptığımız şeylere
// tıklayabilelim istiyorum") breadcrumb'taki her klasör segmentinin kendi
// /wiki/browse/... linkine ihtiyacı var, bunun için id şart.
function findFolderPath(folders, targetFolderId, path = []) {
  for (const folder of folders) {
    if (folder.id === targetFolderId) return [...path, { id: folder.id, name: folder.name }];
    const found = findFolderPath(folder.children, targetFolderId, [...path, { id: folder.id, name: folder.name }]);
    if (found) return found;
  }
  return null;
}

// Kullanıcı spec'i (2026-08-07, ÜÇÜNCÜ geçiş - "9. Önemli: Grid sistemini
// yeniden kur") - üç sütun artık flexbox DEĞİL, gerçek bir CSS Grid. Bunun
// asıl nedeni: Sağ panel'e (ve İçindekiler'e) bir "collapse/expand" özelliği
// eklendi, kolon genişliği artık SABİT bir Tailwind sınıfı değil, iki
// bağımsız state'e (showToc, isPanelCollapsed) göre DEĞİŞEN bir değer.
// Flexbox'ta bu "içeriğe göre daralt" ile doğal olarak çözülürdü, ama grid'de
// track genişliği net tanımlanmalı - bu yüzden 4 olası kombinasyon için TAM,
// LİTERAL Tailwind sınıf string'leri burada tanımlı (Tailwind'in derleme
// zamanı tarayıcısı `${değişken}` ile İÇİNE interpole edilmiş bir
// `grid-cols-[...]` sınıfını ASLA bulamaz - sadece kaynak kodda TAM yazılı
// string'leri görebiliyor, bu yüzden olası her kombinasyon burada ayrı ayrı
// yazılı duruyor, çalışma zamanında sadece HANGİSİNİN kullanılacağı seçiliyor).
// DÖRDÜNCÜ geçişte (2026-08-07 - "8. Grid Yapısı: 230px | 1100-1200px |
// 280px") sağ panel 240/260'tan 260/280'e çıkarılmıştı.
// BEŞİNCİ geçiş - UI/UX denetimi (2026-08-12, "sidebarlar daha kompakt
// olmalı"): panel 260/280'den 200/240'a geri indirildi. Bilgi Kutusu'ndaki
// satırlar (Departman/Erişim/Oluşturan/Tarih/Okuma Süresi/Etiketler) zaten
// o genişliği doldurmuyordu - içerik sütunu (`1fr` track) bu daralmadan
// doğrudan kazançlı çıkıyor. TOC genişliğine BİLEREK dokunulmadı (denetimde
// sadece sağ panel "gereksiz geniş" olarak işaretlenmişti).
const GRID_TEMPLATES = {
  "toc-open|panel-open": "lg:grid-cols-[200px_1fr_200px] xl:grid-cols-[220px_1fr_240px]",
  "toc-open|panel-closed": "lg:grid-cols-[200px_1fr_44px] xl:grid-cols-[220px_1fr_44px]",
  "toc-closed|panel-open": "lg:grid-cols-[44px_1fr_200px] xl:grid-cols-[44px_1fr_240px]",
  "toc-closed|panel-closed": "lg:grid-cols-[44px_1fr_44px] xl:grid-cols-[44px_1fr_44px]",
};

// Wikipedia'nın makale sayfasıyla aynı fikir: küçük bir dialog yerine,
// kendi URL'i olan (paylaşılabilir, geri/ileri tuşlarıyla gezilebilir) tam
// bir sayfa. WikiPageTable ve WikiSearch'teki eski salt-okunur dialog'ların
// yerini bu aldı - ikisi de artık buraya link veriyor.
function WikiArticlePage({ token }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const { userId, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [page, setPage] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [folderPath, setFolderPath] = useState([]);
  // "Faydalı oldu mu" BİLEREK hiçbir yere kaydedilmiyor - sadece yerel bir
  // teşekkür mesajı gösteriyor. Gerçek bir geri bildirim/oylama sistemi
  // (backend'e yazan) ayrı, kendi başına bir özellik olurdu.
  const [feedbackGiven, setFeedbackGiven] = useState(null);
  // Wikipedia sekmeleri ("Madde"/"Tartışma") + İçindekiler kutusunun aç/kapa
  // durumu - Hooks kuralı gereği BURADA, erken return'lerin (isLoading/error
  // dallarının, aşağıda) ÖNCESİNDE tanımlanmalı.
  const [activeTab, setActiveTab] = useState("article");
  // Kullanıcı geri bildirimi (2026-08-07, ALTINCI geçiş - "içerik ortada dar
  // bir sütun gibi görünmesin... sağ/sol boşlukları azalt") - önceki
  // turlarda içerik div'inin İÇ max-width'i kaldırılmıştı (bkz. dördüncü
  // geçiş) ama GRID TRACK'İNİN KENDİSİ (İçindekiler + Sağ Panel sabit
  // genişlikleri) hâlâ tipik laptop genişliklerinde (1366-1440px) içerik
  // sütununu ~600px'e sıkıştırıyordu - "geniş hücrede dar metin" hatası
  // düzeldi ama "iki sabit yan sütun, ORTA sütunu dar bırakıyor" sorunu
  // DEVAM ediyordu. Kesin çözüm: İçindekiler VE Sağ Panel artık VARSAYILAN
  // OLARAK daraltılmış (44px ikon şeridi) - makale varsayılan durumda
  // neredeyse TÜM 1fr track'ini alıyor, ikisi de hâlâ tek tıkla açılabiliyor
  // (özellik kaldırılmadı, sadece varsayılanı değişti). localStorage'da
  // kalıcı - bir kullanıcı bilerek açarsa bir sonraki makalede de açık kalır.
  const [showToc, setShowToc] = useState(() => localStorage.getItem("wikiShowToc") === "true");
  const [isPanelCollapsed, setIsPanelCollapsed] = useState(
    () => localStorage.getItem("wikiPanelCollapsed") !== "false"
  );
  // Scroll-spy: kullanıcı makaleyi kaydırırken İçindekiler'de o an ekranda
  // olan başlığı otomatik vurgulamak için (spec, 2026-08-07: "Scroll
  // sırasında aktif bölüm otomatik değişsin") - aşağıdaki IntersectionObserver
  // effect'i güncelliyor.
  const [activeHeadingId, setActiveHeadingId] = useState(null);
  // Mobil/tablette İçindekiler artık inline stack değil, ayrı bir çekmece
  // (drawer) - "Okuma Ayarları" çekmecesiyle AYNI desende ama SOLDAN
  // açılıyor (ikisini görsel olarak ayırt etmek için - biri "menü" hissi,
  // diğeri "ayar" hissi). ÜÇÜNCÜ geçişte (spec'in "7. Responsive" tablosu -
  // "Tablet: TOC collapse") bu artık SADECE mobilde değil, tablette de
  // devrede - eşiği `lg` (1024px), önceki `md`'den yükseltildi.
  const [isMobileTocOpen, setIsMobileTocOpen] = useState(false);

  // Pinle/Favorilere Ekle (kullanıcı geri bildirimi, 2026-08-07, YEDİNCİ
  // geçiş: "pinle favorilere ekle ve paylaşta kullanılabilsin" - öncekinde
  // ikisi de tamamen dekoratifti, tıklanamıyordu). ESKİDEN localStorage'da
  // tutuluyordu (cihazlar arası senkron olmuyordu, erişimi kaybedilmiş bir
  // sayfa sessizce listede kalıyordu) - artık gerçek, kullanıcı bazlı bir
  // backend tablosu (bkz. UserPageFavorite/UserPagePin, Wiki.Domain).
  const [isPinned, setIsPinned] = useState(false);
  const [isFavorited, setIsFavorited] = useState(false);

  // Okuma Ayarları (kullanıcı spec'i, 2026-08-05: "Sağ Tarafta Yardım Paneli") -
  // üç tercih de localStorage'da kalıcı (tema tercihinin WikiLayout'ta
  // kalıcı olmasıyla AYNI desen) - bir kez seçilince başka bir makaleye
  // geçince de korunuyor. AYNI Hooks kuralı gereği bunlar da erken
  // return'lerden ÖNCE.
  const [fontSize, setFontSize] = useState(() => localStorage.getItem("wikiFontSize") || "medium");
  const [lineWidth, setLineWidth] = useState(() => localStorage.getItem("wikiLineWidth") || "medium");
  const [lineHeightKey, setLineHeightKey] = useState(() => localStorage.getItem("wikiLineHeight") || "normal");
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [isMobileSettingsOpen, setIsMobileSettingsOpen] = useState(false);
  // WikiLayout'taki toggleTheme ile AYNI desen (bkz. oradaki not) - burada
  // AYRI bir kopyası duruyor, paylaşılan bir ThemeContext YOK çünkü şu ana
  // kadar bu iki yerin (üst bar + bu okuma paneli) dışında tema değiştiren
  // üçüncü bir nokta olmadı, ayrı bir context açmak şimdilik YAGNI olurdu.
  const [isDark, setIsDark] = useState(() => document.documentElement.classList.contains("dark"));

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(null);

    getWikiPageById(token, id)
      .then((result) => {
        if (!cancelled) setPage(result);
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

  useEffect(() => {
    if (!page) return;
    let cancelled = false;

    if (!page.folderId) {
      setFolderPath([]);
      return;
    }

    getWikiFolderTree(token, page.departmentName)
      .then((tree) => {
        if (!cancelled) setFolderPath(findFolderPath(tree.folders, page.folderId) ?? []);
      })
      .catch(() => {
        if (!cancelled) setFolderPath([]);
      });

    return () => {
      cancelled = true;
    };
  }, [token, page?.departmentName, page?.folderId]);

  // Favori/pin durumu - Toggle endpoint'i işlemden SONRAKİ durumu zaten
  // döndürdüğü için (bkz. handleTogglePin/handleToggleFavorite) burada SADECE
  // sayfa ilk açıldığında mevcut durumu öğrenmek için tam liste çekiliyor.
  // Hata sessizce yutuluyor - favori/pin rozetinin yanlış görünmesi sayfanın
  // asıl içeriğini engelleyecek bir hata değil.
  useEffect(() => {
    if (!page || !token) return;
    let cancelled = false;

    Promise.all([getFavoritePages(token), getPinnedPages(token)])
      .then(([favorites, pinned]) => {
        if (cancelled) return;
        setIsFavorited(favorites.some((p) => p.id === page.id));
        setIsPinned(pinned.some((p) => p.id === page.id));
      })
      .catch(() => {
        // Giriş yapmamış bir ziyaretçi için de bu effect çalışır (token
        // null/undefined ise fetch zaten 401 döner) - sessizce yut.
      });

    return () => {
      cancelled = true;
    };
  }, [token, page?.id]);

  useEffect(() => {
    localStorage.setItem("wikiFontSize", fontSize);
  }, [fontSize]);
  useEffect(() => {
    localStorage.setItem("wikiLineWidth", lineWidth);
  }, [lineWidth]);
  useEffect(() => {
    localStorage.setItem("wikiLineHeight", lineHeightKey);
  }, [lineHeightKey]);
  useEffect(() => {
    localStorage.setItem("wikiPanelCollapsed", String(isPanelCollapsed));
  }, [isPanelCollapsed]);
  useEffect(() => {
    localStorage.setItem("wikiShowToc", String(showToc));
  }, [showToc]);

  // İki yönlü bir "tema uygula" fonksiyonu - eski toggleTheme (sadece
  // "tersine çevir") yerine 2026-08-07'de bu geldi, çünkü Okuma Ayarları
  // paneli artık bir toggle DEĞİL, "Açık"/"Koyu" iki seçenekli bir
  // segmented control (bkz. ReadingSettingsPanel) - segmented control'ün
  // doğrudan HANGİ değerin seçildiğini bilmesi/ayarlaması gerekiyor.
  function applyTheme(nextDark) {
    if (nextDark) {
      document.documentElement.classList.add("dark");
      localStorage.setItem("theme", "dark");
    } else {
      document.documentElement.classList.remove("dark");
      localStorage.setItem("theme", "light");
    }
    setIsDark(nextDark);
  }

  async function handleDelete() {
    if (!window.confirm("Bu sayfayı silmek istediğine emin misin?")) return;

    setIsDeleting(true);
    try {
      await deleteWikiPage(token, id);
      navigate("/wiki");
    } catch (err) {
      setError(err.message);
      setIsDeleting(false);
    }
  }

  // React'ın Hooks kuralı: bir Hook, erken return'lerin (isLoading/error
  // dallarının) ÖNCESİNDE, HER render'da aynı sırayla çağrılmalı - bu yüzden
  // useMemo burada, "page henüz yok" ihtimalini kendi içinde (page?.content
  // ile) ele alarak duruyor, "page var mı" kontrolünden SONRAYA taşınmıyor
  // (taşınsaydı bazı render'larda hiç çağrılmamış olurdu - canlı test
  // sırasında yakalanan gerçek bir hataydı, bkz. konsoldaki "change in the
  // order of Hooks" uyarısı).
  const { nodes, headings } = useMemo(
    () => renderWikiMarkdown(page?.content ?? ""),
    [page?.content]
  );

  // Medium'un "X dakika okuma" göstergesi (Rıdvan'ın "Medium gibi birkaç
  // siteyi kontrol edip ek özellik ekleyebiliriz" geri bildiriminin tek
  // somut, henüz yapılmamış parçasıydı - İçindekiler/scroll-spy/okuma
  // ayarları zaten vardı, bkz. readingTime.js). Ayrı bir useMemo - headings/
  // nodes render'ıyla farklı bir sorumluluk, aynı `page?.content` bağımlılığı
  // dışında birbirinden bağımsız.
  const readingMinutes = useMemo(
    () => estimateReadingMinutes(page?.content ?? ""),
    [page?.content]
  );

  // Scroll-spy - headings render edildikten SONRA, DOM'daki gerçek başlık
  // elemanlarını gözlemliyor. `isFullscreen` BİLEREK dependency listesinde:
  // tam ekran moduna geçince/çıkınca bu bileşen aşağıda TAMAMEN FARKLI bir
  // JSX ağacı döndürüyor (erken return, bkz. altta) - React bu durumda eski
  // başlık elemanlarını unmount edip yenilerini mount ediyor, effect
  // yeniden çalışıp YENİ elemanları gözlemlemezse gözlemci "boşta" kalır.
  useEffect(() => {
    if (headings.length === 0) return undefined;

    const elements = headings.map((h) => document.getElementById(h.id)).filter(Boolean);
    if (elements.length === 0) return undefined;

    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries.filter((e) => e.isIntersecting);
        if (visible.length === 0) return;
        // Ekranda birden fazla başlık görünürse (kısa bölümler) en üstteki
        // "aktif" sayılıyor - Wikipedia'nın kendi İçindekiler davranışıyla aynı.
        const topMost = visible.reduce((a, b) => (a.boundingClientRect.top <= b.boundingClientRect.top ? a : b));
        setActiveHeadingId(topMost.target.id);
      },
      // Üstten ~56px içeri girince "geçildi" say, alttan geniş bir pay
      // bırak (%70) - bu sayede bir başlık ekranın ÇOK altında belli
      // belirsiz görünürken erken aktif olmuyor. 56px BİLEREK küçük tutuldu:
      // başlıkların kendi `scroll-mt-20` (80px) çapa payı + üst header'ın
      // yüksekliği nedeniyle bir başlığa TIKLANINCA gerçekte ~70px'te
      // sonlanıyor - eşik daha büyük (ör. 96px) olsaydı, tam o anda
      // "aktif" hiç işaretlenmiyordu (canlı test sırasında yakalandı).
      { rootMargin: "-56px 0px -70% 0px", threshold: 0 }
    );

    elements.forEach((el) => observer.observe(el));
    return () => observer.disconnect();
  }, [headings, isFullscreen]);

  // Okuma İlerleme Çubuğu (spec: "Uzun rehberlerde üst tarafta çok küçük bir
  // reading progress indicator... Ancak minimal olmalı, sayfayı gereksiz UI
  // elementleriyle doldurma") - makalenin tepesi viewport'un tepesine
  // ulaştığında 0%, altı viewport'un altına ulaştığında 100%. BİLEREK SADECE
  // normal (tam ekran OLMAYAN) modda çalışıyor - tam ekran modu KENDİ ayrı
  // scroll konteynerini kullanıyor (`overflow-y-auto`, window scroll DEĞİL),
  // bunu da desteklemek ayrı bir izleme mantığı gerektirirdi; "Day 1" kapsamı
  // bilerek normal moda sınırlı tutuldu, istenirse ayrı bir adımda genişletilir.
  const articleRef = useRef(null);
  const [readingProgress, setReadingProgress] = useState(0);
  // "Kısa sayfada çubuk gösterme" kararı BİLEREK ayrı bir state - `articleRef.
  // current`'ı doğrudan JSX'te okumak, ref'in AYNI render'daki elemana henüz
  // bağlanmamış olabileceği (ref'ler commit SONRASI dolar) bir "bir render
  // geride" hatasına yol açardı; bunun yerine effect içinde HESAPLANIP state'e
  // yazılıyor, JSX sadece bu state'i okuyor.
  const [showProgressBar, setShowProgressBar] = useState(false);

  useEffect(() => {
    if (isFullscreen) return undefined;

    function handleScroll() {
      const article = articleRef.current;
      if (!article) return;

      // `article.offsetTop` KULLANILMADI - `offsetTop`, en yakın
      // KONUMLANDIRILMIŞ (position: static OLMAYAN) ata elemana göre
      // ölçülüyor, o atanın kim olduğu bu düzende garanti değil (canlı test
      // sırasında yanlış/sabit "0" değer üretiği yakalandı). `getBoundingClientRect()`
      // + o anki `window.scrollY` toplamı, ata zincirinden TAMAMEN bağımsız,
      // her zaman doğru bir MUTLAK sayfa konumu veriyor.
      const rect = article.getBoundingClientRect();
      const articleTop = rect.top + window.scrollY;
      const articleHeight = article.offsetHeight;
      const viewportHeight = window.innerHeight;

      // Makale, tek bir ekrana zaten sığıyorsa (kısa sayfa) ilerleme kavramı
      // anlamsız - çubuk hiç gösterilmiyor.
      if (articleHeight <= viewportHeight) {
        setShowProgressBar(false);
        return;
      }
      setShowProgressBar(true);

      const scrollable = articleHeight - viewportHeight;
      const scrolled = window.scrollY - articleTop;
      const percent = Math.min(100, Math.max(0, (scrolled / scrollable) * 100));
      setReadingProgress(percent);
    }

    handleScroll();
    window.addEventListener("scroll", handleScroll, { passive: true });
    window.addEventListener("resize", handleScroll);
    return () => {
      window.removeEventListener("scroll", handleScroll);
      window.removeEventListener("resize", handleScroll);
    };
  }, [isFullscreen, page?.content]);

  // Arama sonucundan derin link (spec: "AI search sonucunda kullanıcı
  // doğrudan ilgili bölüme yönlendirilebilmeli") - WikiSearch.jsx, eşleşen
  // chunk metnini `navigate(..., { state: { chunkText } })` ile buraya
  // taşıyor. Backend'de chunk<->başlık ilişkisi HİÇ SAKLANMIYOR (embedding'ler
  // sabit boyutlu bir kayan pencereyle bölünüyor, başlık sınırlarını
  // BİLMİYOR) - bu yüzden eşleştirme İSTEMCİ tarafında, kaba ama yeterli bir
  // yöntemle yapılıyor: chunk metninin HAM içerikteki konumunu bulup, o
  // konumdan ÖNCE gelen EN YAKIN başlığı seçiyoruz (markdown.jsx'teki
  // `lineIndex` bunun için eklendi). Backend'e yeni bir alan eklemeden,
  // TAMAMEN mevcut veriyle çözülüyor.
  useEffect(() => {
    const chunkText = location.state?.chunkText;
    if (!chunkText || !page?.content || headings.length === 0) return;

    // DÜZELTME (canlı test sırasında yakalandı): snippet'i chunk'ın
    // BAŞINDAN almak yanlıştı - `TextChunker` sabit boyutlu bir kayan
    // pencereyle böldüğü için bir chunk sıkça bir bölüm SINIRINI ortadan
    // kesiyor (ör. bir chunk'ın ilk cümlesi ÖNCEKİ bölümün son cümlesi,
    // geri kalanı YENİ bölümün tamamı olabiliyor - tam olarak bu örnekte
    // yaşandı: chunk "...odaklanıyor.\n\n# Departman Bazlı Erişim\n\n..."
    // şeklindeydi, ilk 60 karakter hâlâ ÖNCEKİ bölümdeydi, sonuç yanlış
    // başlığa (bir önceki, "CQRS Komutu") kaydırıyordu). Chunk'ın ORTASINDAN
    // bir snippet almak çok daha güvenilir - bir chunk'ın "asıl konusu"
    // neredeyse hep ortasında, sınır-kesme etkisi sadece uçlarda oluyor.
    const middle = Math.max(0, Math.floor(chunkText.trim().length / 2) - 30);
    const snippet = chunkText.trim().slice(middle, middle + 60);
    const charIndex = page.content.indexOf(snippet);
    if (charIndex === -1) return; // eşleşme bulunamadı - sessizce vazgeç, sayfa tepesinde kalır

    const lineIndex = page.content.slice(0, charIndex).split("\n").length - 1;
    const nearestHeading = [...headings].reverse().find((h) => h.lineIndex <= lineIndex);
    if (!nearestHeading) return; // eşleşme İLK başlıktan ÖNCE - kaydırmaya gerek yok

    const timeoutId = setTimeout(() => {
      document.getElementById(nearestHeading.id)?.scrollIntoView({ behavior: "smooth", block: "start" });
      setActiveHeadingId(nearestHeading.id);
    }, 0);
    return () => clearTimeout(timeoutId);
  }, [location.state, page?.content, headings]);

  if (isLoading) {
    return <p style={{ color: "var(--text)" }}>Yükleniyor...</p>;
  }

  if (error || !page) {
    return (
      <div>
        <p style={{ color: "red" }} className="mb-3 text-sm">
          {error ?? "Bu sayfa artık mevcut değil ya da görme yetkin yok."}
        </p>
        <Link to="/wiki" className="underline">
          Sayfa listesine dön
        </Link>
      </div>
    );
  }

  const canEdit = isAdmin || page.createdByUserId === userId;

  async function togglePin() {
    try {
      const result = await apiTogglePin(token, page.id);
      setIsPinned(result);
    } catch (err) {
      toast("Pin durumu güncellenemedi", { description: err.message });
    }
  }

  async function toggleFavorite() {
    try {
      const result = await apiToggleFavorite(token, page.id);
      setIsFavorited(result);
    } catch (err) {
      toast("Favori durumu güncellenemedi", { description: err.message });
    }
  }

  // Web Share API (mobil cihazlarda gerçek bir "paylaş" sayfası açar) varsa
  // onu, yoksa (çoğu masaüstü tarayıcı) panoya kopyalamayı dene - ikisi de
  // "yakında" değil, GERÇEK ve şimdi çalışan bir davranış.
  async function handleShare() {
    const url = window.location.href;
    if (navigator.share) {
      try {
        await navigator.share({ title: page.title, url });
      } catch {
        // Kullanıcı paylaşım penceresini iptal etti - sessizce yut, hata değil.
      }
      return;
    }
    try {
      await navigator.clipboard.writeText(url);
      toast("Bağlantı panoya kopyalandı");
    } catch {
      toast("Bağlantı kopyalanamadı", { description: "Tarayıcı izni gerekebilir." });
    }
  }

  // Departman ve klasör segmentleri artık tıklanabilir (bkz. findFolderPath'teki
  // not) - /wiki/browse/... ile o klasörün/departmanın içeriğini listeleyen
  // yeni sayfaya gidiyor (bkz. WikiFolderBrowsePage). Sayfanın kendi başlığı
  // BİLEREK link DEĞİL - zaten o an görüntülenen sayfa, kendine link vermek
  // anlamsız olurdu.
  const breadcrumbParts = [
    { label: `${page.departmentName} Departmanı`, to: `/wiki/browse/${page.departmentName}` },
    ...folderPath.map((f) => ({ label: f.name, to: `/wiki/browse/${page.departmentName}/${f.id}` })),
    { label: page.title, to: null },
  ];
  // Backend'de virgülle ayrılmış TEK bir string olarak saklanıyor (bkz.
  // WikiPage.cs'teki not) - görüntüleme için burada listeye ayrılıyor.
  const tagList = page.tags ? page.tags.split(",").filter(Boolean) : [];

  // Okuma tercihlerinin GERÇEK CSS karşılığı - normal görünüm + tam ekran
  // İKİSİNDE de AYNI hesaplama kullanılıyor, tek yerde tutuluyor.
  const contentStyle = {
    color: "var(--text)",
    fontSize: FONT_SIZE_PX[fontSize],
    lineHeight: LINE_HEIGHT_VALUE[lineHeightKey],
    maxWidth: LINE_WIDTH_VALUE[lineWidth] === "none" ? "none" : LINE_WIDTH_VALUE[lineWidth],
  };

  const readingSettingsProps = {
    fontSize,
    onFontSizeChange: setFontSize,
    lineWidth,
    onLineWidthChange: setLineWidth,
    lineHeightKey,
    onLineHeightChange: setLineHeightKey,
    isFullscreen,
    onToggleFullscreen: () => setIsFullscreen((f) => !f),
    theme: isDark ? "dark" : "light",
    onThemeChange: (value) => applyTheme(value === "dark"),
  };

  // Kullanıcı geri bildirimi (2026-08-07, DOKUZUNCU geçiş - "genişe
  // tıklayınca ekranı küçültmesin, biz tekrar küçültmeye tıklarsak
  // küçülsün") - ÖNCEKİ turdaki çözüm "Geniş" seçilince İçindekiler/Sağ
  // Paneli OTOMATİK daraltıyordu (showToc/isPanelCollapsed state'ini
  // DEĞİŞTİRİYORDU) - kullanıcı bunu "elle tıklamadığım hâlde bir şey
  // küçüldü" olarak yaşadı, üstelik "Orta"ya dönünce panel eski hâline
  // dönmüyordu (state kalıcı olarak değişmişti). Yeni çözüm state'e HİÇ
  // DOKUNMUYOR - "Geniş" iken İçindekiler/Sağ Panel grid'den TAMAMEN
  // ÇIKARILIYOR (aşağıdaki `lineWidth !== "wide"` koşullarına bkz.), ama
  // showToc/isPanelCollapsed'in KENDİSİ hiç değişmiyor - "Orta"ya
  // dönüldüğünde ikisi TAM kullanıcının son bıraktığı hâliyle geri geliyor.
  const gridTemplateClass =
    lineWidth === "wide"
      ? "lg:grid-cols-1"
      : GRID_TEMPLATES[`${showToc ? "toc-open" : "toc-closed"}|${isPanelCollapsed ? "panel-closed" : "panel-open"}`];

  // Tam Ekran Okuma Modu - WikiLayout'un header/sidebar'ını gerçekten
  // gizlemek yerine (bunun için state'i yukarı taşımak gerekirdi, WikiLayout
  // bu sayfanın PARENT'ı), TÜM viewport'u kaplayan sabit (fixed) bir katman
  // olarak üstüne biniyor - aynı içerik (nodes) yeniden kullanılıyor, tek
  // fark konteyner ve "çık" düğmesi. WikiLayout'a HİÇ dokunmadan çalışıyor.
  if (isFullscreen) {
    return (
      <div className="fixed inset-0 z-50 overflow-y-auto" style={{ background: "var(--page-bg)" }}>
        <div className="mx-auto max-w-3xl px-6 py-8 text-left">
          <button
            type="button"
            onClick={() => setIsFullscreen(false)}
            className="mb-6 flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-sm font-medium"
            style={{ borderColor: "var(--border)", color: "var(--text)" }}
          >
            <X size={15} /> Tam ekrandan çık
          </button>
          <h1 className="mb-4 text-[32px] leading-tight font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
            {page.title}
          </h1>
          <div style={{ ...contentStyle, maxWidth: "none" }} className="font-normal">
            {nodes}
          </div>
        </div>
      </div>
    );
  }

  return (
    // Kullanıcı spec'i (2026-08-07, ÜÇÜNCÜ geçiş - "1. Genel Yerleşim") -
    // "Büyük monitörlerde içerik ~1600-1700px'e kadar büyüyebilmeli" - üst
    // sınır 1500'den 1650'ye çıkarıldı. mx-auto ile AYNI mantık korunuyor:
    // 1650px'in ALTINDAKİ ekranlarda (neredeyse tüm laptoplar) bu sınır hiç
    // devreye girmiyor, SADECE 1650px+ monitörlerde makale ortalanıp
    // sınırlı kalıyor - WikiLayout'un kendi `p-4 md:p-6` dolgusu (16-24px)
    // zaten spec'in istediği "24-40px kenar boşluğu" aralığında, ayrıca bir
    // şey eklemeye gerek yok.
    <article ref={articleRef} className="mx-auto max-w-[1650px] text-left text-[15px]">
      {/* Okuma İlerleme Çubuğu - global sticky header'ın (WikiLayout.jsx,
          ölçülen yüksekliği 50px) HEMEN ALTINA sabitleniyor. `top-[50px]`
          BİLEREK sabit bir piksel değeri - bu dosyada zaten var olan bir
          emsal var (scroll-spy'ın IntersectionObserver rootMargin'i de aynı
          şekilde header yüksekliğine göre elle ayarlanmış, bkz. yukarıdaki
          "56px BİLEREK küçük tutuldu" notu). Header'ın yüksekliği değişirse
          ikisi birlikte güncellenmeli. */}
      {showProgressBar && (
        <div className="fixed top-[50px] right-0 left-0 z-10 h-[2px]" style={{ background: "var(--border)" }}>
          <div
            className="h-full"
            style={{ width: `${readingProgress}%`, background: "var(--brand-accent)", transition: "width 0.1s linear" }}
          />
        </div>
      )}
      {/* Breadcrumb */}
      <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
        {/* Kullanıcı referans mockup'ı (2026-08-07, "buna benzer olsun") -
            her breadcrumb segmentinin önünde küçük bir ikon (departman ->
            Building2, klasörler -> Folder). Sayfanın kendi başlığı segmenti
            zaten link DEĞİL, o yüzden ikon almıyor - sadece "nerede
            olduğunu" gösteren pasif metin. */}
        <nav className="flex flex-wrap items-center gap-1 text-xs" style={{ color: "var(--text)", opacity: 0.85 }}>
          {breadcrumbParts.map((part, idx) => (
            <span key={idx} className="flex items-center gap-1">
              {idx > 0 && <ChevronRight size={12} className="opacity-60" />}
              {part.to ? (
                <Link to={part.to} className="flex items-center gap-1 hover:underline" style={{ color: "var(--brand-accent)" }}>
                  {idx === 0 ? <Building2 size={12} /> : <Folder size={12} />}
                  {part.label}
                </Link>
              ) : (
                <span className="font-semibold">{part.label}</span>
              )}
            </span>
          ))}
        </nav>

        {/* Kullanıcı spec'i (2026-08-07, ÜÇÜNCÜ geçiş - "7. Responsive"
            tablosu): "Tablet: TOC collapse, Sağ panel collapse / Mobil:
            Sadece makale, diğerleri drawer" - yani TABLET artık MOBİL'le
            AYNI davranıyor (ikisi de sadece makaleyi inline gösteriyor,
            İKİ panel de çekmece). Bu yüzden İçindekiler tetikleyicisi
            ÖNCEKİ turdaki `md:hidden`den `lg:hidden`e yükseltildi - Okuma
            Ayarları tetikleyicisiyle ARTIK AYNI eşikte. */}
        <div className="flex items-center gap-2">
          {headings.length > 1 && (
            <button
              type="button"
              onClick={() => setIsMobileTocOpen(true)}
              className="flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium lg:hidden"
              style={{ borderColor: "var(--border)", color: "var(--text)" }}
            >
              <List size={14} /> İçindekiler
            </button>
          )}
          {/* "Geniş" satır genişliği seçiliyken (bkz. aşağıdaki not) sağ panel
              lg+'de bile HİÇ render edilmiyor - bu düğme o modda ORADA da
              görünür kalmalı, yoksa kullanıcı Okuma Ayarları'na geri dönecek
              hiçbir yol bulamaz. */}
          <button
            type="button"
            onClick={() => setIsMobileSettingsOpen(true)}
            className={
              lineWidth === "wide"
                ? "flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium"
                : "flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium lg:hidden"
            }
            style={{ borderColor: "var(--border)", color: "var(--text)" }}
          >
            <Settings2 size={14} /> Okuma Ayarları
          </button>
        </div>
      </div>

      {/* Wikipedia Article Title - sayfanın GERÇEK, tek seferlik başlığı.
          İçerik başlıkları (markdown.jsx'teki HEADING_SIZES) beşinci geçişte
          21/15/12px'e yarıya indirildi - bu sayfa başlığı 46px'te bırakılınca
          içerik H1'inin (21px) 2 katından fazlası oluyordu, orantısız
          duruyordu ("başlıklar... küçültülsün" geri bildirimi buna da
          uygulandı) - 32px'e indirildi, hâlâ en tepede ama artık makul bir
          oranda. */}
      <h1 className="mt-1 mb-1 text-[32px] leading-tight font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
        {page.title}
      </h1>

      {/* Wikipedia Subtabs Bar */}
      <div className="mb-3 flex flex-wrap items-center justify-between border-b" style={{ borderColor: "var(--border)" }}>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setActiveTab("article")}
            className={`wiki-tab ${activeTab === "article" ? "active" : ""}`}
          >
            Madde
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("talk")}
            className={`wiki-tab ${activeTab === "talk" ? "active" : ""}`}
          >
            Tartışma
          </button>
        </div>

        <div className="flex items-center gap-1">
          {/* Pinle/Favorilere Ekle/Paylaş - kullanıcı geri bildirimiyle
              (2026-08-07, YEDİNCİ geçiş) artık GERÇEKTEN çalışıyor (bkz.
              yukarıdaki togglePin/toggleFavorite/handleShare notu - Pin/
              Favori localStorage'da kalıcı, Paylaş panoya kopyalıyor/Web
              Share API'sini kullanıyor). Aktifken dolu ikon + vurgu rengiyle
              işaretleniyor. */}
          <button
            type="button"
            onClick={togglePin}
            className="wiki-tab"
            style={{ color: isPinned ? "var(--brand-accent)" : undefined }}
            title={isPinned ? "Pini kaldır" : "Pinle"}
          >
            <Pin size={13} fill={isPinned ? "currentColor" : "none"} /> {isPinned ? "Pinlendi" : "Pinle"}
          </button>
          <button
            type="button"
            onClick={toggleFavorite}
            className="wiki-tab"
            style={{ color: isFavorited ? "var(--brand-accent)" : undefined }}
            title={isFavorited ? "Favorilerden çıkar" : "Favorilere Ekle"}
          >
            <Star size={13} fill={isFavorited ? "currentColor" : "none"} /> {isFavorited ? "Favoride" : "Favorilere Ekle"}
          </button>
          <button type="button" onClick={handleShare} className="wiki-tab" title="Bağlantıyı paylaş">
            <Share2 size={13} /> Paylaş
          </button>
          <button
            type="button"
            className="wiki-tab active"
          >
            Oku
          </button>
          {canEdit && (
            <Link to={`/wiki/${page.id}/edit`} className="wiki-tab">
              <Pencil size={13} /> Düzenle
            </Link>
          )}
          {canEdit && (
            <button type="button" onClick={handleDelete} disabled={isDeleting} className="wiki-tab text-red-500 hover:text-red-600">
              <Trash2 size={13} /> {isDeleting ? "Siliniyor..." : "Sil"}
            </button>
          )}
        </div>
      </div>

      <p className="mb-4 text-xs italic" style={{ color: "var(--text)", opacity: 0.75 }}>
        Atlas Wiki, şirket bilgi platformu ansiklopedisi
      </p>

      {activeTab === "talk" ? (
        <div className="rounded-lg border p-6" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
          <h2 className="mb-4 text-base font-semibold" style={{ color: "var(--text-h)" }}>
            "{page.title}" Tartışma Sayfası
          </h2>
          <DiscussionPanel token={token} pageId={page.id} />
        </div>
      ) : (
        // Kullanıcı spec'i (2026-08-07, ÜÇÜNCÜ geçiş - "9. Önemli: Grid
        // sistemini yeniden kur") - flexbox row yerine GERÇEK bir CSS Grid.
        // Track genişlikleri `gridTemplateClass` (yukarıda tanımlı, showToc/
        // isPanelCollapsed'e göre 4 sabit seçenekten biri) ile geliyor.
        // `transition-[grid-template-columns]` - panel açılıp kapanınca
        // (spec: "Açıldığında sağ taraftan slide şeklinde gelsin") track
        // genişliği ANİ değil, YUMUŞAK geçiyor; içindeki `<aside>` kendi
        // genişliğini HER ZAMAN grid hücresinden aldığı (w-full) için
        // içerik de bu geçiş boyunca doğal olarak "kayarak" büyüyüp küçülüyor.
        <div
          className={`grid grid-cols-1 gap-6 transition-[grid-template-columns] duration-300 ease-in-out ${gridTemplateClass}`}
        >
          {/* Left Table of Contents - dar, sade, sticky, scroll-spy ile
              aktif başlığı yeşil sol çizgiyle vurguluyor (bkz. yukarıdaki
              IntersectionObserver effect'i). SADECE lg ve üzerinde (masaüstü)
              görünür - altındaki tüm genişliklerde üstteki "İçindekiler"
              çekmece düğmesi devreye giriyor. */}
          {lineWidth !== "wide" && headings.length > 1 && (
            <div className="hidden lg:block">
              {showToc ? (
                <nav className="h-fit w-full lg:sticky lg:top-4">
                  <div className="mb-2 flex items-baseline justify-between">
                    <span className="text-[14px] font-bold" style={{ color: "var(--text-h)" }}>
                      İçindekiler
                    </span>
                    <button
                      type="button"
                      onClick={() => setShowToc(false)}
                      title="İçindekiler'i daralt"
                      aria-label="İçindekiler'i daralt"
                      className="rounded p-0.5 hover:bg-[var(--brand-accent)]/10"
                      style={{ color: "var(--brand-accent)" }}
                    >
                      <PanelRightOpen size={14} className="rotate-180" />
                    </button>
                  </div>

                  <ol className="flex flex-col gap-0.5 text-[12.5px]">
                    {headings.map((h) => {
                      const isActive = activeHeadingId === h.id;
                      return (
                        <li key={h.id}>
                          <a
                            href={`#${h.id}`}
                            className="block border-l-2 py-0.5 pr-1 leading-snug"
                            style={{
                              paddingLeft: 8 + (h.level - 1) * 10,
                              borderColor: isActive ? "var(--brand-accent)" : "transparent",
                              color: isActive ? "var(--brand-accent)" : "var(--text)",
                              fontWeight: isActive ? 600 : 400,
                              opacity: isActive ? 1 : 0.8,
                            }}
                          >
                            {h.text}
                          </a>
                        </li>
                      );
                    })}
                  </ol>
                </nav>
              ) : (
                <button
                  type="button"
                  onClick={() => setShowToc(true)}
                  title="İçindekiler'i göster"
                  aria-label="İçindekiler'i göster"
                  className="mx-auto flex h-9 w-9 items-center justify-center rounded-lg border lg:sticky lg:top-4"
                  style={{ borderColor: "var(--border)", background: "var(--code-bg)" }}
                >
                  <List size={16} style={{ color: "var(--text-h)" }} />
                </button>
              )}
            </div>
          )}

          {/* Center Article Content - artık en geniş grid track'i (1fr,
              kalan TÜM alanı alıyor) - yazı boyutu/satır aralığı/satır
              genişliği sağdaki Okuma Ayarları panelinden seçilen tercihlere
              göre (bkz. contentStyle). */}
          <div className="min-w-0">
            <div style={contentStyle} className="mx-auto font-normal">
              {nodes}
            </div>

            <div
              className="mt-8 flex flex-wrap items-center justify-between gap-3 border-t pt-4 text-xs"
              style={{ borderColor: "var(--border)", color: "var(--text)" }}
            >
              {feedbackGiven === null ? (
                <div className="flex items-center gap-2">
                  <span>Bu sayfa faydalı oldu mu?</span>
                  <Button variant="outline" size="sm" onClick={() => setFeedbackGiven("yes")}>
                    <ThumbsUp size={13} className="mr-1" /> Evet
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => setFeedbackGiven("no")}>
                    <ThumbsDown size={13} className="mr-1" /> Hayır
                  </Button>
                </div>
              ) : (
                <span style={{ opacity: 0.8 }}>Geri bildirimin için teşekkürler!</span>
              )}

              <span style={{ opacity: 0.7 }}>
                Son güncelleme: {formatUtcTimestamp(page.updatedAtUtc ?? page.createdAtUtc)}
              </span>
            </div>
          </div>

          {/* Right column: Bilgi Kutusu + Okuma Ayarları - kullanıcı spec'i
              (2026-08-07, ÜÇÜNCÜ geçiş - "4. Sağ Panel"): artık TAMAMEN
              kapatılabiliyor. Kapalıyken SADECE küçük bir ikon düğmesi
              (44px'lik grid track'i tam dolduran, ortalı bir kare) - açıkken
              tam içerik + üstte "daralt" düğmesi. SADECE lg+'de VE "Geniş"
              satır genişliği seçili DEĞİLKEN görünür ("Geniş"te grid'den
              tamamen çıkıyor, bkz. gridTemplateClass'taki not - Okuma
              Ayarları'na o modda üstteki tetikleyici düğmeden ulaşılıyor). */}
          {lineWidth !== "wide" && (
          <aside className="hidden w-full lg:sticky lg:top-4 lg:flex lg:flex-col lg:gap-2.5 lg:overflow-hidden">
            {isPanelCollapsed ? (
              <button
                type="button"
                onClick={() => setIsPanelCollapsed(false)}
                title="Paneli genişlet"
                aria-label="Paneli genişlet"
                className="mx-auto flex h-9 w-9 items-center justify-center rounded-lg border"
                style={{ borderColor: "var(--border)", background: "var(--code-bg)" }}
              >
                <PanelRightOpen size={16} style={{ color: "var(--text-h)" }} />
              </button>
            ) : (
              <>
                <button
                  type="button"
                  onClick={() => setIsPanelCollapsed(true)}
                  title="Paneli daralt"
                  aria-label="Paneli daralt"
                  className="ml-auto flex items-center gap-1 rounded px-1.5 py-1 text-[11px] font-medium hover:bg-[var(--brand-accent)]/10"
                  style={{ color: "var(--brand-accent)" }}
                >
                  Daralt <PanelRightClose size={13} />
                </button>

                <div className="overflow-hidden rounded-lg border" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
                  <div className="border-b px-2.5 py-1.5 text-center" style={{ borderColor: "var(--border)", background: "var(--code-bg)" }}>
                    <h3 className="text-[15px] font-bold text-[var(--text-h)]">{page.title}</h3>
                    <p className="text-[11px] text-[var(--text)] opacity-70">Bilgi Kutusu</p>
                  </div>
                  <div className="flex flex-col divide-y text-[13px]" style={{ borderColor: "var(--border)" }}>
                    <div className="flex justify-between px-2.5 py-1">
                      <span className="font-semibold text-[var(--text-h)]">Departman</span>
                      <span className="text-[var(--text)]">{page.departmentName}</span>
                    </div>
                    <div className="flex justify-between px-2.5 py-1">
                      <span className="font-semibold text-[var(--text-h)]">Erişim</span>
                      <span className="font-medium text-[var(--brand-accent)]">
                        {page.visibility === "Public" ? "Herkese Açık" : "Departmana Özel"}
                      </span>
                    </div>
                    <div className="flex justify-between px-2.5 py-1">
                      <span className="font-semibold text-[var(--text-h)]">Oluşturan</span>
                      <span className="truncate max-w-[140px] text-[var(--text)]">{page.createdByEmail ?? "Bilinmiyor"}</span>
                    </div>
                    <div className="flex justify-between px-2.5 py-1">
                      <span className="font-semibold text-[var(--text-h)]">Tarih</span>
                      <span className="text-[var(--text)]">{formatUtcTimestamp(page.createdAtUtc)}</span>
                    </div>
                    <div className="flex justify-between px-2.5 py-1">
                      <span className="font-semibold text-[var(--text-h)]">Okuma Süresi</span>
                      <span className="text-[var(--text)]">~{readingMinutes} dk</span>
                    </div>
                    {tagList.length > 0 && (
                      <div className="p-2.5">
                        <span className="block mb-1 font-semibold text-[var(--text-h)]">Etiketler</span>
                        <div className="flex flex-wrap gap-1">
                          {tagList.map((tag) => (
                            <Badge key={tag} variant="outline" className="text-[11px] py-0 px-1.5 font-normal">
                              {tag}
                            </Badge>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                <ReadingSettingsPanel {...readingSettingsProps} />
              </>
            )}
          </aside>
          )}
        </div>
      )}

      {/* Mobil/tablet İçindekiler çekmecesi - SOLDAN açılıyor (Okuma
          Ayarları çekmecesi sağdan açılıyor - bkz. aşağıda) - ikisini
          görsel olarak ayırt etmek için. Sadece lg ALTINDA anlamlı (lg+'de
          zaten inline TOC var), bu yüzden içindeki panel de lg:hidden. */}
      {isMobileTocOpen && headings.length > 1 && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div className="absolute inset-0" style={{ background: "rgba(0,0,0,0.4)" }} onClick={() => setIsMobileTocOpen(false)} />
          <div
            className="absolute top-0 left-0 flex h-full w-72 max-w-[85vw] flex-col gap-3 overflow-y-auto p-4"
            style={{ background: "var(--page-bg)" }}
          >
            <div className="flex items-center justify-between">
              <span className="text-sm font-bold" style={{ color: "var(--text-h)" }}>
                İçindekiler
              </span>
              <button type="button" onClick={() => setIsMobileTocOpen(false)} className="rounded p-1 hover:bg-[var(--brand-accent)]/10">
                <X size={16} style={{ color: "var(--text)" }} />
              </button>
            </div>
            <ol className="flex flex-col gap-0.5 text-sm">
              {headings.map((h) => {
                const isActive = activeHeadingId === h.id;
                return (
                  <li key={h.id}>
                    <a
                      href={`#${h.id}`}
                      onClick={() => setIsMobileTocOpen(false)}
                      className="block border-l-2 py-1.5 pr-1.5"
                      style={{
                        paddingLeft: 10 + (h.level - 1) * 12,
                        borderColor: isActive ? "var(--brand-accent)" : "transparent",
                        color: isActive ? "var(--brand-accent)" : "var(--text)",
                        fontWeight: isActive ? 600 : 400,
                      }}
                    >
                      {h.text}
                    </a>
                  </li>
                );
              })}
            </ol>
          </div>
        </div>
      )}

      {/* Mobil/tablet Okuma Ayarları çekmecesi - SAĞDAN açılıyor. Arka plan
          (backdrop) tıklanınca da kapanıyor. GERÇEK BUG (2026-08-07, ONUNCU
          geçiş, kullanıcı bildirdi: "tıklanınca bir şey gelmiyor") - bu
          çekmece HER ZAMAN `lg:hidden` idi (normalde lg+'de zaten inline sağ
          panel olduğu için çekmeceye gerek yoktu) - ama "Geniş" satır
          genişliği seçiliyken sağ panel lg+'de de TAMAMEN kalkıyor
          (yukarıdaki not), tetikleyici düğme o durumda BİLEREK lg+'de de
          gösterilmeye başlanmıştı AMA bu çekmecenin kendisi unutulup hâlâ
          `lg:hidden` bırakılmıştı - düğme tıklanıp state değişiyordu ama
          çekmece CSS ile gizli kaldığı için görsel olarak HİÇBİR ŞEY
          olmuyordu. Düzeltme: tetikleyiciyle BİREBİR AYNI koşul. */}
      {isMobileSettingsOpen && (
        <div className={lineWidth === "wide" ? "fixed inset-0 z-40" : "fixed inset-0 z-40 lg:hidden"}>
          <div
            className="absolute inset-0"
            style={{ background: "rgba(0,0,0,0.4)" }}
            onClick={() => setIsMobileSettingsOpen(false)}
          />
          <div
            className="absolute top-0 right-0 flex h-full w-72 max-w-[85vw] flex-col gap-3 overflow-y-auto p-4"
            style={{ background: "var(--page-bg)" }}
          >
            <div className="flex items-center justify-between">
              <span className="text-sm font-bold" style={{ color: "var(--text-h)" }}>
                Okuma Ayarları
              </span>
              <button type="button" onClick={() => setIsMobileSettingsOpen(false)} className="rounded p-1 hover:bg-[var(--brand-accent)]/10">
                <X size={16} style={{ color: "var(--text)" }} />
              </button>
            </div>
            <ReadingSettingsPanel {...readingSettingsProps} />
          </div>
        </div>
      )}
    </article>
  );
}

export default WikiArticlePage;
