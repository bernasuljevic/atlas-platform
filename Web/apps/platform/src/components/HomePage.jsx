import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";
import {
  ArrowRight,
  Bell,
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
import { getNotifications, getWikiDashboard } from "../api";
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
    <Panel title="Bildirimler" action={<Bell size={13} style={{ color: "var(--text)", opacity: 0.5 }} />}>
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
      {notifications?.map((n) => (
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
    // "Modernize et" (2026-08-12): sınır rengi inline style'dan Tailwind
      // sınıfına taşındı (border-[var(--border)]) - inline style hover:
      // sınıfını EZERDİ (inline stil, aynı özellik için HER ZAMAN dış CSS
      // kuralını kazanır, :hover pseudo-class'ı fark etmeksizin), bu yüzden
      // hover'da yeşile dönebilmesi için başka türlü mümkün değildi. Küçük
      // bir kaldırma (-translate-y-0.5) eklendi - mevcut `transition` sınıfı
      // zaten `transform`ı da kapsıyor, ekstra bir süre/easing tanımlamaya
      // gerek kalmadı. "Abartısız" hedefine sadık kalmak için sadece 2px'lik
      // bir hareket - göze çarpan ama rahatsız etmeyen bir mikro-etkileşim.
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
          {/* UI/UX denetimi (2026-08-12): text-lg (15.75px) bir karşılama
              başlığı için zayıf ölçülmüştü - ama yukarıdaki yorumdaki
              "Karşılama alanı küçültülsün" kararıyla ÇELİŞMEMEK için text-2xl
              gibi büyük bir sıçrama YAPILMADI, sadece bir kademe (text-xl,
              17.5px) - şeridin "ince" karakteri korunuyor, sadece başlık artık
              MiniStat rakamlarından (text-xs) daha net ayrışıyor. */}
          <h1 className="text-xl font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
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
        // "Modernize et" (2026-08-15, kullanıcı isteği: "diğerlerinin
        // yazdıkları da şöyle gözüksün sağ tarafta") - ana içerik artık TEK
        // sütun DEĞİL, Medium'un kendi yerleşimiyle AYNI fikir: sol/geniş
        // sütun makaleler, sağ/dar sütun (Yazmaya Başla + Bildirimler + Son
        // Güncellemeler + Popüler Kategoriler) kalıcı bir "yan şerit".
        // `xl:` ALTINDA grid tek sütuna düşüyor - sidebar `hidden` DEĞİL,
        // DOM sırası gereği doğal olarak makalelerin ALTINA akıyor (WikiArticlePage'in
        // TOC/panel'inde de aynı "dar ekranda gizleme yerine akıt" tercihi var).
        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1fr_300px] xl:items-start">
          {/* (1) SOL/GENİŞ sütun - en dikkat çeken alan, yoğun makale kart
              ızgarası (spec madde 2 + 6: "en dikkat çeken alan makaleler
              olmalı"). */}
          {gridArticles.length > 0 && (
            <section className="min-w-0">
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
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-2">
                {gridArticles.map((a) => (
                  <ArticleCard key={a.id} article={a} />
                ))}
              </div>
            </section>
          )}

          {/* (2) SAĞ/DAR sütun - Yazmaya Başla + Bildirimler + Son
              Güncellemeler + Popüler Kategoriler. id="son-guncellemeler" -
              üstteki Hızlı Erişim şeridindeki "Son Güncellenenler" düğmesi
              hâlâ buraya kaydırıyor. */}
          <aside id="son-guncellemeler" className="flex scroll-mt-4 flex-col gap-4 xl:sticky xl:top-4">
            <WritePromptCard />
            <NotificationsPanel token={token} />
            {recentUpdates.length > 0 && <RecentUpdatesBox updates={recentUpdates} />}

            {dashboard.popularTags.length > 0 && (
              <Panel title="Popüler Kategoriler">
                {dashboard.popularTags.map((t) => (
                  <PopularTagRow key={t.tag} tag={t.tag} count={t.count} />
                ))}
              </Panel>
            )}
          </aside>
        </div>
      )}
    </div>
  );
}

export default HomePage;
