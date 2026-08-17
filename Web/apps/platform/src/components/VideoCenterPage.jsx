import { useEffect, useState } from "react";
import { Link } from "react-router";
import { PlayCircle, Video } from "lucide-react";
import { getWikiPages } from "../api";
import { extractVideosFromContent } from "../videoExtraction";
import { VideoBlock } from "../markdown";
import { Badge } from "@atlas/ui/badge";

// Video Merkezi (Eksik-özellik listesi C grubu, Gün 2, 2026-08-17) - BİLEREK
// yeni bir backend endpoint'i/depolama katmanı YOK (kapsam kullanıcıyla
// AskUserQuestion ile netleştirildi). Wiki'nin zaten departman-görünürlüğüne
// göre filtrelenmiş döndürdüğü sayfa listesi (getWikiPages - GetWikiPagesQuery,
// AYNI "tüm veriyi çek, bellekte filtrele/tara" kabul edilmiş ölçek varsayımı
// burada da geçerli) istemci tarafında taranıp ":::video" blokları
// (videoExtraction.js) çıkarılıyor - Wiki'nin görünürlük kuralı, VERİYİ
// backend'den ALIRKEN zaten uygulanmış oluyor, burada AYRICA bir filtre
// gerekmiyor.
//
// pageSize=100 sayfa BÜYÜKLÜĞÜNÜN kabul edilmiş üst sınırı (bkz.
// GetWikiPagesQueryHandler'daki Math.Clamp) - totalPages 1'den büyükse ek
// sayfalar sırayla çekiliyor.
async function fetchAllVisibleWikiPages(token) {
  const pageSize = 100;
  const first = await getWikiPages(token, 1, pageSize);
  const items = [...first.items];

  for (let pageNumber = 2; pageNumber <= first.totalPages; pageNumber++) {
    const next = await getWikiPages(token, pageNumber, pageSize);
    items.push(...next.items);
  }

  return items;
}

// Her sayfadaki HER video bloğu, o sayfanın başlığı/departmanıyla birlikte
// düz (flat) bir listeye açılıyor - bir sayfada birden fazla video varsa
// (ör. bir eğitim rehberi) her biri galeride AYRI bir kart olarak görünür.
function extractAllVideos(pages) {
  const videos = [];
  for (const page of pages) {
    const pageVideos = extractVideosFromContent(page.content);
    for (const video of pageVideos) {
      videos.push({
        ...video,
        pageId: page.id,
        pageTitle: page.title,
        departmentName: page.departmentName,
      });
    }
  }
  return videos;
}

// Tıklanana kadar iframe HİÇ render EDİLMİYOR ("lazy play") - bir galeride
// onlarca YouTube/Vimeo/Loom iframe'ini baştan yüklemek hem yavaş hem
// gereksiz ağ trafiği olurdu. Kaynak servise özel bir ikon/etiket BİLEREK
// YOK - bu bilgi zaten VideoBlock'un (markdown.jsx) kendi embed algılama
// mantığında yaşıyor, burada AYRI bir kopyasını tutmak (ör. "YouTube" mi
// "Vimeo" mu diye ikinci bir regex seti) o mantığın iki yerde senkron
// kalmasını gerektirirdi - sade bir "Video" ikonu yeterli.
function VideoCard({ video }) {
  const [isPlaying, setIsPlaying] = useState(false);

  if (isPlaying) {
    return (
      <div className="flex flex-col gap-2 rounded-lg border p-3" style={{ borderColor: "var(--border)" }}>
        <VideoBlock url={video.url} caption={video.caption} />
        <Link
          to={`/wiki/${video.pageId}`}
          className="truncate text-xs hover:underline"
          style={{ color: "var(--brand-accent)" }}
        >
          Kaynak: {video.pageTitle}
        </Link>
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={() => setIsPlaying(true)}
      className="flex flex-col gap-2 rounded-lg border p-3 text-left hover:bg-[var(--brand-accent)]/10"
      style={{ borderColor: "var(--border)" }}
    >
      <div
        className="flex aspect-video w-full items-center justify-center rounded-lg"
        style={{ background: "var(--code-bg)" }}
      >
        <PlayCircle size={40} style={{ color: "var(--brand-accent)", opacity: 0.8 }} />
      </div>
      <p className="truncate text-sm font-medium" style={{ color: "var(--text-h)" }}>
        {video.caption || video.pageTitle}
      </p>
      <div className="flex items-center justify-between gap-2">
        <span className="truncate text-xs" style={{ color: "var(--text)", opacity: 0.65 }}>
          {video.pageTitle}
        </span>
        <Badge variant="outline" className="shrink-0 text-[10px] font-normal">
          {video.departmentName}
        </Badge>
      </div>
    </button>
  );
}

function VideoCenterPage({ token }) {
  const [videos, setVideos] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    fetchAllVisibleWikiPages(token)
      .then((pages) => {
        if (!cancelled) setVideos(extractAllVideos(pages));
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  return (
    <div className="mx-auto max-w-5xl text-left">
      <div className="mb-1 flex items-center gap-2">
        <Video size={20} style={{ color: "var(--brand-accent)" }} />
        <h1 className="text-2xl font-medium" style={{ color: "var(--text-h)" }}>
          Video Merkezi
        </h1>
      </div>
      <p className="mb-6 text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
        Wiki sayfalarına eklenmiş tüm videolar (görebildiğin sayfalardan)
      </p>

      {error && <p style={{ color: "red" }} className="mb-3 text-sm">{error}</p>}

      {videos === null ? (
        <p style={{ color: "var(--text)" }}>Yükleniyor...</p>
      ) : videos.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-lg border py-16 text-center" style={{ borderColor: "var(--border)" }}>
          <Video size={32} style={{ color: "var(--text)", opacity: 0.3 }} />
          <p className="font-medium" style={{ color: "var(--text-h)" }}>Henüz hiçbir sayfada video yok</p>
          <p className="max-w-xs text-sm" style={{ color: "var(--text)", opacity: 0.7 }}>
            Bir wiki sayfası düzenlerken araç çubuğundaki "🎬 Video" düğmesiyle YouTube, Vimeo, Loom ya da
            doğrudan bir video dosyası bağlantısı ekleyebilirsin.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {videos.map((video, idx) => (
            <VideoCard key={`${video.pageId}-${idx}`} video={video} />
          ))}
        </div>
      )}
    </div>
  );
}

export default VideoCenterPage;
