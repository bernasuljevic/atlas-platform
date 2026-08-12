import { describe, expect, it } from "vitest";
import { formatUtcTimestamp } from "./dateUtils";

// Bu test dosyasının varlığı BAŞLI BAŞINA bir ders: CLAUDE.md "Öğrenilen
// dersler #19"da anlatılan "...ZZ" bug'ı (Postgres kaynaklı bir zaman
// damgasına sabit bir "+ Z" ekleyip tarihi bozmak) canlıda push edilmeden
// ÖNCE fark edilip düzeltilmişti - ama o ana kadar bunu koruyan otomatik
// bir test YOKTU, sadece manuel gözlem vardı. Bu testler o regresyonun bir
// daha sessizce geri gelemeyeceğini garanti ediyor.
describe("formatUtcTimestamp", () => {
  it("boş/null/undefined girdi için boş string döner", () => {
    expect(formatUtcTimestamp(null)).toBe("");
    expect(formatUtcTimestamp(undefined)).toBe("");
    expect(formatUtcTimestamp("")).toBe("");
  });

  it("SQL Server kaynaklı (Z'siz) bir zaman damgasını doğru ayrıştırır", () => {
    // Kind bilgisi SQL Server'da kayboluyor - backend "Z" OLMADAN yazıyor
    // (bkz. dateUtils.js'teki not).
    const result = formatUtcTimestamp("2026-08-12T10:30:00");
    expect(result).not.toBe("Invalid Date");
    expect(result.length).toBeGreaterThan(0);
  });

  it("Postgres kaynaklı (Z'li) bir zaman damgasına İKİNCİ bir Z EKLEMEZ", () => {
    // BULUNAN GERÇEK BUG'IN (Ders #19) test karşılığı: Npgsql Kind=Utc'yi
    // koruyor, backend "Z" İLE yazıyor - sabit bir "+ Z" burada "...ZZ"
    // üretip new Date()'i "Invalid Date"e çevirirdi.
    const result = formatUtcTimestamp("2026-08-12T10:30:00Z");
    expect(result).not.toBe("Invalid Date");
    expect(result.length).toBeGreaterThan(0);
  });

  it("Z'li ve Z'siz AYNI ANI temsil eden iki değer AYNI sonucu üretir", () => {
    // İki farklı kaynaktan (SQL Server/Postgres) gelen AYNI UTC an, kullanıcıya
    // FARKLI görünmemeli - dateUtils'in asıl garantisi bu.
    const withZ = formatUtcTimestamp("2026-08-12T10:30:00Z");
    const withoutZ = formatUtcTimestamp("2026-08-12T10:30:00");
    expect(withZ).toBe(withoutZ);
  });
});
