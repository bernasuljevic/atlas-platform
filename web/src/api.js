// API'nin adresi tek bir yerde tanımlı - ileride değişirse (örn. gerçek bir sunucuya
// taşındığında) sadece burayı değiştireceğiz, kod içinde arama yapmayacağız.
const API_URL = "http://localhost:5080";

export async function login(email, password) {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw new Error("Email veya şifre yanlış");
  }

  const data = await response.json();
  return data.token;
}

export async function getWikiPages(department) {
  const query = department ? `?department=${department}` : "";
  const response = await fetch(`${API_URL}/api/wiki/pages${query}`);

  if (!response.ok) {
    throw new Error("Wiki sayfaları yüklenemedi");
  }

  return response.json();
}

export async function createWikiPage(token, page) {
  const response = await fetch(`${API_URL}/api/wiki/pages`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      // Token'ı burada gönderiyoruz - hatırlarsan backend'de .RequireAuthorization()
      // tam bunu bekliyordu.
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(page),
  });

  if (!response.ok) {
    throw new Error("Sayfa oluşturulamadı - giriş yapmış olman lazım");
  }

  return response.json();
}