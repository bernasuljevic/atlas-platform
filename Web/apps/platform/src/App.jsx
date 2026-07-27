import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { AuthProvider, useAuth } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import Login from "./components/Login";
import Register from "./components/Register";
import WikiBoard from "./components/WikiBoard";

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

function WikiRoute() {
  const { token, logout } = useAuth();
  return <WikiBoard token={token} onLogout={logout} />;
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
        <Routes>
          <Route path="/" element={<RootRedirect />} />
          <Route path="/login" element={<LoginRoute />} />
          <Route path="/register" element={<RegisterRoute />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/wiki" element={<WikiRoute />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
