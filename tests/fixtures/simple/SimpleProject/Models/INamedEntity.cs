namespace SimpleProject.Models;

/// <summary>
/// Implemented by entities that expose a display name.
/// </summary>
public interface INamedEntity
{
    string FirstName { get; set; }

    string LastName { get; set; }

    string GetDisplayName();
}
