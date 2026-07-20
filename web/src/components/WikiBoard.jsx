import { useState, useEffect } from "react";
import { getWikiPages, createWikiPage } from "../api";

function WikiBoard({ token, onLogout }) {
  const [pages, setPages] = useState([]);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [department, setDepartment] = useState("IT");
  const [visibility, setVisibility] = useState("Public");
  // filterDepartment: liste tarafındaki filtre - form'daki "department" state'inden
  // BİLEREK ayrı tutuyoruz, çünkü "hangi departmana sayfa eklediğim" ile
  // "hangi departmanı görüntülediğim" birbirinden bağımsız iki karar.
  const [filterDepartment, setFilterDepartment] = useState("");
  const [error, setError] = useState(null);
  const [isLoadingPages, setIsLoadingPages] = useState(true);
  const [isCreating, setIsCreating] = useState(false);

  // İkinci parametre artık [filterDepartment] - yani filtre her değiştiğinde
  // bu effect yeniden çalışacak, listeyi güncelleyecek. Dünkü halinde []
  // vardı, sadece ilk açılışta çalışıyordu.
  useEffect(() => {
    loadPages();
  }, [filterDepartment]);

  async function loadPages() {
    setIsLoadingPages(true);
    try {
      const data = await getWikiPages(filterDepartment || undefined);
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

      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 8 }}>
        <h3 style={{ margin: 0 }}>Sayfalar</h3>
        <div>
          <label style={{ marginRight: 8 }}>Departman filtresi:</label>
          <input
            placeholder="(boş = sadece herkese açık)"
            value={filterDepartment}
            onChange={(e) => setFilterDepartment(e.target.value)}
            style={{ padding: 6 }}
          />
        </div>
      </div>

      {isLoadingPages ? (
        <p>Yükleniyor...</p>
      ) : pages.length === 0 ? (
        <p>Bu filtreyle görünen sayfa yok.</p>
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