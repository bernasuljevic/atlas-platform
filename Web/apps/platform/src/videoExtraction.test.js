import { describe, expect, it } from "vitest";
import { extractVideosFromContent, isRecognizedVideoUrl } from "./videoExtraction";

describe("extractVideosFromContent", () => {
  it("boş/null/undefined içerik için boş liste döner", () => {
    expect(extractVideosFromContent(null)).toEqual([]);
    expect(extractVideosFromContent(undefined)).toEqual([]);
    expect(extractVideosFromContent("")).toEqual([]);
  });

  it("video bloğu olmayan bir içerik için boş liste döner", () => {
    const content = "# Başlık\n\nSadece düz bir paragraf, hiç video yok.";
    expect(extractVideosFromContent(content)).toEqual([]);
  });

  it("tek bir video bloğunu url+alt yazıyla doğru çıkarır", () => {
    const content = ":::video\nhttps://www.youtube.com/watch?v=dQw4w9WgXcQ\nTanıtım videosu\n:::";
    expect(extractVideosFromContent(content)).toEqual([
      { url: "https://www.youtube.com/watch?v=dQw4w9WgXcQ", caption: "Tanıtım videosu" },
    ]);
  });

  it("alt yazısı olmayan bir video bloğunda caption boş string olur", () => {
    const content = ":::video\nhttps://vimeo.com/76979871\n:::";
    expect(extractVideosFromContent(content)).toEqual([
      { url: "https://vimeo.com/76979871", caption: "" },
    ]);
  });

  it("birden fazla satıra yayılmış bir alt yazıyı TEK bir string'e birleştirir", () => {
    const content = ":::video\nhttps://www.loom.com/share/abc123\nİlk satır\nİkinci satır\n:::";
    expect(extractVideosFromContent(content)).toEqual([
      { url: "https://www.loom.com/share/abc123", caption: "İlk satır İkinci satır" },
    ]);
  });

  it("aynı sayfadaki birden fazla video bloğunun HEPSİNİ, sırayla çıkarır", () => {
    const content = [
      "# Eğitim Videoları",
      "",
      ":::video",
      "https://www.youtube.com/watch?v=aaaaaaaaaaa",
      "Birinci video",
      ":::",
      "",
      "Aradaki normal paragraf.",
      "",
      ":::video",
      "https://vimeo.com/123456789",
      "İkinci video",
      ":::",
    ].join("\n");

    expect(extractVideosFromContent(content)).toEqual([
      { url: "https://www.youtube.com/watch?v=aaaaaaaaaaa", caption: "Birinci video" },
      { url: "https://vimeo.com/123456789", caption: "İkinci video" },
    ]);
  });

  it("boş bir video bloğunu (url hiç yazılmamış) ATLAR - galeri için anlamsız", () => {
    const content = ":::video\n:::";
    expect(extractVideosFromContent(content)).toEqual([]);
  });

  it("diğer blok tiplerini (callout/kod) video sanıp KARIŞTIRMAZ", () => {
    const content = ":::info\nBu bir video değil.\n:::\n\n```\nhttps://www.youtube.com/watch?v=xxxxxxxxxxx\n```";
    expect(extractVideosFromContent(content)).toEqual([]);
  });
});

describe("isRecognizedVideoUrl", () => {
  it("boş/null/undefined için false döner", () => {
    expect(isRecognizedVideoUrl(null)).toBe(false);
    expect(isRecognizedVideoUrl(undefined)).toBe(false);
    expect(isRecognizedVideoUrl("")).toBe(false);
  });

  it("YouTube/Vimeo/Loom/video dosyası linklerinin HEPSİ için true döner", () => {
    expect(isRecognizedVideoUrl("https://www.youtube.com/watch?v=dQw4w9WgXcQ")).toBe(true);
    expect(isRecognizedVideoUrl("https://youtu.be/dQw4w9WgXcQ")).toBe(true);
    expect(isRecognizedVideoUrl("https://vimeo.com/76979871")).toBe(true);
    expect(isRecognizedVideoUrl("https://player.vimeo.com/video/76979871")).toBe(true);
    expect(isRecognizedVideoUrl("https://www.loom.com/share/e883fd8fe3c745deabe1f66655e0916c")).toBe(true);
    expect(isRecognizedVideoUrl("https://cdn.example.com/tanitim.mp4")).toBe(true);
  });

  it("tanınmayan bir link (ör. bir haber sitesi) için false döner", () => {
    expect(isRecognizedVideoUrl("https://example.com/haberler/makale")).toBe(false);
  });

  it("video linkini İÇEREN ama TAMAMEN video linki OLMAYAN bir cümle için de true döner (regex kısmi eşleşiyor - ayrım paste algılayıcısında yapılıyor)", () => {
    // Not: fonksiyonun kendisi kısmi eşleşmeyi reddetmiyor (regex'ler `test()`
    // ile kısmi eşleşme arıyor) - "SADECE bir video linki mi yapıştırıldı"
    // ayrımı BİLEREK burada değil, WikiEditorPage'in paste algılayıcısında
    // (trimmed metnin İÇİNDE başka bir şey olup olmadığını kontrol ederek)
    // yapılıyor - bu fonksiyon sadece "içinde tanınan bir desen var mı" sorar.
    expect(isRecognizedVideoUrl("bak şu videoya: https://youtu.be/dQw4w9WgXcQ diyorum")).toBe(true);
  });
});
