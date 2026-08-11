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
import WikiFolderBrowsePage from "./components/WikiFolderBrowsePage";
import WikiFavoritesPage from "./components/WikiFavoritesPage";
import WikiPinnedPage from "./components/WikiPinnedPage";
import AuditLogPage from "./components/AuditLogPage";
import VaultPage from "./components/VaultPage";
import VaultEntryFormPage from "./components/VaultEntryFormPage";

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

function WikiFolderBrowseRoute() {
  const { token } = useAuth();
  return <WikiFolderBrowsePage token={token} />;
}

// Audit Log/Vault'un AKSİNE bunlar Wiki İÇERİĞİ (kullanıcının kendi wiki
// sayfalarının bir alt kümesi) - o yüzden top-level DEĞİL, /wiki altında
// nested, WikiLayout'un sidebar/header'ını (Vault/Audit Log gibi ayrı bir
// "araç" sayfası değil, Wiki'nin doğal bir parçası) paylaşıyor.
function WikiFavoritesRoute() {
  const { token } = useAuth();
  return <WikiFavoritesPage token={token} />;
}

function WikiPinnedRoute() {
  const { token } = useAuth();
  return <WikiPinnedPage token={token} />;
}

function AuditLogRoute() {
  const { token } = useAuth();
  return <AuditLogPage token={token} />;
}

// Audit Log ile AYNI gerekçeyle Wiki'nin dışında, üst seviye bir route -
// Vault, Wiki içeriği DEĞİL (bkz. Vault modülünün "WikiPage değil, tamamen
// ayrı bir entity" tasarım kararı), WikiLayout'un sidebar/klasör ağacı
// içinde görünmesi kavramsal olarak yanlış olurdu.
function VaultRoute() {
  const { token } = useAuth();
  return <VaultPage token={token} />;
}

function VaultEntryFormRoute() {
  const { token } = useAuth();
  return <VaultEntryFormPage token={token} />;
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
              {/* Breadcrumb'taki departman/klasör segmentlerinin gittiği yer
                  (bkz. WikiFolderBrowsePage'deki not) - folderId opsiyonel,
                  yoksa departmanın kökü gösteriliyor. */}
              <Route path="browse/:departmentName" element={<WikiFolderBrowseRoute />} />
              <Route path="browse/:departmentName/:folderId" element={<WikiFolderBrowseRoute />} />
              <Route path="favorites" element={<WikiFavoritesRoute />} />
              <Route path="pinned" element={<WikiPinnedRoute />} />
              <Route path=":id" element={<WikiArticleRoute />} />
              <Route path=":id/edit" element={<WikiEditorRoute />} />
            </Route>
            <Route path="/audit-log" element={<AuditLogRoute />} />
            {/* /vault/new'in /vault/:id/edit ile ÇAKIŞMAMASI için (react-router
                "new" ile ":id"yi ayırt edemez sanılabilir ama static segment
                her zaman dynamic'ten önce eşleşir) - WikiEditorPage'in
                /wiki/new + /wiki/:id/edit deseniyle BİREBİR aynı sıra. */}
            <Route path="/vault" element={<VaultRoute />} />
            <Route path="/vault/new" element={<VaultEntryFormRoute />} />
            <Route path="/vault/:id/edit" element={<VaultEntryFormRoute />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
