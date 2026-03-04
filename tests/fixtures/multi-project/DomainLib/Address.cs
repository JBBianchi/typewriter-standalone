namespace DomainLib;

/// <summary>
/// Value object representing a postal address.
/// </summary>
public class Address
{
    /// <summary>
    /// Gets or sets the street line.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city name.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal/zip code.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;
}
