using System.Text.Json.Serialization;

namespace Atlas.Modules.AI.Infrastructure.Embeddings;

// Voyage AI'ın "POST /v1/embeddings" sözleşmesi (dokümantasyon, 2026-08).
// snake_case alan adları JsonPropertyName ile eşleniyor - System.Text.Json'ın
// gövde geneli naming policy'sine güvenmek yerine her alanı açıkça işaretlemek,
// Voyage'ın alan adlarını (bizim C# konvansiyonumuzdan bağımsız) tek bir yerde
// belgelemiş de oluyor.
internal record VoyageEmbeddingRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] IReadOnlyList<string> Input,
    [property: JsonPropertyName("output_dimension")] int OutputDimension,
    [property: JsonPropertyName("truncation")] bool Truncation);

internal record VoyageEmbeddingResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<VoyageEmbeddingDatum> Data);

internal record VoyageEmbeddingDatum(
    [property: JsonPropertyName("embedding")] float[] Embedding,
    [property: JsonPropertyName("index")] int Index);
