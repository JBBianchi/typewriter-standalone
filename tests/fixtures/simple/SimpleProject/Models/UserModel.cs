namespace SimpleProject.Models;

/// <summary>
/// Base class for domain entities with a shared identifier.
/// </summary>
public class EntityBase
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents a user in the system.
/// </summary>
public class UserModel : EntityBase, INamedEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int Age { get; set; }

    public bool IsActive { get; set; }

    public UserRole Role { get; set; }

    public List<string> Tags { get; set; } = [];

    public DateTime? LastLoginAt { get; set; }

    public string GetDisplayName()
    {
        return $"{FirstName} {LastName}";
    }

    public bool HasRole(UserRole role)
    {
        return Role == role;
    }
}
