using DomainLib;

namespace ApiLib;

/// <summary>
/// User entity that extends EntityBase from DomainLib.
/// </summary>
public class UserEntity : EntityBase
{
    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's home address (cross-project type reference).
    /// </summary>
    public Address HomeAddress { get; set; } = new();
}
