import { useState } from "react";
import { Link, useNavigate } from "react-router";
import { register } from "../api";
import { Button } from "@atlas/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@atlas/ui/card";
import { Input } from "@atlas/ui/input";
import { Label } from "@atlas/ui/label";
import { RadioGroup, RadioGroupItem } from "@atlas/ui/radio-group";
import { DEPARTMENTS as REAL_DEPARTMENTS } from "../departments";

// "Departmansız" seçeneği burada ekleniyor - kullanıcının kendi departmanı
// nullable (WikiBoard'daki sayfa departmanının aksine), bu yüzden temel listeye
// (departments.js) sadece burada bir seçenek ekleniyor.
const DEPARTMENTS = [
  { value: "", label: "Departmansız (sadece herkese açık sayfalar)" },
  ...REAL_DEPARTMENTS,
];

function Register() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [fullName, setFullName] = useState("");
  const [password, setPassword] = useState("");
  const [department, setDepartment] = useState("");
  const [error, setError] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await register(email, fullName, password, department);
      navigate("/login");
    } catch (err) {
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: "80px auto" }}>
      <Card className="border-[var(--border)] bg-[var(--bg)] text-[var(--text)]">
        <CardHeader>
          <CardTitle className="text-center text-2xl" style={{ color: "var(--text-h)" }}>
            Kayıt Ol
          </CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="register-email">Email</Label>
              <Input
                id="register-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isLoading}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="register-fullname">Ad Soyad</Label>
              <Input
                id="register-fullname"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                disabled={isLoading}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="register-password">Şifre</Label>
              <Input
                id="register-password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isLoading}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label>Departman</Label>
              <RadioGroup value={department} onValueChange={setDepartment} className="flex flex-col gap-2">
                {DEPARTMENTS.map((d) => (
                  <div key={d.value} className="flex items-center gap-2">
                    <RadioGroupItem value={d.value} id={`department-${d.value || "none"}`} disabled={isLoading} />
                    <Label htmlFor={`department-${d.value || "none"}`}>{d.label}</Label>
                  </div>
                ))}
              </RadioGroup>
            </div>
            {error && <p style={{ color: "red" }} className="text-sm">{error}</p>}
            <Button
              type="submit"
              disabled={isLoading}
              className="w-full bg-[var(--brand-accent)] text-[var(--text-h)] hover:opacity-90"
            >
              {isLoading ? "Kayıt olunuyor..." : "Kayıt Ol"}
            </Button>
            <p className="text-center text-sm" style={{ color: "var(--text)" }}>
              Zaten hesabın var mı?{" "}
              <Link to="/login" style={{ color: "var(--brand-accent)" }}>
                Giriş yap
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export default Register;
