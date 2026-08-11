import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";
import {
  ArrowRight,
  Clock,
  FilePlus2,
  FileText,
  ListChecks,
  Pin,
  Plus,
  ShieldCheck,
  Star,
  Users,
} from "lucide-react";
import { getWikiDashboard } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { Badge } from "@atlas/ui/badge";
import DiscussionPanel from "./DiscussionPanel";

// Kullanıcının verdiği referans mockup'taki (2026-08-04) "kart" deseni - başlıklı,
// bir sağ üst köşe eylemi (opsiyonel) olabilen, alt satırları divide-y ile
// ayrılan bir panel.
function Panel({ title, action, children }) {
  return (
    <section className="overflow-hidden rounded-lg border" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
      <div className="flex items-center justify-between border-b px-3.5 py-2.5" style={{ borderColor: "var(--border)" }}>
        <h2 className="text-xs font-semibold uppercase tracking-wider" style={{ color: "var(--text-h)" }}>
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
function MiniStat({ icon, value, label }) {
  return (
    <span className="flex items-center gap-1.5 text-xs font-medium whitespace-nowrap" style={{ color: "var(--text)" }}>
      <span style={{ color: "var(--brand-accent)" }}>{icon}</span>
      <span className="font-bold" style={{ color: "var(--text-h)" }}>
        {value}
      </span>
      <span style={{ opacity: 0.7 }}>{label}</span>
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

function PopularTagRow({ tag, count }) {
  return (
    <div className="flex items-center justify-between px-3.5 py-2">
      <span className="flex items-center gap-2 text-xs font-medium" style={{ color: "var(--text-h)" }}>
        <span className="h-1.5 w-1.5 shrink-0 rounded-full" style={{ background: "var(--brand-accent)" }} />
        {tag}
      </span>
      <span
        className="rounded-full px-2 py-0.5 text-[11px] font-semibold"
        style={{ background: "var(--code-bg)", color: "var(--text)" }}
      >
        {count}
      </span>
    </div>
  );
}

// Wikipedia'nın "Tarihte bugün" kutusunun bizdeki karşılığı - tarihsel trivia
// YERİNE (elimizde böyle bir veri yok), platformdaki GERÇEK son güncellemeleri
// listeliyoruz. Aynı "kutu + madde listesi + alt bilgi satırı" iskeleti
// korunuyor, sadece içerik platforma özgü.
function RecentUpdatesBox({ updates }) {
  return (
    <Panel title="Son Güncellemeler">
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
      className="group flex h-full flex-col overflow-hidden rounded-lg border transition hover:shadow-md"
      style={{ borderColor: "var(--border)", background: "var(--bg)" }}
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

// Giriş yapınca artık doğrudan makale listesine değil buraya (Dashboard)
// geliniyor (bkz. App.jsx - /wiki index route'u).
//
// 2026-08-07 - kapsamlı yeniden yerleşim (kullanıcının 8 maddelik "Atlas
// Wiki Ana Sayfasını Yeniden Tasarla" spec'i + referans admin panel
// görüntüsü). Önceki sürümdeki geniş boşluklar (max-w-6xl + büyük karşılama
// kutusu + ikili büyük makale kutuları) kaldırıldı:
// - Dış kap artık max-w-6xl DEĞİL, w-full (bkz. aşağıdaki kök div) -
//   WikiLayout'un <main>'i zaten kapsız (bkz. oradaki not), boşluğun asıl
//   kaynağı burasıydı.
// - Karşılama kutusu küçültüldü, istatistikler artık ayrı bir kart ızgarası
//   değil, karşılama satırının İÇİNDE küçük MiniStat'lar.
// - "Hızlı Erişim" artık bir Panel değil, üstte ince bir ikon şeridi
//   (QuickActionButton).
// - Makale ızgarası artık 2 büyük ikili kutu değil, yoğun bir kart ızgarası
//   (ArticleCard, 3/2/1 sütun - bkz. aşağıdaki grid className).
// - Son Güncellemeler + Popüler Kategoriler ikinci plana alınıp EN ALTA,
//   daha küçük şekilde taşındı (spec madde 6: "istatistik kartları ikinci
//   planda kalmalı").
function HomePage({ token }) {
  const { isAdmin, fullName, department: ownDepartment } = useMemo(() => getUserInfoFromToken(token), [token]);
  const [dashboard, setDashboard] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    getWikiDashboard(token)
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
  // ArticleCard'ın placeholder'ı) listeden hiç ATILMIYOR. 9'a çıkarıldı
  // (eskiden 6) - kart ızgarası artık daha yoğun (xl:grid-cols-3), 9 kart
  // 3 satırı düzgün dolduruyor.
  const gridArticles = [...(dashboard?.recentlyAdded ?? [])]
    .sort((a, b) => (b.coverImageUrl ? 1 : 0) - (a.coverImageUrl ? 1 : 0))
    .slice(0, 9);
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

        {/* Kullanıcı geri bildirimi (2026-08-07, YEDİNCİ geçiş - "yeni sayfa
            yazısı iki tane olmuş... okunun yanındaki kalsın işareti olsun
            sadece artı şeklinde") - "Yeni Sayfa" hem burada (Oku'nun
            yanında) HEM aşağıdaki Hızlı Erişim şeridinde tekrarlanıyordu.
            Aşağıdakinden kaldırıldı, burası kalıp metin yerine sade bir "+"
            ikonuna indirildi. */}
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

      {/* Hızlı Erişim şeridi - spec madde 5, büyük kartlar yerine küçük
          ikonlu butonlar, üst tarafta. "Yeni Sayfa" (yukarıdaki tab çubuğunda
          zaten var) ve "Gelişmiş Arama" (üst bardaki arama kutusuyla AYNI yere
          - /wiki/pages - gidiyordu, "Tüm Sayfalar" ile birebir aynıydı)
          kullanıcı geri bildirimiyle (2026-08-07) kaldırıldı. */}
      <div className="flex flex-wrap items-center gap-2">
        <QuickActionButton icon={<ListChecks size={14} />} label="Tüm Sayfalar" to="/wiki/pages" />
        <QuickActionButton icon={<Clock size={14} />} label="Son Güncellenenler" href="#son-guncellemeler" />
        <QuickActionButton icon={<Star size={14} />} label="Favoriler" to="/wiki/favorites" />
        <QuickActionButton icon={<Pin size={14} />} label="Pinlenenler" to="/wiki/pinned" />
        {isAdmin && <QuickActionButton icon={<ShieldCheck size={14} />} label="Audit Log" to="/audit-log" />}
      </div>

      {/* Küçültülmüş karşılama satırı - spec madde 3, "Karşılama alanı
          küçültülsün... altında direkt içerikler başlasın". Eskiden ayrı bir
          kart + tek bir "Toplam Sayfa Sayısı" rozeti vardı, alttaki 4'lü
          StatCard ızgarası ayrı bir bölümdü - üçü de burada, tek satırlık
          ince bir şeride indirildi. */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-b pb-3" style={{ borderColor: "var(--border)" }}>
        <div>
          <h1 className="text-lg font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
            Atlas Wiki'ye hoş geldiniz{fullName ? `, ${fullName}` : ""}!
          </h1>
          <p className="text-xs" style={{ color: "var(--text)", opacity: 0.75 }}>
            Şirket bilgi platformuna hoş geldiniz - aradığınız tüm bilgiye tek noktadan ulaşın.
          </p>
        </div>
        {dashboard && (
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
            <MiniStat icon={<FileText size={13} />} value={dashboard.totalPageCount} label="toplam makale" />
            <MiniStat icon={<FilePlus2 size={13} />} value={dashboard.addedThisWeekCount} label="bu hafta eklendi" />
            <MiniStat icon={<Clock size={13} />} value={dashboard.updatedThisWeekCount} label="bu hafta güncellendi" />
            {ownDepartment && (
              <MiniStat icon={<Users size={13} />} value={dashboard.departmentSpecificCount} label={ownDepartment} />
            )}
          </div>
        )}
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
          {/* pageId BİLEREK verilmiyor - bu, tek bir sayfaya değil PLATFORMUN
              GENELİNE ait yorumlar demek (bkz. backend'deki Comment.PageId
              notu). */}
          <DiscussionPanel token={token} />
        </div>
      ) : dashboard && (
        <div className="flex flex-col gap-6">
          {/* (1) EN ÜSTTE, en dikkat çeken alan - yoğun makale kart ızgarası
              (spec madde 2 + 6: "en dikkat çeken alan makaleler olmalı"). */}
          {gridArticles.length > 0 && (
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
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
                {gridArticles.map((a) => (
                  <ArticleCard key={a.id} article={a} />
                ))}
              </div>
            </section>
          )}

          {/* (2) EN ALTTA, ikinci planda - Son Güncellemeler + Popüler
              Kategoriler (spec madde 6: "istatistik kartları ikinci planda
              kalmalı"). id="son-guncellemeler" - üstteki Hızlı Erişim
              şeridindeki "Son Güncellenenler" düğmesi buraya kaydırıyor. */}
          <div id="son-guncellemeler" className="grid grid-cols-1 gap-5 scroll-mt-4 lg:grid-cols-2">
            {recentUpdates.length > 0 && <RecentUpdatesBox updates={recentUpdates} />}

            {dashboard.popularTags.length > 0 && (
              <Panel title="Popüler Kategoriler">
                {dashboard.popularTags.map((t) => (
                  <PopularTagRow key={t.tag} tag={t.tag} count={t.count} />
                ))}
              </Panel>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default HomePage;
