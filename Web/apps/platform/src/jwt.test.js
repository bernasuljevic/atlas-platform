import { describe, expect, it } from "vitest";
import { decodeJwtPayload, getUserInfoFromToken } from "./jwt";

// JwtTokenGenerator.cs'teki GERÇEK claim isimleriyle (uzun URI'ler) BİREBİR
// eşleşen bir payload üretiyoruz - backend'in claim şeklini değiştirmesi
// halinde bu test de kırılır, tam istenen şey bu (frontend/backend
// sözleşmesinin sessizce kırılmasını yakalasın diye).
function base64url(obj) {
  // btoa() SADECE Latin-1 (0-255) karakterleri kabul ediyor - Türkçe
  // karakterler (ş/ğ/ü/ö/ç/İ) çok baytlı UTF-8 olduğu için düz
  // btoa(JSON.stringify(...)) "InvalidCharacterError" fırlatır. Önce
  // TextEncoder ile GERÇEK UTF-8 baytlarına çevirip SONRA base64'lüyoruz -
  // gerçek bir JWT'nin (backend'in System.Text.Json + Convert.ToBase64String'i)
  // ürettiği KODLAMAYLA aynı.
  const bytes = new TextEncoder().encode(JSON.stringify(obj));
  let binary = "";
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function fakeToken(payload) {
  // Header ve imza gerçek değil - decodeJwtPayload ikisini de hiç okumuyor
  // (bkz. jwt.js'teki "imzayı DOĞRULAMIYORUZ" notu), sadece 2. parçayı böler.
  return `${base64url({ alg: "HS256" })}.${base64url(payload)}.fake-signature`;
}

describe("decodeJwtPayload", () => {
  it("Türkçe karakter içeren bir payload'ı DOĞRU çözer", () => {
    // atob/decodeURIComponent zinciri özellikle bunun için var - Türkçe
    // karakterler (İ, ş, ğ vb.) çok baytlı UTF-8, düz bir atob() bunları
    // bozardı.
    const token = fakeToken({ name: "Ayşe Öztürk" });
    expect(decodeJwtPayload(token).name).toBe("Ayşe Öztürk");
  });
});

describe("getUserInfoFromToken", () => {
  const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
  const NAME_IDENTIFIER_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
  const NAME_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
  const EMAIL_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

  it("Admin rolündeki bir token'dan isAdmin=true çıkarır", () => {
    const token = fakeToken({
      [NAME_IDENTIFIER_CLAIM]: "user-1",
      [NAME_CLAIM]: "Admin Kullanıcı",
      [EMAIL_CLAIM]: "admin@atlas.local",
      [ROLE_CLAIM]: "Admin",
      department: "IT",
    });

    const info = getUserInfoFromToken(token);

    expect(info).toEqual({
      userId: "user-1",
      fullName: "Admin Kullanıcı",
      email: "admin@atlas.local",
      department: "IT",
      isAdmin: true,
    });
  });

  it("Member rolündeki bir token'dan isAdmin=false çıkarır", () => {
    const token = fakeToken({
      [NAME_IDENTIFIER_CLAIM]: "user-2",
      [ROLE_CLAIM]: "Member",
    });

    expect(getUserInfoFromToken(token).isAdmin).toBe(false);
  });

  it("department claim'i yoksa null döner (departmansız kullanıcı senaryosu)", () => {
    // CreateWikiPageCommandHandler'ın "departmanın olmadığı için sayfa
    // oluşturamazsın" kuralının frontend tarafındaki karşılığı - bu bilginin
    // doğru okunması UI'ın doğru butonu gizleyip göstermesi için önemli.
    const token = fakeToken({ [NAME_IDENTIFIER_CLAIM]: "user-3", [ROLE_CLAIM]: "Member" });

    expect(getUserInfoFromToken(token).department).toBeNull();
  });
});
