import { useState, useEffect, useMemo } from "react";
import { Link } from "react-router";
import { getAuditLog } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { Badge } from "@atlas/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@atlas/ui/table";

const PAGE_SIZE = 20;

// Sadece Admin görebiliyor - backend zaten .RequireRole("Admin") ile
// koruyor (yetkisiz bir istek 403 alır), buradaki isAdmin kontrolü sadece
// UI kararı (sayfayı hiç göstermemek/yönlendirmek), gerçek yetkilendirme
// değil - WikiPageTable'daki "Sil" butonuyla aynı desen.
function AuditLogPage({ token }) {
  const { isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

  const [entries, setEntries] = useState([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  const [detailsFilter, setDetailsFilter] = useState("");
  const [fromUtc, setFromUtc] = useState("");
  const [toUtc, setToUtc] = useState("");
  // Filtre formundaki değerler her tuşta değil, sadece "Filtrele"ye basılınca
  // uygulanıyor - appliedFilters ayrı tutuluyor ki her karakterde yeni bir
  // istek atılmasın.
  const [appliedFilters, setAppliedFilters] = useState({ details: "", fromUtc: "", toUtc: "" });

  useEffect(() => {
    if (!isAdmin) return;
    loadEntries(pageNumber, appliedFilters);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, appliedFilters, isAdmin]);

  async function loadEntries(targetPageNumber, filters) {
    setIsLoading(true);
    try {
      const result = await getAuditLog(token, { ...filters, pageNumber: targetPageNumber, pageSize: PAGE_SIZE });
      setEntries(result.items);
      setTotalPages(Math.max(1, result.totalPages));
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  function handleFilterSubmit(e) {
    e.preventDefault();
    setPageNumber(1);
    setAppliedFilters({ details: detailsFilter.trim(), fromUtc, toUtc });
  }

  if (!isAdmin) {
    return (
      <div style={{ maxWidth: 600, margin: "80px auto" }} className="px-4 text-center">
        <p style={{ color: "var(--text)" }}>Bu sayfayı görme yetkin yok.</p>
        <Link to="/wiki" className="underline">
          Wiki'ye dön
        </Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 1100, margin: "40px auto" }} className="px-4">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Audit Log
        </h1>
        <Link to="/wiki">
          <Button variant="outline">← Wiki'ye dön</Button>
        </Link>
      </div>

      <Card className="mb-6 border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardContent>
          <form onSubmit={handleFilterSubmit} className="flex flex-wrap items-end gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="filter-details">Detay (sayfa başlığı)</Label>
              <Input
                id="filter-details"
                placeholder="ör. Sunucu Bakım"
                value={detailsFilter}
                onChange={(e) => setDetailsFilter(e.target.value)}
                className="w-56"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="filter-from">Başlangıç</Label>
              <Input
                id="filter-from"
                type="date"
                value={fromUtc}
                onChange={(e) => setFromUtc(e.target.value)}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="filter-to">Bitiş</Label>
              <Input
                id="filter-to"
                type="date"
                value={toUtc}
                onChange={(e) => setToUtc(e.target.value)}
              />
            </div>
            <Button type="submit" className="bg-[var(--brand-accent)] text-[var(--text-h)] hover:opacity-90">
              Filtrele
            </Button>
          </form>
        </CardContent>
      </Card>

      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      <Card className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardContent>
          {isLoading ? (
            <p>Yükleniyor...</p>
          ) : entries.length === 0 ? (
            <p>Kayıt bulunamadı.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Kullanıcı</TableHead>
                  <TableHead>Eylem</TableHead>
                  <TableHead>Detay</TableHead>
                  <TableHead className="hidden md:table-cell">Kaynak ID</TableHead>
                  <TableHead>Zaman (UTC)</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {entries.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell className="max-w-[180px] truncate" title={entry.userEmail ?? ""}>
                      {entry.userEmail ?? "—"}
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{entry.action}</Badge>
                    </TableCell>
                    <TableCell className="max-w-[200px] truncate" title={entry.details ?? ""}>
                      {entry.details ?? "—"}
                    </TableCell>
                    <TableCell className="hidden max-w-[220px] truncate font-mono text-xs text-[var(--text)]/70 md:table-cell">
                      {entry.resourceId ?? "—"}
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-sm">
                      {new Date(entry.occurredAtUtc + "Z").toLocaleString()}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          {!isLoading && entries.length > 0 && (
            <div className="mt-4 flex items-center justify-between">
              <Button
                variant="outline"
                disabled={pageNumber <= 1 || isLoading}
                onClick={() => setPageNumber((p) => p - 1)}
              >
                ← Önceki
              </Button>
              <span className="text-sm" style={{ color: "var(--text)" }}>
                Sayfa {pageNumber} / {totalPages}
              </span>
              <Button
                variant="outline"
                disabled={pageNumber >= totalPages || isLoading}
                onClick={() => setPageNumber((p) => p + 1)}
              >
                Sonraki →
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default AuditLogPage;
