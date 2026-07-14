namespace Atlas.Shared.Kernel.Entities;

/// <summary>
/// Tüm modüllerin Domain katmanındaki entity'lerinin miras alacağı temel sınıf.
/// Amaç: "Id nasıl tutulur, nasıl karşılaştırılır" gibi ortak mekaniği tek yerde
/// yazıp her modülde tekrar tekrar yazmamak.
///
/// Neden burada (Shared.Kernel) ve modülün kendi Domain'inde değil?
/// Çünkü bu sınıfın hiçbir iş kuralı içermiyor olması lazım - sadece teknik bir iskelet.
/// Wiki'ye özel, Auth'a özel hiçbir şey barındırmıyor, o yüzden paylaşılabilir.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
