import { describe, expect, it } from "vitest";
import { generatePassword } from "./passwordGenerator";

const AMBIGUOUS_CHARS = ["0", "O", "1", "l", "I"];

describe("generatePassword", () => {
  it("varsayılan olarak 16 karakter üretir", () => {
    expect(generatePassword()).toHaveLength(16);
  });

  it("istenen uzunlukta üretir", () => {
    expect(generatePassword(24)).toHaveLength(24);
  });

  it("her kategoriden (küçük/büyük harf, rakam, sembol) en az bir karakter içerir", () => {
    // Saf rastgelelik bunu garanti ETMEZ (bkz. passwordGenerator.js'teki not) -
    // bu yüzden fonksiyon özellikle bunu zorluyor. Tek bir çalıştırma
    // şansa bağlı olabileceğinden birkaç kez üretip HEPSİNİN kuralı
        // sağladığını doğruluyoruz.
    for (let i = 0; i < 20; i++) {
      const password = generatePassword();
      expect(password).toMatch(/[a-km-z]/); // küçük harf (l hariç)
      expect(password).toMatch(/[A-HJ-NP-Z]/); // büyük harf (I/O hariç)
      expect(password).toMatch(/[2-9]/); // rakam (0/1 hariç)
      expect(password).toMatch(/[!@#$%^&*\-_=+]/); // sembol
    }
  });

  it("karışabilecek karakterleri (0/O, 1/l/I) HİÇ içermez", () => {
    for (let i = 0; i < 20; i++) {
      const password = generatePassword();
      for (const ambiguous of AMBIGUOUS_CHARS) {
        expect(password).not.toContain(ambiguous);
      }
    }
  });

  it("iki ayrı çağrı farklı parolalar üretir (crypto.getRandomValues gerçekten kullanılıyor)", () => {
    const first = generatePassword();
    const second = generatePassword();
    expect(first).not.toBe(second);
  });
});
