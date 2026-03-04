using System.Reflection;
using System.Runtime.Loader;

namespace Typewriter.Generation;

/// <summary>
/// Collectible, isolated <see cref="AssemblyLoadContext"/> for loading compiled template assemblies.
/// Probes the template's output directory first, then <see cref="AppContext.BaseDirectory"/>,
/// and falls back to the default resolution (returns <c>null</c>) for anything not found locally.
/// </summary>
public sealed class TemplateAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _assemblyDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateAssemblyLoadContext"/> class.
    /// </summary>
    /// <param name="assemblyDir">
    /// Absolute path to the directory containing the compiled template assembly and its dependencies.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="assemblyDir"/> is null or whitespace.</exception>
    public TemplateAssemblyLoadContext(string assemblyDir)
        : base(name: "TemplateAssemblyLoadContext", isCollectible: true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyDir);
        _assemblyDir = assemblyDir;
    }

    /// <summary>
    /// Resolves an assembly by name using the following probe order:
    /// <list type="number">
    ///   <item><description>The template assembly directory (<c>assemblyDir</c>).</description></item>
    ///   <item><description><see cref="AppContext.BaseDirectory"/> (the CLI host directory).</description></item>
    ///   <item><description>Default resolution (returns <c>null</c>, letting the runtime handle it).</description></item>
    /// </list>
    /// </summary>
    /// <param name="assemblyName">The identity of the assembly to resolve.</param>
    /// <returns>The loaded assembly, or <c>null</c> if not found in any probed location.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // If the assembly is already loaded in the default context, return null so the
        // runtime resolves it from there. This preserves type identity for shared
        // assemblies (e.g. Typewriter.CodeModel) — without this, the template's
        // compiled code would see a different `Class` type than the host, causing
        // extension method lookups via IsAssignableFrom to fail.
        if (Default.Assemblies.Any(a => string.Equals(
                a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var fileName = assemblyName.Name + ".dll";

        // 1. Probe the template assembly directory.
        var localPath = Path.Combine(_assemblyDir, fileName);
        if (File.Exists(localPath))
        {
            return LoadFromAssemblyPath(localPath);
        }

        // 2. Probe AppContext.BaseDirectory (CLI host directory).
        var basePath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(basePath))
        {
            return LoadFromAssemblyPath(basePath);
        }

        // 3. Fall back to the default resolution (returns null for unknown assemblies).
        return null;
    }
}
