// Vault'un "Parola Üret" düğmesi için bağımsız, bağımlılıksız bir üreteç.
// Math.random() KULLANILMIYOR - o kriptografik olarak güvenli değil (tahmin
// edilebilir bir PRNG), tarayıcının kendi CSPRNG'i olan crypto.getRandomValues
// kullanılıyor. Karışabilecek karakterler (0/O, 1/l/I) BİLEREK karakter
// setinden çıkarıldı - kullanıcı üretilen parolayı elle bir yere kopyalarken
// (ör. başka bir cihaza) yanlış okuma riskini azaltmak için.
const CHARSETS = {
  lower: "abcdefghijkmnopqrstuvwxyz",
  upper: "ABCDEFGHJKLMNPQRSTUVWXYZ",
  digits: "23456789",
  symbols: "!@#$%^&*-_=+",
};

function randomChar(charset) {
  const bytes = new Uint32Array(1);
  crypto.getRandomValues(bytes);
  return charset[bytes[0] % charset.length];
}

export function generatePassword(length = 16) {
  const all = CHARSETS.lower + CHARSETS.upper + CHARSETS.digits + CHARSETS.symbols;

  // Saf rastgelelik bazen hiç rakam/sembol içermeyen bir sonuç üretebilir -
  // her kategoriden en az bir karakter garantiliyoruz.
  const required = [
    randomChar(CHARSETS.lower),
    randomChar(CHARSETS.upper),
    randomChar(CHARSETS.digits),
    randomChar(CHARSETS.symbols),
  ];
  const rest = Array.from({ length: Math.max(0, length - required.length) }, () => randomChar(all));
  const combined = [...required, ...rest];

  // Fisher-Yates shuffle - garanti edilen 4 karakter hep baştaymış gibi durmasın.
  for (let i = combined.length - 1; i > 0; i--) {
    const bytes = new Uint32Array(1);
    crypto.getRandomValues(bytes);
    const j = bytes[0] % (i + 1);
    [combined[i], combined[j]] = [combined[j], combined[i]];
  }

  return combined.join("");
}
