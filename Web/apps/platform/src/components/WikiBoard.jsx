import { useState, useEffect } from "react";
import { getWikiPages, createWikiPage } from "../api";

function WikiBoard({ token, onLogout }) {
  const [pages, setPages] = useState([]);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [department, setDepartment] = useState("IT");
  const [visibility, setVisibility] = useState("Public");
  const [error, setError] = useState(null);
  const [isLoadingPages, setIsLoadingPages] = useState(true);
  const [isCreating, setIsCreating] = useState(false);

  useEffect(() => {
    loadPages();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadPages() {
    setIsLoadingPages(true);
    try {
      // Departman filtresi artık burada seçilebilir bir şey değil - backend,
      // token'daki kullanıcının GERÇEK departmanına göre otomatik filtreliyor
      // (bkz. GetWikiPagesQueryHandler). Önceden serbest metin olarak
      // gönderilebiliyordu, bu da başka departmanların içeriğini tahmin ederek
      // görebilmeyi mümkün kılan bir güvenlik açığıydı.
      const data = await getWikiPages(token);
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
        visibility,
      });

      setTitle("");
      setContent("");
      await loadPages();
    } catch (err) {
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

        {/* Görünürlük seçimi - backend'deki WikiVisibility enum'ının React karşılığı.
            "Public" ve "DepartmentOnly" string'leri backend'in beklediği değerlerle
            BİREBİR aynı olmalı - Enum.Parse<WikiVisibility> bunu bekliyor. */}
        <div style={{ marginBottom: 8 }}>
          <label style={{ marginRight: 16 }}>
            <input
              type="radio"
              name="visibility"
              value="Public"
              checked={visibility === "Public"}
              onChange={(e) => setVisibility(e.target.value)}
              disabled={isCreating}
            />{" "}
            Herkese Açık
          </label>
          <label>
            <input
              type="radio"
              name="visibility"
              value="DepartmentOnly"
              checked={visibility === "DepartmentOnly"}
              onChange={(e) => setVisibility(e.target.value)}
              disabled={isCreating}
            />{" "}
            Sadece Departman
          </label>
        </div>

        {error && <p style={{ color: "red" }}>{error}</p>}
        <button type="submit" disabled={isCreating}>
          {isCreating ? "Ekleniyor..." : "Ekle"}
        </button>
      </form>

      <h3 style={{ margin: "0 0 8px" }}>Sayfalar</h3>

      {isLoadingPages ? (
        <p>Yükleniyor...</p>
      ) : pages.length === 0 ? (
        <p>Görebileceğin bir sayfa yok.</p>
      ) : (
        pages.map((p) => (
          <div key={p.id} style={{ borderBottom: "1px solid #eee", padding: "12px 0" }}>
            <strong>{p.title}</strong> <em>({p.departmentName}, {p.visibility})</em>
            <p>{p.content}</p>
          </div>
        ))
      )}
    </div>
  );
}

export default WikiBoard;