import { useState } from "react";
import { Link } from "react-router";
import { Check, Copy } from "lucide-react";

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
// Aksi halde normal bir dış bağlantı. **kalın**, *italik* - iç içe geçmiyor
// (basit tutuldu, editördeki araç çubuğu zaten bunları iç içe üretmiyor).
const INLINE_PATTERN = /!\[([^\]]*)\]\(([^)]+)\)|\[([^\]]+)\]\(([^)]+)\)|\*\*([^*]+)\*\*|\*([^*]+)\*/g;

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

    const [full, imgAlt, imgSrc, linkText, linkTarget, boldText, italicText] = match;
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
const HEADING_SIZES = {
  1: "mt-4 mb-1.5 text-[21px] leading-[1.25] font-bold tracking-tight text-[var(--text-h)] border-b pb-1 border-[var(--border)]",
  2: "mt-3.5 mb-1.5 text-[15px] leading-[1.3] font-bold tracking-tight text-[var(--text-h)]",
  3: "mt-3 mb-1 text-[12px] leading-[1.3] font-semibold text-[var(--text-h)]",
  4: "mt-2.5 mb-1 text-[12px] font-medium text-[var(--text-h)]",
  5: "mt-2 mb-1 text-[11px] font-medium text-[var(--text-h)]",
  6: "mt-2 mb-0.5 text-[11px] font-semibold uppercase tracking-wider text-[var(--text-h)]",
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

// Görselin gerçek "blok" hâli - kendi satırında tek başına duran bir
// "![alt](url)" için (bkz. STANDALONE_IMAGE_PATTERN). CodeBlock/tablo gibi
// diğer blok elemanlarla AYNI seviyede, doğrudan blocks[]'a itiliyor.
function ImageBlock({ src, alt }) {
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
      <img
        src={src}
        alt={alt || "Görsel"}
        className="mx-auto max-h-[560px] w-full rounded-lg border object-contain shadow-sm"
        style={{ borderColor: "var(--border)" }}
      />
      {alt && (
        <figcaption className="mt-1.5 text-center text-[13px] italic opacity-75" style={{ color: "var(--text)" }}>
          {alt}
        </figcaption>
      )}
    </figure>
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

      headings.push({ id, text, level });
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
