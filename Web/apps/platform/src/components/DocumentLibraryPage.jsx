import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router";
import { toast } from "sonner";
import { Plus } from "lucide-react";
import { deleteDocument, downloadDocument, getDocuments } from "../api";
import { getUserInfoFromToken } from "../jwt";
import { formatUtcTimestamp } from "../dateUtils";
import { DEPARTMENTS } from "../departments";
import { formatFileSize, getDocumentIcon, getDocumentTypeLabel } from "../documentIcons";
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

const PAGE_SIZE = 10;

// WikiBoard/WikiPageTable'ın liste iskeletiyle AYNI desen (sayfalama, satıra
// tıklayınca detay, owner-or-admin'e görünen Sil düğmesi) - Vault/Wiki'nin
// aksine indirme de burada BİR TIKLA yapılabiliyor (satır başına "İndir"
// düğmesi), çünkü Document Library'nin ana eylemi tam olarak bu.
function DocumentLibraryPage({ token }) {
  const { userId, isAdmin } = useMemo(() => getUserInfoFromToken(token), [token]);
  const navigate = useNavigate();

  const [documents, setDocuments] = useState([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [departmentFilter, setDepartmentFilter] = useState("");
  const [deletingId, setDeletingId] = useState(null);

  useEffect(() => {
    loadDocuments(pageNumber, departmentFilter);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, departmentFilter]);

  async function loadDocuments(targetPageNumber, department) {
    setIsLoading(true);
    try {
      const result = await getDocuments(token, {
        departmentName: department || undefined,
        pageNumber: targetPageNumber,
        pageSize: PAGE_SIZE,
      });
      setDocuments(result.items);
      setTotalPages(Math.max(1, result.totalPages));
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleDownload(doc, e) {
    e.stopPropagation();
    try {
      await downloadDocument(token, doc.id);
    } catch (err) {
      toast("Belge indirilemedi", { description: err.message });
    }
  }

  async function handleDelete(doc, e) {
    e.stopPropagation();
    if (!window.confirm(`"${doc.title}" belgesini silmek istediğine emin misin?`)) return;

    setDeletingId(doc.id);
    try {
      await deleteDocument(token, doc.id);
      await loadDocuments(pageNumber, departmentFilter);
    } catch (err) {
      toast("Belge silinemedi", { description: err.message });
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="mx-auto max-w-5xl text-left">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Belgeler
        </h1>
        <Link to="/documents/upload">
          <Button className="text-white hover:opacity-90" style={{ background: "var(--brand-accent)" }}>
            <Plus size={16} /> Belge Yükle
          </Button>
        </Link>
      </div>

      <div className="mb-4 flex items-center gap-2">
        <label className="text-sm" style={{ color: "var(--text)" }}>
          Departman:
        </label>
        <select
          value={departmentFilter}
          onChange={(e) => {
            setDepartmentFilter(e.target.value);
            setPageNumber(1);
          }}
          className="rounded border px-2 py-1 text-sm"
          style={{ borderColor: "var(--border)", background: "var(--bg)", color: "var(--text)" }}
        >
          <option value="">Tümü</option>
          {DEPARTMENTS.map((d) => (
            <option key={d.value} value={d.value}>
              {d.label}
            </option>
          ))}
        </select>
      </div>

      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      <Card className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardContent>
          {isLoading ? (
            <p>Yükleniyor...</p>
          ) : documents.length === 0 ? (
            <p>Görebileceğin bir belge yok.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Başlık</TableHead>
                  <TableHead className="hidden sm:table-cell">Tür</TableHead>
                  <TableHead>Departman</TableHead>
                  <TableHead className="hidden md:table-cell">Boyut</TableHead>
                  <TableHead className="hidden lg:table-cell">Yüklenme</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {documents.map((doc) => {
                  const Icon = getDocumentIcon(doc.fileExtension);
                  return (
                    <TableRow
                      key={doc.id}
                      onClick={() => navigate(`/documents/${doc.id}`)}
                      className="cursor-pointer hover:bg-[var(--brand-accent)]/10"
                    >
                      <TableCell className="max-w-[160px] truncate font-medium" title={doc.title}>
                        <span className="flex items-center gap-2">
                          <Icon size={15} className="shrink-0" style={{ color: "var(--brand-accent)" }} />
                          {doc.title}
                        </span>
                      </TableCell>
                      <TableCell className="hidden sm:table-cell">{getDocumentTypeLabel(doc.fileExtension)}</TableCell>
                      <TableCell>
                        <Badge variant="outline">{doc.departmentName}</Badge>
                      </TableCell>
                      <TableCell className="hidden text-sm md:table-cell">{formatFileSize(doc.sizeBytes)}</TableCell>
                      <TableCell className="hidden whitespace-nowrap text-sm text-[var(--text)]/70 lg:table-cell">
                        {formatUtcTimestamp(doc.createdAtUtc)}
                      </TableCell>
                      <TableCell onClick={(e) => e.stopPropagation()}>
                        <div className="flex gap-1.5">
                          <Button variant="outline" size="sm" onClick={(e) => handleDownload(doc, e)}>
                            İndir
                          </Button>
                          {/* Silme yetkisi burada da sadece "gösterme" kararı -
                              gerçek kontrol DeleteDocumentCommandHandler'da. */}
                          {(isAdmin || doc.createdByUserId === userId) && (
                            <Button
                              variant="destructive"
                              size="sm"
                              disabled={deletingId === doc.id}
                              onClick={(e) => handleDelete(doc, e)}
                            >
                              {deletingId === doc.id ? "Siliniyor..." : "Sil"}
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}

          {!isLoading && documents.length > 0 && (
            <div className="mt-4 flex items-center justify-between">
              <Button variant="outline" disabled={pageNumber <= 1} onClick={() => setPageNumber((p) => p - 1)}>
                ← Önceki
              </Button>
              <span className="text-sm" style={{ color: "var(--text)" }}>
                Sayfa {pageNumber} / {totalPages}
              </span>
              <Button variant="outline" disabled={pageNumber >= totalPages} onClick={() => setPageNumber((p) => p + 1)}>
                Sonraki →
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default DocumentLibraryPage;
