using Pgvector;
using Atlas.Shared.Kernel.Entities;

namespace Atlas.Modules.AI.Domain.Entities;

// WikiPageEmbedding'in P5'teki karşılığı - AI, Documents modülünün Document
// entity'sini hiç tanımıyor, sadece DocumentId (Guid) ile hangi belgeye ait
// olduğunu tutuyor (WikiPageEmbedding'in Wiki'yi tanımamasıyla AYNI izolasyon
// kuralı). Ayrı bir tablo/entity - WikiPageEmbedding'le AYNI SQL tablosunu
// PAYLAŞMIYOR: WikiPageId ile DocumentId iki farklı kaynağın kimliği, ikisini
// tek bir "polymorphic" tabloda birleştirmek (ör. nullable WikiPageId +
// nullable DocumentId) hem entity'yi kirletir hem de "ikisi de null/ikisi de
// dolu" gibi geçersiz durumlara açık hale getirirdi - iki ayrı, temiz tablo
// tercih edildi, birleştirme SADECE arama sorgusunda (Handler seviyesinde) oluyor.
public class DocumentEmbedding : Entity<Guid>
{
    public const int EmbeddingDimension = EmbeddingDimensions.Standard;

    public Guid DocumentId { get; private set; }

    // Bir belge birden fazla chunk'a bölünüyor - WikiPageEmbedding'deki AYNI
    // gerekçeyle DocumentId üzerinde unique constraint YOK, ChunkIndex sırayı tutuyor.
    public int ChunkIndex { get; private set; }

    public string ChunkText { get; private set; } = string.Empty;

    // Title/DepartmentName/Visibility - Documents'ın Document entity'sinden
    // DENORMALIZE edilmiş, WikiPageEmbedding'deki AYNI üç gerekçe: (1) arama
    // sonucunda "hangi belge" bilgisini Documents'a geri sorgu atmadan
    // gösterebilmek, (2) DepartmentName + Visibility - IWikiVisibilityChecker
    // ile görünürlük filtresini uygulayabilmek için ZORUNLU (bkz. CLAUDE.md
    // "Öğrenilen dersler #10"daki güvenlik açığıyla aynı sınıf hata).
    public string Title { get; private set; } = string.Empty;

    public string DepartmentName { get; private set; } = string.Empty;

    public string Visibility { get; private set; } = string.Empty;

    public Vector Embedding { get; private set; } = default!;

    public DateTime CreatedAtUtc { get; private set; }

    private DocumentEmbedding() { }

    private DocumentEmbedding(
        Guid id, Guid documentId, int chunkIndex, string chunkText, string title,
        string departmentName, string visibility, Vector embedding, DateTime createdAtUtc)
        : base(id)
    {
        DocumentId = documentId;
        ChunkIndex = chunkIndex;
        ChunkText = chunkText;
        Title = title;
        DepartmentName = departmentName;
        Visibility = visibility;
        Embedding = embedding;
        CreatedAtUtc = createdAtUtc;
    }

    // WikiPageEmbedding.Create ile BİREBİR aynı fail-fast validasyon deseni -
    // float[] alıyor, Pgvector'ı çağıran hiç bilmek zorunda kalmıyor.
    public static DocumentEmbedding Create(
        Guid documentId, int chunkIndex, string chunkText, string title,
        string departmentName, string visibility, float[] embedding)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId boş olamaz.", nameof(documentId));

        if (chunkIndex < 0)
            throw new ArgumentException("ChunkIndex negatif olamaz.", nameof(chunkIndex));

        if (string.IsNullOrWhiteSpace(chunkText))
            throw new ArgumentException("ChunkText boş olamaz.", nameof(chunkText));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title boş olamaz.", nameof(title));

        if (string.IsNullOrWhiteSpace(departmentName))
            throw new ArgumentException("DepartmentName boş olamaz.", nameof(departmentName));

        if (string.IsNullOrWhiteSpace(visibility))
            throw new ArgumentException("Visibility boş olamaz.", nameof(visibility));

        if (embedding is null || embedding.Length == 0)
            throw new ArgumentException("Embedding boş olamaz.", nameof(embedding));

        if (embedding.Length != EmbeddingDimension)
            throw new ArgumentException(
                $"Embedding {EmbeddingDimension} boyutunda olmalı, {embedding.Length} geldi.",
                nameof(embedding));

        return new DocumentEmbedding(
            Guid.NewGuid(), documentId, chunkIndex, chunkText, title, departmentName, visibility,
            new Vector(embedding), DateTime.UtcNow);
    }
}
