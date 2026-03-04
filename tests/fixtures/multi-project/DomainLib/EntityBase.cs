namespace DomainLib;

/// <summary>
/// Abstract base class for all domain entities.
/// </summary>
public abstract class EntityBase : IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
