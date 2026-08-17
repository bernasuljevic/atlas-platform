import { useEffect, useMemo, useRef, useState } from "react";
import { Link, Outlet, useNavigate } from "react-router";
import {
  Bell,
  ChevronDown,
  FileText,
  Menu,
  Moon,
  PanelLeftClose,
  Plus,
  Search,
  Sun,
} from "lucide-react";
import { getWikiSearchSuggestions } from "../api";
import { getUserInfoFromToken } from "../jwt";
import WikiFolderTree from "./WikiFolderTree";

const SEARCH_DEBOUNCE_MS = 250;

// Wikipedia'nın "üst bar + sol menü + geniş içerik" mimarisini ödünç
// alıyoruz (bkz. kullanıcının referans mockup'ları) - Atlas Wiki'nin kendi
// ismi/renk paleti DEĞİŞMİYOR, sadece iskelet ve bileşen yerleşimi aynı.
// Bildirim zili ve "Geri Bildirim" BİLEREK dekoratif (henüz bir bildirim
// merkezi/geri bildirim formu yok) - tema değiştirme ve sidebar daraltma ise
// gerçek, çalışan özellikler.
function WikiLayout({ token, onLogout }) {
  const { isAdmin, department: ownDepartment, fullName, email } = useMemo(
    () => getUserInfoFromToken(token),
    [token]
  );
  const navigate = useNavigate();

  // Masaüstünde varsayılan açık, dar ekranda varsayılan kapalı - aynı state
  // hem üst bardaki "☰" (mobil aç/kapa) hem sidebar altındaki "Menüyü Daralt"
  // (masaüstü daraltma) düğmesi tarafından paylaşılıyor.
  const [isSidebarOpen, setIsSidebarOpen] = useState(
    () => typeof window === "undefined" || window.innerWidth >= 768
  );
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [isDark, setIsDark] = useState(() => document.documentElement.classList.contains("dark"));

  function toggleTheme() {
    const nextDark = !isDark;
    if (nextDark) {
      document.documentElement.classList.add("dark");
      localStorage.setItem("theme", "dark");
    } else {
      document.documentElement.classList.remove("dark");
      localStorage.setItem("theme", "light");
    }
    setIsDark(nextDark);
  }

  const [searchQuery, setSearchQuery] = useState("");
  const [suggestions, setSuggestions] = useState([]);
  const [isSuggestionsOpen, setIsSuggestionsOpen] = useState(false);
  const debounceRef = useRef(null);

  useEffect(() => {
    return () => clearTimeout(debounceRef.current);
  }, []);

  function handleSearchChange(e) {
    const value = e.target.value;
    setSearchQuery(value);
    clearTimeout(debounceRef.current);

    if (!value.trim()) {
      setSuggestions([]);
      setIsSuggestionsOpen(false);
      return;
    }

    debounceRef.current = setTimeout(async () => {
      try {
        const results = await getWikiSearchSuggestions(token, value);
        setSuggestions(results);
        setIsSuggestionsOpen(true);
      } catch {
        setSuggestions([]);
      }
    }, SEARCH_DEBOUNCE_MS);
  }

  function handleSuggestionSelect(pageId) {
    setIsSuggestionsOpen(false);
    setSearchQuery("");
    navigate(`/wiki/${pageId}`);
  }

  const displayName = fullName ?? email ?? "Kullanıcı";
  const initials = displayName.trim().charAt(0).toUpperCase();

  return (
    <div className="flex min-h-screen flex-col text-left">
      {/* "Modernize et" (2026-08-12): header eskiden `position: static` idi -
          sayfa kaydırılınca tamamen gözden kayboluyordu, arama/tema/bildirim
          gibi her an erişilebilir olması gereken kontrollere ulaşmak için
          kullanıcının tepeye geri dönmesi gerekiyordu. Artık sticky - koyu/
          açık modun ikisinde de opak `var(--bg)` zemini koruyor (yarı-saydam/
          backdrop-blur BİLEREK eklenmedi, "sade" hedefiyle gereksiz bir
          gösteriş olurdu), z-20 ile TOC/sağ panel gibi diğer sticky
          elemanların üzerinde kalıyor. */}
      <header
        className="sticky top-0 z-20 flex items-center gap-3 border-b px-4 py-2.5"
        style={{ borderColor: "var(--border)", background: "var(--bg)" }}
      >
        <button
          type="button"
          onClick={() => setIsSidebarOpen((o) => !o)}
          className="rounded p-1.5 hover:bg-[var(--brand-accent)]/10"
          aria-label="Menüyü aç/kapat"
        >
          <Menu size={18} style={{ color: "var(--text)" }} />
        </button>

        <Link to="/wiki" className="flex items-center gap-2 font-semibold" style={{ color: "var(--text-h)" }}>
          <img src="/logo.png" alt="Atlas Wiki" className="h-7 w-7 rounded-full object-cover" />
          <span className="hidden sm:inline">Atlas Wiki</span>
        </Link>

        <div className="relative mx-auto hidden max-w-md flex-1 md:block">
          <Search size={15} className="absolute top-1/2 left-2.5 -translate-y-1/2 opacity-50" style={{ color: "var(--text)" }} />
          <input
            value={searchQuery}
            onChange={handleSearchChange}
            onFocus={() => searchQuery.trim() && suggestions.length > 0 && setIsSuggestionsOpen(true)}
            onBlur={() => setIsSuggestionsOpen(false)}
            placeholder="Ara..."
            className="w-full rounded-lg border py-1.5 pr-16 pl-8 text-sm outline-none focus:ring-2"
            style={{ borderColor: "var(--border)", background: "var(--bg)", color: "var(--text)" }}
          />
          <kbd
            className="pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 rounded border px-1.5 py-0.5 text-[10px] font-medium opacity-70"
            style={{ borderColor: "var(--border)", background: "var(--code-bg)", color: "var(--text)" }}
          >
            Ctrl + K
          </kbd>

          {isSuggestionsOpen && (
            <div
              className="absolute top-full left-0 z-20 mt-1 w-full overflow-hidden rounded-lg border shadow-lg"
              style={{ borderColor: "var(--border)", background: "var(--bg)" }}
            >
              {suggestions.length === 0 ? (
                <p className="px-3 py-3 text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
                  Eşleşen bir sayfa bulunamadı.
                </p>
              ) : (
                suggestions.map((s) => (
                  <button
                    key={s.id}
                    type="button"
                    onMouseDown={() => handleSuggestionSelect(s.id)}
                    className="flex w-full items-start gap-2 px-3 py-2 text-left hover:bg-[var(--brand-accent)]/10"
                  >
                    <FileText size={14} className="mt-0.5 shrink-0 opacity-50" style={{ color: "var(--text)" }} />
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-medium" style={{ color: "var(--text-h)" }}>
                        {s.title}
                      </span>
                      <span className="block truncate text-xs" style={{ color: "var(--text)", opacity: 0.65 }}>
                        {s.departmentName} · {s.excerpt}
                      </span>
                    </span>
                  </button>
                ))
              )}
            </div>
          )}
        </div>

        <div className="relative ml-auto flex items-center gap-2">
          <button
            type="button"
            onClick={toggleTheme}
            className="rounded p-1.5 hover:bg-[var(--brand-accent)]/10"
            title={isDark ? "Açık temaya geç" : "Koyu temaya geç"}
          >
            {isDark ? <Sun size={18} style={{ color: "var(--text)" }} /> : <Moon size={18} style={{ color: "var(--text)" }} />}
          </button>

          <button
            type="button"
            className="rounded p-1.5 hover:bg-[var(--brand-accent)]/10"
            title="Bildirimler (yakında)"
          >
            <Bell size={18} style={{ color: "var(--text)" }} />
          </button>

          <button
            type="button"
            onClick={() => setIsUserMenuOpen((o) => !o)}
            className="flex items-center gap-2 rounded-lg px-2 py-1 hover:bg-[var(--brand-accent)]/10"
          >
            <span
              className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold"
              style={{ background: "var(--brand-accent)", color: "#fff" }}
            >
              {initials}
            </span>
            <span className="hidden text-sm font-medium sm:inline" style={{ color: "var(--text-h)" }}>
              {displayName}
            </span>
            <ChevronDown size={14} className="opacity-60" style={{ color: "var(--text)" }} />
          </button>

          {isUserMenuOpen && (
            <div
              className="absolute top-full right-0 z-20 mt-2 w-44 overflow-hidden rounded-lg border shadow-lg"
              style={{ borderColor: "var(--border)", background: "var(--bg)" }}
            >
              {/* Audit Log'un AKSİNE Admin'e özel değil - Vault, departman/rol
                  kavramından bağımsız KİŞİSEL bir kasa (bkz. Vault'un
                  "sadece oluşturan kullanıcı + Admin görebilir" yetki kararı),
                  o yüzden her giriş yapmış kullanıcı burada görüyor. */}
              <Link
                to="/vault"
                onClick={() => setIsUserMenuOpen(false)}
                className="block px-3 py-2 text-sm hover:bg-[var(--brand-accent)]/10"
                style={{ color: "var(--text)" }}
              >
                Şifre Kasası
              </Link>
              {/* Vault'la AYNI gerekçeyle Admin'e özel değil - Document Library
                  herkesin gezip görünürlük kuralına göre erişebildiği bir alan. */}
              <Link
                to="/documents"
                onClick={() => setIsUserMenuOpen(false)}
                className="block px-3 py-2 text-sm hover:bg-[var(--brand-accent)]/10"
                style={{ color: "var(--text)" }}
              >
                Belgeler
              </Link>
              {isAdmin && (
                <Link
                  to="/audit-log"
                  onClick={() => setIsUserMenuOpen(false)}
                  className="block px-3 py-2 text-sm hover:bg-[var(--brand-accent)]/10"
                  style={{ color: "var(--text)" }}
                >
                  Audit Log
                </Link>
              )}
              <button
                type="button"
                onClick={onLogout}
                className="block w-full px-3 py-2 text-left text-sm hover:bg-[var(--brand-accent)]/10"
                style={{ color: "var(--text)" }}
              >
                Çıkış Yap
              </button>
            </div>
          )}
        </div>
      </header>

      <div className="flex flex-1 items-stretch">
        {/* Kullanıcı spec'i (2026-08-07, DÖRDÜNCÜ geçiş - "7. Sol Sidebar":
            "Sadece biraz daha dar olabilir - yaklaşık 220-240px") -
            256px'ten (w-64) 230px'e indirildi, makale alanına birkaç ekstra
            piksel bırakmak için. */}
        <aside
          className={`${isSidebarOpen ? "flex" : "hidden"} w-full shrink-0 flex-col gap-4 border-r p-4 md:w-[230px]`}
          style={{ borderColor: "var(--border)", background: "var(--bg)" }}
        >
          <WikiFolderTree token={token} ownDepartment={ownDepartment} />

          {/* Audit Log ve Çıkış Yap BİLEREK burada YOK (2026-08-05, kullanıcı
              geri bildirimi: "sağ üstte var zaten") - ikisi de zaten üst
              bardaki kullanıcı menüsünde (avatar dropdown) duruyor, burada
              tekrarlamak gereksizdi. Referans mockup'ta (2026-08-07,
              "buna benzer olsun") sidebar'ın en altında "Çıkış Yap" da
              gösteriliyordu ama o kararı ŞİMDİ tekrar açmak, iki turda önce
              verilen açık bir "çıkart, tekrarlı" geri bildirimiyle çelişirdi -
              o yüzden BİLEREK yine eklenmedi. "+ Yeni Sayfa Oluştur" ise
              referans mockup'ta olup burada hiç OLMAYAN, gerçek bir eksikti
              (tek yol WikiFolderTree'nin üstündeki genel "Yeni Sayfa" linkiydi,
              sidebar'ın kendi içinde hiç yoktu) - dolu, vurgulu bir buton
              olarak eklendi. */}
          <div className="mt-auto flex flex-col gap-2 border-t pt-3" style={{ borderColor: "var(--border)" }}>
            <Link
              to="/wiki/new"
              className="flex items-center justify-center gap-1.5 rounded-lg px-3 py-2 text-sm font-semibold text-white hover:opacity-90"
              style={{ background: "var(--brand-accent)" }}
            >
              <Plus size={16} /> Yeni Sayfa Oluştur
            </Link>
            <button
              type="button"
              onClick={() => setIsSidebarOpen(false)}
              className="hidden items-center gap-2 rounded px-2 py-1.5 text-left text-sm hover:bg-[var(--brand-accent)]/10 md:flex"
              style={{ color: "var(--text)" }}
            >
              <PanelLeftClose size={15} /> Menüyü Daralt
            </button>
          </div>
        </aside>

        <main className="min-w-0 flex-1 p-4 md:p-6" style={{ background: "var(--page-bg)" }}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default WikiLayout;
