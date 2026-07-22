// API'nin adresi tek bir yerde tanımlı - ileride değişirse (örn. gerçek bir sunucuya
// taşındığında) sadece burayı değiştireceğiz, kod içinde arama yapmayacağız.
const API_URL = "http://localhost:5000";

export async function login(email, password) {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw new Error("Email veya şifre yanlış");
  }

  // { accessToken, refreshToken } - App.jsx ikisini de localStorage'a yazacak.
  return response.json();
}

// GÜVENLİK DÜZELTMESİ: Artık departman filtresi bir query parametresi değil -
// backend, giriş yapmış kullanıcının GERÇEK departmanını JWT'den okuyor. Bu
// yüzden token'ı buraya da göndermemiz gerekiyor (önceden hiç göndermiyorduk -
// bu da düzeltmeden önce bile department-bazlı görünürlüğün React tarafında
// hiç gerçek anlamda çalışmadığı, sadece elle test edilebildiği anlamına geliyordu).
export async function getWikiPages(accessToken) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const response = await fetch(`${API_URL}/api/wiki/pages`, { headers });

  if (!response.ok) {
    throw new Error("Wiki sayfaları yüklenemedi");
  }

  return response.json();
}

// Access token'ın süresi dolunca (artık 15 dakika - eskiden 8 saatti) sunucudan
// 401 gelir. Bu fonksiyon, localStorage'daki refresh token'ı kullanarak yeni bir
// access+refresh token çifti almayı dener. Refresh token da geçersizse (7 gün
// dolmuş ya da zaten kullanılmışsa - rotation) kullanıcı çıkış yapmış sayılır ve
// "atlas:auth-expired" event'i yayınlanır, App.jsx bunu dinleyip login ekranına döner.
async function refreshAccessToken() {
  const refreshToken = localStorage.getItem("refreshToken");
  if (!refreshToken) return null;

  const response = await fetch(`${API_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    window.dispatchEvent(new Event("atlas:auth-expired"));
    return null;
  }

  const tokens = await response.json();
  localStorage.setItem("accessToken", tokens.accessToken);
  localStorage.setItem("refreshToken", tokens.refreshToken);
  // App.jsx'teki React state'i de güncel tutalım - yoksa bir sonraki render'da
  // hâlâ eski (süresi dolmuş) access token prop olarak geçilir.
  window.dispatchEvent(new CustomEvent("atlas:tokens-refreshed", { detail: tokens }));

  return tokens.accessToken;
}

export async function createWikiPage(accessToken, page) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/pages`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        // Token'ı burada gönderiyoruz - hatırlarsan backend'de .RequireAuthorization()
        // tam bunu bekliyordu.
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(page),
    });

  let response = await doRequest(accessToken);

  // 401 = access token süresi dolmuş olabilir - refresh token ile bir kez daha dene.
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    throw new Error("Sayfa oluşturulamadı - giriş yapmış olman lazım");
  }

  return response.json();
}
