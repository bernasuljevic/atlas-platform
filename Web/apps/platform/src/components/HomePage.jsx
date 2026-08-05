import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";
import {
  ArrowRight,
  Clock,
  FilePlus2,
  FileText,
  ListChecks,
  ScrollText,
  ShieldCheck,
  Users,
} from "lucide-react";
import { getWikiDashboard } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";

// Kullanıcının verdiği referans mockup'taki (2026-08-04) "kart" deseni - başlıklı,
// bir sağ üst köşe eylemi (opsiyonel) olabilen, alt satırları divide-y ile
// ayrılan bir panel. Hem sağdaki Hızlı Erişim/Popüler Kategoriler panelleri
// HEM ana kolondaki "Son Eklenen Makaleler" listesi AYNI bu iskeleti kullanıyor.
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

function StatCard({ icon, label, value, hint }) {
  return (
    <div className="flex items-center gap-2.5 rounded-lg border p-2.5" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
      <span
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md"
        style={{ background: "var(--brand-accent-bg)", color: "var(--brand-accent)" }}
      >
        {icon}
      </span>
      <div className="min-w-0">
        <p className="text-base leading-none font-bold" style={{ color: "var(--text-h)" }}>
          {value}
        </p>
        <p className="mt-0.5 truncate text-[11px] font-medium" style={{ color: "var(--text-h)", opacity: 0.85 }}>
          {label}
          {hint && <span className="opacity-70"> · {hint}</span>}
        </p>
      </div>
    </div>
  );
}

function ArticleRow({ article }) {
  return (
    <Link
      to={`/wiki/${article.id}`}
      className="flex items-start gap-3 px-3.5 py-3 hover:bg-[var(--brand-accent)]/5"
    >
      <FileText size={16} className="mt-0.5 shrink-0" style={{ color: "var(--brand-accent)" }} />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold" style={{ color: "var(--text-h)" }}>
          {article.title}
        </p>
        {article.excerpt && (
          <p className="mt-0.5 line-clamp-2 text-xs leading-relaxed" style={{ color: "var(--text)" }}>
            {article.excerpt}
          </p>
        )}
        <p className="mt-1 text-[11px]" style={{ color: "var(--text)", opacity: 0.6 }}>
          {formatUtcTimestamp(article.createdAtUtc)} · {article.departmentName}
        </p>
      </div>
    </Link>
  );
}

function QuickAccessRow({ icon, title, subtitle, to }) {
  return (
    <Link to={to} className="flex items-center gap-2.5 px-3.5 py-2.5 hover:bg-[var(--brand-accent)]/5">
      <span
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md"
        style={{ background: "var(--code-bg)", color: "var(--brand-accent)" }}
      >
        {icon}
      </span>
      <div className="min-w-0">
        <p className="text-xs font-medium" style={{ color: "var(--text-h)" }}>
          {title}
        </p>
        <p className="truncate text-[11px]" style={{ color: "var(--text)", opacity: 0.7 }}>
          {subtitle}
        </p>
      </div>
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

  const [featured, ...rest] = dashboard?.recentlyAdded ?? [];
  const otherRecentlyAdded = rest.slice(0, 3);

  const [activeTab, setActiveTab] = useState("home");

  return (
    <div className="mx-auto flex w-full max-w-6xl flex-col gap-4 px-4 py-2 text-left md:px-6">
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
          <Link to="/wiki/new" className="wiki-tab">
            Yeni Sayfa
          </Link>
        </div>
      </div>

      {/* Wikipedia Style Welcome Box */}
      <div
        className="rounded-lg border p-4 shadow-sm"
        style={{ borderColor: "var(--border)", background: "var(--bg)" }}
      >
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="mt-0 mb-1 text-xl font-bold tracking-tight" style={{ color: "var(--text-h)" }}>
              Atlas Wiki'ye hoş geldiniz{fullName ? `, ${fullName}` : ""}!
            </h1>
            <p className="text-xs font-normal" style={{ color: "var(--text)" }}>
              Şirket bilgi platformuna hoş geldiniz. Aradığınız tüm bilgiye tek noktadan ulaşabilirsiniz.
            </p>
          </div>
          {dashboard && (
            <div className="flex items-center gap-2 rounded-md border px-3 py-1.5 text-xs font-semibold" style={{ borderColor: "var(--border)", background: "var(--code-bg)" }}>
              <span style={{ color: "var(--text)" }}>Toplam Sayfa Sayısı:</span>
              <span style={{ color: "var(--brand-accent)" }}>{dashboard.totalPageCount}</span>
            </div>
          )}
        </div>
      </div>

      {error && <p style={{ color: "red" }} className="text-xs">{error}</p>}

      {activeTab === "talk" ? (
        <div className="rounded-lg border p-6 text-center" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
          <h2 className="text-base font-semibold" style={{ color: "var(--text-h)" }}>
            Anasayfa Tartışması
          </h2>
          <p className="mt-2 text-xs" style={{ color: "var(--text)" }}>
            Genel platform konuları ve duyurular için alan.
          </p>
        </div>
      ) : dashboard && (
        <div className="grid grid-cols-1 gap-5 lg:grid-cols-3">
          <div className="flex flex-col gap-5 lg:col-span-2">
            {featured && (
              <Link
                to={`/wiki/${featured.id}`}
                className="block rounded-lg border p-5 transition-shadow hover:shadow-lg"
                style={{ borderColor: "var(--border)", background: "var(--bg)" }}
              >
                <span className="text-[11px] font-bold tracking-wider uppercase" style={{ color: "var(--brand-accent)" }}>
                  ⭐ Haftanın Seçkin Maddesi
                </span>
                <h2 className="mt-2 text-xl font-bold leading-snug" style={{ color: "var(--text-h)" }}>
                  {featured.title}
                </h2>
                <p className="mt-2 text-sm leading-relaxed font-normal" style={{ color: "var(--text-h)", opacity: 0.9 }}>
                  {featured.excerpt}
                </p>
                <p className="mt-3 text-xs font-medium" style={{ color: "var(--text)", opacity: 0.7 }}>
                  ✍️ {featured.createdByEmail ?? "Bilinmiyor"} · 📅 {formatUtcTimestamp(featured.createdAtUtc)} · 🏢 {featured.departmentName}
                </p>
              </Link>
            )}

            <div className="grid grid-cols-2 gap-2.5 md:grid-cols-4">
              <StatCard icon={<FileText size={15} />} label="Toplam Makale" value={dashboard.totalPageCount} hint="Tüm içerikler" />
              <StatCard icon={<FilePlus2 size={15} />} label="Son Eklenen" value={dashboard.addedThisWeekCount} hint="Bu hafta" />
              <StatCard icon={<Clock size={15} />} label="Son Güncellenen" value={dashboard.updatedThisWeekCount} hint="Bu hafta" />
              {ownDepartment && (
                <StatCard icon={<Users size={15} />} label="Sizin İçerikleriniz" value={dashboard.departmentSpecificCount} hint={ownDepartment} />
              )}
            </div>

            {otherRecentlyAdded.length > 0 && (
              <Panel
                title="Son Eklenen Makaleler"
                action={
                  <Link
                    to="/wiki/pages"
                    className="flex items-center gap-1 text-xs font-medium hover:opacity-80"
                    style={{ color: "var(--brand-accent)" }}
                  >
                    Tümünü Gör <ArrowRight size={13} />
                  </Link>
                }
              >
                {otherRecentlyAdded.map((a) => (
                  <ArticleRow key={a.id} article={a} />
                ))}
              </Panel>
            )}
          </div>

          <aside className="flex flex-col gap-5">
            <Panel title="Hızlı Erişim">
              <QuickAccessRow
                icon={<ListChecks size={15} />}
                title="Tüm Sayfalar"
                subtitle="Tüm içeriklere göz atın"
                to="/wiki/pages"
              />
              <QuickAccessRow
                icon={<ScrollText size={15} />}
                title="Yeni Sayfa"
                subtitle="Yeni bir makale oluşturun"
                to="/wiki/new"
              />
              {isAdmin && (
                <QuickAccessRow
                  icon={<ShieldCheck size={15} />}
                  title="Audit Log"
                  subtitle="Sistem kayıtlarını görüntüleyin"
                  to="/audit-log"
                />
              )}
            </Panel>

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
