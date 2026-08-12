import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { BookOpen, FileText } from "lucide-react";
import { searchByMeaning } from "../api";
import { formatUtcTimestamp } from "../dateUtils";
import { Button } from "@atlas/ui/button";
import { Card, CardContent } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { Badge } from "@atlas/ui/badge";

// Backend'in SemanticSearchResultDto.SourceType'ıyla AYNI değer - typo'ya
// karşı tek yerde. Diğer olası değer ("WikiPage") burada AYRICA bir sabit
// olarak TUTULMUYOR - iki değerli bir küme olduğu için "Document DEĞİLSE
// WikiPage'dir" (aşağıdaki isDocument ternary'si) yeterli, ekstra bir sabit
// sadece dolaylılık eklerdi.
const SOURCE_TYPE_DOCUMENT = "Document";

// Ayrı bir component olarak açıldı - WikiBoard.jsx zaten liste/oluşturma/silme/
// detay dialogu ile 350 satıra yaklaşmıştı, arama özelliğini oraya eklemek
// onu daha da büyütürdü (bkz. CLAUDE.md "İzlenecek teknik borç"). Sonuca
// tıklanınca artık ayrıca sayfa çekip bir dialog açmıyoruz - direkt gerçek
// /wiki/:id ya da /documents/:id sayfasına gidiyoruz (bkz. WikiArticlePage/
// DocumentDetailPage) - P5'ten (Documents→AI/RAG entegrasyonu) itibaren
// sonuçlar İKİ kaynaktan (wiki sayfaları + belgeler) birleşik geliyor.
function WikiSearch({ token, initialQuery }) {
  const navigate = useNavigate();
  const [queryText, setQueryText] = useState(initialQuery ?? "");
  // İsteğe bağlı, normal aramaya EK bir daraltma - boş bırakılırsa arama
  // eskisi gibi tarih filtresiz çalışır (bkz. api.js'teki searchByMeaning).
  const [fromUtc, setFromUtc] = useState("");
  const [toUtc, setToUtc] = useState("");
  const [results, setResults] = useState(null);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState(null);

  async function runSearch(text) {
    if (!text.trim()) return;

    setError(null);
    setIsSearching(true);
    try {
      const found = await searchByMeaning(token, text, 5, { fromUtc, toUtc });
      setResults(found);
    } catch (err) {
      setError(err.message);
      setResults(null);
    } finally {
      setIsSearching(false);
    }
  }

  // Üst bardaki arama kutusundan (WikiLayout) "searchQuery" ile buraya
  // yönlendirildiğinde - kutuyu doldurup aramayı KENDİLİĞİNDEN çalıştırıyor,
  // kullanıcının aynı sorguyu iki kez yazmasına gerek kalmıyor.
  useEffect(() => {
    if (initialQuery) runSearch(initialQuery);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialQuery]);

  async function handleSearch(e) {
    e.preventDefault();
    await runSearch(queryText);
  }

  return (
    <Card className="mb-6 border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
      <CardContent>
        <form onSubmit={handleSearch} className="flex flex-wrap items-end gap-2">
          <Input
            value={queryText}
            onChange={(e) => setQueryText(e.target.value)}
            placeholder="Anlamına göre ara... (örn. sunucu bakım prosedürü)"
            disabled={isSearching}
            className="min-w-48 flex-1"
          />
          {/* İsteğe bağlı tarih daraltması - normal aramaya EK, boş bırakılabilir. */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="search-from" className="text-xs">Başlangıç</Label>
            <Input
              id="search-from"
              type="date"
              value={fromUtc}
              onChange={(e) => setFromUtc(e.target.value)}
              disabled={isSearching}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="search-to" className="text-xs">Bitiş</Label>
            <Input
              id="search-to"
              type="date"
              value={toUtc}
              onChange={(e) => setToUtc(e.target.value)}
              disabled={isSearching}
            />
          </div>
          <Button
            type="submit"
            disabled={isSearching || !queryText.trim()}
            className="text-white hover:opacity-90"
            style={{ background: "var(--brand-accent)" }}
          >
            {isSearching ? "Aranıyor..." : "Ara"}
          </Button>
        </form>

        {error && <p style={{ color: "red" }} className="mt-3 text-sm">{error}</p>}

        {results && (
          <div className="mt-4 flex flex-col gap-2">
            {results.length === 0 ? (
              <p className="text-sm" style={{ color: "var(--text)" }}>
                Bu sorguyla eşleşen (ve görebileceğin) bir sayfa ya da belge bulunamadı.
              </p>
            ) : (
              results.map((r) => {
                // P5'ten itibaren bir sonuç ya bir wiki sayfası ya bir belge
                // olabiliyor - ikisinin id UZAY'ı FARKLI (WikiPage.Id ile
                // Document.Id birbirinden bağımsız), bu yüzden hedef rota
                // sourceType'a göre dallanıyor. Aynı gerekçeyle React key'i
                // SADECE resourceId DEĞİL - iki ayrı kaynaktan (istatistiksel
                // olarak imkansız ama yapısal olarak mümkün) aynı GUID gelirse
                // key çakışmasın diye sourceType de key'e katılıyor.
                const isDocument = r.sourceType === SOURCE_TYPE_DOCUMENT;
                const targetPath = isDocument ? `/documents/${r.resourceId}` : `/wiki/${r.resourceId}`;
                const SourceIcon = isDocument ? FileText : BookOpen;

                return (
                  <div
                    key={`${r.sourceType}-${r.resourceId}`}
                    onClick={() => navigate(targetPath)}
                    className="cursor-pointer rounded-lg border border-[var(--border)] p-3 hover:bg-[var(--brand-accent)]/10"
                  >
                    <div className="mb-1 flex items-center justify-between gap-2">
                      <span className="flex items-center gap-1.5 font-medium">
                        <SourceIcon
                          size={16}
                          style={{ color: "var(--text)", opacity: 0.6 }}
                          aria-label={isDocument ? "Belge" : "Wiki Sayfası"}
                        />
                        {r.title}
                      </span>
                      <div className="flex items-center gap-2">
                        <Badge variant="outline">{r.departmentName}</Badge>
                        {/* Skor 0-1 arası (1'e yakın = daha benzer) - kullanıcıya
                            ham cosine mesafesi yerine yüzde olarak gösteriyoruz. */}
                        <Badge className="text-white" style={{ background: "var(--brand-accent)" }}>
                          %{Math.round(r.score * 100)}
                        </Badge>
                      </div>
                    </div>
                    <p className="text-sm whitespace-pre-wrap" style={{ color: "var(--text)" }}>
                      {r.chunkText}
                    </p>
                    <p className="mt-1 text-xs" style={{ color: "var(--text)", opacity: 0.6 }}>
                      {formatUtcTimestamp(r.createdAtUtc)}
                    </p>
                  </div>
                );
              })
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default WikiSearch;
