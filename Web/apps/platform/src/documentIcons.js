import {
  Archive,
  File,
  FileCode,
  FileSpreadsheet,
  FileText,
  Image,
  Music,
  Presentation,
  Video,
} from "lucide-react";

// Backend'in UploadDocumentCommandValidator'daki allowlist'iyle AYNI
// kategoriler - burada da tekrarlanıyor (Wiki'nin DEPARTMENTS'i gibi TEK bir
// paylaşılan kaynak olamıyor çünkü biri C# biri JS, iki ayrı runtime) ikonlar
// bu yüzden buradaki liste ile senkron kalmalı.
const EXTENSION_ICON_MAP = {
  pdf: FileText, doc: FileText, docx: FileText, odt: FileText, rtf: FileText, txt: FileText, md: FileText,
  ppt: Presentation, pptx: Presentation, odp: Presentation,
  xls: FileSpreadsheet, xlsx: FileSpreadsheet, csv: FileSpreadsheet, ods: FileSpreadsheet,
  json: FileCode, xml: FileCode, yaml: FileCode, yml: FileCode, sql: FileCode, log: FileCode,
  png: Image, jpg: Image, jpeg: Image, webp: Image, svg: Image,
  mp4: Video, webm: Video, mov: Video,
  mp3: Music, wav: Music,
  zip: Archive,
};

const EXTENSION_LABEL_MAP = {
  pdf: "PDF", doc: "Word Belgesi", docx: "Word Belgesi", odt: "OpenDocument Metin",
  rtf: "Zengin Metin", txt: "Düz Metin", md: "Markdown",
  ppt: "PowerPoint Sunumu", pptx: "PowerPoint Sunumu", odp: "OpenDocument Sunum",
  xls: "Excel Tablosu", xlsx: "Excel Tablosu", csv: "CSV Tablosu", ods: "OpenDocument Tablo",
  json: "JSON", xml: "XML", yaml: "YAML", yml: "YAML", sql: "SQL", log: "Log Dosyası",
  png: "Görsel", jpg: "Görsel", jpeg: "Görsel", webp: "Görsel", svg: "Vektör Görsel",
  mp4: "Video", webm: "Video", mov: "Video",
  mp3: "Ses Dosyası", wav: "Ses Dosyası",
  zip: "ZIP Arşiv",
};

export function getDocumentIcon(fileExtension) {
  return EXTENSION_ICON_MAP[fileExtension?.toLowerCase()] ?? File;
}

export function getDocumentTypeLabel(fileExtension) {
  return EXTENSION_LABEL_MAP[fileExtension?.toLowerCase()] ?? "Dosya";
}

export function formatFileSize(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
