using System.Reflection;
using Microsoft.CodeAnalysis;
using Typewriter.Generation.Performance;

namespace Typewriter.Generation;

/// <summary>
/// Compiles template code from a <see cref="ShadowClass"/> into a loadable assembly type.
/// Adapted from upstream <c>Typewriter.Generation.Compiler</c> with VS coupling removed:
/// <list type="bullet">
///   <item><c>EnvDTE.ProjectItem</c> replaced with <c>string templateFilePath</c>.</item>
///   <item><c>Assembly.LoadFrom</c> replaced with <see cref="TemplateAssemblyLoadContext"/>.</item>
///   <item><c>ErrorList</c> / <c>Log</c> (VS OutputWindow) removed; diagnostics are surfaced
///         via the exception message on compilation failure.</item>
///   <item>Per-invocation caching via <see cref="InvocationCache"/> skips Roslyn compilation on
///         repeated calls with the same template path.</item>
/// </list>
/// Roslyn <see cref="Microsoft.CodeAnalysis.CSharp.CSharpCompilation"/> logic is preserved
/// in <see cref="ShadowClass.Compile(string)"/>.
/// </summary>
public sealed class Compiler : IDisposable
{
    private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "Typewriter");

    private readonly InvocationCache _cache;
    private readonly string _subDirectory;

    /// <summary>
    /// Initializes a new <see cref="Compiler"/> with the specified invocation cache.
    /// </summary>
    /// <param name="cache">
    /// The per-invocation cache used to store compiled template assemblies, keyed by
    /// normalized absolute template path.
    /// </param>
    public Compiler(InvocationCache cache)
    {
        _cache = cache;
        _subDirectory = Path.Combine(TempDirectory, Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Compiles the shadow class and returns the generated <c>__Typewriter.Template</c> type,
    /// loaded in an isolated <see cref="TemplateAssemblyLoadContext"/>.
    /// On repeated calls with the same <paramref name="templateFilePath"/>, the cached assembly
    /// is returned without invoking Roslyn compilation.
    /// </summary>
    /// <param name="templateFilePath">Absolute path to the <c>.tst</c> template file.</param>
    /// <param name="shadowClass">The shadow class containing parsed template code.</param>
    /// <returns>The compiled template <see cref="Type"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when compilation produces errors.</exception>
    public Type Compile(string templateFilePath, ShadowClass shadowClass)
    {
        var assembly = _cache.GetOrAddTemplate(templateFilePath, _ =>
        {
            Directory.CreateDirectory(_subDirectory);

            // Copy referenced assemblies to the subdirectory so the isolated load context
            // can resolve them at runtime.
            foreach (var refAsm in shadowClass.ReferencedAssemblies)
            {
                var asmSourcePath = ShadowClass.ResolveAssemblyPath(refAsm);
                if (string.IsNullOrEmpty(asmSourcePath))
                {
                    continue;
                }

                var asmDestPath = Path.Combine(_subDirectory, Path.GetFileName(asmSourcePath));

                var sourceAssemblyName = AssemblyName.GetAssemblyName(asmSourcePath);

                // Skip the copy if the destination already has the same version.
                if (File.Exists(asmDestPath))
                {
                    var destAssemblyName = AssemblyName.GetAssemblyName(asmDestPath);
                    if (sourceAssemblyName.Version is not null
                        && destAssemblyName.Version is not null
                        && sourceAssemblyName.Version.CompareTo(destAssemblyName.Version) == 0)
                    {
                        continue;
                    }
                }

                try
                {
                    File.Copy(asmSourcePath, asmDestPath, overwrite: true);
                }
                catch (IOException)
                {
                    // File may be in use by another template compilation; non-fatal.
                }
            }

            var fileName = Path.GetRandomFileName();
            var path = Path.Combine(_subDirectory, fileName);

            var result = shadowClass.Compile(path);

            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error
                         || d.Severity == DiagnosticSeverity.Warning);

            var hasErrors = false;
            var errorMessages = new List<string>();

            foreach (var diagnostic in errors)
            {
                var message = diagnostic.GetMessage();
                message = message.Replace("__Typewriter.", string.Empty);
                message = message.Replace("publicstatic", string.Empty);

                if (diagnostic.Severity == DiagnosticSeverity.Error || diagnostic.IsWarningAsError)
                {
                    hasErrors = true;
                    errorMessages.Add($"error {diagnostic.Id}: {message}");
                }
            }

            if (result.Success && !hasErrors)
            {
                var assemblyDir = Path.GetDirectoryName(path)!;
                var loadContext = new TemplateAssemblyLoadContext(assemblyDir);
                return loadContext.LoadFromAssemblyPath(path);
            }

            throw new InvalidOperationException(
                $"Failed to compile template '{templateFilePath}'. Errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, errorMessages));
        });

        var type = assembly.GetType("__Typewriter.Template");
        return type ?? throw new InvalidOperationException(
            $"Compiled assembly does not contain the expected type '__Typewriter.Template'. Template: {templateFilePath}");
    }

    /// <summary>
    /// Performs best-effort deletion of the per-invocation temporary subdirectory.
    /// Locked or already-deleted files do not cause exceptions to propagate.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_subDirectory))
            {
                Directory.Delete(_subDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Files may be locked by another process; best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Insufficient permissions on some platforms; best-effort cleanup.
        }
    }
}
