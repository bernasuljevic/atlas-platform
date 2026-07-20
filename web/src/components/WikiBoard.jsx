import { useState, useEffect } from "react";
import { getWikiPages, createWikiPage } from "../api";

function WikiBoard({ token, onLogout }) {
  const [pages, setPages] = useState([]);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [department, setDepartment] = useState("IT");
  const [error, setError] = useState(null);
  const [isLoadingPages, setIsLoadingPages] = useState(true);
  const [isCreating, setIsCreating] = useState(false);

  useEffect(() => {
    loadPages();
  }, []);

  async function loadPages() {
    setIsLoadingPages(true);
    try {
      const data = await getWikiPages();
      setPages(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoadingPages(false);
    }
  }

  async function handleCreate(e) {
    e.preventDefault();
    setError(null);
    setIsCreating(true);

    try {
      await createWikiPage(token, {
        title,
        content,
        departmentName: department,
        visibility: "Public",
      });

      setTitle("");
      setContent("");
      await loadPages();
    } catch (err) {
      // Backend'den 401/403 gelirse özel bir mesaj gösterelim - kullanıcı
      // "neden olmadı" sorusuna daha net bir cevap alsın.
      setError(err.message.includes("giriş") ? err.message : "Sayfa oluşturulamadı: " + err.message);
    } finally {
      setIsCreating(false);
    }
  }

  return (
    <div style={{ maxWidth: 600, margin: "40px auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1>Atlas Wiki</h1>
        <button onClick={onLogout} style={{ height: 36 }}>
          Çıkış Yap
        </button>
      </div>

      <form onSubmit={handleCreate} style={{ marginBottom: 32, border: "1px solid #ccc", padding: 16 }}>
        <h3>Yeni Sayfa</h3>
        <input
          placeholder="Başlık"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          disabled={isCreating}
          style={{ width: "100%", padding: 8, marginBottom: 8 }}
        />
        <textarea
          placeholder="İçerik"
          value={content}
          onChange={(e) => setContent(e.target.value)}
          disabled={isCreating}
          style={{ width: "100%", padding: 8, marginBottom: 8, minHeight: 80 }}
        />
        <input
          placeholder="Departman"
          value={department}
          onChange={(e) => setDepartment(e.target.value)}
          disabled={isCreating}
          style={{ width: "100%", padding: 8, marginBottom: 8 }}
        />
        {error && <p style={{ color: "red" }}>{error}</p>}
        <button type="submit" disabled={isCreating}>
          {isCreating ? "Ekleniyor..." : "Ekle"}
        </button>
      </form>

      <h3>Sayfalar</h3>
      {isLoadingPages ? (
        <p>Yükleniyor...</p>
      ) : pages.length === 0 ? (
        <p>Henüz sayfa yok.</p>
      ) : (
        pages.map((p) => (
          <div key={p.id} style={{ borderBottom: "1px solid #eee", padding: "12px 0" }}>
            <strong>{p.title}</strong> <em>({p.departmentName})</em>
            <p>{p.content}</p>
          </div>
        ))
      )}
    </div>
  );
}

export default WikiBoard;