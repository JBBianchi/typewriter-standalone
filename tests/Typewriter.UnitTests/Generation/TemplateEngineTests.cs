using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using NSubstitute;
using Typewriter.CodeModel.Configuration;
using Typewriter.CodeModel.Implementation;
using Typewriter.Generation;
using Typewriter.Metadata;
using Xunit;
using File = System.IO.File;
using Type = System.Type;

namespace Typewriter.UnitTests.Generation;

/// <summary>
/// Template engine acceptance tests covering reference-directive loading,
/// single-file mode rendering, and Linux/macOS assembly resolution.
/// </summary>
public class TemplateEngineTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    /// Initializes a new test instance, creating a temporary directory for test artifacts.
    /// </summary>
    public TemplateEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tw-engine-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Cleans up the temporary directory created for test isolation.
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
            // On Windows, loaded assemblies may hold file locks briefly after unload.
        }
    }

    /// <summary>
    /// Verifies that a template with a <c>#reference path/to/some.dll</c> directive
    /// compiles successfully via <see cref="TemplateCodeParser"/> and
    /// <see cref="Compiler"/>, and that the resulting compiled type is loaded
    /// by a <see cref="TemplateAssemblyLoadContext"/>.
    /// </summary>
    [Fact]
    public void ReferenceDirective_LoadsExternalAssembly()
    {
        // Arrange — copy assemblies to the temp directory so the
        // #reference directives can resolve them relative to the template path.
        CopyAssemblyToTemp(typeof(TemplateAssemblyLoadContext).Assembly); // Typewriter.Generation
        CopyAssemblyToTemp(typeof(Settings).Assembly);                   // Typewriter.Metadata

        var generationDllName = Path.GetFileName(typeof(TemplateAssemblyLoadContext).Assembly.Location);
        var metadataDllName = Path.GetFileName(typeof(Settings).Assembly.Location);

        var templateFilePath = Path.Combine(_tempDir, "test.tst");

        // The ShadowClass boilerplate uses 'using Typewriter.Configuration;' which lives
        // in Typewriter.Metadata, so that reference is needed for compilation. The
        // Typewriter.Generation reference is the external assembly under test.
        var templateContent = $"#reference {metadataDllName}\n#reference {generationDllName}\n";

        var extensions = new List<Type>();

        // Act
        var result = TemplateCodeParser.Parse(templateFilePath, templateContent, extensions);

        // Assert — compilation succeeded and produced a type.
        Assert.NotEmpty(extensions);

        // The compiled template assembly must be loaded via TemplateAssemblyLoadContext.
        var compiledAssembly = extensions[0].Assembly;
        var loadContext = AssemblyLoadContext.GetLoadContext(compiledAssembly);
        Assert.IsType<TemplateAssemblyLoadContext>(loadContext);
    }

    /// <summary>
    /// Executes a simple single-file template against a minimal code model fixture
    /// and compares the output to an inline expected string.
    /// Uses <see cref="SingleFileParser"/> directly with a wildcard filter template.
    /// </summary>
    [Fact]
    public void SingleFileMode_MatchesBaseline()
    {
        // Arrange — build a minimal File with one class via mocked metadata.
        var classMetadata = Substitute.For<IClassMetadata>();
        classMetadata.Name.Returns("CustomerDto");
        classMetadata.FullName.Returns("TestApp.CustomerDto");
        classMetadata.Namespace.Returns("TestApp");
        classMetadata.AssemblyName.Returns("TestApp");
        classMetadata.Attributes.Returns(Enumerable.Empty<IAttributeMetadata>());
        classMetadata.Constants.Returns(Enumerable.Empty<IConstantMetadata>());
        classMetadata.Delegates.Returns(Enumerable.Empty<IDelegateMetadata>());
        classMetadata.Events.Returns(Enumerable.Empty<IEventMetadata>());
        classMetadata.Fields.Returns(Enumerable.Empty<IFieldMetadata>());
        classMetadata.Interfaces.Returns(Enumerable.Empty<IInterfaceMetadata>());
        classMetadata.Methods.Returns(Enumerable.Empty<IMethodMetadata>());
        classMetadata.Properties.Returns(Enumerable.Empty<IPropertyMetadata>());
        classMetadata.StaticReadOnlyFields.Returns(Enumerable.Empty<IStaticReadOnlyFieldMetadata>());
        classMetadata.TypeParameters.Returns(Enumerable.Empty<ITypeParameterMetadata>());
        classMetadata.TypeArguments.Returns(Enumerable.Empty<ITypeMetadata>());
        classMetadata.NestedClasses.Returns(Enumerable.Empty<IClassMetadata>());
        classMetadata.NestedEnums.Returns(Enumerable.Empty<IEnumMetadata>());
        classMetadata.NestedInterfaces.Returns(Enumerable.Empty<IInterfaceMetadata>());

        var fileMetadata = Substitute.For<IFileMetadata>();
        fileMetadata.Name.Returns("Customer.cs");
        fileMetadata.FullName.Returns("/src/Customer.cs");
        fileMetadata.Classes.Returns(new[] { classMetadata });
        fileMetadata.Records.Returns(Enumerable.Empty<IRecordMetadata>());
        fileMetadata.Delegates.Returns(Enumerable.Empty<IDelegateMetadata>());
        fileMetadata.Enums.Returns(Enumerable.Empty<IEnumMetadata>());
        fileMetadata.Interfaces.Returns(Enumerable.Empty<IInterfaceMetadata>());

        var settings = new SettingsImpl(
            Path.Combine(_tempDir, "template.tst"),
            solutionFullName: "");
        var file = new FileImpl(fileMetadata, settings);

        // Use a wildcard filter so ItemFilter sets matchFound = true.
        const string template = "$Classes(*)[$Name]";
        var templateFilePath = Path.Combine(_tempDir, "template.tst");

        // Act — render via SingleFileParser (no compiled extensions needed for
        // simple property access without lambda filters or extension methods).
        var output = SingleFileParser.Parse(
            templateFilePath,
            [file],
            template,
            extensions: [],
            out var success);

        // Assert
        Assert.True(success, "SingleFileParser should report success.");
        Assert.Equal("CustomerDto", output);
    }

    /// <summary>
    /// Verifies that <see cref="TemplateAssemblyLoadContext"/> resolves an assembly
    /// from the <c>assemblyDir</c> probe path on Linux and macOS.
    /// Skipped on Windows — targets non-Windows file-path and resolver behavior.
    /// </summary>
    [Fact]
    public void TemplateAssemblyLoadContext_ResolvesOnLinux()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // This test targets Linux/macOS resolver behavior.
        }

        // Arrange — copy a known assembly into the temp directory.
        var sourceAssembly = typeof(TemplateAssemblyLoadContext).Assembly;
        var sourcePath = sourceAssembly.Location;
        var destPath = Path.Combine(_tempDir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destPath);

        var context = new TemplateAssemblyLoadContext(_tempDir);

        // Act
        var loaded = context.LoadFromAssemblyName(
            new AssemblyName(sourceAssembly.GetName().Name!));

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(sourceAssembly.GetName().Name, loaded.GetName().Name);

        var loadContext = AssemblyLoadContext.GetLoadContext(loaded);
        Assert.IsType<TemplateAssemblyLoadContext>(loadContext);

        // Cleanup
        context.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Verifies that the simple fixture <c>Interfaces.tst</c> template is parseable
    /// by <see cref="TemplateCodeParser"/>.
    /// </summary>
    [Fact]
    public void SimpleFixture_InterfacesTemplate_IsParseable()
    {
        var templatePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "tests", "fixtures", "simple", "SimpleProject", "Interfaces.tst"));
        var templateContent = File.ReadAllText(templatePath);
        var extensions = new List<Type>();

        var result = TemplateCodeParser.Parse(templatePath, templateContent, extensions);

        Assert.NotNull(result);
        Assert.NotEmpty(extensions);
    }

    /// <summary>
    /// Verifies that the simple fixture <c>Enums.tst</c> template is parseable
    /// by <see cref="TemplateCodeParser"/>.
    /// </summary>
    [Fact]
    public void SimpleFixture_EnumsTemplate_IsParseable()
    {
        var templatePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "tests", "fixtures", "simple", "SimpleProject", "Enums.tst"));
        var templateContent = File.ReadAllText(templatePath);
        var extensions = new List<Type>();

        var result = TemplateCodeParser.Parse(templatePath, templateContent, extensions);

        Assert.NotNull(result);
        Assert.NotEmpty(extensions);
    }

    /// <summary>
    /// Copies the specified assembly's DLL to <see cref="_tempDir"/>.
    /// </summary>
    private void CopyAssemblyToTemp(Assembly assembly)
    {
        var fileName = Path.GetFileName(assembly.Location);
        var dest = Path.Combine(_tempDir, fileName);
        if (!File.Exists(dest))
        {
            File.Copy(assembly.Location, dest);
        }
    }
}
