import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";
import { Star } from "lucide-react";
import { getFavoritePages } from "../api";
import { formatUtcTimestamp } from "../dateUtils";
import { Badge } from "@atlas/ui/badge";
import { Input } from "@atlas/ui/input";
import { Button } from "@atlas/ui/button";

// WikiBoard'un liste iskeletine benzer ama BİLEREK daha kompakt - kişisel bir
// hızlı-erişim listesi, "Son Eklenen Makaleler"deki büyük kart ızgarasının
// AYNISI değil (kullanıcının kendi notu: "büyük kartlar kullanmak zorunda
// değil, kompakt liste daha uygun"). Sayfalama YOK - Vault'un "kişisel liste
// küçük" kararıyla aynı gerekçe, kişisel favori listesi büyük bir sayfalama
// gerektirecek boyuta ulaşması beklenmiyor.
function WikiFavoritesPage({ token }) {
  const [pages, setPages] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    let cancelled = false;
    getFavoritePages(token)
      .then((result) => {
        if (!cancelled) setPages(result);
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
  }, [token]);

  const visiblePages = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return pages;
    return pages.filter((p) => p.title.toLowerCase().includes(q));
  }, [pages, searchQuery]);

  return (
    <div className="mx-auto max-w-2xl text-left">
      <div className="mb-1 flex items-center gap-2">
        <Star size={20} style={{ color: "var(--brand-accent)" }} fill="var(--brand-accent)" />
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Favoriler
        </h1>
      </div>
      <p className="mb-6 text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
        Favorilerinize eklediğiniz sayfalar
      </p>

      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      {isLoading ? (
        <p style={{ color: "var(--text)" }}>Yükleniyor...</p>
      ) : pages.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-lg border py-16 text-center" style={{ borderColor: "var(--border)" }}>
          <Star size={32} style={{ color: "var(--text)", opacity: 0.3 }} />
          <p className="font-medium" style={{ color: "var(--text-h)" }}>Henüz favoriniz yok</p>
          <p className="max-w-xs text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
            Daha sonra kolayca erişmek istediğiniz sayfaları favorilerinize ekleyebilirsiniz.
          </p>
          <Link to="/wiki/pages" className="mt-2">
            <Button variant="outline">Sayfaları Keşfet</Button>
          </Link>
        </div>
      ) : (
        <>
          {pages.length > 5 && (
            <Input
              placeholder="Favorilerde ara..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="mb-4"
            />
          )}
          <div className="flex flex-col divide-y rounded-lg border" style={{ borderColor: "var(--border)" }}>
            {visiblePages.map((page) => (
              <Link
                key={page.id}
                to={`/wiki/${page.id}`}
                className="flex items-center gap-3 px-4 py-3 hover:bg-[var(--brand-accent)]/10"
              >
                <Star size={15} className="shrink-0" style={{ color: "var(--brand-accent)" }} fill="var(--brand-accent)" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium" style={{ color: "var(--text-h)" }}>
                    {page.title}
                  </p>
                  <p className="flex items-center gap-1.5 text-xs" style={{ color: "var(--text)", opacity: 0.65 }}>
                    <Badge variant="outline" className="text-[10px] font-normal">{page.departmentName}</Badge>
                    {formatUtcTimestamp(page.createdAtUtc)}
                  </p>
                </div>
              </Link>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

export default WikiFavoritesPage;
