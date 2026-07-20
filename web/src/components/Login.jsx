import { useState } from "react";
import { login } from "../api";

function Login({ onLoginSuccess }) {
  const [email, setEmail] = useState("admin2@atlas.local");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  // isLoading: istek sürerken true, bitince false. Butonun devre dışı kalması
  // ve metninin değişmesi için kullanacağız - kullanıcı "bir şey oluyor" bilsin.
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const token = await login(email, password);
      onLoginSuccess(token);
    } catch (err) {
      setError(err.message);
    } finally {
      // finally: hem başarı hem hata durumunda çalışır - "istek bitti" bilgisini
      // tek bir yerde, unutmadan işaretlemek için ideal.
      setIsLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: "80px auto", textAlign: "center" }}>
      <h1 style={{ fontSize: 28, marginBottom: 24 }}>Atlas Platform</h1>
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: 12 }}>
          <label>Email</label>
          <br />
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={isLoading}
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
            disabled={isLoading}
            style={{ width: "100%", padding: 8 }}
          />
        </div>
        {error && <p style={{ color: "red" }}>{error}</p>}
        <button type="submit" disabled={isLoading} style={{ width: "100%", padding: 10 }}>
          {isLoading ? "Giriş yapılıyor..." : "Giriş Yap"}
        </button>
      </form>
    </div>
  );
}

export default Login;