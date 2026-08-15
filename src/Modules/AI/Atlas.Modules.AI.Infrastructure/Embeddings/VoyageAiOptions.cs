namespace Atlas.Modules.AI.Infrastructure.Embeddings;

/// <summary>
/// appsettings.json'daki "VoyageAi" bölümünden bind ediliyor. ApiKey BİLEREK
/// appsettings.json'da DEĞİL - Jwt:Key ile AYNI gerekçeyle (bkz. CLAUDE.md
/// Ders #16) User Secrets'tan (Development) ya da ortam değişkeninden
/// (Production/Docker, VoyageAi__ApiKey) gelecek. Model/BaseUrl gizli
/// olmadığı için appsettings.json'da normal şekilde durabilir.
/// </summary>
public class VoyageAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    // voyage-3.5: 1024 boyutlu vektörü VARSAYILAN olarak üretiyor (Voyage AI
    // dokümantasyonu, 2026-08) - EmbeddingDimensions.Standard'ın neden 1024
    // seçildiğinin gerekçesiyle birebir örtüşüyor. Farklı bir model/boyut
    // seçilirse appsettings.json'dan override edilebilir.
    public string Model { get; set; } = "voyage-3.5";

    public string BaseUrl { get; set; } = "https://api.voyageai.com/v1/";
}
