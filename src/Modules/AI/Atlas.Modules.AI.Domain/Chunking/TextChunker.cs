namespace Atlas.Modules.AI.Domain.Chunking;

// WikiVisibilityRules (Wiki.Domain) ile aynı desen: durumsuz (stateless), saf bir
// iş kuralı - DB'ye, HTTP'ye, hiçbir dış sisteme ihtiyacı yok, bu yüzden bir
// entity değil, static bir sınıf. Chunking, uzun bir metni sabit boyutlu ve
// birbiriyle ÜST ÜSTE BİNEN (overlap) parçalara bölüyor - overlap olmasaydı,
// bir cümle tam chunk sınırında ikiye bölünürse, o cümlenin tam anlamı HİÇBİR
// chunk'ta bütün olarak yer almazdı.
public static class TextChunker
{
    public const int DefaultChunkSize = 500;
    public const int DefaultOverlap = 50;

    public static IReadOnlyList<string> Chunk(string content, int chunkSize = DefaultChunkSize, int overlap = DefaultOverlap)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("İçerik boş olamaz.", nameof(content));

        if (chunkSize <= 0)
            throw new ArgumentException("chunkSize sıfırdan büyük olmalı.", nameof(chunkSize));

        if (overlap < 0)
            throw new ArgumentException("overlap negatif olamaz.", nameof(overlap));

        // KRİTİK KONTROL: overlap, chunkSize'a eşit ya da büyük olursa, aşağıdaki
        // döngünün "step" (ilerleme) miktarı sıfır ya da negatif olur - pencere
        // hiç ilerlemez ve döngü SONSUZA kadar aynı yerde döner. Bunu burada,
        // döngüye hiç girmeden engelliyoruz.
        if (overlap >= chunkSize)
            throw new ArgumentException(
                "overlap, chunkSize'dan küçük olmalı - aksi halde kayan pencere hiç ilerlemez.",
                nameof(overlap));

        var chunks = new List<string>();
        var step = chunkSize - overlap;
        var position = 0;

        while (position < content.Length)
        {
            var length = Math.Min(chunkSize, content.Length - position);
            chunks.Add(content.Substring(position, length));

            // Bu chunk zaten içeriğin sonuna ulaştıysa dur - aksi halde son
            // chunk'ı bir kez daha (kısmen tekrar ederek) eklemiş oluruz.
            // Bu satır aynı zamanda "içerik chunkSize'dan kısaysa" durumunu da
            // AYRI bir kod yolu yazmadan otomatik olarak halleder: ilk turda
            // length zaten content.Length'e eşit olur ve hemen dururuz.
            if (position + length >= content.Length)
                break;

            position += step;
        }

        return chunks;
    }
}
