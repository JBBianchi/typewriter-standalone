using System.Reflection;
using System.Runtime.InteropServices;
using Typewriter.Generation;
using Xunit;

namespace Typewriter.UnitTests.Generation;

/// <summary>
/// Tests for <see cref="TemplateAssemblyLoadContext"/> assembly resolution behavior.
/// Verifies the two-stage probe order (assemblyDir then <see cref="AppContext.BaseDirectory"/>)
/// and graceful fallback on all platforms, with explicit coverage on Linux and macOS.
/// </summary>
public class AssemblyLoadContextTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    /// Initializes a new test instance, creating a temporary directory for assembly probing.
    /// </summary>
    public AssemblyLoadContextTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tw-alc-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Cleans up the temporary directory created for test isolation.
    /// On Windows, loaded assemblies may keep file locks briefly after
    /// <see cref="AssemblyLoadContext.Unload"/>, so deletion is best-effort.
    /// </summary>
    public void Dispose()
    {
        if (!Directory.Exists(_tempDir))
        {
            return;
        }

        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
            // On Windows the assembly file lock may outlive Unload + GC;
            // the OS temp directory will clean it up eventually.
        }
    }

    /// <summary>
    /// Verifies that the constructor rejects null and whitespace assembly directory paths.
    /// </summary>
    [Fact]
    public void Constructor_NullOrWhitespace_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TemplateAssemblyLoadContext(null!));
        Assert.Throws<ArgumentException>(() => new TemplateAssemblyLoadContext(""));
        Assert.Throws<ArgumentException>(() => new TemplateAssemblyLoadContext("   "));
    }

    /// <summary>
    /// Verifies that an assembly placed in the assemblyDir is resolved by the first probe path.
    /// Skipped on Windows — targets Linux/macOS path handling and file existence checks.
    /// </summary>
    [Fact]
    public void Load_AssemblyInAssemblyDir_ResolvesFromAssemblyDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // This test targets Linux/macOS resolver behavior.
        }

        // Copy Typewriter.Generation.dll into the temp assembly directory.
        var sourceAssembly = typeof(TemplateAssemblyLoadContext).Assembly;
        var sourcePath = sourceAssembly.Location;
        var destPath = Path.Combine(_tempDir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destPath);

        var context = new TemplateAssemblyLoadContext(_tempDir);

        var loaded = context.LoadFromAssemblyName(
            new AssemblyName(sourceAssembly.GetName().Name!));

        Assert.NotNull(loaded);
        Assert.Equal(sourceAssembly.GetName().Name, loaded.GetName().Name);

        UnloadAndRelease(context);
    }

    /// <summary>
    /// Verifies that when the assemblyDir does not contain the requested assembly,
    /// the resolver falls through to <see cref="AppContext.BaseDirectory"/> and loads it from there.
    /// Skipped on Windows — targets Linux/macOS resolver behavior.
    /// </summary>
    [Fact]
    public void Load_AssemblyInBaseDirectory_ResolvesFromBaseDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // This test targets Linux/macOS resolver behavior.
        }

        // Find a Typewriter assembly that exists in AppContext.BaseDirectory.
        var baseDir = AppContext.BaseDirectory;
        var candidate = Directory.GetFiles(baseDir, "Typewriter.Generation.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault();

        Assert.NotNull(candidate); // Precondition: DLL must exist in base directory.

        // _tempDir is empty, so assemblyDir probe will miss; should fall through to BaseDirectory.
        var context = new TemplateAssemblyLoadContext(_tempDir);

        var loaded = context.LoadFromAssemblyName(new AssemblyName(candidate));

        Assert.NotNull(loaded);
        Assert.Equal(candidate, loaded.GetName().Name);

        context.Unload();
    }

    /// <summary>
    /// Verifies that when an assembly is not found in either probe location,
    /// the <c>Load</c> override returns <c>null</c> (graceful fallback) rather than throwing.
    /// Uses reflection because <c>Load</c> is a protected method on a sealed class.
    /// Skipped on Windows — targets Linux/macOS resolver behavior.
    /// </summary>
    [Fact]
    public void Load_UnknownAssembly_ReturnsNull()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // This test targets Linux/macOS resolver behavior.
        }

        var context = new TemplateAssemblyLoadContext(_tempDir);

        var loadMethod = typeof(TemplateAssemblyLoadContext)
            .GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(loadMethod);

        var unknownName = new AssemblyName("NonExistent.Assembly.That.Does.Not.Exist");
        var result = loadMethod!.Invoke(context, [unknownName]);

        Assert.Null(result);

        context.Unload();
    }

    /// <summary>
    /// Verifies the assemblyDir probe path resolves an assembly correctly on any platform.
    /// This test runs unconditionally (Windows, Linux, macOS).
    /// </summary>
    [Fact]
    public void Load_AssemblyInAssemblyDir_ResolvesOnAllPlatforms()
    {
        var sourceAssembly = typeof(TemplateAssemblyLoadContext).Assembly;
        var sourcePath = sourceAssembly.Location;
        var destPath = Path.Combine(_tempDir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destPath);

        var context = new TemplateAssemblyLoadContext(_tempDir);

        var loaded = context.LoadFromAssemblyName(
            new AssemblyName(sourceAssembly.GetName().Name!));

        Assert.NotNull(loaded);
        Assert.Equal(sourceAssembly.GetName().Name, loaded.GetName().Name);

        UnloadAndRelease(context);
    }

    /// <summary>
    /// Verifies the BaseDirectory fallback resolves an assembly on any platform.
    /// This test runs unconditionally (Windows, Linux, macOS).
    /// </summary>
    [Fact]
    public void Load_AssemblyInBaseDirectory_ResolvesOnAllPlatforms()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Directory.GetFiles(baseDir, "Typewriter.Generation.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault();

        Assert.NotNull(candidate);

        // _tempDir is empty, so assemblyDir probe misses; falls through to BaseDirectory.
        var context = new TemplateAssemblyLoadContext(_tempDir);

        var loaded = context.LoadFromAssemblyName(new AssemblyName(candidate));

        Assert.NotNull(loaded);
        Assert.Equal(candidate, loaded.GetName().Name);

        context.Unload();
    }

    /// <summary>
    /// Verifies the graceful null fallback on any platform.
    /// This test runs unconditionally (Windows, Linux, macOS).
    /// </summary>
    [Fact]
    public void Load_UnknownAssembly_ReturnsNull_OnAllPlatforms()
    {
        var context = new TemplateAssemblyLoadContext(_tempDir);

        var loadMethod = typeof(TemplateAssemblyLoadContext)
            .GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(loadMethod);

        var unknownName = new AssemblyName("NonExistent.Assembly.That.Does.Not.Exist");
        var result = loadMethod!.Invoke(context, [unknownName]);

        Assert.Null(result);

        context.Unload();
    }

    /// <summary>
    /// Verifies that <see cref="TemplateAssemblyLoadContext"/> resolves assemblies from a
    /// per-invocation subdirectory path structure (e.g. <c>Typewriter/&lt;guid&gt;/</c>),
    /// matching the directory layout produced by <see cref="Compiler"/>.
    /// </summary>
    [Fact]
    public void Load_AssemblyInPerInvocationSubdirectory_Resolves()
    {
        // Arrange: create a nested path that mirrors Compiler._subDirectory layout.
        var perInvocationDir = Path.Combine(_tempDir, "Typewriter", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(perInvocationDir);

        var sourceAssembly = typeof(TemplateAssemblyLoadContext).Assembly;
        var sourcePath = sourceAssembly.Location;
        var destPath = Path.Combine(perInvocationDir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destPath);

        // Act: create the context pointing at the per-invocation subdirectory, same as
        // Compiler.cs line ~120: new TemplateAssemblyLoadContext(Path.GetDirectoryName(path)!)
        var context = new TemplateAssemblyLoadContext(perInvocationDir);
        var loaded = context.LoadFromAssemblyName(
            new AssemblyName(sourceAssembly.GetName().Name!));

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(sourceAssembly.GetName().Name, loaded.GetName().Name);

        UnloadAndRelease(context);
    }

    /// <summary>
    /// Unloads the context and triggers garbage collection so that
    /// Windows releases file locks on assemblies loaded from <see cref="_tempDir"/>.
    /// </summary>
    private static void UnloadAndRelease(TemplateAssemblyLoadContext context)
    {
        context.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
