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

// Embed algılama desenleri (D grubu, Gün 1, 2026-08-17) - ÖNCEDEN
// markdown.jsx'in içinde, SADECE VideoBlock'un kullandığı yerel sabitlerdi.
// Buraya (React'siz, saf bir modüle) TAŞINDI ki HEM VideoBlock (render
// zamanında embed URL'sini üretmek için) HEM WikiEditorPage'in yapıştırma
// (paste) algılayıcısı (kullanıcı düz bir video linki yapıştırınca otomatik
// ":::video:::" bloğuna çevirmek için) AYNI kaynaktan beslensin - iki ayrı
// yerde iki ayrı regex seti YAŞAMASIN (biri güncellenip diğeri unutulursa
// sessizce birbirinden sapardı).
export const YOUTUBE_PATTERN = /(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})/;
// Vimeo video ID'si sayısal - hem "vimeo.com/123456789" hem
// "player.vimeo.com/video/123456789" (zaten embed formatındaki bir URL'nin
// yapıştırılması) aynı desenle yakalanıyor.
export const VIMEO_PATTERN = /vimeo\.com\/(?:video\/)?(\d+)/;
// Loom paylaşım ID'si alfasayısal (32 karakter hex) - hem "loom.com/share/..."
// (kullanıcının kopyaladığı normal paylaşım linki) hem "loom.com/embed/..."
// aynı desenle yakalanıyor.
export const LOOM_PATTERN = /loom\.com\/(?:share|embed)\/([a-zA-Z0-9]+)/;
export const VIDEO_FILE_PATTERN = /\.(mp4|webm|ogg|mov)(\?.*)?$/i;

// Bir metnin İÇİNDE tanınan bir video kaynağına ait desen olup olmadığını
// söylüyor - KISMİ eşleşme (desenlerin kendisi ^...$ ile ÇAPALANMIŞ değil,
// bkz. VideoBlock'un `url.match(...)` kullanımı - orada da ID'yi metnin
// HERHANGİ bir yerinden çıkarabilmek gerekiyor). "Yapıştırılan metnin
// TAMAMI SADECE bir video linki mi" ayrımı BİLEREK burada YAPILMIYOR -
// bu, çağıranın (WikiEditorPage'in yapıştırma algılayıcısı) sorumluluğu;
// orada trimmed metinde BOŞLUK KARAKTERİ olup olmadığı da ayrıca kontrol
// ediliyor (bkz. o dosyadaki yorum).
export function isRecognizedVideoUrl(text) {
  if (!text) return false;
  return (
    YOUTUBE_PATTERN.test(text) ||
    VIMEO_PATTERN.test(text) ||
    LOOM_PATTERN.test(text) ||
    VIDEO_FILE_PATTERN.test(text)
  );
}

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
    // ":::transcript" (D grubu takip, 2026-08-17) - markdown.jsx'teki AYNI
    // "bu bir alt-fence DEĞİL, video bloğunun kendi kapanışına kadar geri
    // kalan her şeyi transkript sayan tek satırlık bölüm sınırı" mantığı -
    // BURADA ATLANMASI ŞART, yoksa transkript metni caption'a karışırdı
    // (galeri kartında "IIk satır ... transkript metni ..." gibi anlamsız,
    // devasa bir alt yazı görünürdü).
    let inTranscript = false;
    while (i < lines.length && lines[i].trim() !== ":::") {
      if (!inTranscript && lines[i].trim() === ":::transcript") {
        inTranscript = true;
      } else if (!inTranscript) {
        blockLines.push(lines[i]);
      }
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
