import { useState, useEffect, useMemo } from "react";
import { Link } from "react-router";
import { getWikiPages } from "../api";
import { getUserInfoFromToken } from "../jwt";
import WikiSearch from "./WikiSearch";
import CreateWikiPageDialog from "./CreateWikiPageDialog";
import WikiPageTable from "./WikiPageTable";
import { Button } from "@atlas/ui/button";

const PAGE_SIZE = 5;

// Liste/oluşturma/silme/detay tek component'te (~350 satır) yaşıyordu -
// CreateWikiPageDialog ve WikiPageTable'a bölündü (bkz. CLAUDE.md "İzlenecek
// teknik borç"). WikiBoard artık sadece sayfa listesinin state'ini (token'dan
// türeyen kullanıcı bilgisi dahil) tutan ve alt component'lere dağıtan bir
// orkestratör - her alt component kendi form/dialog/hata state'ini kendi
// yönetiyor, sadece "liste değişti, yenile" anlamına gelen bir callback'le
// (onCreated/onDeleted) parent'a haber veriyor.
function WikiBoard({ token, onLogout }) {
  // JWT'yi sadece UI kararları için okuyoruz (buton/alan göster-gizle) - gerçek
  // yetkilendirme her zaman backend'de. Token değişirse (refresh sonrası) yeniden hesaplanır.
  const { userId, department: ownDepartment, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);

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

  async function handlePageCreated() {
    // Yeni sayfa en yeni olarak 1. sayfada görünecek (backend CreatedAtUtc'ye
    // göre azalan sırada döndürüyor) - zaten 1. sayfadaysak yeniden yükle,
    // değilsek 1. sayfaya dön (bu da kendiliğinden yeniden yükletir, bkz. useEffect).
    if (pageNumber === 1) {
      await loadPages(1);
    } else {
      setPageNumber(1);
    }
  }

  return (
    <div style={{ maxWidth: 1100, margin: "40px auto" }} className="px-4">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Atlas Wiki
        </h1>
        <div className="flex gap-2">
          {/* Sadece Admin görüyor - gerçek yetkilendirme zaten backend'de
              (GET /api/audit-log, RequireRole("Admin")); bu sadece UI
              kararı, normal kullanıcı zaten göremeyeceği bir sayfaya
              gitmeye çalışmasın diye. */}
          {isAdmin && (
            <Link to="/audit-log">
              <Button variant="outline">Audit Log</Button>
            </Link>
          )}
          <CreateWikiPageDialog
            token={token}
            isAdmin={isAdmin}
            ownDepartment={ownDepartment}
            onCreated={handlePageCreated}
          />
          <Button variant="outline" onClick={onLogout}>
            Çıkış Yap
          </Button>
        </div>
      </div>

      <WikiSearch token={token} />

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
