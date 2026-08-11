import { useEffect, useState } from "react";
import { Link } from "react-router";
import { Pin } from "lucide-react";
import { getPinnedPages } from "../api";
import { formatUtcTimestamp } from "../dateUtils";
import { Badge } from "@atlas/ui/badge";
import { Button } from "@atlas/ui/button";

// WikiFavoritesPage'in pin karşılığı - bkz. o dosyadaki not. Arama kutusu
// BİLEREK yok - pinlenen sayfa sayısı favoriden bile daha az olması beklenen
// bir "hızlı erişim" listesi (kullanıcının kendi tanımı: "sürekli hızlı
// erişmek istediğim" az sayıda sayfa), bir arama kutusu gereksiz olurdu.
function WikiPinnedPage({ token }) {
  const [pages, setPages] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    getPinnedPages(token)
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

  return (
    <div className="mx-auto max-w-2xl text-left">
      <div className="mb-1 flex items-center gap-2">
        <Pin size={20} style={{ color: "var(--brand-accent)" }} fill="var(--brand-accent)" />
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Pinlenenler
        </h1>
      </div>
      <p className="mb-6 text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
        Hızlı erişim için sabitlediğiniz sayfalar
      </p>

      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      {isLoading ? (
        <p style={{ color: "var(--text)" }}>Yükleniyor...</p>
      ) : pages.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-lg border py-16 text-center" style={{ borderColor: "var(--border)" }}>
          <Pin size={32} style={{ color: "var(--text)", opacity: 0.3 }} />
          <p className="font-medium" style={{ color: "var(--text-h)" }}>Henüz pinlenmiş sayfa yok</p>
          <p className="max-w-xs text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
            Sık kullandığınız sayfaları sabitleyerek buradan hızlıca erişebilirsiniz.
          </p>
          <Link to="/wiki/pages" className="mt-2">
            <Button variant="outline">Sayfaları Keşfet</Button>
          </Link>
        </div>
      ) : (
        <div className="flex flex-col divide-y rounded-lg border" style={{ borderColor: "var(--border)" }}>
          {pages.map((page) => (
            <Link
              key={page.id}
              to={`/wiki/${page.id}`}
              className="flex items-center gap-3 px-4 py-3 hover:bg-[var(--brand-accent)]/10"
            >
              <Pin size={15} className="shrink-0" style={{ color: "var(--brand-accent)" }} fill="var(--brand-accent)" />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium" style={{ color: "var(--text-h)" }}>
                  {page.title}
                </p>
                <p className="flex items-center gap-1.5 text-xs" style={{ color: "var(--text)", opacity: 0.65 }}>
                  <Badge variant="outline" className="text-[10px] font-normal">{page.departmentName}</Badge>
                  {page.updatedAtUtc
                    ? `Güncellendi ${formatUtcTimestamp(page.updatedAtUtc)}`
                    : formatUtcTimestamp(page.createdAtUtc)}
                </p>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

export default WikiPinnedPage;
