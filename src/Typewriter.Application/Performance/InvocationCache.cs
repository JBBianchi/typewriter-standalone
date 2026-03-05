using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Typewriter.Application.Performance;

/// <summary>
/// Per-invocation, memory-only cache for compiled template assemblies and Roslyn
/// <see cref="Compilation"/> objects. Scoped to a single process lifetime.
/// </summary>
/// <remarks>
/// All keys are normalized to absolute paths via <see cref="Path.GetFullPath(string)"/>
/// to ensure consistent cache hits regardless of relative-path variations.
/// This class uses instance-level state only (no static mutable state).
/// </remarks>
public sealed class InvocationCache
{
    private readonly ConcurrentDictionary<string, Assembly> _templateAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Compilation> _compilations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the cached template assemblies, keyed by absolute template file path.
    /// </summary>
    public ConcurrentDictionary<string, Assembly> TemplateAssemblies => _templateAssemblies;

    /// <summary>
    /// Gets the cached Roslyn compilations, keyed by absolute project file path.
    /// </summary>
    public ConcurrentDictionary<string, Compilation> Compilations => _compilations;

    /// <summary>
    /// Returns the cached <see cref="Assembly"/> for the given template path, or invokes
    /// <paramref name="factory"/> to produce and cache one.
    /// </summary>
    /// <param name="templatePath">
    /// The template file path. Normalized to an absolute path via <see cref="Path.GetFullPath(string)"/>.
    /// </param>
    /// <param name="factory">
    /// A factory delegate invoked with the normalized path when no cached entry exists.
    /// </param>
    /// <returns>The cached or newly created <see cref="Assembly"/>.</returns>
    public Assembly GetOrAddTemplate(string templatePath, Func<string, Assembly> factory)
    {
        var key = Path.GetFullPath(templatePath);
        return _templateAssemblies.GetOrAdd(key, factory);
    }

    /// <summary>
    /// Returns the cached <see cref="Compilation"/> for the given project path, or invokes
    /// <paramref name="factory"/> to produce and cache one.
    /// </summary>
    /// <param name="projectPath">
    /// The project file path. Normalized to an absolute path via <see cref="Path.GetFullPath(string)"/>.
    /// </param>
    /// <param name="factory">
    /// A factory delegate invoked with the normalized path when no cached entry exists.
    /// </param>
    /// <returns>The cached or newly created <see cref="Compilation"/>.</returns>
    public Compilation GetOrAddCompilation(string projectPath, Func<string, Compilation> factory)
    {
        var key = Path.GetFullPath(projectPath);
        return _compilations.GetOrAdd(key, factory);
    }
}
