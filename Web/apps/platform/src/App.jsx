import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { Toaster } from "@atlas/ui/sonner";
import { AuthProvider, useAuth } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import Login from "./components/Login";
import Register from "./components/Register";
import VerifyEmailPage from "./components/VerifyEmailPage";
import WikiLayout from "./components/WikiLayout";
import HomePage from "./components/HomePage";
import WikiBoard from "./components/WikiBoard";
import WikiArticlePage from "./components/WikiArticlePage";
import WikiEditorPage from "./components/WikiEditorPage";
import AuditLogPage from "./components/AuditLogPage";

// Zaten giriş yapılmışsa /login'e gitmeye çalışmak anlamsız - /wiki'ye yönlendiriyoruz.
function LoginRoute() {
  const { token, login } = useAuth();

  if (token) {
    return <Navigate to="/wiki" replace />;
  }

  return <Login onLoginSuccess={login} />;
}

// Aynı mantık /register için de geçerli - zaten giriş yapmış birinin kayıt
// formunu görmesine gerek yok.
function RegisterRoute() {
  const { token } = useAuth();

  if (token) {
    return <Navigate to="/wiki" replace />;
  }

  return <Register />;
}

// Sidebar (klasör ağacı + çıkış/audit-log) artık burada, /wiki altındaki TÜM
// alt sayfalarda (liste, tam sayfa okuma, editör) sabit kalıyor - bkz.
// WikiLayout'taki mimari notu.
function WikiLayoutRoute() {
  const { token, logout } = useAuth();
  return <WikiLayout token={token} onLogout={logout} />;
}

// Artık giriş yapınca doğrudan makale listesine değil buraya (Dashboard)
// geliniyor - liste eski davranışını /wiki/pages'te koruyor (bkz.
// WikiPagesRoute), sidebar'daki "Atlas Wiki" başlık linki de zaten /wiki'ye
// (yani buraya) gidiyor - ayrıca bir "Ana Sayfa" linkine gerek kalmadı.
function WikiIndexRoute() {
  const { token } = useAuth();
  return <HomePage token={token} />;
}

function WikiPagesRoute() {
  const { token } = useAuth();
  return <WikiBoard token={token} />;
}

function WikiArticleRoute() {
  const { token } = useAuth();
  return <WikiArticlePage token={token} />;
}

function WikiEditorRoute() {
  const { token } = useAuth();
  return <WikiEditorPage token={token} />;
}

function AuditLogRoute() {
  const { token } = useAuth();
  return <AuditLogPage token={token} />;
}

// "/" hiçbir zaman kendi başına bir sayfa değil - sadece giriş durumuna göre
// doğru yere yönlendiren bir trafik yönlendiricisi.
function RootRedirect() {
  const { token } = useAuth();
  return <Navigate to={token ? "/wiki" : "/login"} replace />;
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        {/* Tek bir yerde, kök seviyede - herhangi bir component toast()
            çağırdığında (ör. ProtectedRoute'taki SignalR bildirimi) burada
            render edilir. */}
        <Toaster position="top-right" />
        <Routes>
          <Route path="/" element={<RootRedirect />} />
          <Route path="/login" element={<LoginRoute />} />
          <Route path="/register" element={<RegisterRoute />} />
          {/* Giriş yapmamış OLMASI gereken bir kullanıcının erişebileceği tek
              "ara" ekran - kayıt olmuş ama henüz e-postasını doğrulamamış. */}
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/wiki" element={<WikiLayoutRoute />}>
              <Route index element={<WikiIndexRoute />} />
              <Route path="pages" element={<WikiPagesRoute />} />
              <Route path="new" element={<WikiEditorRoute />} />
              <Route path=":id" element={<WikiArticleRoute />} />
              <Route path=":id/edit" element={<WikiEditorRoute />} />
            </Route>
            <Route path="/audit-log" element={<AuditLogRoute />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
