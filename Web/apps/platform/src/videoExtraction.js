// Video Merkezi (Eksik-özellik listesi C grubu, Gün 2, 2026-08-17) - saf bir
// fonksiyon, dateUtils.js/readingTime.js ile AYNI desen. markdown.jsx'teki
// renderWikiMarkdown'ın ":::video" blok algılama mantığıyla BİREBİR aynı
// kural (ilk dolu satır URL, kalanı alt yazı) - TAM markdown render'ını
// (başlık/liste/tablo vb.) tekrar üretmeye gerek yok, sadece video bloklarını
// bulmak yeterli. Backend'e YENİ bir endpoint EKLENMEDİ - Video Merkezi
// sayfası, Wiki'nin zaten departman-görünürlüğüne göre filtrelenmiş
// döndürdüğü sayfa içeriğini (bkz. VideoCenterPage.jsx) istemci tarafında
// bu fonksiyonla tarıyor.
const VIDEO_BLOCK_OPEN_PATTERN = /^:::video$/;

export function extractVideosFromContent(content) {
  if (!content) return [];

  const lines = content.split("\n");
  const videos = [];
  let i = 0;

  while (i < lines.length) {
    if (!VIDEO_BLOCK_OPEN_PATTERN.test(lines[i].trim())) {
      i++;
      continue;
    }

    i++; // ":::video" açılış satırını atla
    const blockLines = [];
    while (i < lines.length && lines[i].trim() !== ":::") {
      blockLines.push(lines[i]);
      i++;
    }
    i++; // kapanış ":::" satırını atla (varsa)

    const nonEmptyLines = blockLines.filter((l) => l.trim() !== "");
    const [urlLine, ...captionLines] = nonEmptyLines;
    const url = (urlLine ?? "").trim();

    // Boş bir blok (url hiç yazılmamış) galeri için anlamsız - atlanıyor.
    if (url) {
      videos.push({ url, caption: captionLines.join(" ").trim() });
    }
  }

  return videos;
}
