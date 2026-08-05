import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { resendVerificationCode, verifyEmail } from "../api";
import { Button } from "@atlas/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";

const RESEND_COOLDOWN_SECONDS = 60;

// Register/Login'den e-posta state ile taşınıyor (bkz. o iki component'teki
// navigate çağrıları) - burada AYRICA düzenlenebilir tutuyoruz, çünkü sayfa
// yenilenirse (F5) React Router state'i kaybolur, kullanıcı email'i elle
// girip devam edebilsin diye.
function VerifyEmailPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState(location.state?.email ?? "");
  const [code, setCode] = useState("");
  const [error, setError] = useState(null);
  const [isVerifying, setIsVerifying] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [resendMessage, setResendMessage] = useState(null);
  const [cooldownSeconds, setCooldownSeconds] = useState(0);
  const cooldownIntervalRef = useRef(null);

  useEffect(() => {
    return () => clearInterval(cooldownIntervalRef.current);
  }, []);

  function startCooldown() {
    setCooldownSeconds(RESEND_COOLDOWN_SECONDS);
    cooldownIntervalRef.current = setInterval(() => {
      setCooldownSeconds((s) => {
        if (s <= 1) {
          clearInterval(cooldownIntervalRef.current);
          return 0;
        }
        return s - 1;
      });
    }, 1000);
  }

  async function handleVerify(e) {
    e.preventDefault();
    setError(null);
    setIsVerifying(true);

    try {
      await verifyEmail(email, code);
      // Backend'in Login akışıyla AYNI - doğrulama sonrası otomatik giriş
      // yapmıyoruz, kullanıcı şifresini yeniden girip normal giriş yapıyor
      // (basit tutmak için - kod doğrulamanın kendisi bir kimlik doğrulama
      // token'ı üretmiyor, sadece hesabı aktif hale getiriyor).
      navigate("/login", { state: { justVerified: true } });
    } catch (err) {
      setError(err.message);
    } finally {
      setIsVerifying(false);
    }
  }

  async function handleResend() {
    setError(null);
    setResendMessage(null);
    setIsResending(true);

    try {
      await resendVerificationCode(email);
      setResendMessage("Yeni kod gönderildi - gelen kutunu kontrol et.");
      startCooldown();
    } catch (err) {
      setError(err.message);
    } finally {
      setIsResending(false);
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: "80px auto" }}>
      <Card className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardHeader>
          <CardTitle className="text-center text-2xl" style={{ color: "var(--text-h)" }}>
            E-postanı Doğrula
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-4 text-sm" style={{ color: "var(--text)" }}>
            <strong>{email || "e-posta adresine"}</strong> gönderdiğimiz 6 haneli kodu gir. Kod 10 dakika
            geçerli.
          </p>
          <form onSubmit={handleVerify} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="verify-email">Email</Label>
              <Input
                id="verify-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isVerifying}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="verify-code">Doğrulama Kodu</Label>
              <Input
                id="verify-code"
                inputMode="numeric"
                maxLength={6}
                placeholder="123456"
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                disabled={isVerifying}
                className="text-center text-lg tracking-[0.3em]"
              />
            </div>

            {error && <p style={{ color: "red" }} className="text-sm">{error}</p>}
            {resendMessage && (
              <p className="text-sm" style={{ color: "var(--brand-accent)" }}>
                {resendMessage}
              </p>
            )}

            <Button
              type="submit"
              disabled={isVerifying || code.length !== 6 || !email}
              className="w-full text-white hover:opacity-90"
              style={{ background: "var(--brand-accent)" }}
            >
              {isVerifying ? "Doğrulanıyor..." : "Doğrula"}
            </Button>

            <Button
              type="button"
              variant="outline"
              className="w-full"
              disabled={isResending || cooldownSeconds > 0 || !email}
              onClick={handleResend}
            >
              {cooldownSeconds > 0
                ? `Yeniden gönder (${cooldownSeconds}s)`
                : isResending
                  ? "Gönderiliyor..."
                  : "Kodu tekrar gönder"}
            </Button>

            <p className="text-center text-sm" style={{ color: "var(--text)" }}>
              <Link to="/login" style={{ color: "var(--brand-accent)" }}>
                Giriş ekranına dön
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export default VerifyEmailPage;
