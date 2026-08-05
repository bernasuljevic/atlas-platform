import { createContext, useContext, useEffect, useState } from "react";
import { refreshAccessToken } from "../api";

const AuthContext = createContext(null);

// JWT'nin payload'ını (imza doğrulaması OLMADAN, sadece "exp" alanını okumak
// için) çözüyor - jwt.js'teki decodeJwtPayload ile aynı iş ama Context burada
// dairesel bir import'a (jwt.js zaten başka yerlerde bu dosyayı import etmiyor
// olsa da) girmemek için küçük, tek satırlık kopyası tutuluyor.
function getTokenExpiryMs(token) {
  try {
    const payloadBase64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const payload = JSON.parse(atob(payloadBase64));
    return payload.exp * 1000;
  } catch {
    return null;
  }
}

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

  useEffect(() => {
    // GERÇEK BUG (canlı doğrulanıp bulundu, 2026-08-03): Wiki modülündeki
    // "açık" endpoint'ler (ör. GET /api/wiki/pages, /api/wiki/dashboard) hiç
    // .RequireAuthorization() KULLANMIYOR - giriş yapmamış bir ziyaretçi bile
    // (sadece Public içerikle) erişebilsin diye bilerek böyle. Ama bu yüzden
    // süresi dolmuş bir access token'la yapılan istekte ASP.NET Core JWT
    // middleware'i 401 DÖNDÜRMÜYOR - kimliği sessizce "anonim" sayıp isteğin
    // devam etmesine izin veriyor. Sonuç: kullanıcı hiçbir hata görmeden
    // departmana özel içeriği (DepartmentOnly sayfalar, Dashboard'daki
    // "Departmanına Özel İçerikler" bölümü) aniden kaybolmuş gibi görüyordu -
    // api.js'teki "401 alınca yenile" deseni bu endpoint'lerde HİÇ
    // tetiklenmiyordu çünkü 401 hiç gelmiyordu. Kalıcı çözüm: süresi dolmadan
    // (15dk) ÖNCE, arka planda kendiliğinden yenile - böylece açık/korumalı
    // hiçbir endpoint süresi dolmuş bir token'la hiç karşılaşmıyor.
    if (!token) return;

    const expiresAtMs = getTokenExpiryMs(token);
    if (expiresAtMs === null) return;

    const refreshAtMs = expiresAtMs - 60_000; // süresi dolmadan 1 dakika önce
    const delayMs = Math.max(0, refreshAtMs - Date.now());

    const timeoutId = setTimeout(() => {
      refreshAccessToken();
    }, delayMs);

    return () => clearTimeout(timeoutId);
  }, [token]);

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
