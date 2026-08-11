import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router";
import { toast } from "sonner";
import { Eye, EyeOff, Copy, Plus, KeyRound } from "lucide-react";
import { deletePasswordEntry, getPasswordEntries, revealPasswordEntry } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Badge } from "@atlas/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@atlas/ui/table";

// Faz 7/Gün 3 - liste sayfası. Wiki'nin aksine burada sayfalama YOK (bkz.
// backend'deki tasarım notu - Vault'un tüm kayıtlarını tek seferde bellekte
// tutmak, bir kullanıcının kişisel kasasının boyutu için makul bir varsayım;
// büyürse Wiki'deki gibi bir pageNumber/pageSize eklenir). Kategori filtresi
// istemci tarafında - kayıtlar zaten küçük bir küme, her filtre değişiminde
// yeniden istek atmaya gerek yok.
function VaultPage({ token }) {
  const { userId, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);
  const navigate = useNavigate();

  const [entries, setEntries] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [categoryFilter, setCategoryFilter] = useState("");

  // id -> düz metin parola. Bir kez "reveal" edilen kayıt burada önbelleğe
  // alınıyor - "Göster"e tekrar basmak ya da "Kopyala" ikinci kez tıklanınca
  // her seferinde yeni bir audit satırı ("Revealed") üretmesin diye (ilk
  // reveal zaten denetleniyor, aynı oturumda tekrar tekrar aynı parolayı
  // göstermek/kopyalamak ayrı bir güvenlik olayı sayılmıyor).
  const [revealedPasswords, setRevealedPasswords] = useState({});
  const [visibleIds, setVisibleIds] = useState(() => new Set());
  const [revealingId, setRevealingId] = useState(null);
  const [deletingId, setDeletingId] = useState(null);

  useEffect(() => {
    loadEntries();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadEntries() {
    setIsLoading(true);
    try {
      const result = await getPasswordEntries(token);
      setEntries(result);
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  const categories = useMemo(() => {
    const set = new Set(entries.map((e) => e.category).filter(Boolean));
    return Array.from(set).sort();
  }, [entries]);

  const visibleEntries = useMemo(() => {
    if (!categoryFilter) return entries;
    return entries.filter((e) => e.category === categoryFilter);
  }, [entries, categoryFilter]);

  async function ensureRevealed(id) {
    if (revealedPasswords[id]) return revealedPasswords[id];
    setRevealingId(id);
    try {
      const password = await revealPasswordEntry(token, id);
      setRevealedPasswords((prev) => ({ ...prev, [id]: password }));
      return password;
    } finally {
      setRevealingId(null);
    }
  }

  async function handleToggleShow(id) {
    if (visibleIds.has(id)) {
      setVisibleIds((prev) => {
        const next = new Set(prev);
        next.delete(id);
        return next;
      });
      return;
    }

    try {
      await ensureRevealed(id);
      setVisibleIds((prev) => new Set(prev).add(id));
    } catch (err) {
      toast("Parola görüntülenemedi", { description: err.message });
    }
  }

  async function handleCopy(id) {
    try {
      const password = await ensureRevealed(id);
      await navigator.clipboard.writeText(password);
      toast("Parola panoya kopyalandı");
    } catch (err) {
      toast("Parola kopyalanamadı", { description: err.message });
    }
  }

  async function handleDelete(entry, e) {
    e.stopPropagation();
    if (!window.confirm(`"${entry.title}" kaydını silmek istediğine emin misin?`)) return;

    setDeletingId(entry.id);
    try {
      await deletePasswordEntry(token, entry.id);
      setEntries((prev) => prev.filter((x) => x.id !== entry.id));
    } catch (err) {
      toast("Kayıt silinemedi", { description: err.message });
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="mx-auto max-w-4xl text-left">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <h1 className="flex items-center gap-2 text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          <KeyRound size={22} /> Şifre Kasası
        </h1>
        <Link to="/vault/new">
          <Button className="text-white hover:opacity-90" style={{ background: "var(--brand-accent)" }}>
            <Plus size={16} /> Yeni Kayıt
          </Button>
        </Link>
      </div>

      {categories.length > 0 && (
        <div className="mb-4 flex items-center gap-2">
          <label className="text-sm" style={{ color: "var(--text)" }}>Kategori:</label>
          <select
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
            className="rounded border px-2 py-1 text-sm"
            style={{ borderColor: "var(--border)", background: "var(--bg)", color: "var(--text)" }}
          >
            <option value="">Tümü</option>
            {categories.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>
      )}

      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      <Card className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardContent>
          {isLoading ? (
            <p>Yükleniyor...</p>
          ) : visibleEntries.length === 0 ? (
            <p>
              {entries.length === 0
                ? "Henüz bir kaydın yok. \"Yeni Kayıt\" ile ilk parolanı ekleyebilirsin."
                : "Bu kategoride kayıt yok."}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Başlık</TableHead>
                  <TableHead className="hidden sm:table-cell">Kullanıcı Adı</TableHead>
                  <TableHead>Parola</TableHead>
                  <TableHead className="hidden md:table-cell">Kategori</TableHead>
                  <TableHead className="hidden lg:table-cell">Son Erişim</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {visibleEntries.map((entry) => {
                  const isVisible = visibleIds.has(entry.id);
                  const isRevealingThis = revealingId === entry.id;
                  return (
                    <TableRow
                      key={entry.id}
                      onClick={() => navigate(`/vault/${entry.id}/edit`)}
                      className="cursor-pointer hover:bg-[var(--brand-accent)]/10"
                    >
                      <TableCell className="max-w-[140px] truncate font-medium" title={entry.title}>
                        {entry.title}
                        {entry.url && (
                          <a
                            href={entry.url}
                            target="_blank"
                            rel="noreferrer"
                            onClick={(e) => e.stopPropagation()}
                            className="ml-1.5 text-xs underline opacity-60"
                          >
                            ↗
                          </a>
                        )}
                      </TableCell>
                      <TableCell className="hidden max-w-[140px] truncate sm:table-cell">
                        {entry.username ?? "—"}
                      </TableCell>
                      <TableCell className="font-mono text-sm" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center gap-1.5">
                          <span className="min-w-[7rem]" style={{ letterSpacing: isVisible ? "normal" : "2px" }}>
                            {isVisible ? revealedPasswords[entry.id] : "••••••••"}
                          </span>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon-sm"
                            disabled={isRevealingThis}
                            onClick={() => handleToggleShow(entry.id)}
                            title={isVisible ? "Gizle" : "Göster"}
                          >
                            {isVisible ? <EyeOff size={14} /> : <Eye size={14} />}
                          </Button>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon-sm"
                            disabled={isRevealingThis}
                            onClick={() => handleCopy(entry.id)}
                            title="Kopyala"
                          >
                            <Copy size={14} />
                          </Button>
                        </div>
                      </TableCell>
                      <TableCell className="hidden md:table-cell">
                        {entry.category ? <Badge variant="outline">{entry.category}</Badge> : "—"}
                      </TableCell>
                      <TableCell className="hidden whitespace-nowrap text-sm text-[var(--text)]/70 lg:table-cell">
                        {entry.lastAccessedAtUtc ? formatUtcTimestamp(entry.lastAccessedAtUtc) : "Hiç"}
                      </TableCell>
                      <TableCell onClick={(e) => e.stopPropagation()}>
                        {/* Silme yetkisi burada da sadece "gösterme" kararı -
                            gerçek kontrol DeletePasswordEntryCommandHandler'da
                            (WikiPageTable'daki AYNI desen). */}
                        {(isAdmin || entry.createdByUserId === userId) && (
                          <Button
                            variant="destructive"
                            size="sm"
                            disabled={deletingId === entry.id}
                            onClick={(e) => handleDelete(entry, e)}
                          >
                            {deletingId === entry.id ? "Siliniyor..." : "Sil"}
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default VaultPage;
