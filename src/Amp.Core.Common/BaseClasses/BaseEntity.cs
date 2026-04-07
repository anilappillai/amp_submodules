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
