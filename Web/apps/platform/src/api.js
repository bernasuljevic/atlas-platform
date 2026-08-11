// API'nin adresi tek bir yerde tanımlı - ileride değişirse (örn. gerçek bir sunucuya
// taşındığında) sadece burayı değiştireceğiz, kod içinde arama yapmayacağız.
const API_URL = "http://localhost:5000";

export async function register(email, fullName, password, department) {
  const response = await fetch(`${API_URL}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    // department boşsa (Departmansız seçildiyse) hiç göndermiyoruz - backend
    // zaten null/eksikse departmansız kullanıcı olarak kaydediyor.
    body: JSON.stringify(department ? { email, fullName, password, department } : { email, fullName, password }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? body?.detail ?? "Kayıt oluşturulamadı");
  }

  return response.json();
}

export async function login(email, password) {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  // Backend, doğrulanmamış bir hesapla giriş denemesini 403 ile (yanlış
  // şifredeki 401'den AYRI) reddediyor (bkz. LoginCommandHandler) - burada da
  // ayırt ediyoruz ki Login.jsx kullanıcıyı doğrulama ekranına yönlendirebilsin.
  if (response.status === 403) {
    const body = await response.json().catch(() => null);
    const error = new Error(body?.detail ?? "E-postanı doğrulamadan giriş yapamazsın.");
    error.emailNotVerified = true;
    throw error;
  }

  if (!response.ok) {
    throw new Error("Email veya şifre yanlış");
  }

  // { accessToken, refreshToken } - App.jsx ikisini de localStorage'a yazacak.
  return response.json();
}

export async function verifyEmail(email, code) {
  const response = await fetch(`${API_URL}/api/auth/verify-email`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, code }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? body?.detail ?? "Kod doğrulanamadı");
  }
}

export async function resendVerificationCode(email) {
  const response = await fetch(`${API_URL}/api/auth/resend-verification-code`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (response.status === 429) {
    throw new Error("Çok sık denedin - bir dakika bekleyip tekrar dene.");
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? "Kod gönderilemedi");
  }
}

// GÜVENLİK DÜZELTMESİ: Artık departman filtresi bir query parametresi değil -
// backend, giriş yapmış kullanıcının GERÇEK departmanını JWT'den okuyor. Bu
// yüzden token'ı buraya da göndermemiz gerekiyor (önceden hiç göndermiyorduk -
// bu da düzeltmeden önce bile department-bazlı görünürlüğün React tarafında
// hiç gerçek anlamda çalışmadığı, sadece elle test edilebildiği anlamına geliyordu).
// Backend artık sayfalanmış bir sonuç dönüyor: {items, pageNumber, pageSize,
// totalCount, totalPages}. pageNumber/pageSize opsiyonel - verilmezse backend
// varsayılan olarak 1/10 kullanıyor.
export async function getWikiPages(accessToken, pageNumber = 1, pageSize = 10) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const response = await fetch(
    `${API_URL}/api/wiki/pages?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    { headers }
  );

  if (!response.ok) {
    throw new Error("Wiki sayfaları yüklenemedi");
  }

  return response.json();
}

// Üst bardaki arama kutusunun harf harf çağırdığı hafif öneri endpoint'i -
// searchWikiPages'teki (AI/anlam bazlı) aramadan BİLEREK ayrı, bkz.
// backend'deki SearchWikiPageSuggestionsQuery'nin notu.
export async function getWikiSearchSuggestions(accessToken, queryText) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const response = await fetch(
    `${API_URL}/api/wiki/search-suggestions?q=${encodeURIComponent(queryText)}`,
    { headers }
  );

  if (!response.ok) {
    throw new Error("Öneriler yüklenemedi");
  }

  return response.json();
}

export async function getWikiDashboard(accessToken) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const response = await fetch(`${API_URL}/api/wiki/dashboard`, { headers });

  if (!response.ok) {
    throw new Error("Ana sayfa yüklenemedi");
  }

  return response.json();
}

// Access token'ın süresi dolunca (artık 15 dakika - eskiden 8 saatti) sunucudan
// 401 gelir. Bu fonksiyon, localStorage'daki refresh token'ı kullanarak yeni bir
// access+refresh token çifti almayı dener. Refresh token da geçersizse (7 gün
// dolmuş ya da zaten kullanılmışsa - rotation) kullanıcı çıkış yapmış sayılır ve
// "atlas:auth-expired" event'i yayınlanır, App.jsx bunu dinleyip login ekranına döner.
export async function refreshAccessToken() {
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

export async function getWikiPageById(accessToken, pageId) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const response = await fetch(`${API_URL}/api/wiki/pages/${pageId}`, { headers });

  if (response.status === 404) {
    throw new Error("Bu sayfa artık mevcut değil ya da görme yetkin yok.");
  }
  if (!response.ok) {
    throw new Error("Sayfa yüklenemedi");
  }

  return response.json();
}

// fromUtc/toUtc opsiyonel - normal semantik aramaya EK, isteğe bağlı bir
// tarih daraltması (bkz. SearchWikiPagesByMeaningQuery'deki not).
export async function searchWikiPages(accessToken, queryText, topN = 5, { fromUtc, toUtc } = {}) {
  const params = new URLSearchParams({ q: queryText, topN });
  if (fromUtc) params.set("fromUtc", fromUtc);
  if (toUtc) params.set("toUtc", toUtc);

  const doRequest = (token) =>
    fetch(`${API_URL}/api/ai/search?${params.toString()}`, {
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);

  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? "Arama yapılamadı");
  }

  return response.json();
}

export async function deleteWikiPage(accessToken, pageId) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/pages/${pageId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);

  // createWikiPage'deki AYNI 401 -> refresh -> tekrar dene deseni.
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error("Bu sayfayı silme yetkin yok.");
    }
    throw new Error("Sayfa silinemedi");
  }
}

// Sadece Admin - backend zaten .RequireRole("Admin") ile koruyor, burada
// filtreleri (hepsi opsiyonel) query string'e çeviriyoruz. Boş/undefined
// filtreler hiç query string'e eklenmiyor (backend'in "hiç filtre yok"
// varsayılan davranışını tetiklemesi için).
export async function getAuditLog(accessToken, { details, fromUtc, toUtc, pageNumber = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams({ pageNumber, pageSize });
  if (details) params.set("details", details);
  if (fromUtc) params.set("fromUtc", fromUtc);
  if (toUtc) params.set("toUtc", toUtc);

  const doRequest = (token) =>
    fetch(`${API_URL}/api/audit-log?${params.toString()}`, {
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);

  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error("Audit log'u görme yetkin yok.");
    }
    throw new Error("Audit log yüklenemedi");
  }

  return response.json();
}

// GetWikiFolderTree açık bir endpoint (token'sız da çalışır) - ama token varsa
// gönderiyoruz ki kendi departmanını gezerken TAM erişim (Public+DepartmentOnly)
// alsın, başka bir departmanı gezerken zaten Handler bunu Public'e budayacak
// (bkz. GetWikiFolderTreeQueryHandler).
export async function getWikiFolderTree(accessToken, departmentName) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const response = await fetch(
    `${API_URL}/api/wiki/folders?departmentName=${encodeURIComponent(departmentName)}`,
    { headers }
  );

  if (!response.ok) {
    throw new Error("Klasör ağacı yüklenemedi");
  }

  return response.json();
}

export async function createWikiFolder(accessToken, folder) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/folders`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify(folder),
    });

  let response = await doRequest(accessToken);

  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? "Klasör oluşturulamadı");
  }

  return response.json();
}

export async function updateWikiPage(accessToken, pageId, page) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/pages/${pageId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify(page),
    });

  let response = await doRequest(accessToken);

  // createWikiPage'deki AYNI 401 -> refresh -> tekrar dene deseni.
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error("Bu sayfayı düzenleme yetkin yok.");
    }
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? "Sayfa güncellenemedi");
  }
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

// "Tartışma" sekmesi (bkz. DiscussionPanel.jsx) - pageId verilmezse ana
// sayfadaki genel platform tartışmasının yorumları geliyor. Açık endpoint
// (getWikiPages ile AYNI desen) ama görünürlük backend'de zaten uygulanıyor.
export async function getComments(accessToken, pageId) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const query = pageId ? `?pageId=${encodeURIComponent(pageId)}` : "";
  const response = await fetch(`${API_URL}/api/wiki/comments${query}`, { headers });

  if (!response.ok) {
    throw new Error("Yorumlar yüklenemedi");
  }

  return response.json();
}

export async function createComment(accessToken, { pageId, content }) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/comments`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify({ pageId: pageId ?? null, content }),
    });

  let response = await doRequest(accessToken);

  // createWikiPage'deki AYNI 401 -> refresh -> tekrar dene deseni.
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? "Yorum gönderilemedi");
  }

  return response.json();
}

// Favoriler/Pinler - eskiden TAMAMEN localStorage'daydı (bkz. WikiArticlePage.jsx'in
// eski togglePin/toggleFavorite'i), artık gerçek, kullanıcı bazlı bir backend
// tablosu. Toggle endpoint'leri dönüş değerinde işlemden SONRAKİ durumu (bool)
// veriyor - istemci bunun için ayrı bir GET atmak zorunda kalmıyor.
export async function toggleFavorite(accessToken, pageId) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/pages/${pageId}/favorite`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    throw new Error("Favori durumu güncellenemedi");
  }

  const body = await response.json();
  return body.isFavorited;
}

export async function togglePin(accessToken, pageId) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/pages/${pageId}/pin`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    throw new Error("Pin durumu güncellenemedi");
  }

  const body = await response.json();
  return body.isPinned;
}

export async function getFavoritePages(accessToken) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/favorites`, {
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    throw new Error("Favoriler yüklenemedi");
  }

  return response.json();
}

export async function getPinnedPages(accessToken) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/pinned`, {
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    throw new Error("Pinlenenler yüklenemedi");
  }

  return response.json();
}

// Şifre Kasası (Vault) - Faz 7/Gün 3. WikiPage'in aksine TÜM uç noktalar
// (liste dahil) token gerektiriyor - backend'de hiçbiri anonim erişime açık
// değil (bkz. VaultEndpoints - hepsi .RequireAuthorization()). Bu yüzden
// hepsi createWikiPage/getAuditLog'daki AYNI 401 -> refresh -> tekrar dene
// desenini kullanıyor, getWikiPages'teki gibi "token opsiyonel" davranış YOK.
export async function getPasswordEntries(accessToken, category) {
  const params = category ? `?category=${encodeURIComponent(category)}` : "";
  const doRequest = (token) =>
    fetch(`${API_URL}/api/vault/entries${params}`, {
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    throw new Error("Kasa kayıtları yüklenemedi");
  }

  return response.json();
}

// GetPasswordEntryByIdQueryHandler'daki "varlığını bile sızdırma" kuralı
// yüzünden 404, "kayıt yok" ile "görme yetkin yok"u BİLEREK ayırt etmiyor.
export async function getPasswordEntryById(accessToken, id) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/vault/entries/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (response.status === 404) {
    throw new Error("Bu kayıt artık mevcut değil ya da görme yetkin yok.");
  }
  if (!response.ok) {
    throw new Error("Kayıt yüklenemedi");
  }

  return response.json();
}

export async function createPasswordEntry(accessToken, entry) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/vault/entries`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify(entry),
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? body?.detail ?? "Kayıt oluşturulamadı");
  }

  return response.json();
}

export async function updatePasswordEntry(accessToken, id, entry) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/vault/entries/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify(entry),
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error("Bu kaydı düzenleme yetkin yok.");
    }
    const body = await response.json().catch(() => null);
    const firstError = body?.errors && Object.values(body.errors)[0]?.[0];
    throw new Error(firstError ?? "Kayıt güncellenemedi");
  }
}

export async function deletePasswordEntry(accessToken, id) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/vault/entries/${id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error("Bu kaydı silme yetkin yok.");
    }
    throw new Error("Kayıt silinemedi");
  }
}

// Şifrenin KENDİSİNİ döndüren TEK yol - her çağrı backend'de "Revealed" audit
// kaydı bırakır ve LastAccessedAtUtc'yi günceller (bkz. RevealPasswordEntryCommand'daki
// not) - o yüzden SADECE kullanıcı gerçekten "Göster"/"Kopyala"ya bastığında
// çağrılmalı, liste/detay çekerken hiç otomatik tetiklenmemeli.
export async function revealPasswordEntry(accessToken, id) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/vault/entries/${id}/reveal`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);
  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error("Bu kaydın parolasını görme yetkin yok.");
    }
    throw new Error("Parola görüntülenemedi");
  }

  const body = await response.json();
  return body.password;
}

export async function deleteComment(accessToken, commentId) {
  const doRequest = (token) =>
    fetch(`${API_URL}/api/wiki/comments/${commentId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` },
    });

  let response = await doRequest(accessToken);

  if (response.status === 401) {
    const newAccessToken = await refreshAccessToken();
    if (newAccessToken) {
      response = await doRequest(newAccessToken);
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error("Bu yorumu silme yetkin yok.");
    }
    throw new Error("Yorum silinemedi");
  }
}
