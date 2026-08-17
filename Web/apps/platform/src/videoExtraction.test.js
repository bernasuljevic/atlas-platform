import { describe, expect, it } from "vitest";
import { extractVideosFromContent } from "./videoExtraction";

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
