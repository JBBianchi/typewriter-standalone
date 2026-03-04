namespace DomainLib;

/// <summary>
/// Base entity interface with an identifier.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    int Id { get; set; }
}
