namespace Amp.Core.Common.BaseClasses;

/// <summary>
/// Base entity with standard audit fields. All database entities should inherit from this.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last modification.</summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>Username or system identifier that created the record.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Username or system identifier that last modified the record.</summary>
    public string? ModifiedBy { get; set; }

    /// <summary>Soft-delete flag. Deleted records are excluded from normal queries.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the record was soft-deleted.</summary>
    public DateTime? DeletedDate { get; set; }

    /// <summary>Row version for optimistic concurrency control.</summary>
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Base entity with a typed integer primary key.
/// </summary>
public abstract class BaseEntity<TKey> : BaseEntity
{
    public TKey Id { get; set; } = default!;
}

/// <summary>
/// Convenience alias: <see cref="BaseEntity{TKey}"/> with an <see cref="int"/> key.
/// </summary>
public abstract class BaseIntEntity : BaseEntity<int> { }

/// <summary>
/// Convenience alias: <see cref="BaseEntity{TKey}"/> with a <see cref="Guid"/> key.
/// </summary>
public abstract class BaseGuidEntity : BaseEntity<Guid>
{
    protected BaseGuidEntity() => Id = Guid.NewGuid();
}
