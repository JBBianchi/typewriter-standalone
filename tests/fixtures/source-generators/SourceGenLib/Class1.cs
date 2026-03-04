namespace SourceGenLib;

/// <summary>
/// Handwritten class in <c>SourceGenLib</c>.
/// The source generator additionally produces <c>GeneratedHelper</c>,
/// which is not present in any <c>.cs</c> file but should appear in the
/// Roslyn compilation and be visible to Typewriter templates.
/// </summary>
public sealed class Class1
{
    /// <summary>Gets a greeting message.</summary>
    public string Greeting { get; set; } = "Hello";
}
