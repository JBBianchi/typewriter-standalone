using System.Reflection;
using Typewriter.Generation.Lexing;

namespace Typewriter.Generation;

/// <summary>
/// Manages a shadow Roslyn compilation workspace for template code.
/// This is a compilation stub; the full implementation will be provided
/// when ShadowClass is ported from upstream.
/// </summary>
public class ShadowClass
{
    private readonly List<Snippet> _snippets = [];
    private readonly HashSet<Assembly> _referencedAssemblies = [];

    /// <summary>Gets the collected code snippets.</summary>
    public IEnumerable<Snippet> Snippets => _snippets;

    /// <summary>Gets the referenced assemblies.</summary>
    public IEnumerable<Assembly> ReferencedAssemblies => _referencedAssemblies;

    /// <summary>Clears all snippets and resets referenced assemblies.</summary>
    public void Clear()
    {
        _snippets.Clear();
        _referencedAssemblies.Clear();
    }

    /// <summary>Finalizes the shadow class for compilation.</summary>
    public void Parse()
    {
    }

    /// <summary>
    /// Adds a using directive snippet.
    /// </summary>
    /// <param name="code">The using directive code.</param>
    /// <param name="startIndex">Start index in the template.</param>
    public void AddUsing(string code, int startIndex)
    {
        _snippets.Add(Snippet.Create(SnippetType.Using, code, 0, startIndex, startIndex + code.Length));
    }

    /// <summary>
    /// Adds a code block snippet.
    /// </summary>
    /// <param name="code">The code block text.</param>
    /// <param name="startIndex">Start index in the template.</param>
    public void AddBlock(string code, int startIndex)
    {
        _snippets.Add(Snippet.Create(SnippetType.Code, code, 0, startIndex, startIndex + code.Length));
    }

    /// <summary>
    /// Adds an assembly reference by path or name.
    /// </summary>
    /// <param name="pathOrName">DLL file path or assembly name.</param>
    public void AddReference(string pathOrName)
    {
    }

    /// <summary>
    /// Adds a lambda expression snippet.
    /// </summary>
    /// <param name="code">The lambda code.</param>
    /// <param name="type">The full type name of the lambda parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="startIndex">Start index in the template.</param>
    public void AddLambda(string code, string type, string name, int startIndex)
    {
        _snippets.Add(Snippet.Create(SnippetType.Lambda, code, 0, startIndex, startIndex + code.Length));
    }
}
