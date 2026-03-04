namespace Typewriter.Generation.Lexing;

/// <summary>
/// Represents a code snippet within a shadow class compilation unit.
/// Ported as-is from upstream <c>Typewriter.TemplateEditor.Lexing.Roslyn.Snippet</c>.
/// </summary>
public class Snippet
{
    /// <summary>
    /// Creates a new <see cref="Snippet"/> instance.
    /// </summary>
    /// <param name="type">The snippet classification.</param>
    /// <param name="code">The source code text.</param>
    /// <param name="offset">The offset within the shadow class.</param>
    /// <param name="startIndex">Start index in the original template.</param>
    /// <param name="endIndex">End index in the original template.</param>
    /// <param name="internalOffset">Internal offset for position mapping.</param>
    /// <returns>A new <see cref="Snippet"/>.</returns>
    public static Snippet Create(SnippetType type, string code, int offset = 0, int startIndex = -1, int endIndex = -1, int internalOffset = 0)
    {
        return new Snippet
        {
            Type = type,
            Code = code,
            Length = code.Length,
            StartIndex = startIndex,
            EndIndex = endIndex,
            Offset = offset,
            InternalOffset = internalOffset
        };
    }

    /// <summary>Gets the snippet classification.</summary>
    public SnippetType Type { get; private set; }

    /// <summary>Gets the source code text.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Gets or sets the code length.</summary>
    public int Length { get; set; }

    /// <summary>Gets the offset within the shadow class.</summary>
    public int Offset { get; private set; }

    private int StartIndex { get; set; }

    private int EndIndex { get; set; }

    private int InternalOffset { get; set; }

    /// <summary>
    /// Converts a shadow class index to the original template index.
    /// </summary>
    /// <param name="index">Shadow class index.</param>
    /// <returns>Original template index.</returns>
    public int FromShadowIndex(int index)
    {
        return index + StartIndex - Offset + InternalOffset;
    }

    /// <summary>
    /// Converts an original template index to a shadow class index.
    /// </summary>
    /// <param name="index">Original template index.</param>
    /// <returns>Shadow class index.</returns>
    public int ToShadowIndex(int index)
    {
        return index - StartIndex + Offset - InternalOffset;
    }

    /// <summary>
    /// Checks whether the given original index falls within this snippet.
    /// </summary>
    /// <param name="index">Original template index.</param>
    /// <returns><c>true</c> if the index is within this snippet's range.</returns>
    public bool Contains(int index)
    {
        return StartIndex <= index - InternalOffset && EndIndex >= index - InternalOffset;
    }
}

/// <summary>
/// Classifies the type of a shadow class snippet.
/// </summary>
public enum SnippetType
{
    /// <summary>A using directive.</summary>
    Using,

    /// <summary>A code block.</summary>
    Code,

    /// <summary>A lambda expression.</summary>
    Lambda,

    /// <summary>Shadow class infrastructure (not user code).</summary>
    Class
}
