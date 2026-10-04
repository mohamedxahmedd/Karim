namespace NationalAirspace.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>Entity whose timestamps are maintained centrally on save and which supports optimistic concurrency.</summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Optimistic concurrency token, regenerated on every update.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}

public interface IOrderable
{
    Guid Id { get; }
    int DisplayOrder { get; set; }
}

/// <summary>Content addressable by a per-language URL slug.</summary>
public interface ISluggable
{
    Guid Id { get; }
    string SlugAr { get; set; }
    string SlugEn { get; set; }
}
