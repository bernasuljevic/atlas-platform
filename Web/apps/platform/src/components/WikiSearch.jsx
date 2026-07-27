import { useState } from "react";
import { searchWikiPages, getWikiPageById } from "../api";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Badge } from "@atlas/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@atlas/ui/dialog";

// Ayrı bir component olarak açıldı - WikiBoard.jsx zaten liste/oluşturma/silme/
// detay dialogu ile 350 satıra yaklaşmıştı, arama özelliğini oraya eklemek
// onu daha da büyütürdü (bkz. CLAUDE.md "İzlenecek teknik borç").
function WikiSearch({ token }) {
  const [queryText, setQueryText] = useState("");
  const [results, setResults] = useState(null);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState(null);

  // Arama sonucu sadece bir chunk (kısa parça) içeriyor - tıklanınca tam
  // sayfayı GET /api/wiki/pages/{id} ile ayrıca çekiyoruz (bkz. api.js).
  const [selectedPage, setSelectedPage] = useState(null);
  const [isLoadingPage, setIsLoadingPage] = useState(false);
  const [pageError, setPageError] = useState(null);

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

  async function handleResultClick(wikiPageId) {
    setPageError(null);
    setIsLoadingPage(true);
    try {
      const page = await getWikiPageById(token, wikiPageId);
      setSelectedPage(page);
    } catch (err) {
      setPageError(err.message);
    } finally {
      setIsLoadingPage(false);
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
        {pageError && <p style={{ color: "red" }} className="mt-3 text-sm">{pageError}</p>}

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
                  onClick={() => handleResultClick(r.wikiPageId)}
                  className="cursor-pointer rounded-lg border border-[var(--border)] p-3 hover:bg-[var(--brand-accent)]/10"
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

      {/* WikiBoard'daki sayfa detay Dialog'uyla aynı desen - Trigger'sız,
          kontrollü open state'i. isLoadingPage true iken de dialog açık
          tutuluyor ki kullanıcı "bir şey oluyor" hissi alsın. */}
      <Dialog
        open={selectedPage !== null || isLoadingPage}
        onOpenChange={(open) => !open && setSelectedPage(null)}
      >
        <DialogContent className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)] sm:max-w-lg">
          {isLoadingPage ? (
            <p className="text-sm">Yükleniyor...</p>
          ) : (
            <>
              <DialogHeader>
                <DialogTitle style={{ color: "var(--text-h)" }}>{selectedPage?.title}</DialogTitle>
                <div className="flex gap-2">
                  <Badge variant="outline">{selectedPage?.departmentName}</Badge>
                  {selectedPage?.visibility === "Public" ? (
                    <Badge className="bg-[var(--brand-accent)] text-[var(--text-h)]">Herkese Açık</Badge>
                  ) : (
                    <Badge variant="outline">Sadece Departman</Badge>
                  )}
                </div>
              </DialogHeader>
              <p className="max-h-[60vh] overflow-y-auto whitespace-pre-wrap text-sm">
                {selectedPage?.content}
              </p>
            </>
          )}
        </DialogContent>
      </Dialog>
    </Card>
  );
}

export default WikiSearch;
