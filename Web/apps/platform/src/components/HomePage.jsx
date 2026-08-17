import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router";
import {
  ArrowRight,
  Bell,
  Building2,
  Clock,
  FilePlus2,
  FileText,
  ListChecks,
  MessageSquare,
  Pin,
  Plus,
  Search,
  ShieldCheck,
  Star,
  Tag,
  Upload,
  Users,
  Video,
} from "lucide-react";
import {
  getComments,
  getDocuments,
  getFavoritePages,
  getNotifications,
  getPinnedPages,
  getWikiDashboard,
  getWikiPages,
} from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { extractVideosFromContent } from "../videoExtraction";
import { getDocumentIcon } from "../documentIcons";
import { Badge } from "@atlas/ui/badge";
import { Button } from "@atlas/ui/button";
import DiscussionPanel from "./DiscussionPanel";

// Kullanıcının verdiği referans mockup'taki (2026-08-04) "kart" deseni - başlıklı,
// bir sağ üst köşe eylemi (opsiyonel) olabilen, alt satırları divide-y ile
// ayrılan bir panel.
function Panel({ title, action, children, icon }) {
  return (
    <section className="overflow-hidden rounded-lg border" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
      <div className="flex items-center justify-between border-b px-3.5 py-2.5" style={{ borderColor: "var(--border)" }}>
        <h2 className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider" style={{ color: "var(--text-h)" }}>
          {icon}
          {title}
        </h2>
        {action}
      </div>
      <div className="flex flex-col divide-y" style={{ borderColor: "var(--border)" }}>
        {children}
      </div>
    </section>
  );
}

// Eski büyük 4'lü StatCard ızgarasının yerini aldı (2026-08-07 yerleşim
// düzenlemesi, spec'in "6. İçerik Öncelikli Tasarım - İstatistik kartları
// ikinci planda kalmalı" maddesi) - artık karşılama satırının içinde, tek
// satırlık küçük bir şerit. Kendi kutusu/border'ı YOK, sadece ayraçlarla
// (divider) ayrılan minik metin grupları - dikkat hâlâ makalelerde.
// `variant="onGradient"` (Görsel Tasarım Yenileme Gün 2) - Hero'nun içinde,
// renkli gradient zemin üzerinde kullanıldığında metin/ikon rengini var(--text)
// yerine SABİT beyaza zorluyor - Hero'nun arka planı temadan (açık/koyu)
// BAĞIMSIZ her zaman aynı gradient, dolayısıyla üzerindeki metin de temadan
// bağımsız hep beyaz kalmalı (aksi halde açık modda --text koyu renk gradient
// üzerinde neredeyse okunmaz olurdu).
function MiniStat({ icon, value, label, variant }) {
  const onGradient = variant === "onGradient";
  return (
    <span
      className="flex items-center gap-1.5 text-xs font-medium whitespace-nowrap"
      style={{ color: onGradient ? "rgba(255,255,255,0.92)" : "var(--text)" }}
    >
      <span style={{ color: onGradient ? "#ffffff" : "var(--brand-accent)" }}>{icon}</span>
      <span className="font-bold" style={{ color: onGradient ? "#ffffff" : "var(--text-h)" }}>
        {value}
      </span>
      <span style={{ opacity: onGradient ? 0.85 : 0.7 }}>{label}</span>
    </span>
  );
}

// "Hızlı Erişim" artık büyük bir Panel/kart değil, üstte küçük ikonlu bir
// düğme şeridi (kullanıcı spec'i, "5. Hızlı Erişim - büyük kartlar yerine
// küçük aksiyon butonları"). `to` (React Router linki) VEYA `href` (aynı
// sayfa içinde bir çapaya kaydırmak için, ör. #son-guncellemeler) kabul
// ediyor - ikisi birden verilmez. `disabled` HÂLÂ destekleniyor (WikiLayout'taki
// "Bildirimler (yakında)" Bell düğmesiyle AYNI desende bir title tooltip'i
// gösterir, gerçek bir backend'i olmayan özellikler için) - Favoriler/
// Pinlenenler artık gerçek bir backend'e sahip olduğu için BU İKİSİ disabled
// değil (bkz. UserPageFavorite/UserPagePin, Wiki.Domain).
function QuickActionButton({ icon, label, to, href, disabled }) {
  const classes =
    "flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-xs font-medium whitespace-nowrap transition hover:bg-[var(--brand-accent)]/10";
  const style = {
    borderColor: "var(--border)",
    color: "var(--text-h)",
    opacity: disabled ? 0.55 : 1,
    cursor: disabled ? "not-allowed" : "pointer",
  };

  if (disabled) {
    return (
      <button type="button" className={classes} style={style} title={`${label} (yakında)`}>
        {icon} {label}
      </button>
    );
  }

  if (href) {
    return (
      <a href={href} className={classes} style={style}>
        {icon} {label}
      </a>
    );
  }

  return (
    <Link to={to} className={classes} style={style}>
      {icon} {label}
    </Link>
  );
}

// Wikipedia'nın "Tarihte bugün" kutusunun bizdeki karşılığı - tarihsel trivia
// YERİNE (elimizde böyle bir veri yok), platformdaki GERÇEK son güncellemeleri
// listeliyoruz. Aynı "kutu + madde listesi + alt bilgi satırı" iskeleti
// korunuyor, sadece içerik platforma özgü.
function RecentUpdatesBox({ updates }) {
  return (
    <Panel title="Son Güncellemeler" icon={<Clock size={13} />}>
      <ul className="flex flex-col gap-2.5 p-4 text-sm" style={{ color: "var(--text)" }}>
        {updates.map((u) => (
          <li key={u.id} className="flex items-baseline gap-1.5">
            <span className="shrink-0" style={{ color: "var(--brand-accent)" }}>
              •
            </span>
            <span>
              <Link to={`/wiki/${u.id}`} className="font-semibold hover:underline" style={{ color: "var(--brand-accent)" }}>
                {u.title}
              </Link>{" "}
              - {u.departmentName} departmanında güncellendi ({formatUtcTimestamp(u.updatedAtUtc ?? u.createdAtUtc)})
            </span>
          </li>
        ))}
      </ul>
      <div className="flex items-center gap-3 border-t px-4 py-2 text-xs font-medium" style={{ borderColor: "var(--border)", color: "var(--brand-accent)" }}>
        <Link to="/wiki/pages" className="hover:underline">
          Tüm Sayfalar
        </Link>
        <span style={{ color: "var(--text)", opacity: 0.4 }}>·</span>
        <Link to="/wiki/new" className="hover:underline">
          Yeni sayfa oluştur
        </Link>
      </div>
    </Panel>
  );
}

// Medium'un sağ sütunundaki "+ Just start writing" kartının karşılığı
// (kullanıcı isteği, 2026-08-15). BİLEREK Medium'daki dekoratif illüstrasyon/
// ekstra "yazarlık ipuçları" linkleri YOK - "Medium'dan özellik alınabilir
// ama Atlas'ın tasarımı Medium'un kopyası olmamalı" ilkesine göre, ÇEKİRDEK
// fikir (her an görünen, tek tıkla yazmaya başlama girişi) alındı, dekor
// alınmadı - "sade" hedefiyle tutarlı. Panel'in KENDİSİNİ kullanmıyor (o,
// başlık çubuğu + divide-y liste deseni için - burası tek, tıklanabilir bir
// satır, farklı bir görsel dil).
function WritePromptCard() {
  return (
    <Link
      to="/wiki/new"
      className="flex items-center gap-2.5 rounded-lg border p-3.5 text-sm transition hover:border-[var(--brand-accent-border)]"
      style={{ borderColor: "var(--border)", background: "var(--bg)" }}
    >
      <span
        className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full"
        style={{ background: "var(--brand-accent-bg)", color: "var(--brand-accent)" }}
      >
        <Plus size={16} />
      </span>
      <span style={{ color: "var(--text)", opacity: 0.75 }}>Yazmaya başla...</span>
    </Link>
  );
}

// Kalıcı bildirim geçmişi (2026-08-15, Gün 2 - backend Gün 1'de eklendi,
// bkz. GetNotificationsQuery). Panel'in KENDİ fetch'ini yapan, self-contained
// bir alt bileşeni - DiscussionPanel'in (token alıp kendi verisini kendi
// çeken) AYNI deseni. Görünürlük filtresi backend'de ZATEN uygulanıyor
// (IWikiVisibilityChecker) - burası hiçbir departman/rol kontrolü YAPMIYOR,
// sadece API'nin döndürdüğünü gösteriyor (Wiki listesi/AI aramasıyla AYNI
// "gerçek yetkilendirme her zaman backend'de" ilkesi).
function NotificationsPanel({ token }) {
  const [notifications, setNotifications] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    getNotifications(token, 8)
      .then((result) => {
        if (!cancelled) setNotifications(result);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  return (
    <Panel title="Bildirimler" icon={<Bell size={13} />}>
      {error && (
        <p className="px-3.5 py-3 text-xs" style={{ color: "red" }}>
          {error}
        </p>
      )}
      {!error && notifications && notifications.length === 0 && (
        <p className="px-3.5 py-3 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
          Henüz bir bildirim yok.
        </p>
      )}
      {notifications?.slice(0, 5).map((n) => (
        <Link
          key={n.id}
          to={`/wiki/${n.resourceId}`}
          className="flex flex-col gap-0.5 px-3.5 py-2.5 text-xs hover:bg-[var(--brand-accent)]/5"
        >
          <span style={{ color: "var(--text)", opacity: 0.75 }}>
            <span className="font-semibold" style={{ color: "var(--text-h)" }}>
              {n.actorEmail ?? "Biri"}
            </span>{" "}
            yeni bir sayfa ekledi
          </span>
          <span className="truncate font-medium" style={{ color: "var(--brand-accent)" }}>
            {n.title}
          </span>
          <span style={{ color: "var(--text)", opacity: 0.5 }}>
            {n.departmentName} · {formatUtcTimestamp(n.createdAtUtc)}
          </span>
        </Link>
      ))}
    </Panel>
  );
}

// "Son Eklenen Makaleler" artık ikili büyük kutular yerine yoğun bir kart
// ızgarası (kullanıcı spec'i, "2. Ana Sayfa Kartları" - referans admin
// panelindeki gibi ekranı daha verimli dolduran, birbirine yakın yükseklikte
// kartlar). Kartın TAMAMI tek bir Link - "Devamını oku" bu yüzden kendi
// başına ayrı bir <a> DEĞİL (bir <a> içine ikinci bir <a> geçersiz HTML
// olurdu), sadece vurgulu bir metin.
function ArticleCard({ article }) {
  const excerpt = article.excerpt.endsWith("…") ? article.excerpt.slice(0, -1) + "…" : article.excerpt;

  return (
    <Link
      to={`/wiki/${article.id}`}
      className="group flex h-full flex-col overflow-hidden rounded-lg border border-[var(--border)] transition hover:-translate-y-0.5 hover:border-[var(--brand-accent-border)] hover:shadow-md"
      style={{ background: "var(--bg)" }}
    >
      {article.coverImageUrl ? (
        <img src={article.coverImageUrl} alt="" className="h-32 w-full object-cover" />
      ) : (
        <div className="flex h-32 w-full items-center justify-center" style={{ background: "var(--code-bg)" }}>
          <FileText size={26} style={{ color: "var(--text)", opacity: 0.3 }} />
        </div>
      )}

      <div className="flex flex-1 flex-col gap-1.5 p-3.5">
        <Badge variant="outline" className="w-fit text-[10px] font-normal">
          {article.departmentName}
        </Badge>

        <h3 className="line-clamp-2 text-sm leading-snug font-bold group-hover:underline" style={{ color: "var(--text-h)" }}>
          {article.title}
        </h3>

        <p className="line-clamp-2 flex-1 text-xs leading-relaxed" style={{ color: "var(--text)", opacity: 0.8 }}>
          {excerpt}
        </p>

        <div
          className="mt-1 flex items-center justify-between border-t pt-2 text-[11px]"
          style={{ borderColor: "var(--border)", color: "var(--text)", opacity: 0.65 }}
        >
          <span className="truncate">{article.createdByEmail ?? "Bilinmiyor"}</span>
          <span className="shrink-0">{formatUtcTimestamp(article.createdAtUtc)}</span>
        </div>

        <span className="text-xs font-semibold" style={{ color: "var(--brand-accent)" }}>
          Devamını oku →
        </span>
      </div>
    </Link>
  );
}

// "Son Eklenen Makaleler" carousel'ı (2026-08-17 takip, referans mockup'taki
// nokta işaretleri) - "Tümünü Gör" linkinin (başka sayfaya gider) YANINA,
// sayfada KALARAK ilerlemeyi sağlayan bir sayfalama ekliyor. Gerçek bir
// kaydırma/animasyon kütüphanesi EKLENMEDİ - sadece bir dizi dilimleme +
// nokta düğmeleri, projenin geri kalanındaki "framework'e ihtiyaç olmayan
// yerde framework ekleme" tercihiyle tutarlı. `page` state'i BİLEREK
// component'in kendi içinde - parent (HomePage) `articles` prop'unu sadece
// bir kez (dashboard fetch'i tamamlanınca) dolduruyor, sonrasında
// değişmiyor, bu yüzden bir reset mekanizması gerekmiyor.
function RecentArticlesCarousel({ articles }) {
  const [page, setPage] = useState(0);
  const totalPages = Math.ceil(articles.length / RECENT_ARTICLES_PAGE_SIZE);
  const start = page * RECENT_ARTICLES_PAGE_SIZE;
  const visible = articles.slice(start, start + RECENT_ARTICLES_PAGE_SIZE);

  return (
    <>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-2">
        {visible.map((a) => (
          <ArticleCard key={a.id} article={a} />
        ))}
      </div>

      {totalPages > 1 && (
        <div className="mt-3 flex items-center justify-center gap-1.5">
          {Array.from({ length: totalPages }, (_, i) => (
            <button
              key={i}
              type="button"
              onClick={() => setPage(i)}
              aria-label={`${i + 1}. sayfa`}
              aria-current={page === i}
              className="h-1.5 cursor-pointer rounded-full transition-all hover:opacity-80"
              style={{
                width: page === i ? "18px" : "6px",
                background: page === i ? "var(--brand-accent)" : "var(--border)",
              }}
            />
          ))}
        </div>
      )}
    </>
  );
}

// Hero (Görsel Tasarım Yenileme Gün 2, 2026-08-17) - kullanıcının referans
// mockup'ındaki gradient karşılama alanı + arama kutusu. `--gradient-hero`
// (index.css, Gün 1'de hazırlanmıştı) temadan BAĞIMSIZ SABİT bir gradient -
// bu yüzden üzerindeki TÜM metin/ikon/input rengi de temadan bağımsız
// SABİT (beyaz/yarı-saydam beyaz) tutuluyor, MiniStat'ın "onGradient"
// varyantı da bu yüzden var.
//
// Arama kutusu DEKORATİF DEĞİL - gerçek, çalışan bir arama: submit olunca
// `/wiki/pages?q=...`'a yönlendiriyor. Bu akışın WikiBoard/WikiSearch
// tarafı ZATEN VARDI (`initialQuery` prop'u + kendi useEffect'i, yorumunda
// "üst bardaki arama kutusundan yönlendirildiğinde" diye yazıyordu) ama
// hiçbir yer bu URL parametresini OKUMUYORDU - bağlanmamış bir uçtu,
// WikiBoard.jsx'e `useSearchParams` eklenerek TAMAMLANDI (Hero'ya özel bir
// ikinci arama mekanizması İCAT EDİLMEDİ).
function HeroSection({ fullName }) {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");

  function handleSubmit(e) {
    e.preventDefault();
    if (!query.trim()) return;
    navigate(`/wiki/pages?q=${encodeURIComponent(query.trim())}`);
  }

  return (
    <section className="relative overflow-hidden rounded-2xl px-6 py-8 md:px-10 md:py-10" style={{ background: "var(--gradient-hero)" }}>
      <h1 className="text-2xl font-bold tracking-tight md:text-3xl" style={{ color: "#ffffff" }}>
        Atlas Wiki'ye hoş geldiniz{fullName ? `, ${fullName}` : ""}! 👋
      </h1>
      <p className="mt-1.5 max-w-lg text-sm" style={{ color: "rgba(255,255,255,0.88)" }}>
        Tüm bilgi ve belgelere tek yerden ulaşın, paylaşın ve birlikte geliştirin.
      </p>

      <form onSubmit={handleSubmit} className="mt-5 flex max-w-lg gap-2">
        <div className="relative flex-1">
          <Search size={15} className="absolute top-1/2 left-3 -translate-y-1/2 opacity-60" style={{ color: "#0d222b" }} />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Aramak istediğiniz konuyu yazın..."
            className="w-full rounded-lg py-2.5 pr-3 pl-9 text-sm outline-none"
            style={{ background: "rgba(255,255,255,0.96)", color: "#0d222b" }}
          />
        </div>
        <Button type="submit" style={{ background: "#0d222b", color: "#ffffff" }} className="hover:opacity-90">
          Ara
        </Button>
      </form>
    </section>
  );
}

// "Öne Çıkan Makale" (Görsel Tasarım Yenileme Gün 2) - Son Eklenen
// Makaleler'in İLK'i (en yeni sayfa) burada büyük bir kart olarak tekrar
// vurgulanıyor. Ayrı bir "editör seçimi" kavramı İCAT EDİLMEDİ (backend'de
// böyle bir alan yok, eklemek YAGNI olurdu) - "en yeni sayfa" zaten doğal
// ve gerçek bir öncelik sinyali.
function FeaturedArticleCard({ article }) {
  const excerpt = article.excerpt.endsWith("…") ? article.excerpt.slice(0, -1) + "…" : article.excerpt;

  return (
    <Link
      to={`/wiki/${article.id}`}
      className="group flex flex-col overflow-hidden rounded-lg border transition hover:border-[var(--brand-accent-border)] md:flex-row"
      style={{ borderColor: "var(--border)", background: "var(--bg)" }}
    >
      {article.coverImageUrl ? (
        <img src={article.coverImageUrl} alt="" className="h-40 w-full object-cover md:h-auto md:w-64 md:shrink-0" />
      ) : (
        <div className="flex h-40 w-full items-center justify-center md:h-auto md:w-64 md:shrink-0" style={{ background: "var(--code-bg)" }}>
          <FileText size={32} style={{ color: "var(--text)", opacity: 0.3 }} />
        </div>
      )}
      <div className="flex flex-1 flex-col gap-2 p-5">
        <Badge variant="outline" className="w-fit text-[10px] font-normal" style={{ borderColor: "var(--accent-warm-border)", color: "var(--accent-warm)" }}>
          Öne Çıkan Makale
        </Badge>
        <h3 className="text-lg leading-snug font-bold group-hover:underline" style={{ color: "var(--text-h)" }}>
          {article.title}
        </h3>
        <p className="line-clamp-3 text-sm leading-relaxed" style={{ color: "var(--text)", opacity: 0.85 }}>
          {excerpt}
        </p>
        <div className="mt-auto flex items-center gap-2 pt-2 text-xs" style={{ color: "var(--text)", opacity: 0.65 }}>
          <span>{article.createdByEmail ?? "Bilinmiyor"}</span>
          <span>·</span>
          <span>{formatUtcTimestamp(article.createdAtUtc)}</span>
        </div>
      </div>
    </Link>
  );
}

// Küçük, kompakt liste satırı - Favoriler/Pinlenenler/Belgeler widget'larının
// hepsi AYNI iskeleti kullanıyor (ikon + başlık + alt bilgi), sadece ikon ve
// hedef URL değişiyor - dört ayrı liste bileşeni yazmak yerine tek, esnek
// bir satır bileşeni.
function CompactRow({ to, icon, title, subtitle }) {
  return (
    <Link to={to} className="flex items-center gap-2.5 px-3.5 py-2.5 text-sm hover:bg-[var(--brand-accent)]/5">
      <span className="shrink-0" style={{ color: "var(--brand-accent)" }}>
        {icon}
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium" style={{ color: "var(--text-h)" }}>
          {title}
        </p>
        {subtitle && (
          <p className="truncate text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
            {subtitle}
          </p>
        )}
      </div>
    </Link>
  );
}

// Favoriler/Pinlenenler (Görsel Tasarım Yenileme Gün 2) - kendi kendine
// yeten (self-contained), NotificationsPanel'le AYNI desen. Sayfada gerçek
// bir "ayrı bölüm" (spec madde 5/6) olarak SOL/geniş sütuna kondu - sağ
// sütun zaten kalabalık.
function SimplePageListPanel({ title, icon, fetcher, token, viewAllTo }) {
  const [pages, setPages] = useState(null);

  useEffect(() => {
    let cancelled = false;
    fetcher(token)
      .then((result) => {
        if (!cancelled) setPages(result);
      })
      .catch(() => {
        if (!cancelled) setPages([]);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  if (pages !== null && pages.length === 0) return null;

  return (
    <Panel
      title={title}
      icon={icon}
      action={
        <Link to={viewAllTo} className="text-[11px] font-medium hover:underline" style={{ color: "var(--brand-accent)" }}>
          Tümünü Gör
        </Link>
      }
    >
      {pages === null ? (
        <p className="px-3.5 py-3 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
          Yükleniyor...
        </p>
      ) : (
        pages.slice(0, 4).map((p) => (
          <CompactRow
            key={p.id}
            to={`/wiki/${p.id}`}
            icon={icon}
            title={p.title}
            subtitle={`${p.departmentName} · ${formatUtcTimestamp(p.createdAtUtc)}`}
          />
        ))
      )}
    </Panel>
  );
}

// [Departman] İçin Öne Çıkanlar (2026-08-17, ikinci tur kullanıcı geri
// bildirimi - sağ sütunun sticky/CSS-stretch İLE DEĞİL, GERÇEK kendi
// içeriğiyle uzaması isteniyordu). Backend'in `dashboard.departmentSpecific`
// alanı (GetWikiDashboardQueryHandler'da ZATEN hesaplanıyordu) şimdiye kadar
// frontend'de HİÇ render EDİLMİYORDU - sadece Count'u (Hero'nun altındaki
// MiniStat şeridinde) kullanılıyordu. Ek bir fetch/endpoint GEREKMEDİ, veri
// zaten `dashboard` objesinin içinde duruyordu. Departmanı olmayan (ya da
// giriş yapmamış) bir kullanıcı için backend zaten boş dizi döndürüyor
// (bkz. Handler'daki not), bu yüzden burada AYRICA bir "departman var mı"
// kontrolüne gerek yok - `pages.length === 0` kontrolü zaten yeterli.
function DepartmentHighlightsPanel({ pages, department }) {
  if (!pages || pages.length === 0) return null;

  return (
    <Panel title={`${department} İçin Öne Çıkanlar`} icon={<Building2 size={13} />}>
      {pages.slice(0, 5).map((p) => (
        <CompactRow
          key={p.id}
          to={`/wiki/${p.id}`}
          icon={<Building2 size={15} />}
          title={p.title}
          subtitle={formatUtcTimestamp(p.createdAtUtc)}
        />
      ))}
    </Panel>
  );
}

// Sol sütun - Tartışmalar'dan SONRA gelen beş bölüm (2026-08-17, üçüncü tur
// kullanıcı geri bildirimi: "sol kolon Tartışmalar'da bitip altında CSS'le
// doldurulmuş boşluk kalıyor, GERÇEK içerikle uzasın"). Hepsi AYNI Panel/
// CompactRow deseni - yeni bir "dashboard" İCAT EDİLMEDİ, sadece sağ
// sütunda ZATEN kullanılan görsel dil sol sütunda da tekrar ediyor.

// (1) Departmanlara Göre Öne Çıkanlar - DepartmentHighlightsPanel'in AKSİNE
// (o SADECE viewer'ın kendi departmanını gösteriyor) TÜM departmanları
// kapsıyor - ek bir fetch GEREKMEDİ, zaten çekilmiş olan gridArticles
// (Son Eklenen Makaleler carousel'ının kullandığı AYNI veri, bkz. HomePage
// içindeki hesaplama) departman bazında gruplanıp her departmanın en
// yenisi seçiliyor.
function DepartmentSpotlightPanel({ articles }) {
  if (!articles || articles.length === 0) return null;

  const latestByDepartment = new Map();
  for (const a of articles) {
    const existing = latestByDepartment.get(a.departmentName);
    if (!existing || new Date(a.createdAtUtc) > new Date(existing.createdAtUtc)) {
      latestByDepartment.set(a.departmentName, a);
    }
  }
  const entries = Array.from(latestByDepartment.values()).sort(
    (a, b) => new Date(b.createdAtUtc) - new Date(a.createdAtUtc)
  );
  if (entries.length === 0) return null;

  return (
    <Panel title="Departmanlara Göre Öne Çıkanlar" icon={<Building2 size={13} />}>
      {entries.map((a) => (
        <CompactRow
          key={a.id}
          to={`/wiki/${a.id}`}
          icon={<Building2 size={15} />}
          title={a.title}
          subtitle={`${a.departmentName} · ${formatUtcTimestamp(a.createdAtUtc)}`}
        />
      ))}
    </Panel>
  );
}

// (2) En Aktif Katkıda Bulunanlar - RecentVideosWidget'ın AYNI "kendi
// verisini kendi çeken, dashboard'a bağımlı olmayan" deseni (getWikiPages
// ile 50 sayfayı tarayıp CreatedByEmail'e göre sayıyor) - dashboard
// endpoint'i BÜTÜN yazarları saymaya yetecek kadar veri döndürmüyor,
// yeni bir backend endpoint'i İCAT ETMEK yerine var olan GET /api/wiki/pages
// zaten yeterli.
function TopContributorsPanel({ token }) {
  const [contributors, setContributors] = useState(null);

  useEffect(() => {
    let cancelled = false;
    getWikiPages(token, 1, 50)
      .then((result) => {
        if (cancelled) return;
        const counts = new Map();
        for (const page of result.items) {
          if (!page.createdByEmail) continue;
          counts.set(page.createdByEmail, (counts.get(page.createdByEmail) ?? 0) + 1);
        }
        const sorted = Array.from(counts.entries())
          .sort((a, b) => b[1] - a[1])
          .slice(0, 5)
          .map(([email, count]) => ({ email, count }));
        setContributors(sorted);
      })
      .catch(() => {
        if (!cancelled) setContributors([]);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  if (contributors !== null && contributors.length === 0) return null;

  return (
    <Panel title="En Aktif Katkıda Bulunanlar" icon={<Users size={13} />}>
      {contributors === null ? (
        <p className="px-3.5 py-3 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
          Yükleniyor...
        </p>
      ) : (
        contributors.map((c) => (
          <div key={c.email} className="flex items-center justify-between px-3.5 py-2.5 text-sm">
            <span className="truncate" style={{ color: "var(--text-h)" }}>
              {c.email}
            </span>
            <Badge variant="outline" className="shrink-0 text-[10px] font-normal">
              {c.count} sayfa
            </Badge>
          </div>
        ))
      )}
    </Panel>
  );
}

// (3) Son Aktiviteler - sağ sütundaki "Son Güncellenenler" kutusunun
// (SADECE güncellemeleri, tarih listesi olarak gösteren) AKSİNE - hem
// OLUŞTURULMA hem GÜNCELLENME olaylarını TEK bir kronolojik akışta,
// tam CompactRow kartlarıyla birleştiriyor. dashboard.recentlyAdded/
// recentlyUpdated ZATEN fetch edilmiş veri - ek bir çağrı GEREKMEDİ.
function ActivityFeedPanel({ added, updated }) {
  const events = [
    ...(added ?? []).map((p) => ({ ...p, kind: "created", timestamp: p.createdAtUtc })),
    ...(updated ?? []).map((p) => ({ ...p, kind: "updated", timestamp: p.updatedAtUtc })),
  ]
    .filter((e) => e.timestamp)
    .sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))
    .slice(0, 6);

  if (events.length === 0) return null;

  return (
    <Panel title="Son Aktiviteler" icon={<Clock size={13} />}>
      {events.map((e, idx) => (
        <CompactRow
          key={`${e.id}-${e.kind}-${idx}`}
          to={`/wiki/${e.id}`}
          icon={e.kind === "created" ? <FilePlus2 size={15} /> : <Clock size={15} />}
          title={e.title}
          subtitle={`${e.kind === "created" ? "Oluşturuldu" : "Güncellendi"} · ${formatUtcTimestamp(e.timestamp)}`}
        />
      ))}
    </Panel>
  );
}

// (4) Hızlı Başlangıç Rehberi - BİLEREK statik (veri gerektirmiyor) - yeni
// bir kullanıcının platformu nasıl kullanacağını anlatan sabit bir adım
// listesi, Hızlı Erişim panelinin (sağ sütun) AYNI CompactRow-benzeri satır
// deseninde ama linksiz (her adım bir eylem değil, bir açıklama).
function GettingStartedPanel() {
  const steps = [
    { icon: <Search size={15} />, text: "Arama kutusuyla aradığın bilgiyi anında bul" },
    { icon: <Plus size={15} />, text: '"Yeni Sayfa Oluştur" ile kendi bilgini paylaş' },
    { icon: <Star size={15} />, text: "Sık kullandığın sayfaları favorile veya pinle" },
    { icon: <Upload size={15} />, text: "Belgelerini yükleyip ekip arkadaşlarınla paylaş" },
  ];

  return (
    <Panel title="Hızlı Başlangıç Rehberi" icon={<ListChecks size={13} />}>
      {steps.map((s, idx) => (
        <div key={idx} className="flex items-center gap-3 px-3.5 py-2.5 text-sm" style={{ color: "var(--text)" }}>
          <span className="shrink-0" style={{ color: "var(--brand-accent)" }}>
            {s.icon}
          </span>
          {s.text}
        </div>
      ))}
    </Panel>
  );
}

// (5) Daha Fazla Keşfet - sol sütunun kapanışı, sayfanın diğer büyük
// alanlarına (Tüm Sayfalar/Video Merkezi/Belgeler) yönlendiren sade bir
// CTA - ayrı bir "keşfet" sayfası İCAT EDİLMEDİ, var olan üç rotaya
// yönlendiriyor.
function ExplorePanel() {
  return (
    <Panel title="Daha Fazla Keşfet" icon={<ArrowRight size={13} />}>
      <CompactRow to="/wiki/pages" icon={<FileText size={15} />} title="Tüm Sayfaları Gör" />
      <CompactRow to="/wiki/videos" icon={<Video size={15} />} title="Video Merkezi" />
      <CompactRow to="/documents" icon={<Upload size={15} />} title="Belge Kütüphanesi" />
    </Panel>
  );
}

// Belgeler (Görsel Tasarım Yenileme Gün 2) - Documents modülünden gerçek
// veri, format-özel ikonlar (documentIcons.js, DocumentDetailPage'in ZATEN
// kullandığı AYNI harita - burada ikinci bir ikon eşlemesi İCAT EDİLMEDİ).
function DocumentsWidget({ token }) {
  const [documents, setDocuments] = useState(null);

  useEffect(() => {
    let cancelled = false;
    // 5 -> 8 (2026-08-17, ikinci tur geri bildirim) - sağ sütun artık
    // sticky/stretch DEĞİL, gerçek içerikle uzuyor; bu panel de kendi
    // payına biraz daha fazla gerçek öğe gösteriyor.
    getDocuments(token, { pageSize: 8 })
      .then((result) => {
        if (!cancelled) setDocuments(result.items);
      })
      .catch(() => {
        if (!cancelled) setDocuments([]);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  if (documents !== null && documents.length === 0) return null;

  return (
    <Panel
      title="Belgeler"
      icon={<FileText size={13} />}
      action={
        <Link to="/documents" className="text-[11px] font-medium hover:underline" style={{ color: "var(--brand-accent)" }}>
          Tümünü Gör
        </Link>
      }
    >
      {documents === null ? (
        <p className="px-3.5 py-3 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
          Yükleniyor...
        </p>
      ) : (
        documents.map((d) => {
          const Icon = getDocumentIcon(d.fileExtension);
          return <CompactRow key={d.id} to={`/documents/${d.id}`} icon={<Icon size={15} />} title={d.title} subtitle={d.departmentName} />;
        })
      )}
    </Panel>
  );
}

// Videolar/Eğitimler (Görsel Tasarım Yenileme Gün 2) - VideoCenterPage'in
// (Eksik-özellik listesi C grubu) AYNI çıkarma mantığını (extractVideosFromContent)
// tekrar kullanıyor, ikinci bir video-algılama YAZILMADI. VideoCenterPage'in
// AKSİNE burada TÜM sayfalar taranmıyor - sadece son ~20 sayfalık bir dilim
// (ana sayfa widget'ı için yeterli, tam galeri zaten /wiki/videos'ta).
function RecentVideosWidget({ token }) {
  const [videos, setVideos] = useState(null);

  useEffect(() => {
    let cancelled = false;
    getWikiPages(token, 1, 20)
      .then((result) => {
        if (cancelled) return;
        const found = [];
        for (const page of result.items) {
          for (const v of extractVideosFromContent(page.content)) {
            found.push({ ...v, pageId: page.id, pageTitle: page.title });
          }
        }
        setVideos(found);
      })
      .catch(() => {
        if (!cancelled) setVideos([]);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  if (videos !== null && videos.length === 0) return null;

  return (
    <Panel
      title="Videolar / Eğitimler"
      icon={<Video size={13} />}
      action={
        <Link to="/wiki/videos" className="text-[11px] font-medium hover:underline" style={{ color: "var(--brand-accent)" }}>
          Tümünü Gör
        </Link>
      }
    >
      {videos === null ? (
        <p className="px-3.5 py-3 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
          Yükleniyor...
        </p>
      ) : (
        // 4 -> 6 (2026-08-17, ikinci tur geri bildirim - aynı gerekçe).
        videos.slice(0, 6).map((v, idx) => (
          <CompactRow
            key={`${v.pageId}-${idx}`}
            to={`/wiki/${v.pageId}`}
            icon={<Video size={15} />}
            title={v.caption || v.pageTitle}
            subtitle={v.pageTitle}
          />
        ))
      )}
    </Panel>
  );
}

// Tartışmalar (Görsel Tasarım Yenileme Gün 2) - platform GENELİNE ait
// yorumlar (pageId=null, DiscussionPanel'in "Anasayfa Tartışması" sekmesiyle
// AYNI veri kaynağı - bkz. Comment.PageId'nin backend'deki notu). "Tartışmaya
// Katıl" CTA'sı ayrı bir sayfaya DEĞİL, aşağıdaki mevcut "Tartışma" sekmesine
// geçiyor - ikinci bir tartışma sayfası İCAT EDİLMEDİ.
function DiscussionsWidget({ token, onJoinDiscussion }) {
  const [comments, setComments] = useState(null);

  useEffect(() => {
    let cancelled = false;
    getComments(token)
      .then((result) => {
        if (!cancelled) setComments(result);
      })
      .catch(() => {
        if (!cancelled) setComments([]);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  if (comments !== null && comments.length === 0) return null;

  return (
    <section className="overflow-hidden rounded-lg border" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
      <div className="flex items-center justify-between border-b px-3.5 py-2.5" style={{ borderColor: "var(--border)" }}>
        <h2 className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider" style={{ color: "var(--text-h)" }}>
          <MessageSquare size={13} /> Tartışmalar
        </h2>
        <button
          type="button"
          onClick={onJoinDiscussion}
          className="text-[11px] font-medium hover:underline"
          style={{ color: "var(--brand-accent)" }}
        >
          Tartışmaya Katıl
        </button>
      </div>
      {comments === null ? (
        <p className="px-3.5 py-3 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
          Yükleniyor...
        </p>
      ) : (
        <ul className="flex flex-col divide-y" style={{ borderColor: "var(--border)" }}>
          {comments.slice(0, 4).map((c) => (
            <li key={c.id} className="px-3.5 py-2.5 text-sm">
              <p style={{ color: "var(--text)" }}>
                <span className="font-semibold" style={{ color: "var(--text-h)" }}>
                  {c.authorEmail ?? "Bilinmiyor"}
                </span>{" "}
                <span className="opacity-60">· {formatUtcTimestamp(c.createdAtUtc)}</span>
              </p>
              <p className="line-clamp-2 text-xs" style={{ color: "var(--text)", opacity: 0.8 }}>
                {c.content}
              </p>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

// İstatistikler donut grafiği (Görsel Tasarım Yenileme Gün 2) - GERÇEK
// departman dağılımı verisi (backend'e KÜÇÜK bir alan eklendi -
// WikiDashboardDto.DepartmentBreakdown, Handler'ın ZATEN bellekte tuttuğu
// visiblePages'ten türetiliyor, yeni bir sorgu/endpoint gerekmedi). Harici
// bir grafik kütüphanesi EKLENMEDİ - CSS conic-gradient ile saf bir donut,
// ortasında bir "delik" (page-bg renginde iç daire) - grafik kütüphanesi
// olmadan en basit, güvenilir yöntem.
function DepartmentDonutChart({ breakdown, total }) {
  if (!breakdown || breakdown.length === 0 || total === 0) return null;

  // Sabit bir renk paleti YOK (bkz. Gün 1'deki "kategorik çoklu renk"
  // kararı - sade kalmak için brand-accent'in farklı opaklık tonları
  // kullanılıyor, her departman için ayrı, alakasız bir hue İCAT EDİLMEDİ).
  const opacities = [1, 0.75, 0.55, 0.4, 0.28];
  let cumulativePercent = 0;
  const segments = breakdown.slice(0, 5).map((d, idx) => {
    const percent = (d.count / total) * 100;
    const start = cumulativePercent;
    cumulativePercent += percent;
    return { ...d, percent, start, end: cumulativePercent, opacity: opacities[idx] ?? 0.2 };
  });

  const gradientStops = segments
    .map((s) => `color-mix(in srgb, var(--brand-accent) ${s.opacity * 100}%, transparent) ${s.start}% ${s.end}%`)
    .join(", ");

  return (
    <Panel title="İstatistikler" icon={<Users size={13} />}>
      <div className="flex items-center gap-4 p-4">
        <div
          className="relative flex h-24 w-24 shrink-0 items-center justify-center rounded-full"
          style={{ background: `conic-gradient(${gradientStops})` }}
        >
          <div className="flex h-14 w-14 items-center justify-center rounded-full" style={{ background: "var(--bg)" }}>
            <div className="text-center">
              <p className="text-base leading-none font-bold" style={{ color: "var(--text-h)" }}>
                {total}
              </p>
              <p className="text-[9px]" style={{ color: "var(--text)", opacity: 0.6 }}>
                Toplam
              </p>
            </div>
          </div>
        </div>
        <ul className="flex flex-1 flex-col gap-1.5 text-xs">
          {segments.map((s) => (
            <li key={s.departmentName} className="flex items-center justify-between gap-2">
              <span className="flex items-center gap-1.5 truncate" style={{ color: "var(--text)" }}>
                <span
                  className="h-2 w-2 shrink-0 rounded-full"
                  style={{ background: `color-mix(in srgb, var(--brand-accent) ${s.opacity * 100}%, transparent)` }}
                />
                {s.departmentName}
              </span>
              <span className="shrink-0 font-semibold" style={{ color: "var(--text-h)" }}>
                {s.count} ({Math.round(s.percent)}%)
              </span>
            </li>
          ))}
        </ul>
      </div>
    </Panel>
  );
}

// Footer (Görsel Tasarım Yenileme Gün 2) - sade, GERÇEK linkler (mockup'taki
// "SSS"/"Destek Talebi"/sahte sosyal medya ikonları gibi, bu projede
// karşılığı OLMAYAN dekoratif/ölü linkler BİLEREK EKLENMEDİ - bu projenin
// baştan beri sürdürdüğü "dekoratif/çalışmayan bir şey gösterme" ilkesi,
// bkz. Favoriler/Pinlenenler'in eskiden localStorage'dan gerçek backend'e
// taşınma gerekçesi).
function HomeFooter() {
  return (
    <footer className="mt-4 border-t pt-5 pb-2 text-xs" style={{ borderColor: "var(--border)", color: "var(--text)", opacity: 0.7 }}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span>Atlas Wiki - Şirket bilgi platformu</span>
        <nav className="flex flex-wrap items-center gap-3">
          <Link to="/wiki" className="hover:underline">Anasayfa</Link>
          <Link to="/wiki/pages" className="hover:underline">Tüm Sayfalar</Link>
          <Link to="/wiki/favorites" className="hover:underline">Favoriler</Link>
          <Link to="/wiki/pinned" className="hover:underline">Pinlenenler</Link>
          <Link to="/wiki/videos" className="hover:underline">Video Merkezi</Link>
          <Link to="/documents" className="hover:underline">Belgeler</Link>
        </nav>
      </div>
    </footer>
  );
}

// "Son Eklenen Makaleler" carousel'ı (2026-08-17 takip) - Öne Çıkan Makale
// (1) + 3 "sayfa" x 4 kart = 13. Sayfa boyutu (4) BİLEREK mevcut 2 sütunlu
// grid'le (2x2) eşleşiyor.
const RECENT_ARTICLES_PAGE_SIZE = 4;
const RECENT_ARTICLES_TOTAL = 1 + RECENT_ARTICLES_PAGE_SIZE * 3;

// Giriş yapınca artık doğrudan makale listesine değil buraya (Dashboard)
// geliniyor (bkz. App.jsx - /wiki index route'u).
function HomePage({ token }) {
  const { isAdmin, fullName, department: ownDepartment } = useMemo(() => getUserInfoFromToken(token), [token]);
  const [dashboard, setDashboard] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    getWikiDashboard(token, RECENT_ARTICLES_TOTAL)
      .then((result) => {
        if (!cancelled) setDashboard(result);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  // Kapak görsele sahip sayfalar hafifçe önceliklendiriliyor (ızgara tamamen
  // görselsiz kartlarla dolmasın diye), ama görselsiz sayfalar da (bkz.
  // ArticleCard'ın placeholder'ı) listeden hiç ATILMIYOR. Array.sort() stabil
  // olduğu için (modern JS motorları) bu sıralama, backend'in zaten
  // en-yeni-önce verdiği sırayı grup içinde BOZMUYOR.
  const gridArticles = [...(dashboard?.recentlyAdded ?? [])]
    .sort((a, b) => (b.coverImageUrl ? 1 : 0) - (a.coverImageUrl ? 1 : 0));
  // Kullanıcı isteği (2026-08-17, "ilk sayfada 3-5 tane gözükecek, devamı
  // tıklanınca gözükebilir") - Öne Çıkan Makale ZATEN en yeniyi ayrıca büyük
  // gösterdiği için, ikinci koleksiyon SIRADAKİ'leri bir carousel'da (bkz.
  // RecentArticlesCarousel) sayfa sayfa (4'er) gösteriyor - aynı sayfa iki
  // kez görünmüyor, "Tümünü Gör" linki de AYRICA duruyor.
  const featuredArticle = gridArticles[0] ?? null;
  const recentArticles = gridArticles.slice(1);
  const recentUpdates = (dashboard?.recentlyUpdated ?? []).slice(0, 5);

  const [activeTab, setActiveTab] = useState("home");

  return (
    <div className="flex w-full flex-col gap-4 px-4 py-2 text-left md:px-6">
      {/* Wikipedia Subtabs Bar */}
      <div className="flex flex-wrap items-center justify-between border-b" style={{ borderColor: "var(--border)" }}>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setActiveTab("home")}
            className={`wiki-tab ${activeTab === "home" ? "active" : ""}`}
          >
            Anasayfa
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("talk")}
            className={`wiki-tab ${activeTab === "talk" ? "active" : ""}`}
          >
            Tartışma
          </button>
        </div>

        <div className="flex items-center gap-1 text-xs font-medium">
          <span className="wiki-tab active">Oku</span>
          <Link to="/wiki/pages" className="wiki-tab">
            Tüm Sayfalar
          </Link>
          <Link to="/wiki/new" className="wiki-tab" title="Yeni Sayfa" aria-label="Yeni Sayfa">
            <Plus size={14} />
          </Link>
        </div>
      </div>

      {/* Hızlı Erişim şeridi */}
      <div className="flex flex-wrap items-center gap-2">
        <QuickActionButton icon={<ListChecks size={14} />} label="Tüm Sayfalar" to="/wiki/pages" />
        <QuickActionButton icon={<Clock size={14} />} label="Son Güncellenenler" href="#son-guncellemeler" />
        <QuickActionButton icon={<Star size={14} />} label="Favoriler" to="/wiki/favorites" />
        <QuickActionButton icon={<Pin size={14} />} label="Pinlenenler" to="/wiki/pinned" />
        <QuickActionButton icon={<Video size={14} />} label="Video Merkezi" to="/wiki/videos" />
        {isAdmin && <QuickActionButton icon={<ShieldCheck size={14} />} label="Audit Log" to="/audit-log" />}
      </div>

      {error && <p style={{ color: "red" }} className="text-xs">{error}</p>}

      {activeTab === "talk" ? (
        <div className="rounded-lg border p-6" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
          <h2 className="mb-1 text-base font-semibold" style={{ color: "var(--text-h)" }}>
            Anasayfa Tartışması
          </h2>
          <p className="mb-4 text-xs" style={{ color: "var(--text)", opacity: 0.7 }}>
            Genel platform konuları ve duyurular için alan.
          </p>
          <DiscussionPanel token={token} />
        </div>
      ) : dashboard && (
        <>
          {/* (1) HERO - Görsel Tasarım Yenileme Gün 2 */}
          <HeroSection fullName={fullName} />

          {/* (2) İstatistik şeridi - eskiden Hero'nun içindeydi, artık
              Hero'nun HEMEN ALTINDA, ayrı ince bir satır (Hero'nun gradient
              zemini üzerinde ÇOK fazla öğe sıkışmasın diye). */}
          <div className="flex flex-wrap items-center gap-x-5 gap-y-1.5 border-b pb-3" style={{ borderColor: "var(--border)" }}>
            <MiniStat icon={<FileText size={13} />} value={dashboard.totalPageCount} label="toplam makale" />
            <MiniStat icon={<FilePlus2 size={13} />} value={dashboard.addedThisWeekCount} label="bu hafta eklendi" />
            <MiniStat icon={<Clock size={13} />} value={dashboard.updatedThisWeekCount} label="bu hafta güncellendi" />
            {ownDepartment && (
              <MiniStat icon={<Users size={13} />} value={dashboard.departmentSpecificCount} label={ownDepartment} />
            )}
          </div>

          {/* (3) Öne Çıkan Makale */}
          {featuredArticle && (
            <section>
              <h2 className="mb-3 text-sm font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
                Öne Çıkan Makale
              </h2>
              <FeaturedArticleCard article={featuredArticle} />
            </section>
          )}

          {/* İKİ SÜTUN, İKİSİ DE DOĞAL UZUNLUKTA (2026-08-17 takip, ikinci
              tur kullanıcı geri bildirimi) - önceki denemede sağ sütun
              xl:sticky + grid'in stretch'i ile sol sütunun yüksekliğine
              CSS'le "gerilmişti" (kutusu uzuyordu ama içeriği hâlâ üstte
              kompakt duruyordu). Kullanıcı bunu AÇIKÇA reddetti: "sağ
              tarafın boş kalan yüksekliğini CSS ile doldurmanı istemiyorum,
              sağ sütunun İÇERİĞİNİN kendisinin de aşağı doğru devam
              etmesini istiyorum" - yani istenen sahte bir yükseklik eşitliği
              DEĞİL, HER İKİ sütunun da kendi GERÇEK içerik akışıyla uzaması.
              Düzeltme: xl:sticky KALDIRILDI (sağ sütun artık sayfayla
              BİRLİKTE normal akışta kayıyor, viewport'a sabitlenmiyor),
              xl:items-start GERİ KONDU (grid'in stretch'i YOK - her sütunun
              kutusu SADECE kendi içeriği kadar, yapay bir eşitleme yok).
              Sağ sütun artık DAHA UZUN çünkü GERÇEKTEN daha fazla panel
              taşıyor (bkz. aşağıdaki DepartmentHighlightsPanel + Belgeler/
              Videolar panellerinin büyütülmüş öğe sayısı). */}
          <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1fr_320px] xl:items-start">
            {/* SOL/GENİŞ sütun */}
            <div className="flex min-w-0 flex-col gap-6">
              {recentArticles.length > 0 && (
                <section>
                  <div className="mb-3 flex items-center justify-between">
                    <h2 className="text-sm font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
                      Son Eklenen Makaleler
                    </h2>
                    <Link
                      to="/wiki/pages"
                      className="flex items-center gap-1 text-xs font-medium hover:opacity-80"
                      style={{ color: "var(--brand-accent)" }}
                    >
                      Tümünü Gör <ArrowRight size={13} />
                    </Link>
                  </div>
                  <RecentArticlesCarousel articles={recentArticles} />
                </section>
              )}

              <SimplePageListPanel
                title="Favorilere Eklenenler"
                icon={<Star size={13} />}
                fetcher={getFavoritePages}
                token={token}
                viewAllTo="/wiki/favorites"
              />

              <SimplePageListPanel
                title="Pinlenenler"
                icon={<Pin size={13} />}
                fetcher={getPinnedPages}
                token={token}
                viewAllTo="/wiki/pinned"
              />

              <DiscussionsWidget token={token} onJoinDiscussion={() => setActiveTab("talk")} />

              {/* Tartışmalar'dan SONRA, sol sütunun DEVAMI olarak beş yeni
                  bölüm (2026-08-17, üçüncü tur kullanıcı geri bildirimi -
                  "sol kolon Tartışmalar'da bitip altında boşluk kalıyor,
                  CSS ile DEĞİL gerçek içerikle uzasın"). Hepsi AYNI Panel/
                  CompactRow görsel dilini kullanıyor (sağ sütunla, Favoriler/
                  Pinlenenler'le BİREBİR aynı kart/spacing/typography/border) -
                  farklı bir "yeni dashboard" İCAT EDİLMEDİ. */}
              <DepartmentSpotlightPanel articles={gridArticles} />
              <TopContributorsPanel token={token} />
              <ActivityFeedPanel added={dashboard.recentlyAdded} updated={dashboard.recentlyUpdated} />
              <GettingStartedPanel />
              <ExplorePanel />
            </div>

            {/* SAĞ/DAR sütun */}
            <aside id="son-guncellemeler" className="flex scroll-mt-4 flex-col gap-4">
              <WritePromptCard />
              <NotificationsPanel token={token} />

              {dashboard.popularTags.length > 0 && (
                <Panel title="Kategoriler" icon={<Tag size={13} />}>
                  <div className="grid grid-cols-2 gap-2 p-3">
                    {dashboard.popularTags.map((t) => (
                      <div
                        key={t.tag}
                        className="flex flex-col gap-0.5 rounded-lg border px-2.5 py-2"
                        style={{ borderColor: "var(--border)" }}
                      >
                        <span className="truncate text-xs font-semibold" style={{ color: "var(--text-h)" }}>
                          {t.tag}
                        </span>
                        <span className="text-[10px]" style={{ color: "var(--text)", opacity: 0.6 }}>
                          {t.count} makale
                        </span>
                      </div>
                    ))}
                  </div>
                </Panel>
              )}

              <DocumentsWidget token={token} />
              <RecentVideosWidget token={token} />
              {recentUpdates.length > 0 && <RecentUpdatesBox updates={recentUpdates} />}
              <DepartmentDonutChart breakdown={dashboard.departmentBreakdown} total={dashboard.totalPageCount} />

              <Panel title="Hızlı Erişim" icon={<ArrowRight size={13} />}>
                <CompactRow to="/wiki/new" icon={<Plus size={15} />} title="Yeni Sayfa Oluştur" />
                <CompactRow to="/documents/upload" icon={<Upload size={15} />} title="Belge Yükle" />
              </Panel>

              {ownDepartment && (
                <DepartmentHighlightsPanel pages={dashboard.departmentSpecific} department={ownDepartment} />
              )}
            </aside>
          </div>

          <HomeFooter />
        </>
      )}
    </div>
  );
}

export default HomePage;
