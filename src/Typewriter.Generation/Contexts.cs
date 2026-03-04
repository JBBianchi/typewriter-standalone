namespace Typewriter.Generation;

/// <summary>
/// Provides context type information for template lambda parsing.
/// This is a compilation stub; the full implementation will be provided
/// when the template engine is fully ported.
/// </summary>
public class Contexts
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Contexts"/> class.
    /// </summary>
    /// <param name="shadowClass">The shadow class providing extension method metadata.</param>
    public Contexts(ShadowClass shadowClass)
    {
    }

    /// <summary>
    /// Finds a context by name.
    /// </summary>
    /// <param name="name">The context name to look up.</param>
    /// <returns>The matching <see cref="Context"/>, or <c>null</c> if not found.</returns>
    public Context? Find(string name) => null;
}

/// <summary>
/// Represents a type context for template identifiers.
/// </summary>
public class Context
{
    /// <summary>Gets the CLR type associated with this context.</summary>
    public Type Type { get; init; } = typeof(object);
}
