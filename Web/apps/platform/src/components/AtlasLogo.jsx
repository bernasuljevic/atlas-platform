// Eski logo.png (yeşil "blob" + "ATLAS WIKI" yazısı, krem zemin) teal/turuncu
// redesign'la (2026-08-17) renk uyumu KAYBOLUYORDU - statik bir PNG olduğu
// için CSS değişkenleriyle yeniden renklendirilemiyordu. Header'da ZATEN
// `h-7 w-7 rounded-full` ile küçük bir daireye kırpılıp gösterildiği için
// (yazının kendisi bu boyutta okunaklı değildi, sadece blob'un bir parçası
// görünüyordu) - TAM "ATLAS WIKI" yazısını piksel piksel yeniden çizmek
// yerine, aynı boyutta net kalan basit bir SVG marka işareti (gradient
// blob + "A" harfi) tercih edildi. `var(--brand-accent)`/`var(--accent-warm)`
// KULLANMIYOR (SVG'ler her zaman CSS custom property'lerini okuyamaz, ama
// bu ikisi zaten `<img>` yerine `<svg>` olduğu için doğrudan okuyabiliyor) -
// gradient'i index.css'teki `--gradient-hero` ile AYNI iki renk (teal->turuncu)
// oluşturuyor, açık/koyu temada otomatik doğru renklere geçiyor.
function AtlasLogo({ size = 28, className }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 32 32"
      className={className}
      role="img"
      aria-label="Atlas Wiki"
    >
      <defs>
        <linearGradient id="atlas-logo-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="var(--brand-accent)" />
          <stop offset="100%" stopColor="var(--accent-warm)" />
        </linearGradient>
      </defs>
      {/* Hafif düzensiz (organik) bir "blob" - tam bir daire DEĞİL, eski
          logonun karakteristik özelliğiyle (mükemmel geometrik olmayan bir
          şekil) tutarlı kalınıyor. */}
      <path
        d="M16 1.5c6.5 0 12 3.6 13.8 9.6 1.7 5.7-1 12.4-6.4 16.4-5.2 3.8-12.6 3.9-17.9.1C0.4 23.8-1.7 16.9 1.3 10.9 4.1 5.3 9.8 1.5 16 1.5Z"
        fill="url(#atlas-logo-gradient)"
      />
      <text
        x="16"
        y="21.5"
        textAnchor="middle"
        fontSize="15"
        fontWeight="700"
        fontFamily="var(--sans)"
        fill="#ffffff"
      >
        A
      </text>
    </svg>
  );
}

export default AtlasLogo;
