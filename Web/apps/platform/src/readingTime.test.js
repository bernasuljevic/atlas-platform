import { describe, expect, it } from "vitest";
import { estimateReadingMinutes } from "./readingTime";

describe("estimateReadingMinutes", () => {
  it("boş/null/undefined içerik için 1 dakika döner (0 anlamsız görünürdü)", () => {
    expect(estimateReadingMinutes(null)).toBe(1);
    expect(estimateReadingMinutes(undefined)).toBe(1);
    expect(estimateReadingMinutes("")).toBe(1);
  });

  it("200 kelimelik bir metin için 1 dakika döner", () => {
    const content = Array(200).fill("kelime").join(" ");
    expect(estimateReadingMinutes(content)).toBe(1);
  });

  it("201 kelimelik bir metin için 2 dakikaya YUVARLAR (tam bölünmeyen kalan da bir dakika sayılır)", () => {
    const content = Array(201).fill("kelime").join(" ");
    expect(estimateReadingMinutes(content)).toBe(2);
  });

  it("600 kelimelik bir metin için 3 dakika döner", () => {
    const content = Array(600).fill("kelime").join(" ");
    expect(estimateReadingMinutes(content)).toBe(3);
  });

  it("kod bloklarını kelime sayısına KATMAZ", () => {
    const withoutCode = Array(200).fill("kelime").join(" ");
    const withHugeCodeBlock = withoutCode + "\n```\n" + Array(5000).fill("x").join(" ") + "\n```\n";

    // Koca bir kod bloğu eklenmesine rağmen süre DEĞİŞMEMELİ - kod satırları
    // kelime sayısına hiç girmiyor.
    expect(estimateReadingMinutes(withHugeCodeBlock)).toBe(estimateReadingMinutes(withoutCode));
  });

  it("link URL'sini DEĞİL, sadece link METNİNİ sayar", () => {
    const content = `[buraya tıkla](https://cok-uzun-bir-url.ornek.com/a/b/c/d/e/f/g/h/i/j/k?query=deger&baska=deger2)`;
    // Sadece "buraya" + "tıkla" (2 kelime) sayılmalı, URL'nin kendisi
    // sayılırsa 1 dakikadan fazla çıkardı (URL çok uzun olduğu için).
    expect(estimateReadingMinutes(content)).toBe(1);
  });

  it("::: blok işaretlerini kelime sayısına KATMAZ", () => {
    const content = ":::callout-info\nkısa bir not\n:::";
    // Sadece "kısa"/"bir"/"not" (3 kelime) sayılmalı, ":::callout-info" ayrı
    // bir "kelime" gibi sayılmamalı.
    expect(estimateReadingMinutes(content)).toBe(1);
  });
});
