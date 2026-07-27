import { createContext, useContext, useEffect, useState } from "react";

const AuthContext = createContext(null);

// Token state'i ve localStorage senkronizasyonunu tek bir yerde topluyoruz -
// eskiden bunların hepsi App.jsx'teydi, route'lara bölünce her route'un
// (Login, ProtectedRoute, WikiBoard) aynı token bilgisine ihtiyacı olduğu
// için bir Context'e taşımak gerekti (props ile route'lar arası taşımak yerine).
export function AuthProvider({ children }) {
  // Başlangıç değerini localStorage'dan okuyoruz (lazy initializer - fonksiyon
  // sadece ilk render'da çalışır). Sayfa yenilendiğinde token hafızadan uçmasın diye.
  const [token, setToken] = useState(() => localStorage.getItem("accessToken"));

  function login({ accessToken, refreshToken }) {
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("refreshToken", refreshToken);
    setToken(accessToken);
  }

  function logout() {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    setToken(null);
  }

  useEffect(() => {
    // api.js, access token'ı arka planda kendi başına yenilediğinde (401 sonrası)
    // localStorage'ı güncelliyor ama React state'ini (buradaki "token") bilmiyor -
    // bu event ikisini senkron tutuyor. Refresh token da tükenmişse
    // ("atlas:auth-expired") direkt çıkış yaptırıyoruz.
    function handleTokensRefreshed(e) {
      setToken(e.detail.accessToken);
    }
    function handleAuthExpired() {
      logout();
    }

    window.addEventListener("atlas:tokens-refreshed", handleTokensRefreshed);
    window.addEventListener("atlas:auth-expired", handleAuthExpired);
    return () => {
      window.removeEventListener("atlas:tokens-refreshed", handleTokensRefreshed);
      window.removeEventListener("atlas:auth-expired", handleAuthExpired);
    };
  }, []);

  return (
    <AuthContext.Provider value={{ token, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth, AuthProvider içinde kullanılmalı.");
  }
  return context;
}
