import { useState } from "react";
import { Link } from "react-router";
import {
  AlertTriangle,
  Captions,
  Check,
  CheckCircle2,
  Copy,
  FileText,
  Info,
  Maximize2,
  PlayCircle,
  X,
  XCircle,
} from "lucide-react";
import { LOOM_PATTERN, VIDEO_FILE_PATTERN, VIMEO_PATTERN, YOUTUBE_PATTERN } from "./videoExtraction";

// Kasıtlı olarak KÜÇÜK, ELLE YAZILMIŞ bir render katmanı - dışarıdan bir
// markdown kütüphanesi (react-markdown/marked) EKLENMEDİ, çünkü sadece
// WikiEditorPage'in araç çubuğunun ÜRETTİĞİ sözdizimini (başlık/kalın/italik/
// link/liste/alıntı/kod bloğu/tablo) anlaması yeterli, genel amaçlı bir
// markdown motoruna gerek yok - hem harici bağımlılık eklemeden (hocanın
// "harici kütüphane kullanmayalım" notu hâlâ netleşmedi) hem daha az kod
// yüzeyiyle aynı işi görüyor. dangerouslySetInnerHTML KULLANILMIYOR - her şey
// gerçek React elemanı olarak üretiliyor, bu yüzden bir kullanıcının içeriğe
// HTML/script yazması hiçbir zaman çalıştırılmaz (XSS'e kapalı).

// [metin](hedef) - hedef "wiki:GUID" ise Atlas içi VAR OLAN bir sayfaya
// (client-side, sayfa yenilenmeden) gidiyor. "wiki-new:Başlık" ise Wikipedia'nın
// "kırmızı link" fikriyle aynı - hedef sayfa HENÜZ YOK, tıklanınca o başlıkla
// önceden doldurulmuş bir "Yeni Sayfa" ekranına gidiyor (bkz. WikiEditorPage'in
// link penceresindeki arama - sonuç bulunamazsa bu tür bir bağlantı öneriliyor).
// "document:GUID" - P2'de ertelenmiş, P5'te (Documents→AI/RAG entegrasyonu)
// bağlanan içerik-referans bloğu: bir wiki sayfasının içinden Document
// Library'deki bir belgeye işaret ediyor (bkz. WikiEditorPage'in link
// penceresindeki BİRLEŞİK - wiki+belge - arama). "wiki:"den KASITLI olarak
// farklı bir görsel işaret (küçük bir dosya ikonu) taşıyor ki okuyucu
// tıklamadan önce bunun bir wiki sayfası DEĞİL, indirilebilir bir belge
// olduğunu anlasın. Aksi halde normal bir dış bağlantı. **kalın**, *italik* - iç içe geçmiyor
// (basit tutuldu, editördeki araç çubuğu zaten bunları iç içe üretmiyor).
// Faz 1 (2026-08-08, "Atlas İçerik Sistemi" spec'i) - `inline kod` eklendi,
// EN SONA (diğer alternatiflerden sonra) konuldu ki `**kalın**`/`*italik*`
// içindeki `*` karakterleriyle çakışmasın (regex alternation soldan sağa
// denenir, backtick hiçbir diğer deseninde geçmiyor zaten çakışma riski yok).
const INLINE_PATTERN = /!\[([^\]]*)\]\(([^)]+)\)|\[([^\]]+)\]\(([^)]+)\)|\*\*([^*]+)\*\*|\*([^*]+)\*|`([^`]+)`/g;

function renderInline(text, keyPrefix) {
  const nodes = [];
  let lastIndex = 0;
  let match;
  let i = 0;

  INLINE_PATTERN.lastIndex = 0;
  while ((match = INLINE_PATTERN.exec(text)) !== null) {
    if (match.index > lastIndex) {
      nodes.push(text.slice(lastIndex, match.index));
    }

    const [full, imgAlt, imgSrc, linkText, linkTarget, boldText, italicText, inlineCode] = match;
    const key = `${keyPrefix}-${i++}`;

    if (imgSrc !== undefined) {
      // DİKKAT: burada BİLEREK sade bir <img> - <figure>/<figcaption> (blok
      // seviyesi elemanlar) DEĞİL. Bu fonksiyon her zaman bir <p> içine
      // gömülüyor (bkz. flushParagraph) - <p> içine <figure> koymak geçersiz
      // HTML üretiyordu (canlı test sırasında React'ın "cannot be a descendant
      // of <p>" hydration uyarısıyla yakalandı). Görselin KENDİ satırında tek
      // başına durduğu (çoğu kullanım) asıl "blok" hâli aşağıdaki
      // renderWikiMarkdown'da AYRI, gerçek bir blok olarak ele alınıyor - bu
      // dal sadece bir cümlenin ORTASINA serpiştirilmiş nadir bir görsel için.
      nodes.push(
        <img
          key={key}
          src={imgSrc}
          alt={imgAlt || "Görsel"}
          className="my-1 inline-block max-h-[480px] max-w-full rounded-lg border align-middle object-contain shadow-sm"
          style={{ borderColor: "var(--border)" }}
        />
      );
    } else if (linkText !== undefined) {
      if (linkTarget.startsWith("wiki:")) {
        const pageId = linkTarget.slice("wiki:".length);
        // Kullanıcı geri bildirimi (2026-08-07: "Link renkleri daha yumuşak
        // yeşil tonlarında olsun") - yeni bir renk EKLENMEDİ (aynı
        // --brand-accent), sadece varsayılan alt çizgi kaldırıldı (sadece
        // hover'da çıkıyor) - bir bağlantının her zaman kalın bir çizgiyle
        // "bağırması" yerine Notion/Confluence'taki gibi sade duruyor,
        // gerçek renk tonu hiç değişmedi.
        nodes.push(
          <Link key={key} to={`/wiki/${pageId}`} className="hover:underline" style={{ color: "var(--brand-accent)" }}>
            {linkText}
          </Link>
        );
      } else if (linkTarget.startsWith("wiki-new:")) {
        const wantedTitle = decodeURIComponent(linkTarget.slice("wiki-new:".length));
        nodes.push(
          <Link
            key={key}
            to={`/wiki/new?title=${encodeURIComponent(wantedTitle)}`}
            className="underline decoration-dashed"
            style={{ color: "red" }}
            title={`"${wantedTitle}" sayfası henüz yok - oluşturmak için tıkla`}
          >
            {linkText}
          </Link>
        );
      } else if (linkTarget.startsWith("document:")) {
        const documentId = linkTarget.slice("document:".length);
        nodes.push(
          <Link
            key={key}
            to={`/documents/${documentId}`}
            className="inline-flex items-center gap-1 align-text-bottom hover:underline"
            style={{ color: "var(--brand-accent)" }}
            title="Belge Kütüphanesi'nde aç"
          >
            <FileText size={14} className="shrink-0" aria-hidden="true" />
            {linkText}
          </Link>
        );
      } else {
        nodes.push(
          <a
            key={key}
            href={linkTarget}
            target="_blank"
            rel="noreferrer"
            className="hover:underline"
            style={{ color: "var(--brand-accent)" }}
          >
            {linkText}
          </a>
        );
      }
    } else if (boldText !== undefined) {
      nodes.push(<strong key={key}>{boldText}</strong>);
    } else if (italicText !== undefined) {
      nodes.push(<em key={key}>{italicText}</em>);
    } else if (inlineCode !== undefined) {
      // Kod bloğundan (CodeBlock) FARKLI - burası bir CÜMLE içindeki tek bir
      // kelime/ifade için (ör. "npm run dev komutunu kullanın"). Aynı
      // --code-bg değişkeni kullanılıyor (yeni renk YOK), font-mono ile
      // gövde metninden ayrışıyor.
      nodes.push(
        <code
          key={key}
          className="rounded px-1 py-0.5 font-mono text-[0.9em]"
          style={{ background: "var(--code-bg)", color: "var(--text-h)" }}
        >
          {inlineCode}
        </code>
      );
    }

    lastIndex = match.index + full.length;
  }

  if (lastIndex < text.length) {
    nodes.push(text.slice(lastIndex));
  }

  return nodes;
}

// Bir satırın TAMAMI tek başına bir görsel söz dizimi mi ("![alt](url)",
// başında/sonunda başka metin YOK) - editördeki "🖼️ Resim" düğmesinin ürettiği
// en yaygın durum. Bu satırlar renderInline'ın İÇİNE (bir <p> altına) DEĞİL,
// diğer blok seviyesi elemanlar (başlık/kod bloğu/tablo) gibi kendi başına,
// gerçek bir <figure> olarak render ediliyor - bkz. renderInline'daki notla
// AYNI hydration hatası gerekçesi.
const STANDALONE_IMAGE_PATTERN = /^!\[([^\]]*)\]\(([^)]+)\)$/;

// Wikipedia'nın "İçindekiler" bölümüne tıklayınca aynı sayfada ilgili başlığa
// atlaması için her başlığa BENZERSİZ bir #çapa (id) gerekiyor. Türkçe
// karakterleri sadeleştirip aynı metinli iki başlık varsa "-2"/"-3" ekliyoruz -
// GitHub/Wikipedia'nın kendi başlık çapası üretimiyle aynı fikir.
const TURKISH_CHAR_MAP = { ğ: "g", ü: "u", ş: "s", ı: "i", ö: "o", ç: "c" };

function slugify(text, usedSlugs) {
  const base =
    text
      .toLowerCase()
      .replace(/[ğüşıöç]/g, (c) => TURKISH_CHAR_MAP[c])
      .replace(/[^a-z0-9\s-]/g, "")
      .trim()
      .replace(/\s+/g, "-") || "baslik";

  let slug = base;
  let counter = 2;
  while (usedSlugs.has(slug)) {
    slug = `${base}-${counter}`;
    counter++;
  }
  usedSlugs.add(slug);
  return slug;
}

// Kullanıcı geri bildirimi (2026-08-07, BEŞİNCİ geçiş - "başlıklar çok
// büyük... yarı yarıya küçült") - H1/H2/H3 BİREBİR yarıya indirildi
// (42/30/24 -> 21/15/12), kenar boşlukları (mt/mb) da AYNI oranda tıraşlandı
// (küçülen yazının etrafında orantısız BÜYÜK bir boşluk kalmasın diye - aksi
// halde "küçük metin, dev boşluk" görünümü oluşurdu). Not: 15px/12px artık
// gövde metninden (varsayılan 17px) DAHA KÜÇÜK - normalde bir başlığın gövde
// metninden küçük olması bir hiyerarşi hatası sayılırdı, ama burada BİLEREK
// kabul edildi çünkü kullanıcının açık, sayısal talebi buydu (font-weight
// hâlâ hiyerarşiyi taşıyor - H2/H3 hâlâ KALIN, gövde metni değil). H4-H6
// spec'te hiç geçmedi (kullanıcının ekran görüntüsünde görünmüyorlardı) -
// AYNI oranda yarıya indirmek yerine (bu onları 7-10px'e, okunamaz hale
// getirirdi) 11-12px'te bir taban değerde tutuldu, H3'ün ALTINDA kalacak
// şekilde ağırlık/büyük harfle ayrıştırılıyorlar.
// UI/UX denetimi (2026-08-12): H2 (15px) gövde metninden (16px) KÜÇÜKTÜ -
// sadece kalın yazı tipi hiyerarşiyi taşıyordu, bu Wikipedia'nın kendi
// tipografisiyle (H2 gövdeden gözle görülür büyük) karşılaştırınca ters bir
// hiyerarşi yaratıyordu. Beşinci geçişteki ("başlıklar çok büyük, küçült")
// kullanıcı talebinin ARKASINDAKİ asıl amaç korunuyor (başlıklar artık
// 42/30/24 gibi devasa değil) - ama H1/H2/H3 şimdi en azından gövde metninin
// (16px) ÜZERİNE çıkarıldı, aksi halde bir okuyucu H2'yi "yeni bir bölüm"
// değil "vurgulu bir cümle" gibi algılayabiliyordu. H4-H6 BİLEREK dokunulmadı -
// orijinal tasarım kararı onları H3'ün ALTINDA, küçük bir "etiket" tonunda
// tutmaktı (spec'te hiç geçmemişlerdi), bu denetimin bulgusu SADECE H1-H3'ü
// kapsıyordu.
const HEADING_SIZES = {
  1: "mt-4 mb-1.5 text-[24px] leading-[1.3] font-bold tracking-tight text-[var(--text-h)] border-b pb-1.5 border-[var(--border)]",
  2: "mt-4 mb-2 text-[19px] leading-[1.3] font-bold tracking-tight text-[var(--text-h)]",
  3: "mt-3.5 mb-1.5 text-[17px] leading-[1.3] font-semibold text-[var(--text-h)]",
  4: "mt-2.5 mb-1 text-[14px] font-medium text-[var(--text-h)]",
  5: "mt-2 mb-1 text-[13px] font-medium text-[var(--text-h)]",
  6: "mt-2 mb-0.5 text-[12px] font-semibold uppercase tracking-wider text-[var(--text-h)]",
};
const HEADING_TAGS = { 1: "h1", 2: "h2", 3: "h3", 4: "h4", 5: "h5", 6: "h6" };

// Referans mockup'taki kod bloğu (satır numaraları + "Kopyala" düğmesi) -
// gerçek sözdizimi renklendirmesi (syntax highlighting) BİLEREK yok, bunun
// için ya bir kütüphane (highlight.js/prism) ya da elle yazılmış bir
// dil-farkında tokenizer gerekirdi - ikisi de bu özelliğin kapsamı dışında,
// sadece okunabilirliği artıran satır numarası + kopyalama yeterli görüldü.
function CodeBlock({ code }) {
  const [copied, setCopied] = useState(false);
  const lines = code.split("\n");

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      // Panoya erişim reddedilirse (ör. tarayıcı izni yok) sessizce yut -
      // kritik olmayan bir kolaylık özelliği, hata göstermeye değmez.
    }
  }

  return (
    // "Daha modern görünsün" (2026-08-07) - hafif bir gölge + biraz daha
    // yuvarlak köşe, satır numarası sütununa ince bir ayraç eklendi (önceden
    // sadece boşlukla ayrılıyordu, kod uzunlaştıkça numaralar metne
    // "yapışık" görünüyordu).
    <div className="mb-4 overflow-hidden rounded-xl border shadow-sm" style={{ borderColor: "var(--border)" }}>
      <div
        className="flex items-center justify-end border-b px-3.5 py-2"
        style={{ borderColor: "var(--border)", background: "var(--code-bg)" }}
      >
        <button
          type="button"
          onClick={handleCopy}
          className="flex items-center gap-1 rounded px-2 py-0.5 text-xs hover:bg-[var(--brand-accent)]/10"
          style={{ color: "var(--text)" }}
        >
          {copied ? <Check size={12} /> : <Copy size={12} />}
          {copied ? "Kopyalandı" : "Kopyala"}
        </button>
      </div>
      <pre className="overflow-x-auto p-3.5 text-[15px]" style={{ background: "var(--code-bg)" }}>
        <code className="font-mono">
          {lines.map((line, idx) => (
            <div key={idx} className="flex gap-3">
              <span
                className="shrink-0 border-r text-right select-none"
                style={{ minWidth: 22, paddingRight: 10, color: "var(--text)", opacity: 0.4, borderColor: "var(--border)" }}
              >
                {idx + 1}
              </span>
              <span className="whitespace-pre">{line}</span>
            </div>
          ))}
        </code>
      </pre>
    </div>
  );
}

// Eksik-özellik listesi (2026-08-17): fullscreen/lightbox. Tam Ekran Okuma
// Modu'nun (WikiArticlePage.jsx) AYNI `fixed inset-0 z-50` deseni - tutarlı
// bir "bu uygulamada tam ekran overlay böyle görünür" dili. ImageBlock VE
// AlignedImageBlock'un İKİSİ de kendi yerel `isOpen` state'ini tutuyor (paylaşılan
// bir Context/global state İCAT EDİLMEDİ - her görsel zaten bağımsız, aynı
// anda en fazla BİR tanesi açık olabilir, yerel state bunun için yeterli).
function ImageLightbox({ src, alt, onClose }) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-6"
      style={{ background: "rgba(0,0,0,0.9)" }}
      onClick={onClose}
    >
      <button
        type="button"
        onClick={onClose}
        aria-label="Kapat"
        className="absolute top-4 right-4 flex h-9 w-9 items-center justify-center rounded-full"
        style={{ background: "rgba(255,255,255,0.1)", color: "#fff" }}
      >
        <X size={18} />
      </button>
      <img
        src={src}
        alt={alt || "Görsel"}
        className="max-h-[90vh] max-w-[90vw] rounded object-contain"
        onClick={(e) => e.stopPropagation()}
      />
    </div>
  );
}

// Görselin gerçek "blok" hâli - kendi satırında tek başına duran bir
// "![alt](url)" için (bkz. STANDALONE_IMAGE_PATTERN). CodeBlock/tablo gibi
// diğer blok elemanlarla AYNI seviyede, doğrudan blocks[]'a itiliyor.
//
// Boyut BİLEREK sabit kalıyor (2026-08-07 kullanıcı geri bildirimi: "boyutu
// darda olan kadar kalsın" - aşağıdaki max-w-[800px] notuna bkz.) - bu basit
// sözdizimine ("![alt](url)") bir boyut seçeneği EKLENMEDİ. Boyut kontrolü
// isteyen kullanıcı zaten AlignedImageBlock'un (":::image-center-{boyut}")
// "gelişmiş" sözdizimini kullanabiliyor - iki ayrı ihtiyaç seviyesi, basit
// olan basit kalıyor.
function ImageBlock({ src, alt }) {
  const [isLightboxOpen, setIsLightboxOpen] = useState(false);

  return (
    // Kullanıcı geri bildirimi (2026-08-07, DOKUZUNCU geçiş - "görsel
    // hiçbirinde değişmesin, boyutu darda olan kadar kalsın") - ÖNCEKİ
    // sürümde genişlik YÜZDE'ydi (w-[96%]) - içerik sütunu "Satır
    // Genişliği" tercihine göre değiştikçe (Dar/Orta/Geniş) görsel de
    // ORANTILI olarak küçülüp büyüyordu, kullanıcı bunu istemiyor: görsel
    // HER ZAMAN aynı, SABİT boyutta kalmalı. Yüzde yerine SABİT bir piksel
    // üst sınırı (max-w-[800px], "Dar"ın 60rem=840px sütununda %96'nın
    // verdiği ~806px'e denk) - artık hangi Satır Genişliği seçili olursa
    // olsun (sütun ne kadar büyürse büyüsün) görsel BÜYÜMÜYOR, sadece dar
    // bir sütunda (mobil, "Dar" vb.) kendi genişliğine SIĞMAK için küçülüyor
    // (w-full + max-w birlikte - taşma yok, ama büyümüyor da).
    <figure className="my-4 mx-auto w-full max-w-[800px]">
      <button
        type="button"
        onClick={() => setIsLightboxOpen(true)}
        className="group relative block w-full cursor-zoom-in"
        aria-label="Görseli büyüt"
      >
        <img
          src={src}
          alt={alt || "Görsel"}
          className="mx-auto max-h-[560px] w-full rounded-lg border object-contain shadow-sm"
          style={{ borderColor: "var(--border)" }}
        />
        {/* Büyütme ikonu SADECE hover'da görünüyor - "sade" hedefine göre,
            görselin kendisini her zaman bir ikonla kirletmemek için. */}
        <span className="absolute top-2 right-2 flex h-7 w-7 items-center justify-center rounded-full opacity-0 transition group-hover:opacity-100" style={{ background: "rgba(0,0,0,0.55)", color: "#fff" }}>
          <Maximize2 size={13} />
        </span>
      </button>
      {alt && (
        <figcaption className="mt-1.5 text-center text-[13px] italic opacity-75" style={{ color: "var(--text)" }}>
          {alt}
        </figcaption>
      )}
      {isLightboxOpen && <ImageLightbox src={src} alt={alt} onClose={() => setIsLightboxOpen(false)} />}
    </figure>
  );
}

// Video - kullanıcı YouTube/Vimeo/Loom URL'si ya da doğrudan bir video
// dosyası URL'si (mp4/webm/ogg/mov) yapıştırıyor. Tanınan üç servis kendi
// iframe embed'ine, dosya native <video> etiketine düşüyor - DÖRDÜNCÜ,
// TANINMAYAN bir URL ise (ör. kurumsal bir video sunucusu) kırık bir gömme
// DENEMİYORUZ, sade bir "Videoyu Aç" linkine düşüyoruz - her URL şeklini
// "embed edilebilir" varsaymak, kullanıcıya boş bir kare göstermekten daha
// kötü bir deneyim olurdu.
// C grubu, Gün 1 (2026-08-17) - Vimeo/Loom eklendi (YouTube-only mimarisi
// GENİŞLETİLDİ, DEĞİŞTİRİLMEDİ - sıralama kontrolü YouTube→Vimeo→Loom→dosya→
// link, eskisiyle BİREBİR aynı "önce özel servisler, sonra genel dosya, en
// sonda düz link" mantığı).
// D grubu, Gün 1 (2026-08-17) - desenler videoExtraction.js'e TAŞINDI (bu
// dosyadan İÇE AKTARILIYOR) - WikiEditorPage'in yapıştırma algılayıcısı AYNI
// desenlere ihtiyaç duyuyor, iki ayrı kopya YAŞAMASIN diye.

// export EDİLDİ (C grubu Gün 2, Video Merkezi galerisi) - embed algılama
// mantığının (YouTube/Vimeo/Loom/dosya/düz-link) TEK bir yerde yaşamaya
// devam etmesi için VideoCenterPage.jsx bunu DOĞRUDAN tekrar kullanıyor,
// aynı mantığı ikinci bir yerde KOPYALAMIYOR.
//
// transcript (D grubu takip, 2026-08-17 - "video transkript indeksleme")
// - YouTube/Vimeo/Loom'un üçüncü taraf bir videonun transkriptini otomatik
// çekebileceğimiz resmi/güvenilir bir API'si YOK (bkz. o günün araştırması:
// YouTube'un resmi API'si SADECE kendi videonuz için çalışıyor, gayrı-resmi
// endpoint'ler ToS'u ihlal ediyor) - bu yüzden OTOMATİK ÇEKME yerine
// KULLANICININ platformun KENDİ arayüzünden ("Transkripti göster") kopyalayıp
// yapıştırdığı bir metin. Bu metnin AYRICA bir "indeksleme" mekanizmasına
// ihtiyacı YOK - transkript, sayfanın markdown İÇERİĞİNİN bir parçası olarak
// saklanıyor, mevcut TextChunker/embedding pipeline'ı sayfanın TÜM içeriğini
// zaten chunk'layıp embed ediyor (bkz. GenerateWikiPageEmbeddingsCommandHandler),
// transkript de bu chunk'ların içine doğal olarak giriyor - backend'de HİÇBİR
// değişiklik gerekmedi.
export function VideoBlock({ url, caption, transcript }) {
  const youtubeMatch = url.match(YOUTUBE_PATTERN);
  const vimeoMatch = !youtubeMatch && url.match(VIMEO_PATTERN);
  const loomMatch = !youtubeMatch && !vimeoMatch && url.match(LOOM_PATTERN);
  const isVideoFile = !youtubeMatch && !vimeoMatch && !loomMatch && VIDEO_FILE_PATTERN.test(url);

  return (
    <figure className="my-4 mx-auto w-full max-w-[800px]">
      {youtubeMatch ? (
        // youtube-nocookie.com - reklamsız, sade bir gömme (Google'ın kendi
        // önerdiği "gizlilik geliştirilmiş mod" domaini).
        <div className="relative aspect-video overflow-hidden rounded-lg border shadow-sm" style={{ borderColor: "var(--border)" }}>
          <iframe
            src={`https://www.youtube-nocookie.com/embed/${youtubeMatch[1]}`}
            title={caption || "Video"}
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
            className="absolute inset-0 h-full w-full"
          />
        </div>
      ) : vimeoMatch ? (
        <div className="relative aspect-video overflow-hidden rounded-lg border shadow-sm" style={{ borderColor: "var(--border)" }}>
          <iframe
            src={`https://player.vimeo.com/video/${vimeoMatch[1]}`}
            title={caption || "Video"}
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
            className="absolute inset-0 h-full w-full"
          />
        </div>
      ) : loomMatch ? (
        <div className="relative aspect-video overflow-hidden rounded-lg border shadow-sm" style={{ borderColor: "var(--border)" }}>
          <iframe
            src={`https://www.loom.com/embed/${loomMatch[1]}`}
            title={caption || "Video"}
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
            className="absolute inset-0 h-full w-full"
          />
        </div>
      ) : isVideoFile ? (
        <video controls className="w-full rounded-lg border shadow-sm" style={{ borderColor: "var(--border)" }}>
          <source src={url} />
          Tarayıcınız video oynatmayı desteklemiyor.
        </video>
      ) : url ? (
        <a
          href={url}
          target="_blank"
          rel="noreferrer"
          className="flex items-center gap-2 rounded-lg border p-3.5 text-sm hover:bg-[var(--brand-accent)]/10"
          style={{ borderColor: "var(--border)", color: "var(--brand-accent)" }}
        >
          <PlayCircle size={18} /> Videoyu Aç
        </a>
      ) : null}
      {caption && (
        <figcaption className="mt-1.5 text-center text-[13px] italic opacity-75" style={{ color: "var(--text)" }}>
          {caption}
        </figcaption>
      )}
      {transcript && (
        // Native <details>/<summary> - harici bir "accordion" bileşeni EKLENMEDİ,
        // tarayıcının kendi katlanabilir öğesi yeterli. VARSAYILAN KAPALI -
        // uzun bir transkript (30dk'lık bir videonun transkripti kolayca
        // binlerce kelime olabiliyor) sayfayı otomatik ŞİŞİRMESİN diye.
        <details className="mt-2 rounded-lg border text-sm" style={{ borderColor: "var(--border)" }}>
          <summary
            className="flex cursor-pointer items-center gap-1.5 px-3 py-2 font-medium select-none"
            style={{ color: "var(--text-h)" }}
          >
            <Captions size={14} /> Transkript
          </summary>
          <div
            className="border-t px-3 py-2.5 whitespace-pre-line"
            style={{ borderColor: "var(--border)", color: "var(--text)", opacity: 0.85 }}
          >
            {transcript}
          </div>
        </details>
      )}
    </figure>
  );
}

// Wikipedia'nın "thumbnail" (metnin yanına konup metnin etrafından dolandığı
// görsel) fikri - ImageBlock'un AKSİNE (her zaman kendi satırında, ortada,
// sabit boyutta) burada float kullanılıyor, "left"/"right" metnin görseli
// SARMASINA izin veriyor. "center" ImageBlock'la GÖRSEL olarak birebir aynı
// sonucu verir (float yok, ortalı) - üç seçeneği tutarlı tek bir sözdiziminde
// (":::image-<yön>") sunmak, kullanıcının editördeki tek bir select'ten
// hepsine erişebilmesi için.
// Eksik-özellik listesi (2026-08-17): "Resize". Serbest piksel sürükleme
// BİLEREK EKLENMEDİ (bir <textarea> tabanlı düz-metin editöründe fare ile
// sürükleyip tam piksel değerini markdown'a yazmak - hem mouse-tracking hem
// "editördeki taslak" ile "yayınlanan görünüm" arasında pixel-perfect bir
// önizleme gerektirirdi, bu editörün mimarisiyle - bkz. dosyanın en
// başındaki not, "genel amaçlı bir markdown motoruna gerek yok" - UYUŞMAZDI).
// Bunun yerine WikiVisibilityRules/HEADING_SIZES'taki AYNI felsefe: sabit,
// isimlendirilmiş ÜÇ boyut. "left"/"right" (float) İÇİN daha dar bir aralık
// (metnin etrafından dolanması gerektiği için çok büyürse okumayı bozar),
// "center" için ImageBlock'un kendi 800px üst sınırına kadar.
const IMAGE_ALIGN_SIZE_CLASSES = {
  left: {
    small: "float-left mr-4 mb-2 max-w-[200px]",
    medium: "float-left mr-4 mb-2 max-w-[320px]",
    large: "float-left mr-4 mb-2 max-w-[440px]",
  },
  right: {
    small: "float-right ml-4 mb-2 max-w-[200px]",
    medium: "float-right ml-4 mb-2 max-w-[320px]",
    large: "float-right ml-4 mb-2 max-w-[440px]",
  },
  center: {
    small: "mx-auto max-w-[480px]",
    medium: "mx-auto max-w-[800px]",
    large: "mx-auto w-full max-w-[1100px]",
  },
};

function AlignedImageBlock({ src, alt, align, size }) {
  const [isLightboxOpen, setIsLightboxOpen] = useState(false);
  const sizeClasses = IMAGE_ALIGN_SIZE_CLASSES[align] ?? IMAGE_ALIGN_SIZE_CLASSES.center;

  return (
    <figure className={`my-2 ${sizeClasses[size] ?? sizeClasses.medium}`}>
      <button
        type="button"
        onClick={() => setIsLightboxOpen(true)}
        className="group relative block w-full cursor-zoom-in"
        aria-label="Görseli büyüt"
      >
        <img
          src={src}
          alt={alt || "Görsel"}
          className="w-full rounded-lg border object-contain shadow-sm"
          style={{ borderColor: "var(--border)" }}
        />
        <span className="absolute top-2 right-2 flex h-7 w-7 items-center justify-center rounded-full opacity-0 transition group-hover:opacity-100" style={{ background: "rgba(0,0,0,0.55)", color: "#fff" }}>
          <Maximize2 size={13} />
        </span>
      </button>
      {alt && (
        <figcaption className="mt-1.5 text-center text-[12px] italic opacity-75" style={{ color: "var(--text)" }}>
          {alt}
        </figcaption>
      )}
      {isLightboxOpen && <ImageLightbox src={src} alt={alt} onClose={() => setIsLightboxOpen(false)} />}
    </figure>
  );
}

// Callout türleri - kullanıcı spec'i (2026-08-08, "Atlas İçerik Sistemi" -
// "3.3 Callout blokları: 💡 Bilgi/⚠️ Uyarı/❌ Hata/✅ Başarılı") BİREBİR
// karşılığı. Renk paleti kasıtlı olarak SINIRLI tutuldu - "aşırı renkli ve
// karmaşık kartlar kullanma" talimatına uyarak YENİ bir renk EKLENMEDİ,
// sadece bu uygulamada ZATEN var olan iki ton kullanıldı: Bilgi/Başarılı
// --brand-accent (yeşil, linkler/butonlarla AYNI), Uyarı/Hata "red" (Sil
// düğmesi ve kırmızı link özelliğiyle AYNI, bu ikisi zaten uygulamanın
// "dikkat çek" rengi olarak kurulu). Arka plan HER ZAMAN nötr --code-bg -
// tek fark sol kenar çizgisi + ikon rengi, blockquote'un border-l-4
// deseniyle AYNI görsel dilde.
const CALLOUT_CONFIG = {
  info: { icon: Info, label: "Bilgi", color: "var(--brand-accent)" },
  success: { icon: CheckCircle2, label: "Başarılı", color: "var(--brand-accent)" },
  warning: { icon: AlertTriangle, label: "Uyarı", color: "red" },
  error: { icon: XCircle, label: "Hata", color: "red" },
};

function CalloutBlock({ type, text, keyPrefix }) {
  const { icon: Icon, label, color } = CALLOUT_CONFIG[type] ?? CALLOUT_CONFIG.info;
  return (
    <div
      className="mb-4 flex gap-2.5 rounded-lg border-l-4 p-3.5"
      style={{ borderLeftColor: color, background: "var(--code-bg)" }}
    >
      <Icon size={17} className="mt-0.5 shrink-0" style={{ color }} />
      <div className="min-w-0">
        <p className="mb-0.5 text-[13px] font-semibold" style={{ color: "var(--text-h)" }}>
          {label}
        </p>
        <p className="whitespace-pre-wrap text-[15px]" style={{ color: "var(--text)" }}>
          {renderInline(text, keyPrefix)}
        </p>
      </div>
    </div>
  );
}

// Satırları blok blok ("# .." - "###### .." başlık, "```" kod bloğu, "|...|"
// tablo, "- " liste, "> " alıntı, boş satırla ayrılan paragraflar) gruplayıp
// her bloğun kendi içindeki metni renderInline ile biçimlendiriyor - iki
// geçişli basit bir ayrıştırıcı (satır bazlı blok + regex bazlı satır-içi),
// tam bir markdown grameri değil, sadece bu editörün ürettiği alt kümeyi
// kapsıyor. `headings` - WikiArticlePage'in "İçindekiler" kutusunu kurması
// için aynı geçişte toplanan {id, text, level} listesi.
export function renderWikiMarkdown(content) {
  const lines = content.split("\n");
  const blocks = [];
  const headings = [];
  const usedSlugs = new Set();
  let paragraphBuffer = [];
  let i = 0;

  function flushParagraph() {
    if (paragraphBuffer.length === 0) return;
    const text = paragraphBuffer.join("\n");
    blocks.push(
      <p key={`p-${blocks.length}`} className="mb-4 whitespace-pre-wrap">
        {renderInline(text, `p-${blocks.length}`)}
      </p>
    );
    paragraphBuffer = [];
  }

  while (i < lines.length) {
    const line = lines[i];
    const headingMatch = line.match(/^(#{1,6}) (.*)$/);

    if (headingMatch) {
      flushParagraph();
      const level = headingMatch[1].length;
      const text = headingMatch[2];
      const id = slugify(text, usedSlugs);
      const HeadingTag = HEADING_TAGS[level];

      // lineIndex - arama sonucundan tıklanınca "eşleşen chunk hangi
      // başlığın altında" hesaplayabilmek için (bkz. WikiArticlePage'in
      // "arama sonucundan derin link" mantığı) - satır bazlı bir konum,
      // en yakın ÖNCEKİ başlığı bulmak için yeterli, karakter hassasiyeti
      // gerekmiyor.
      headings.push({ id, text, level, lineIndex: i });
      blocks.push(
        <HeadingTag key={`h-${blocks.length}`} id={id} className={`scroll-mt-20 ${HEADING_SIZES[level]}`} style={{ color: "var(--text-h)" }}>
          {renderInline(text, `h-${blocks.length}`)}
        </HeadingTag>
      );
      i++;
      continue;
    }

    const standaloneImageMatch = line.trim().match(STANDALONE_IMAGE_PATTERN);
    if (standaloneImageMatch) {
      flushParagraph();
      const [, alt, src] = standaloneImageMatch;
      blocks.push(<ImageBlock key={`img-${blocks.length}`} src={src} alt={alt} />);
      i++;
      continue;
    }

    if (line.startsWith("```")) {
      flushParagraph();
      i++;
      const codeLines = [];
      while (i < lines.length && !lines[i].startsWith("```")) {
        codeLines.push(lines[i]);
        i++;
      }
      i++; // kapanış ``` satırını atla

      // Kod bloğu içinde inline biçimlendirme (kalın/italik/link) BİLEREK
      // uygulanmıyor - kod, ** veya * karakterlerini OLDUĞU GİBİ göstermeli.
      blocks.push(<CodeBlock key={`code-${blocks.length}`} code={codeLines.join("\n")} />);
      continue;
    }

    // Video - callout ile AYNI ":::<tip>" ... ":::" ailesi, aynı yere kondu.
    // İçindeki ilk dolu satır URL, kalanı (varsa) alt yazı.
    //
    // ":::transcript" (D grubu takip, 2026-08-17) - AYRI bir fence DEĞİL,
    // aynı ":::video ... :::" bloğunun İÇİNDE tek satırlık bir bölüm
    // sınırı - kendi kapanışı YOK, sadece video bloğunun kendi kapanış
    // ":::"ına kadar geri kalan HER şeyi (satır sonları KORUNARAK, caption
    // gibi tek satıra birleştirilmeden) transkript sayıyor. Bilerek NESTED
    // bir ":::...:::" fence'i OLARAK tasarlanmadı - iç içe aynı "`:::`"
    // kapanış işaretini kullanmak, dış video bloğunun kendi kapanışıyla
    // hangisinin "gerçek" kapanış olduğu belirsizliğini yaratırdı.
    const videoMatch = line.trim().match(/^:::video$/);
    if (videoMatch) {
      flushParagraph();
      i++;
      const videoLines = [];
      const transcriptLines = [];
      let inTranscript = false;
      while (i < lines.length && lines[i].trim() !== ":::") {
        if (!inTranscript && lines[i].trim() === ":::transcript") {
          inTranscript = true;
        } else if (inTranscript) {
          transcriptLines.push(lines[i]);
        } else {
          videoLines.push(lines[i]);
        }
        i++;
      }
      i++; // kapanış ":::" satırını atla

      const nonEmptyLines = videoLines.filter((l) => l.trim() !== "");
      const [urlLine, ...captionLines] = nonEmptyLines;
      blocks.push(
        <VideoBlock
          key={`video-${blocks.length}`}
          url={(urlLine ?? "").trim()}
          caption={captionLines.join(" ").trim()}
          transcript={transcriptLines.join("\n").trim() || undefined}
        />
      );
      continue;
    }

    // Hizalı resim - ":::image-left|center|right" (isteğe bağlı "-small|
    // medium|large" boyut eki, bkz. IMAGE_ALIGN_SIZE_CLASSES) ... ":::" -
    // içindeki ilk satır "![alt](url)" olmalı (STANDALONE_IMAGE_PATTERN'in
    // AYNISI), kalan satırlar (varsa) alt yazı olarak alt metnin YERİNE
    // geçiyor. Boyut eki OPSİYONEL - eski içerikte (":::image-left", boyut
    // eklenmeden önce yazılmış) hâlâ eşleşiyor, `size` grubu `undefined`
    // kalıp aşağıda "medium"a düşüyor - GERİYE DÖNÜK UYUMLULUK BOZULMADI.
    const imageAlignMatch = line.trim().match(/^:::image-(left|center|right)(?:-(small|medium|large))?$/);
    if (imageAlignMatch) {
      flushParagraph();
      const align = imageAlignMatch[1];
      const size = imageAlignMatch[2] ?? "medium";
      i++;
      const innerLines = [];
      while (i < lines.length && lines[i].trim() !== ":::") {
        innerLines.push(lines[i]);
        i++;
      }
      i++; // kapanış ":::" satırını atla

      const nonEmptyInner = innerLines.filter((l) => l.trim() !== "");
      const [imageLine, ...captionLines] = nonEmptyInner;
      const imgMatch = (imageLine ?? "").trim().match(STANDALONE_IMAGE_PATTERN);

      if (imgMatch) {
        const [, alt, src] = imgMatch;
        const caption = captionLines.join(" ").trim() || alt;
        blocks.push(
          <AlignedImageBlock key={`img-align-${blocks.length}`} src={src} alt={caption} align={align} size={size} />
        );
      }
      continue;
    }

    // Callout - "```" ile AYNI "açılış satırı + kapanış satırı" desenini
    // izliyor (":::info" ... ":::"), o yüzden kod bloğu kontrolünün hemen
    // ardına, tablo kontrolünden ÖNCE kondu.
    const calloutMatch = line.trim().match(/^:::(info|warning|error|success)$/);
    if (calloutMatch) {
      flushParagraph();
      const type = calloutMatch[1];
      i++;
      const calloutLines = [];
      while (i < lines.length && lines[i].trim() !== ":::") {
        calloutLines.push(lines[i]);
        i++;
      }
      i++; // kapanış ":::" satırını atla
      blocks.push(
        <CalloutBlock key={`callout-${blocks.length}`} type={type} text={calloutLines.join("\n")} keyPrefix={`callout-${blocks.length}`} />
      );
      continue;
    }

    // Divider - tek başına bir satırda "---". Tablo kontrolünden ÖNCE
    // kontrol edilmesine gerek YOK çünkü tablo deseni "|" ile başlayıp
    // bitiyor - çakışma riski yok, ama okunurluk için kod bloğu/callout gibi
    // "kendi başına bir blok" desenlerinin yanına kondu.
    if (line.trim() === "---") {
      flushParagraph();
      blocks.push(<hr key={`hr-${blocks.length}`} className="my-5" style={{ borderColor: "var(--border)" }} />);
      i++;
      continue;
    }

    const isTableRow = /^\|.*\|$/.test(line.trim());
    const nextIsSeparator = i + 1 < lines.length && /^\|[\s:|-]+\|$/.test(lines[i + 1].trim());

    if (isTableRow && nextIsSeparator) {
      flushParagraph();
      const parseRow = (row) =>
        row.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((cell) => cell.trim());

      const headerCells = parseRow(line);
      i += 2; // başlık satırı + ayırıcı satır

      const dataRows = [];
      while (i < lines.length && /^\|.*\|$/.test(lines[i].trim())) {
        dataRows.push(parseRow(lines[i]));
        i++;
      }

      // "Tablolar daha okunabilir olsun" (2026-08-07) - dış çerçeveli,
      // yuvarlak köşeli bir kap + zebra deseni (bir satır atlamalı hafif arka
      // plan) eklendi - önceden sadece alt çizgilerle ayrılan çıplak satırlar
      // vardı, geniş bir tabloda gözün doğru satırda kalması zorlaşıyordu.
      blocks.push(
        <div
          key={`table-${blocks.length}`}
          className="mb-4 overflow-hidden overflow-x-auto rounded-lg border"
          style={{ borderColor: "var(--border)" }}
        >
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr style={{ background: "var(--code-bg)" }}>
                {headerCells.map((cell, idx) => (
                  <th
                    key={idx}
                    className="border-b px-3.5 py-2 text-left font-semibold"
                    style={{ borderColor: "var(--border)", color: "var(--text-h)" }}
                  >
                    {renderInline(cell, `table-${blocks.length}-h-${idx}`)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {dataRows.map((row, rowIdx) => (
                <tr key={rowIdx} style={{ background: rowIdx % 2 === 1 ? "var(--code-bg)" : "transparent" }}>
                  {row.map((cell, cellIdx) => (
                    <td key={cellIdx} className="border-b px-3.5 py-2" style={{ borderColor: "var(--border)" }}>
                      {renderInline(cell, `table-${blocks.length}-r${rowIdx}-${cellIdx}`)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
      continue;
    }

    // Checklist - "- [ ] .."/"- [x] .." de "- " ile BAŞLIYOR, o yüzden bu
    // kontrol düz liste kontrolünden ÖNCE gelmek ZORUNDA - aksi halde "- "
    // yakalayıp "[ ]"/"[x]" kısmını düz metin gibi gösterirdi.
    const checklistMatch = line.match(/^- \[([ xX])\] (.*)$/);
    if (checklistMatch) {
      flushParagraph();
      const items = [];
      while (i < lines.length) {
        const m = lines[i].match(/^- \[([ xX])\] (.*)$/);
        if (!m) break;
        items.push({ checked: m[1].toLowerCase() === "x", text: m[2] });
        i++;
      }
      blocks.push(
        <ul key={`checklist-${blocks.length}`} className="mb-4 flex flex-col gap-1.5">
          {items.map((item, idx) => (
            <li key={idx} className="flex items-start gap-2">
              <input
                type="checkbox"
                checked={item.checked}
                disabled
                className="mt-1 shrink-0 accent-[var(--brand-accent)]"
              />
              <span style={{ opacity: item.checked ? 0.6 : 1, textDecoration: item.checked ? "line-through" : "none" }}>
                {renderInline(item.text, `checklist-${blocks.length}-${idx}`)}
              </span>
            </li>
          ))}
        </ul>
      );
      continue;
    }

    if (line.startsWith("- ")) {
      flushParagraph();
      const items = [];
      while (i < lines.length && lines[i].startsWith("- ")) {
        items.push(lines[i].slice(2));
        i++;
      }
      blocks.push(
        <ul key={`ul-${blocks.length}`} className="mb-4 list-disc pl-6">
          {items.map((item, idx) => (
            <li key={idx}>{renderInline(item, `li-${blocks.length}-${idx}`)}</li>
          ))}
        </ul>
      );
      continue;
    }

    if (line.startsWith("> ")) {
      flushParagraph();
      const quoteLines = [];
      while (i < lines.length && lines[i].startsWith("> ")) {
        quoteLines.push(lines[i].slice(2));
        i++;
      }
      blocks.push(
        <blockquote
          key={`bq-${blocks.length}`}
          className="mb-4 border-l-4 pl-4 italic"
          style={{ borderColor: "var(--brand-accent-border)", color: "var(--text)" }}
        >
          {renderInline(quoteLines.join("\n"), `bq-${blocks.length}`)}
        </blockquote>
      );
      continue;
    }

    if (line.trim() === "") {
      flushParagraph();
      i++;
      continue;
    }

    paragraphBuffer.push(line);
    i++;
  }

  flushParagraph();
  return { nodes: blocks, headings };
}
