import { useState } from "react";
import { searchWikiPages } from "../api";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Badge } from "@atlas/ui/badge";

// Ayrı bir component olarak açıldı - WikiBoard.jsx zaten liste/oluşturma/silme/
// detay dialogu ile 350 satıra yaklaşmıştı, arama özelliğini oraya eklemek
// onu daha da büyütürdü (bkz. CLAUDE.md "İzlenecek teknik borç").
function WikiSearch({ token }) {
  const [queryText, setQueryText] = useState("");
  const [results, setResults] = useState(null);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState(null);

  async function handleSearch(e) {
    e.preventDefault();
    if (!queryText.trim()) return;

    setError(null);
    setIsSearching(true);
    try {
      const found = await searchWikiPages(token, queryText);
      setResults(found);
    } catch (err) {
      setError(err.message);
      setResults(null);
    } finally {
      setIsSearching(false);
    }
  }

  return (
    <Card className="mb-6 border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
      <CardContent>
        <form onSubmit={handleSearch} className="flex gap-2">
          <Input
            value={queryText}
            onChange={(e) => setQueryText(e.target.value)}
            placeholder="Anlamına göre ara... (örn. sunucu bakım prosedürü)"
            disabled={isSearching}
            className="flex-1"
          />
          <Button
            type="submit"
            disabled={isSearching || !queryText.trim()}
            className="bg-[var(--brand-accent)] text-[var(--text-h)] hover:opacity-90"
          >
            {isSearching ? "Aranıyor..." : "Ara"}
          </Button>
        </form>

        {error && <p style={{ color: "red" }} className="mt-3 text-sm">{error}</p>}

        {results && (
          <div className="mt-4 flex flex-col gap-2">
            {results.length === 0 ? (
              <p className="text-sm" style={{ color: "var(--text)" }}>
                Bu sorguyla eşleşen (ve görebileceğin) bir sayfa bulunamadı.
              </p>
            ) : (
              results.map((r) => (
                <div
                  key={r.wikiPageId}
                  className="rounded-lg border border-[var(--border)] p-3"
                >
                  <div className="mb-1 flex items-center justify-between gap-2">
                    <span className="font-medium">{r.title}</span>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline">{r.departmentName}</Badge>
                      {/* Skor 0-1 arası (1'e yakın = daha benzer) - kullanıcıya
                          ham cosine mesafesi yerine yüzde olarak gösteriyoruz. */}
                      <Badge className="bg-[var(--brand-accent)] text-[var(--text-h)]">
                        %{Math.round(r.score * 100)}
                      </Badge>
                    </div>
                  </div>
                  <p className="text-sm whitespace-pre-wrap" style={{ color: "var(--text)" }}>
                    {r.chunkText}
                  </p>
                </div>
              ))
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default WikiSearch;
