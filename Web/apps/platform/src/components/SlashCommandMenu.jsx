// Notion'ın "/" komut menüsü fikri, ama BASİTLEŞTİRİLMİŞ: gerçek bir
// contenteditable/blok editörü YOK (bkz. markdown.jsx'teki mimari not),
// bu yüzden menü imlecin PİKSEL konumunda DEĞİL, WikiEditorPage'in Link/Resim
// popover'larıyla AYNI yerde (toolbar'ın altında, sabit pozisyonda) açılıyor.
// Saf sunum bileşeni - hangi öğelerin listeleneceğine ve seçilince ne
// olacağına WikiEditorPage karar veriyor, bu component sadece görünümü çiziyor.
function SlashCommandMenu({ items, onSelect }) {
  return (
    <div
      className="flex flex-col border border-b-0 border-[var(--border)]"
      style={{ background: "var(--bg)" }}
    >
      <p
        className="border-b px-3 py-1.5 text-[11px] font-semibold tracking-wider uppercase"
        style={{ borderColor: "var(--border)", color: "var(--text)", opacity: 0.6 }}
      >
        Blok Ekle
      </p>
      <div className="flex flex-wrap gap-1 p-2">
        {items.map((item) => {
          const Icon = item.icon;
          return (
            <button
              key={item.key}
              type="button"
              // onMouseDown + preventDefault: textarea aktif olarak yazılırken
              // açılan bir menü olduğu için, onClick beklersek arada oluşan
              // blur/focus kaybı imleç konumunu bozabilir - mousedown'da
              // varsayılanı engelleyip textarea'nın odağını hiç kaybetmiyoruz.
              onMouseDown={(e) => {
                e.preventDefault();
                onSelect(item);
              }}
              className="flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium hover:bg-[var(--brand-accent)]/10"
              style={{ borderColor: "var(--border)", color: "var(--text-h)" }}
            >
              <Icon size={13} style={{ color: "var(--brand-accent)" }} />
              {item.label}
            </button>
          );
        })}
      </div>
    </div>
  );
}

export default SlashCommandMenu;
