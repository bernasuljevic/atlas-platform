import { useState } from "react";
import { login } from "../api";

// "onLoginSuccess" burada bir prop - App.jsx'ten gelen bir fonksiyon. Login başarılı
// olduğunda, token'ı App.jsx'e "geri bildiriyoruz" - Login kendisi token'ı saklamıyor,
// sadece App.jsx'e haber veriyor. Bu, backend'deki "sorumluluk ayrımı" fikrinin
// React karşılığı - Login sadece "giriş formu göster ve sonucu bildir" işini yapıyor.
function Login({ onLoginSuccess }) {
  const [email, setEmail] = useState("admin@atlas.local");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);

  async function handleSubmit(e) {
    e.preventDefault(); // formun sayfayı yenilemesini engeller (tarayıcının varsayılanı)
    setError(null);

    try {
      const token = await login(email, password);
      onLoginSuccess(token);
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div style={{ maxWidth: 320, margin: "80px auto" }}>
      <h1>Atlas Platform</h1>
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: 12 }}>
          <label>Email</label>
          <br />
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            style={{ width: "100%", padding: 8 }}
          />
        </div>
        <div style={{ marginBottom: 12 }}>
          <label>Şifre</label>
          <br />
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ width: "100%", padding: 8 }}
          />
        </div>
        {error && <p style={{ color: "red" }}>{error}</p>}
        <button type="submit" style={{ width: "100%", padding: 10 }}>
          Giriş Yap
        </button>
      </form>
    </div>
  );
}

export default Login;