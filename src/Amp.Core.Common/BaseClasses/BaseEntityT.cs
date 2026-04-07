namespace Amp.Core.Common.BaseClasses;

/// <summary>
/// Base entity with a typed integer primary key.
/// </summary>
public abstract class BaseEntity<TKey> : BaseEntity
{
    public TKey Id { get; set; } = default!;
}
