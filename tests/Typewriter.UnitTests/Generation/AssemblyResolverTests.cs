using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Typewriter.Generation;
using Xunit;

namespace Typewriter.UnitTests.Generation;

/// <summary>
/// Tests for <see cref="ShadowClass"/> assembly resolution via the internal
/// <c>ResolveAssemblyPath</c> method. Covers the three-stage probe order
/// (Location, BaseDirectory, TPA), the null fallback, and graceful handling
/// of assemblies with empty <see cref="Assembly.Location"/> during compilation.
/// </summary>
public class AssemblyResolverTests : IDisposable
{
    private static readonly MethodInfo ResolveMethod = typeof(ShadowClass)
        .GetMethod("ResolveAssemblyPath", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo AddTpaMethod = typeof(ShadowClass)
        .GetMethod("AddTrustedPlatformAssembly", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly FieldInfo ReferencedAssembliesField = typeof(ShadowClass)
        .GetField("_referencedAssemblies", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly List<string> _tempFiles = [];

    /// <summary>
    /// Invokes the internal <c>ResolveAssemblyPath</c> via reflection.
    /// </summary>
    private static string? InvokeResolveAssemblyPath(Assembly assembly)
        => (string?)ResolveMethod.Invoke(null, [assembly]);

    /// <summary>
    /// Creates a minimal in-memory assembly whose <see cref="Assembly.Location"/> is empty,
    /// simulating single-file deployment behavior.
    /// </summary>
    private static Assembly CreateInMemoryAssembly(string assemblyName)
    {
        var code = "public class Stub {}";
        var tree = CSharpSyntaxTree.ParseText(code);

        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator)
            .Where(p =>
            {
                var name = Path.GetFileName(p);
                return string.Equals(name, "System.Runtime.dll", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "System.Private.CoreLib.dll", StringComparison.OrdinalIgnoreCase);
            })
            .Where(File.Exists)
            .Select(p => MetadataReference.CreateFromFile(p))
            .Cast<MetadataReference>()
            .ToList() ?? [];

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var errors = string.Join(", ", result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage()));
            throw new InvalidOperationException($"Failed to create in-memory assembly: {errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup; OS temp directory will reclaim eventually.
            }
        }
    }

    // =========================================================================
    // ResolveAssemblyPath — probe order tests
    // =========================================================================

    /// <summary>
    /// When <see cref="Assembly.Location"/> is non-empty, it is returned directly
    /// without consulting any fallback paths.
    /// </summary>
    [Fact]
    public void ResolveAssemblyPath_LocationNonEmpty_ReturnsLocation()
    {
        var assembly = typeof(ShadowClass).Assembly;

        var result = InvokeResolveAssemblyPath(assembly);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result));
        Assert.Equal(assembly.Location, result);
    }

    /// <summary>
    /// When Location is empty but the DLL exists in <see cref="AppContext.BaseDirectory"/>,
    /// the BaseDirectory path is returned.
    /// </summary>
    [Fact]
    public void ResolveAssemblyPath_LocationEmpty_BaseDirectoryExists_ReturnsBaseDirectoryPath()
    {
        var uniqueName = "TwTestFake_" + Guid.NewGuid().ToString("N")[..8];
        var fakeDll = Path.Combine(AppContext.BaseDirectory, uniqueName + ".dll");
        File.WriteAllBytes(fakeDll, [0]);
        _tempFiles.Add(fakeDll);

        var inMemoryAsm = CreateInMemoryAssembly(uniqueName);
        Assert.Equal(string.Empty, inMemoryAsm.Location); // Precondition

        var result = InvokeResolveAssemblyPath(inMemoryAsm);

        Assert.Equal(fakeDll, result);
    }

    /// <summary>
    /// When Location is empty and the BaseDirectory probe fails, the resolver checks
    /// <c>TRUSTED_PLATFORM_ASSEMBLIES</c> and returns the matching TPA entry.
    /// </summary>
    [Fact]
    public void ResolveAssemblyPath_LocationEmpty_NotInBaseDirectory_FallsBackToTPA()
    {
        // Find a TPA entry whose DLL does NOT also exist in BaseDirectory.
        var tpa = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator) ?? [];

        var tpaEntry = tpa.FirstOrDefault(a =>
        {
            var fileName = Path.GetFileName(a);
            var baseDirPath = Path.Combine(AppContext.BaseDirectory, fileName);
            return !File.Exists(baseDirPath) && File.Exists(a);
        });

        if (tpaEntry == null)
        {
            // All TPA assemblies also exist in BaseDirectory; test cannot exercise this path.
            return;
        }

        var asmName = Path.GetFileNameWithoutExtension(tpaEntry);
        var inMemoryAsm = CreateInMemoryAssembly(asmName);
        Assert.Equal(string.Empty, inMemoryAsm.Location); // Precondition

        var result = InvokeResolveAssemblyPath(inMemoryAsm);

        Assert.NotNull(result);
        Assert.Equal(tpaEntry, result);
    }

    /// <summary>
    /// When Location is empty, the DLL is not in BaseDirectory, and no TPA match is found,
    /// the resolver returns <c>null</c>.
    /// </summary>
    [Fact]
    public void ResolveAssemblyPath_AllProbesFail_ReturnsNull()
    {
        var randomName = "TwNonExistent_" + Guid.NewGuid().ToString("N");
        var inMemoryAsm = CreateInMemoryAssembly(randomName);
        Assert.Equal(string.Empty, inMemoryAsm.Location); // Precondition

        var result = InvokeResolveAssemblyPath(inMemoryAsm);

        Assert.Null(result);
    }

    // =========================================================================
    // ShadowClass.Compile — graceful handling of empty-location assemblies
    // =========================================================================

    /// <summary>
    /// Assemblies with empty <see cref="Assembly.Location"/> in the referenced set
    /// are silently skipped during compilation rather than causing exceptions.
    /// </summary>
    [Fact]
    public void Compile_AssemblyWithEmptyLocation_DoesNotThrow()
    {
        var shadowClass = new ShadowClass();
        shadowClass.Clear();
        shadowClass.Parse();

        // Inject an in-memory assembly (empty Location) into the referenced set.
        var refs = (HashSet<Assembly>)ReferencedAssembliesField.GetValue(shadowClass)!;
        var phantom = CreateInMemoryAssembly("TwPhantom_" + Guid.NewGuid().ToString("N")[..8]);
        refs.Add(phantom);

        var tempPath = Path.Combine(Path.GetTempPath(),
            "tw-resolver-test-" + Guid.NewGuid().ToString("N")[..8] + ".dll");
        _tempFiles.Add(tempPath);

        // The compilation pipeline must not throw due to the unresolvable assembly.
        var exception = Record.Exception(() => shadowClass.Compile(tempPath));
        Assert.Null(exception);
    }

    // =========================================================================
    // AddCoreReferenceIfMissing / AddTrustedPlatformAssembly — TPA fallback
    // =========================================================================

    /// <summary>
    /// The TPA fallback mechanism used by <c>AddCoreReferenceIfMissing</c> successfully
    /// locates and adds a known framework assembly from <c>TRUSTED_PLATFORM_ASSEMBLIES</c>.
    /// </summary>
    [Fact]
    public void AddTrustedPlatformAssembly_KnownAssembly_AddsReference()
    {
        var references = new List<MetadataReference>();

        AddTpaMethod.Invoke(null, [references, "System.Runtime.dll"]);

        Assert.Single(references);
        Assert.Contains("System.Runtime", references[0].Display);
    }

    /// <summary>
    /// Core type references (Object, Uri, Enumerable, Console) are added during compilation
    /// via <c>AddCoreReferenceIfMissing</c>, and the resulting compilation succeeds.
    /// </summary>
    [Fact]
    public void Compile_CoreTypeReferences_AddedSuccessfully()
    {
        var shadowClass = new ShadowClass();
        shadowClass.Clear();
        shadowClass.Parse();

        var tempPath = Path.Combine(Path.GetTempPath(),
            "tw-core-ref-test-" + Guid.NewGuid().ToString("N")[..8] + ".dll");
        _tempFiles.Add(tempPath);

        // Compile includes AddCoreReferenceIfMissing calls for Object, Uri, Enumerable, Console.
        // A successful compilation proves the core references were resolved and added.
        var result = shadowClass.Compile(tempPath);

        Assert.NotNull(result);
        Assert.True(result.Success,
            "Compilation failed: " + string.Join("; ", result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())));
    }
}
