// Tek doğruluk kaynağı: kurumdaki gerçek departmanların listesi burada.
// Register.jsx (kullanıcının kendi departmanı) ve WikiBoard.jsx (sayfanın ait
// olduğu departman) İKİSİ de bu listeden okuyor - serbest metin olsaydı bir
// yazım farkı (örn. "IT" / "Bilgi İşlem"), görünürlük kuralının (departman
// adının BİREBİR eşleşmesine dayanan WikiVisibilityRules) sessizce kırılmasına
// yol açardı: bir kullanıcı kendi departmanının DepartmentOnly sayfalarını hiç
// göremez hale gelirdi.
export const DEPARTMENTS = [
  { value: "IT", label: "IT" },
  { value: "IK", label: "İnsan Kaynakları (IK)" },
];
