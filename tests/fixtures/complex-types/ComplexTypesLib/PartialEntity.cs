#pragma warning disable 67
namespace ComplexTypesLib;

/// <summary>
/// First part of a partial class — defines core identity properties.
/// </summary>
public partial class PartialEntity
{
    /// <summary>
    /// Primary identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Constant from part 1.
    /// </summary>
    public const string DefaultCategory = "General";

    /// <summary>
    /// Delegate defined in part 1.
    /// </summary>
    public delegate void EntityChanged<T>(T entity);

    /// <summary>
    /// Event from part 1.
    /// </summary>
    public event EntityChanged<PartialEntity>? OnCreated;

    /// <summary>
    /// Method from part 1.
    /// </summary>
    public string GetDisplayName()
    {
        return Name;
    }

    /// <summary>
    /// Nested class in part 1.
    /// </summary>
    public class PartialMetadata1
    {
        /// <summary>
        /// Metadata key.
        /// </summary>
        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// Nested enum in part 1.
    /// </summary>
    public enum EntityStatus
    {
        /// <summary>Active status.</summary>
        Active,

        /// <summary>Inactive status.</summary>
        Inactive
    }
}
