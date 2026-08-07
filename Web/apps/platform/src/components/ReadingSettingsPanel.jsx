import { Maximize2, Minimize2 } from "lucide-react";

// Kullanıcının verdiği spec'e (2026-08-05, "Sağ Tarafta Yardım Paneli";
// 2026-08-07'de "5. Okuma Ayarları" ile genişletildi) göre - Wikipedia/
// Medium'un okuma deneyimindeki gibi sade bir "görünüm ayarları" paneli.
// Değerler burada TEK yerde tanımlı, hem bu panel hem WikiArticlePage
// (gerçek stili uygulayan taraf) aynı sabitleri kullanıyor - iki yerde ayrı
// ayrı "small=16px" yazıp birbirinden sapma riski taşımıyor.
// ALTINCI geçişte (2026-08-07 - "başlıklar ve paragraflar biraz
// küçültülsün") medium 17'den 16'ya indirildi.
export const FONT_SIZE_PX = { small: 14, medium: 16, large: 18 };
// DÖRDÜNCÜ geçişte (2026-08-07) - kullanıcının GERÇEK şikayeti şuydu: grid
// track'i (bkz. WikiArticlePage'deki üç sütun) genişletilmişti ama İÇERİK
// DIV'inin KENDİ maxWidth'i (58rem = 812px) hâlâ çok dar bir değerdi -
// sonuç, "geniş bir hücrenin İÇİNDE dar bir metin sütunu yüzüyor" görünümüydü
// (kullanıcının tarif ettiği "makale ortada küçük bir kutu gibi" hatası TAM
// OLARAK buydu). Çözüm sadece sayıları büyütmek değil - "Orta" (varsayılan)
// artık "none": kullanıcı HİÇBİR ayarla oynamadan, İÇERİK SÜTUNUNUN
// TAMAMINI dolduruyor. "Dar" hâlâ geleneksel, daha rahat okunan bir sütun
// (60rem). "Orta" ve "Geniş" ikisi de CSS değeri olarak AYNI ("none") -
// SEKİZİNCİ geçişte (2026-08-07 - "orta ve geniş arasında hâlâ fark yok...
// geniş iyice genişlesin") aralarındaki GERÇEK fark artık burada değil,
// WikiArticlePage'deki bir effect'te: "Geniş" seçilince İçindekiler VE Sağ
// Panel otomatik daraltılıyor, content GRID TRACK'İNİN KENDİSİ büyüyor -
// zaten üst sınırda olan bir maxWidth'i büyütmenin tek gerçek yolu buydu.
export const LINE_WIDTH_VALUE = { narrow: "60rem", medium: "none", wide: "none" };
// Kullanıcı geri bildirimi (2026-08-07, YEDİNCİ geçiş - "satır aralığı geniş
// ve orta arasında fark yok") - 1.7 ile 1.9 arası (0.2) sık kullanılan yazı
// boyutlarında (14-18px) satır başına sadece 3-4px fark ediyordu, gözle
// ayırt edilmesi zordu. Aralık 1.5/1.7/2.2'ye açıldı - "Geniş" artık
// "Normal"den satır başına ~8px daha fazla boşluk bırakıyor, fark net görünüyor.
export const LINE_HEIGHT_VALUE = { tight: 1.5, normal: 1.7, relaxed: 2.2 };

const FONT_SIZE_OPTIONS = [
  { value: "small", label: "Küçük Yazı", sample: 13 },
  { value: "medium", label: "Orta Yazı", sample: 15 },
  { value: "large", label: "Büyük Yazı", sample: 17 },
];
const LINE_WIDTH_OPTIONS = [
  { value: "narrow", label: "Dar" },
  { value: "medium", label: "Orta" },
  { value: "wide", label: "Geniş" },
];
const LINE_HEIGHT_OPTIONS = [
  { value: "tight", label: "Sık" },
  { value: "normal", label: "Normal" },
  { value: "relaxed", label: "Geniş" },
];
const THEME_OPTIONS = [
  { value: "light", label: "Açık" },
  { value: "dark", label: "Koyu" },
];

// Küçük, paylaşılan bir "segmented control" - 2-3 seçenekten birini seçtiren
// yatay düğme grubu. Satır Genişliği/Satır Aralığı/Tema AYNI görsel dili
// paylaşıyor, tek bir yerde tanımlanıp üç kez kullanılıyor.
function SegmentedControl({ options, value, onChange }) {
  return (
    <div className="flex overflow-hidden rounded-md border" style={{ borderColor: "var(--border)" }}>
      {options.map((opt, idx) => (
        <button
          key={opt.value}
          type="button"
          onClick={() => onChange(opt.value)}
          className="flex-1 py-1 text-xs font-medium"
          style={{
            borderLeft: idx > 0 ? "1px solid var(--border)" : "none",
            background: value === opt.value ? "var(--brand-accent)" : "transparent",
            color: value === opt.value ? "#fff" : "var(--text)",
          }}
        >
          {opt.label}
        </button>
      ))}
    </div>
  );
}

// Sayfanın sağında SABİT (sticky) duran, makale okurken kaybolmayan panel
// (bkz. kullanıcının spec'indeki "4. Sağ Tarafta Yardım Paneli" / "5. Okuma
// Ayarları" bölümleri). Tüm durum (state) DIŞARIDA (WikiArticlePage'de)
// tutuluyor - bu bileşen sadece kontrolleri gösteren "controlled component",
// localStorage kalıcılığı/gerçek stil uygulaması çağıran tarafın sorumluluğunda.
//
// 2026-08-07 - Tema artık ayrı bir "Koyu Tema'ya geç" düğmesi DEĞİL, diğer
// üç ayarla (Yazı Boyutu/Satır Genişliği/Satır Aralığı) AYNI iki-seçenekli
// segmented control deseninde (spec'in "Tema ○ Açık ○ Koyu" maddesi) -
// görsel tutarlılık için `theme`/`onThemeChange` prop çiftine geçildi (eski
// `isDark`/`onToggleTheme` bool-toggle deseni yerine), "Tam Ekran Okuma" ise
// bir TERCİH değil bir MOD olduğu için ayrı, kendi satırındaki eylem
// düğmesi olarak kaldı.
function ReadingSettingsPanel({
  fontSize,
  onFontSizeChange,
  lineWidth,
  onLineWidthChange,
  lineHeightKey,
  onLineHeightChange,
  isFullscreen,
  onToggleFullscreen,
  theme,
  onThemeChange,
}) {
  return (
    // Kullanıcı geri bildirimi (2026-08-07, DÖRDÜNCÜ geçiş - "6. Sağ Panel:
    // daha kompakt... padding azalsın") - padding bir kademe daha azaltıldı
    // (p-2.5 -> p-2, segmented control satırları py-1.5 -> py-1).
    <div className="rounded-lg border p-2" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
      <h3 className="mb-1.5 text-[15px] font-bold tracking-wide" style={{ color: "var(--text-h)" }}>
        Görünüm
      </h3>

      <p className="mb-1 text-[12px] font-medium" style={{ color: "var(--text)", opacity: 0.7 }}>
        Yazı Boyutu
      </p>
      <div className="mb-2 flex overflow-hidden rounded-md border" style={{ borderColor: "var(--border)" }}>
        {FONT_SIZE_OPTIONS.map((opt, idx) => (
          <button
            key={opt.value}
            type="button"
            onClick={() => onFontSizeChange(opt.value)}
            title={opt.label}
            aria-label={opt.label}
            className="flex-1 py-1 font-bold"
            style={{
              borderLeft: idx > 0 ? "1px solid var(--border)" : "none",
              background: fontSize === opt.value ? "var(--brand-accent)" : "transparent",
              color: fontSize === opt.value ? "#fff" : "var(--text)",
              fontSize: opt.sample,
            }}
          >
            Aa
          </button>
        ))}
      </div>

      <p className="mb-1 text-[12px] font-medium" style={{ color: "var(--text)", opacity: 0.7 }}>
        Satır Genişliği
      </p>
      <div className="mb-2">
        <SegmentedControl options={LINE_WIDTH_OPTIONS} value={lineWidth} onChange={onLineWidthChange} />
      </div>

      <p className="mb-1 text-[12px] font-medium" style={{ color: "var(--text)", opacity: 0.7 }}>
        Satır Aralığı
      </p>
      <div className="mb-2">
        <SegmentedControl options={LINE_HEIGHT_OPTIONS} value={lineHeightKey} onChange={onLineHeightChange} />
      </div>

      <p className="mb-1 text-[12px] font-medium" style={{ color: "var(--text)", opacity: 0.7 }}>
        Tema
      </p>
      <div>
        <SegmentedControl options={THEME_OPTIONS} value={theme} onChange={onThemeChange} />
      </div>

      <div className="mt-2 border-t pt-2" style={{ borderColor: "var(--border)" }}>
        <button
          type="button"
          onClick={onToggleFullscreen}
          className="flex w-full items-center gap-2 rounded px-1.5 py-1 text-[12px] font-medium hover:bg-[var(--brand-accent)]/10"
          style={{ color: "var(--text)" }}
        >
          {isFullscreen ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
          {isFullscreen ? "Tam ekrandan çık" : "Tam Ekran Okuma"}
        </button>
      </div>
    </div>
  );
}

export default ReadingSettingsPanel;
