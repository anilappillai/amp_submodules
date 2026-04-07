namespace Amp.Core.Common.BaseClasses;

/// <summary>
/// Convenience alias: <see cref="BaseEntity{TKey}"/> with a <see cref="Guid"/> key.
/// </summary>
public abstract class BaseGuidEntity : BaseEntity<Guid>
{
    protected BaseGuidEntity() => Id = Guid.NewGuid();
}
