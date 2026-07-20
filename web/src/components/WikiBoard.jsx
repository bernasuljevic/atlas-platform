import { useState, useEffect } from "react";
import { getWikiPages, createWikiPage } from "../api";

function WikiBoard({ token }) {
  const [pages, setPages] = useState([]);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [department, setDepartment] = useState("IT");
  const [error, setError] = useState(null);

  // useEffect'in ikinci parametresi olan [] (boş dizi), "sadece sayfa İLK açıldığında
  // bir kere çalış" demek. Eğer [] yerine [department] yazsaydık, department her
  // değiştiğinde de yeniden çalışırdı - şimdilik buna ihtiyacımız yok.
  useEffect(() => {
    loadPages();
  }, []);

  async function loadPages() {
    try {
      const data = await getWikiPages();
      setPages(data);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleCreate(e) {
    e.preventDefault();
    setError(null);

    try {
      await createWikiPage(token, {
        title,
        content,
        departmentName: department,
        visibility: "Public",
      });

      // Sayfa eklendikten sonra formu temizle ve listeyi tazele -
      // böylece kullanıcı yeni sayfayı anında görür.
      setTitle("");
      setContent("");
      await loadPages();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div style={{ maxWidth: 600, margin: "40px auto" }}>
      <h1>Atlas Wiki</h1>

      <form onSubmit={handleCreate} style={{ marginBottom: 32, border: "1px solid #ccc", padding: 16 }}>
        <h3>Yeni Sayfa</h3>
        <input
          placeholder="Başlık"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          style={{ width: "100%", padding: 8, marginBottom: 8 }}
        />
        <textarea
          placeholder="İçerik"
          value={content}
          onChange={(e) => setContent(e.target.value)}
          style={{ width: "100%", padding: 8, marginBottom: 8, minHeight: 80 }}
        />
        <input
          placeholder="Departman"
          value={department}
          onChange={(e) => setDepartment(e.target.value)}
          style={{ width: "100%", padding: 8, marginBottom: 8 }}
        />
        {error && <p style={{ color: "red" }}>{error}</p>}
        <button type="submit">Ekle</button>
      </form>

      <h3>Sayfalar</h3>
      {/* .map(), her sayfa için bir <div> üretiyor - React listelerde standart yöntem.
          "key" özelliği React'e her satırı ayırt etmesi için gerekli, unique bir id
          kullanıyoruz (p.id). */}
      {pages.map((p) => (
        <div key={p.id} style={{ borderBottom: "1px solid #eee", padding: "12px 0" }}>
          <strong>{p.title}</strong> <em>({p.departmentName})</em>
          <p>{p.content}</p>
        </div>
      ))}
    </div>
  );
}

export default WikiBoard;