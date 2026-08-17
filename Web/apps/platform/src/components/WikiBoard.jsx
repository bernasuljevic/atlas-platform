import { useState, useEffect, useMemo } from "react";
import { Link, useSearchParams } from "react-router";
import { getWikiPages } from "../api";
import { getUserInfoFromToken } from "../jwt";
import WikiSearch from "./WikiSearch";
import WikiPageTable from "./WikiPageTable";
import { Button } from "@atlas/ui/button";

// Kullanıcı geri bildirimiyle (2026-08-04, "daha aşağı doğru devam ettir") -
// 5 satır sayfayı gereksiz kısa/boş gösteriyordu, 10'a çıkarıldı.
const PAGE_SIZE = 10;

// Liste/silme/detay tek component'te (~350 satır) yaşıyordu - WikiPageTable'a
// bölündü (bkz. CLAUDE.md "İzlenecek teknik borç"). Oluşturma artık bir Dialog
// DEĞİL - Wikipedia'nın referans ekran görüntüsündeki gibi ayrı bir editör
// sekmesine (/wiki/new) gidiyor (bkz. WikiEditorPage), bu yüzden
// CreateWikiPageDialog kaldırıldı. WikiBoard artık sadece sayfa listesinin
// state'ini tutan bir orkestratör.
//
// Başlık/Çıkış Yap/Audit Log burada DEĞİL - WikiLayout'un sidebar'ına taşındı
// (klasör ağacıyla birlikte /wiki altındaki HER alt sayfada sabit duruyor).
function WikiBoard({ token }) {
  // JWT'yi sadece UI kararları için okuyoruz (buton/alan göster-gizle) - gerçek
  // yetkilendirme her zaman backend'de. Token değişirse (refresh sonrası) yeniden hesaplanır.
  const { userId, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

  // Görsel Tasarım Yenileme Gün 2 (2026-08-17) - ana sayfadaki Hero arama
  // kutusu buraya "?q=..." ile yönlendiriyor. WikiSearch'ün initialQuery
  // prop'u/kendi useEffect'i ZATEN vardı (yorumunda "üst bardaki arama
  // kutusundan buraya yönlendirildiğinde" diye açıkça yazıyordu) ama hiçbir
  // yer bu URL parametresini OKUMUYORDU - gerçek bir bağlanmamış uç
  // (dangling wiring) idi, burada tamamlanıyor.
  const [searchParams] = useSearchParams();
  const initialQuery = searchParams.get("q") ?? "";

  const [pages, setPages] = useState([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [error, setError] = useState(null);
  const [isLoadingPages, setIsLoadingPages] = useState(true);

  useEffect(() => {
    loadPages(pageNumber);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber]);

  async function loadPages(targetPageNumber) {
    setIsLoadingPages(true);
    try {
      // Departman filtresi artık burada seçilebilir bir şey değil - backend,
      // token'daki kullanıcının GERÇEK departmanına göre otomatik filtreliyor
      // (bkz. GetWikiPagesQueryHandler). Önceden serbest metin olarak
      // gönderilebiliyordu, bu da başka departmanların içeriğini tahmin ederek
      // görebilmeyi mümkün kılan bir güvenlik açığıydı.
      const result = await getWikiPages(token, targetPageNumber, PAGE_SIZE);
      setPages(result.items);
      setTotalPages(Math.max(1, result.totalPages));
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoadingPages(false);
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Sayfalar
        </h1>
        <Link to="/wiki/new">
          <Button className="text-white hover:opacity-90" style={{ background: "var(--brand-accent)" }}>
            Yeni Sayfa
          </Button>
        </Link>
      </div>

      <WikiSearch token={token} initialQuery={initialQuery} />

      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      <WikiPageTable
        token={token}
        pages={pages}
        isLoadingPages={isLoadingPages}
        pageNumber={pageNumber}
        totalPages={totalPages}
        onPageChange={setPageNumber}
        isAdmin={isAdmin}
        userId={userId}
        onDeleted={() => loadPages(pageNumber)}
      />
    </div>
  );
}

export default WikiBoard;
