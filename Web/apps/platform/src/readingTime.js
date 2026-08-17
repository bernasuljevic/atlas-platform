// Medium'un "X dakika okuma" göstergesinin karşılığı - Rıdvan'ın orijinal
// geri bildirimindeki ("Medium gibi birkaç siteyi kontrol edip belki ek
// özellikler ekleyebiliriz") tek somut, henüz yapılmamış kalan parça.
// İçindekiler (TOC)/scroll-spy/okuma ayarları (font/satır aralığı/genişlik)
// zaten WikiArticlePage.jsx'te vardı - bu SADECE eksik olan tek şeydi.
//
// 200 kelime/dakika - yaygın kullanılan bir vasat okuma hızı varsayımı
// (Medium'un kendi algoritması ~265 kullanıyor, ama bizim içeriğimiz daha
// teknik/kurumsal - okuyucunun kod bloğu gibi kısımlarda yavaşlayacağını
// varsayarak biraz daha muhafazakar bir sayı seçildi). Kesin bir bilim değil,
// kullanıcıya kabaca bir fikir vermesi yeterli.
const WORDS_PER_MINUTE = 200;

// Markdown sözdiziminin kendisini (::: blok işaretleri, kod çiti, resim/video
// markdown'ı, link URL'leri) kelime sayısına KATMIYORUZ - aksi halde
// "[buraya tıkla](https://çok-uzun-bir-url)" gibi bir link, gerçekte okunan
// TEK kelimeyi (link metnini) değil, URL'nin kendisini de kelime gibi
// sayardı, süreyi yapay olarak şişirirdi.
function stripMarkdownSyntax(content) {
  return content
    .replace(/```[\s\S]*?```/g, " ") // kod blokları - kod satırları farklı okunur, süreyi şişirmesin diye hariç
    .replace(/:::[\w-]*/g, " ") // ":::callout-info", ":::video" gibi blok açma/kapama işaretleri
    .replace(/!\[[^\]]*\]\([^)]*\)/g, " ") // resim/video markdown'ı - URL kelime değil
    .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1") // link URL'sini at, sadece link METNİNİ say
    .replace(/[#>*_`-]/g, " "); // kalan başlık/alıntı/vurgu/inline-kod işaretleri
}

/**
 * Verilen markdown içeriğinden kaba bir "X dakika okuma" tahmini üretir.
 * Her zaman en az 1 dakika döner (boş/çok kısa bir sayfa için "0 dakika"
 * anlamsız görünürdü).
 */
export function estimateReadingMinutes(content) {
  if (!content) return 1;

  const plainText = stripMarkdownSyntax(content);
  const wordCount = plainText.trim().split(/\s+/).filter(Boolean).length;

  return Math.max(1, Math.ceil(wordCount / WORDS_PER_MINUTE));
}
