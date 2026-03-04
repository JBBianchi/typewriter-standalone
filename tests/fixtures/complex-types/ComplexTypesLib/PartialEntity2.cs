#pragma warning disable 67
namespace ComplexTypesLib;

/// <summary>
/// Second part of the partial class — adds audit and tagging.
/// </summary>
public partial class PartialEntity
{
    /// <summary>
    /// Nullable update timestamp from part 2.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Tags list from part 2.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Constant from part 2.
    /// </summary>
    public const int MaxTags = 10;

    /// <summary>
    /// Delegate defined in part 2.
    /// </summary>
    public delegate void EntityUpdated(int id);

    /// <summary>
    /// Event from part 2.
    /// </summary>
    public event EntityUpdated? OnUpdated;

    /// <summary>
    /// Method from part 2.
    /// </summary>
    public bool HasTag(string tag)
    {
        return Tags.Contains(tag);
    }

    /// <summary>
    /// Nested class in part 2.
    /// </summary>
    public class PartialMetadata2
    {
        /// <summary>
        /// Metadata value.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Nested enum in part 2.
    /// </summary>
    public enum EntityPriority
    {
        /// <summary>Low priority.</summary>
        Low,

        /// <summary>Medium priority.</summary>
        Medium,

        /// <summary>High priority.</summary>
        High
    }
}
