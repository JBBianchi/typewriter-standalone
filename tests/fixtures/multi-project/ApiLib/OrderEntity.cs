using DomainLib;

namespace ApiLib;

/// <summary>
/// Order entity that extends EntityBase from DomainLib.
/// </summary>
public class OrderEntity : EntityBase
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the customer who placed the order (same-project type reference).
    /// </summary>
    public UserEntity Customer { get; set; } = new();
}
