import { useState } from "react";
import { login } from "../api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

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
      // login() artık { accessToken, refreshToken } döndürüyor - tek bir string değil.
      const tokens = await login(email, password);
      onLoginSuccess(tokens);
    } catch (err) {
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
              className="w-full bg-[var(--brand-accent)] text-[var(--text-h)] hover:opacity-90"
            >
              {isLoading ? "Giriş yapılıyor..." : "Giriş Yap"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export default Login;