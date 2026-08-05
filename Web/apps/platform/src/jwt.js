// JWT'nin ortadaki (payload) parçasını çözüyoruz - imzayı DOĞRULAMIYORUZ, buna
// gerek yok çünkü backend zaten her isteği kendisi doğruluyor. Burada sadece
// UI'da "bu kullanıcı Admin mi, departmanı ne, id'si ne" gibi bilgileri
// GÖSTERMEK için okuyoruz - gerçek yetkilendirme her zaman backend'de.
// Tek bir base64url decode için jwt-decode paketi eklemeye gerek yoktu.
export function decodeJwtPayload(token) {
  const payloadBase64Url = token.split(".")[1];
  const payloadBase64 = payloadBase64Url.replace(/-/g, "+").replace(/_/g, "/");
  const json = decodeURIComponent(
    atob(payloadBase64)
      .split("")
      .map((c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0"))
      .join("")
  );
  return JSON.parse(json);
}

// JwtTokenGenerator.cs'teki claim isimleriyle BİREBİR eşleşiyor - Role/Name/Email
// için ClaimTypes kullanıldığı için kısa isim değil bu uzun URI'ler geliyor,
// department ise özel bir claim olduğu için kısa isim.
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
const NAME_IDENTIFIER_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
const NAME_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
const EMAIL_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

export function getUserInfoFromToken(token) {
  const payload = decodeJwtPayload(token);
  return {
    userId: payload[NAME_IDENTIFIER_CLAIM],
    fullName: payload[NAME_CLAIM] ?? null,
    email: payload[EMAIL_CLAIM] ?? null,
    department: payload.department ?? null,
    isAdmin: payload[ROLE_CLAIM] === "Admin",
  };
}
