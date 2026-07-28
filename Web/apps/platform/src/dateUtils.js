// Backend'in serileştirdiği UTC zaman damgaları TUTARSIZ geliyor: SQL
// Server'dan gelenler (ör. AuditLogEntry, WikiPage) "Z" SİZ (Kind bilgisi
// kaybolduğu için), Postgres'ten gelenler (ör. WikiPageEmbedding - AI arama
// sonuçları) "Z" İLE geliyor (Npgsql, "timestamp with time zone" sütununun
// Kind=Utc'sini korur). Sabit bir "+ Z" eklemek Postgres kaynaklı bir
// değerde "...ZZ" üretip tarihi BOZARDI (canlı doğrulandı, AI arama
// sonuçlarında) - bu yüzden sadece EKSİKSE ekliyoruz.
export function formatUtcTimestamp(utcString) {
  if (!utcString) return "";
  const iso = utcString.endsWith("Z") ? utcString : utcString + "Z";
  return new Date(iso).toLocaleString();
}
