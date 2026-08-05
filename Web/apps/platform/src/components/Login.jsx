import { useState } from "react";
import { Link, useNavigate } from "react-router";
import { login } from "../api";
import { Button } from "@atlas/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";

function Login({ onLoginSuccess }) {
  const navigate = useNavigate();
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
      // login() artık { accessToken, refreshToken } döndürüyor - tek bir string değil.
      const tokens = await login(email, password);
      onLoginSuccess(tokens);
    } catch (err) {
      // emailNotVerified: api.js'in 403 (doğrulanmamış hesap) için özel olarak
      // işaretlediği hata - kullanıcıyı direkt doğrulama ekranına yönlendiriyoruz,
      // "email veya şifre yanlış" gibi yanıltıcı bir mesaj göstermek yerine.
      if (err.emailNotVerified) {
        navigate("/verify-email", { state: { email } });
        return;
      }
      setError(err.message);
    } finally {
      // finally: hem başarı hem hata durumunda çalışır - "istek bitti" bilgisini
      // tek bir yerde, unutmadan işaretlemek için ideal.
      setIsLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: "80px auto" }}>
      {/* shadcn'in varsayılan nötr Card/Button renkleri yerine sitenin kendi
          CSS değişkenlerini (--bg, --border, --text, --accent) kullanıyoruz -
          böylece kart, sitenin mor/koyu temasıyla aynı hissi veriyor. */}
      <Card className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardHeader>
          <CardTitle className="text-center text-2xl" style={{ color: "var(--text-h)" }}>
            Atlas Platform
          </CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="login-email">Email</Label>
              <Input
                id="login-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isLoading}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="login-password">Şifre</Label>
              <Input
                id="login-password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isLoading}
              />
            </div>
            {error && <p style={{ color: "red" }}>{error}</p>}
            <Button
              type="submit"
              disabled={isLoading}
              className="w-full text-white hover:opacity-90"
              style={{ background: "var(--brand-accent)" }}
            >
              {isLoading ? "Giriş yapılıyor..." : "Giriş Yap"}
            </Button>
            <p className="text-center text-sm" style={{ color: "var(--text)" }}>
              Hesabın yok mu?{" "}
              <Link to="/register" style={{ color: "var(--brand-accent)" }}>
                Kayıt ol
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export default Login;
