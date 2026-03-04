namespace ComplexTypesLib;

/// <summary>
/// Demonstrates nullable reference and value types.
/// </summary>
public class NullableTypes
{
    /// <summary>
    /// Nullable reference type (string?).
    /// </summary>
    public string? NullableName { get; set; }

    /// <summary>
    /// Non-nullable reference type.
    /// </summary>
    public string RequiredName { get; set; } = string.Empty;

    /// <summary>
    /// Nullable value type (int?).
    /// </summary>
    public int? NullableAge { get; set; }

    /// <summary>
    /// Non-nullable value type.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Nullable DateTime.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Nullable boolean.
    /// </summary>
    public bool? IsVerified { get; set; }

    /// <summary>
    /// Nullable Guid.
    /// </summary>
    public Guid? ExternalId { get; set; }

    /// <summary>
    /// Method returning a nullable string.
    /// </summary>
    public string? FindDisplayName()
    {
        return NullableName;
    }

    /// <summary>
    /// Method accepting nullable parameters.
    /// </summary>
    public bool MatchesFilter(string? nameFilter, int? minAge)
    {
        return true;
    }
}
